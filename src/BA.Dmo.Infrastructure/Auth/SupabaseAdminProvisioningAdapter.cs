using System.Net;
using System.Net.Http.Json;
using BA.Dmo.Application.Shared.Identity;
using BA.Dmo.Domain.Shared.Kernel;

namespace BA.Dmo.Infrastructure.Auth;

/// <summary>
/// PRIVILEGED provisioning adapter (Plan-V3 GLM-ARCH-14/PV-07, 06_DATA §14–15).
/// The single component allowed to use the service_role credential, and only
/// for explicit privileged operations (bootstrap-admin). It is constructed
/// exclusively by the bootstrap CLI path — never registered in the web
/// request pipeline — and the service-role value never appears in messages,
/// logs, claims or browser assets.
/// </summary>
public sealed class SupabaseAdminProvisioningAdapter : IAdminProvisioningAdapter
{
    private readonly HttpClient _httpClient;
    private readonly string? _supabaseUrl;
    private readonly string? _serviceRoleKey;

    public SupabaseAdminProvisioningAdapter(
        HttpClient httpClient,
        string? supabaseUrl,
        string? serviceRoleKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _supabaseUrl = string.IsNullOrWhiteSpace(supabaseUrl) ? null : supabaseUrl.TrimEnd('/');
        _serviceRoleKey = string.IsNullOrWhiteSpace(serviceRoleKey) ? null : serviceRoleKey;
    }

    public async Task<Result<AuthUser, DomainError>> EnsureAuthUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (_supabaseUrl is null || _serviceRoleKey is null)
            return Result<AuthUser, DomainError>.Failure(MissingConfiguration());

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result<AuthUser, DomainError>.Failure(DomainError.Validation(
                "BOOTSTRAP_CONFIGURATION_MISSING",
                "Provisioning requires an explicit email and password; nothing is defaulted."));

        var created = await SendCreateAsync(email, password, cancellationToken);
        if (created.IsSuccess)
            return created;

        // Idempotent path: the account already exists → look it up.
        if (created.Error.Code == "PROVISIONING_CONFLICT")
            return await FindExistingAsync(email, cancellationToken);

        return created;
    }

    private async Task<Result<AuthUser, DomainError>> SendCreateAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{_supabaseUrl}/auth/v1/admin/users")
        {
            Content = JsonContent.Create(new
            {
                email,
                password,
                email_confirm = true
            })
        };
        AddPrivilegedHeaders(request);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<AuthUser, DomainError>.Failure(ProviderUnavailable());
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.UnprocessableEntity
                or HttpStatusCode.Conflict)
            {
                return Result<AuthUser, DomainError>.Failure(
                    DomainError.DomainConflict(
                        "PROVISIONING_CONFLICT",
                        "The authentication account already exists."));
            }

            if (!response.IsSuccessStatusCode)
                return Result<AuthUser, DomainError>.Failure(ProvisioningFailed());

            var payload = await ReadUserPayloadAsync(response, cancellationToken);
            return payload is null
                ? Result<AuthUser, DomainError>.Failure(ProvisioningFailed())
                : Result<AuthUser, DomainError>.Success(new AuthUser(payload.Value, email));
        }
    }

    private async Task<Result<AuthUser, DomainError>> FindExistingAsync(
        string email,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_supabaseUrl}/auth/v1/admin/users?email={Uri.EscapeDataString(email)}");
        AddPrivilegedHeaders(request);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<AuthUser, DomainError>.Failure(ProviderUnavailable());
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return Result<AuthUser, DomainError>.Failure(ProvisioningFailed());

            try
            {
                var listing = await response.Content.ReadFromJsonAsync<UserListing>(
                    cancellationToken: cancellationToken);
                var match = listing?.Users?.FirstOrDefault(u =>
                    string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                return match is not null && match.Id != Guid.Empty
                    ? Result<AuthUser, DomainError>.Success(new AuthUser(match.Id, email))
                    : Result<AuthUser, DomainError>.Failure(ProvisioningFailed());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result<AuthUser, DomainError>.Failure(ProvisioningFailed());
            }
        }
    }

    private void AddPrivilegedHeaders(HttpRequestMessage request)
    {
        // Service role stays on the wire between server and provider only.
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        request.Headers.Add("apikey", _serviceRoleKey);
    }

    private async Task<Guid?> ReadUserPayloadAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await response.Content.ReadFromJsonAsync<UserPayload>(
                cancellationToken: cancellationToken);
            return user?.Id is Guid id && id != Guid.Empty ? id : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static DomainError MissingConfiguration() => DomainError.Validation(
        "PROVISIONING_CONFIGURATION_MISSING",
        "Privileged provisioning configuration is missing (Supabase URL / service-role). " +
        "Provide explicit environment configuration; nothing is defaulted.");

    private static DomainError ProvisioningFailed() => DomainError.BackendUnavailable(
        "PROVISIONING_FAILED",
        "The privileged provisioning operation failed. No user was provisioned.");

    private static DomainError ProviderUnavailable() => DomainError.BackendUnavailable(
        "AUTH_PROVIDER_UNAVAILABLE",
        "The authentication provider is unavailable. Try again later.");

    private sealed class UserPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public Guid Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    private sealed class UserListing
    {
        [System.Text.Json.Serialization.JsonPropertyName("users")]
        public List<UserPayload>? Users { get; set; }
    }
}

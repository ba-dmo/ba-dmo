using System.Net.Http.Json;
using System.Text.Json;
using BA.Dmo.Application.Shared.Identity;
using BA.Dmo.Domain.Shared.Kernel;

namespace BA.Dmo.Infrastructure.Auth;

/// <summary>
/// Supabase Auth adapter via direct server-side REST (GoTrue). Plan-V3
/// GLM-ARCH-14/PV-06 leaves the concrete provider open: direct REST keeps
/// the runtime dependency-free of provider SDKs and of service_role
/// (PV-07 — the normal request pipeline uses only the anon endpoint and the
/// user's own credentials). Provider types never leave this class.
/// </summary>
public sealed class SupabaseAuthAdapter : ISupabaseAuthAdapter
{
    private readonly HttpClient _httpClient;
    private readonly string? _supabaseUrl;
    private readonly string? _anonKey;

    public SupabaseAuthAdapter(HttpClient httpClient, string? supabaseUrl, string? anonKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _supabaseUrl = string.IsNullOrWhiteSpace(supabaseUrl) ? null : supabaseUrl.TrimEnd('/');
        _anonKey = string.IsNullOrWhiteSpace(anonKey) ? null : anonKey;
    }

    public async Task<Result<AuthUser, DomainError>> SignInWithPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (_supabaseUrl is null || _anonKey is null)
            return Result<AuthUser, DomainError>.Failure(Unavailable());

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result<AuthUser, DomainError>.Failure(InvalidCredentials());

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_supabaseUrl}/auth/v1/token?grant_type=password")
        {
            Content = JsonContent.Create(new { email, password })
        };
        request.Headers.Add("apikey", _anonKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<AuthUser, DomainError>.Failure(Unavailable());
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Generic message: never reveal whether the email exists
                // (design contract) and never echo provider details.
                return Result<AuthUser, DomainError>.Failure(InvalidCredentials());
            }

            try
            {
                var payload = await response.Content.ReadFromJsonAsync<SignInResponse>(
                    cancellationToken: cancellationToken);
                if (payload?.User?.Id is not Guid authUserId || authUserId == Guid.Empty)
                    return Result<AuthUser, DomainError>.Failure(Unavailable());

                return Result<AuthUser, DomainError>.Success(
                    new AuthUser(authUserId, email));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result<AuthUser, DomainError>.Failure(Unavailable());
            }
        }
    }

    private static DomainError InvalidCredentials() => DomainError.Unauthorized(
        "INVALID_CREDENTIALS",
        "Credenciais inválidas.");

    private static DomainError Unavailable() => DomainError.BackendUnavailable(
        "AUTH_PROVIDER_UNAVAILABLE",
        "The authentication provider is unavailable. Try again later.");

    private sealed class SignInResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("user")]
        public UserPayload? User { get; set; }
    }

    private sealed class UserPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public Guid Id { get; set; }
    }
}

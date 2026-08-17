using System.Net;
using BA.Dmo.Domain.Shared.Kernel;
using BA.Dmo.Infrastructure.Auth;

namespace BA.Dmo.IntegrationTests.Identity;

/// <summary>
/// U-05 privileged provisioning adapter tests (Plan-V3 PV-07, 06_DATA §14–15):
/// service_role is used exclusively here, stays on the server-side wire, and
/// never appears in messages; the adapter is idempotent.
/// </summary>
public class SupabaseAdminProvisioningAdapterTests
{
    private const string SupabaseUrl = "https://project.supabase.example";
    private const string ServiceRoleKey = "service-role-secret-value";

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };

    [Fact]
    public async Task CreateUser_SendsServiceRoleOnlyServerSide_AndReturnsTheUserId()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Responders.Enqueue(_ => Json(
            "{\"id\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\",\"email\":\"admin@ba-dmo.example\"}"));
        var adapter = new SupabaseAdminProvisioningAdapter(
            new HttpClient(handler), SupabaseUrl, ServiceRoleKey);

        var result = await adapter.EnsureAuthUserAsync("admin@ba-dmo.example", "password");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            result.Value.AuthUserId);

        var request = Assert.Single(handler.Requests);
        Assert.Equal($"{SupabaseUrl}/auth/v1/admin/users", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(ServiceRoleKey, request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ExistingAccount_IsResolvedIdempotently_ViaAdminLookup()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Responders.Enqueue(_ => Json(
            "{\"msg\":\"User already registered\"}", HttpStatusCode.UnprocessableEntity));
        handler.Responders.Enqueue(_ => Json(
            "{\"users\":[{\"id\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\",\"email\":\"admin@ba-dmo.example\"}]}"));
        var adapter = new SupabaseAdminProvisioningAdapter(
            new HttpClient(handler), SupabaseUrl, ServiceRoleKey);

        var result = await adapter.EnsureAuthUserAsync("admin@ba-dmo.example", "password");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            result.Value.AuthUserId);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("/auth/v1/admin/users?email=", handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task MissingConfiguration_FailsClearly_WithoutHttpCalls()
    {
        var handler = new FakeHttpMessageHandler();
        var adapter = new SupabaseAdminProvisioningAdapter(new HttpClient(handler), null, null);

        var result = await adapter.EnsureAuthUserAsync("admin@ba-dmo.example", "password");

        Assert.True(result.IsFailure);
        Assert.Equal("PROVISIONING_CONFIGURATION_MISSING", result.Error.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task HardFailure_FailsClosed_AndNeverLeaksTheServiceRole()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Responders.Enqueue(_ => Json("{}", HttpStatusCode.InternalServerError));
        var adapter = new SupabaseAdminProvisioningAdapter(
            new HttpClient(handler), SupabaseUrl, ServiceRoleKey);

        var result = await adapter.EnsureAuthUserAsync("admin@ba-dmo.example", "password");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.BackendUnavailable, result.Error.Category);
        Assert.DoesNotContain(ServiceRoleKey, result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NetworkFailure_FailsClosed_AndNeverLeaksTheServiceRole()
    {
        var handler = new FakeHttpMessageHandler { Throw = new HttpRequestException("down") };
        var adapter = new SupabaseAdminProvisioningAdapter(
            new HttpClient(handler), SupabaseUrl, ServiceRoleKey);

        var result = await adapter.EnsureAuthUserAsync("admin@ba-dmo.example", "password");

        Assert.True(result.IsFailure);
        Assert.DoesNotContain(ServiceRoleKey, result.Error.Message, StringComparison.Ordinal);
    }
}

using System.Net;
using BA.Dmo.Application.Shared.Identity;
using BA.Dmo.Domain.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BA.Dmo.IntegrationTests.Identity;

/// <summary>
/// U-05 session/authentication flow tests (Plan-V3 GLM-ACC-01, 05_SHL
/// §5–6): login, logout, protected pages, safe states, Job On landing.
/// Runs against the real web pipeline with fakes for the Supabase adapter
/// and the identity repository — no live Supabase/DB is used (GLM-ARCH-18).
/// </summary>
public class WebAuthSessionTests : IClassFixture<WebAuthSessionTests.AuthTestFixture>
{
    private static readonly Guid AuthUserId =
        Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly AuthTestFixture _fixture;

    public WebAuthSessionTests(AuthTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    [Fact]
    public async Task UnauthenticatedRequest_IsRedirectedToLogin()
    {
        var client = _fixture.CreateTestClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/login", response.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task LoginPage_IsPublic()
    {
        var client = _fixture.CreateTestClient();

        var response = await client.GetAsync("/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SafeStatePages_ArePublic()
    {
        var client = _fixture.CreateTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/no-access")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/access-denied")).StatusCode);
    }

    [Fact]
    public async Task SuccessfulLogin_RedirectsToTheJobOnLanding_WithSessionCookie()
    {
        // Scenarios 1/7: the landing is Job On for every valid identity —
        // never a role-specific redirect.
        _fixture.Repository.User = _fixture.ValidUser();

        var client = _fixture.CreateTestClient();

        var login = await PostFormAsync(client, "/login", new()
        {
            ["email"] = "user@ba-dmo.example",
            ["password"] = "correct"
        });

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/jobon", login.Headers.Location!.ToString());
        Assert.True(login.Headers.Contains("Set-Cookie"));

        // The session reaches the protected surface: "/" resolves the fixed
        // global landing (05_SHL section 5: "/" redirects to landing).
        var home = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
        Assert.Equal("/jobon", home.Headers.Location!.ToString());
    }

    [Fact]
    public async Task InvalidCredentials_ShowGenericError_AndNoSession()
    {
        _fixture.AuthAdapter.Mode = FakeAuthAdapter.AuthMode.InvalidCredentials;

        var client = _fixture.CreateTestClient();

        var login = await PostFormAsync(client, "/login", new()
        {
            ["email"] = "user@ba-dmo.example",
            ["password"] = "wrong"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode); // stays on the form
        var body = System.Net.WebUtility.HtmlDecode(await login.Content.ReadAsStringAsync());
        Assert.Contains("Credenciais inválidas.", body);

        // No session was created: protected pages still redirect to login.
        var home = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
        Assert.StartsWith("/login", home.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task AuthenticatedWithoutInternalMapping_GoesToNoAccessSafeState()
    {
        // GLM-ACC-01.6: INTERNAL_USER_INACTIVE → safe session without access.
        _fixture.Repository.User = null;

        var client = _fixture.CreateTestClient();

        var login = await PostFormAsync(client, "/login", new()
        {
            ["email"] = "user@ba-dmo.example",
            ["password"] = "correct"
        });

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/no-access", login.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AuthenticatedWithInactiveTemplate_GoesToNoAccessSafeState()
    {
        _fixture.Repository.User = _fixture.ValidUser() with { TemplateActive = false };

        var client = _fixture.CreateTestClient();

        var login = await PostFormAsync(client, "/login", new()
        {
            ["email"] = "user@ba-dmo.example",
            ["password"] = "correct"
        });

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/no-access", login.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Logout_ClearsTheSession()
    {
        _fixture.Repository.User = _fixture.ValidUser();
        var client = _fixture.CreateTestClient();

        await PostFormAsync(client, "/login", new()
        {
            ["email"] = "user@ba-dmo.example",
            ["password"] = "correct"
        });
        var logout = await PostFormAsync(client, "/logout", []);
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/login", logout.Headers.Location!.ToString());

        // Session gone: protected surface redirects again.
        var home = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
        Assert.StartsWith("/login", home.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task ProviderFailure_ShowsGenericError_NoSession()
    {
        _fixture.AuthAdapter.Mode = FakeAuthAdapter.AuthMode.ProviderUnavailable;

        var client = _fixture.CreateTestClient();

        var login = await PostFormAsync(client, "/login", new()
        {
            ["email"] = "user@ba-dmo.example",
            ["password"] = "anything"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(
            "Credenciais inválidas.",
            System.Net.WebUtility.HtmlDecode(await login.Content.ReadAsStringAsync()));
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string url, Dictionary<string, string> fields)
    {
        var form = await client.GetAsync(url);
        var html = await form.Content.ReadAsStringAsync();

        var values = new Dictionary<string, string>(fields);
        var tokenStart = html.IndexOf("name=\"__RequestVerificationToken\"", StringComparison.Ordinal);
        if (tokenStart >= 0)
        {
            var valueAttr = html.IndexOf("value=\"", tokenStart, StringComparison.Ordinal);
            if (valueAttr >= 0)
            {
                var tokenValueStart = valueAttr + "value=\"".Length;
                var tokenEnd = html.IndexOf('"', tokenValueStart);
                values["__RequestVerificationToken"] = html[tokenValueStart..tokenEnd];
            }
        }

        return await client.PostAsync(url, new FormUrlEncodedContent(values));
    }

    /// <summary>
    /// Test host with fakes for the provider adapter and the identity
    /// repository; anti-forgery disabled for scripted form posts only.
    /// </summary>
    public sealed class AuthTestFixture : WebApplicationFactory<Program>
    {
        public FakeAuthAdapter AuthAdapter { get; } = new();

        public FakeIdentityRepository Repository { get; } = new();

        public void Reset()
        {
            AuthAdapter.Mode = FakeAuthAdapter.AuthMode.Success;
            Repository.User = null;
            Repository.ThrowOnFind = false;
        }

        public InternalUserRecord ValidUser() => new(
            ActorId: "actor-1",
            AuthUserId: AuthUserId,
            DisplayName: "Utilizador Um",
            ProfileTitle: null,
            UserActive: true,
            TemplateId: "tpl-1",
            TemplateName: "Template 1",
            TemplateActive: true,
            ModulesJson: "[{\"moduleId\":\"boquilhas\",\"capabilities\":[]}]");

        public HttpClient CreateTestClient() => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        protected override void ConfigureWebHost(
            Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                ReplaceSingleton<ISupabaseAuthAdapter>(services, AuthAdapter);
                ReplaceSingleton<IInternalUserRepository>(services, Repository);
                services.Configure<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions>(
                    options => options.Conventions.ConfigureFilter(
                        new IgnoreAntiforgeryTokenAttribute()));
            });
        }

        private static void ReplaceSingleton<TService>(
            IServiceCollection services, TService implementation)
            where TService : class
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(TService)).ToList();
            foreach (var descriptor in descriptors)
                services.Remove(descriptor);
            services.AddSingleton(implementation);
        }
    }

    public sealed class FakeAuthAdapter : ISupabaseAuthAdapter
    {
        public enum AuthMode
        {
            Success,
            InvalidCredentials,
            ProviderUnavailable
        }

        public AuthMode Mode { get; set; } = AuthMode.Success;

        public Task<Result<AuthUser, DomainError>> SignInWithPasswordAsync(
            string email, string password, CancellationToken cancellationToken = default) =>
            Mode switch
            {
                AuthMode.Success => Task.FromResult(Result<AuthUser, DomainError>.Success(
                    new AuthUser(AuthUserId, email))),
                AuthMode.ProviderUnavailable => Task.FromResult(
                    Result<AuthUser, DomainError>.Failure(DomainError.BackendUnavailable(
                        "AUTH_PROVIDER_UNAVAILABLE", "Provider down."))),
                _ => Task.FromResult(Result<AuthUser, DomainError>.Failure(
                    DomainError.Unauthorized("INVALID_CREDENTIALS", "Credenciais inválidas.")))
            };
    }

    public sealed class FakeIdentityRepository : IInternalUserRepository
    {
        public InternalUserRecord? User { get; set; }

        public bool ThrowOnFind { get; set; }

        public Task<InternalUserRecord?> FindByAuthUserIdAsync(
            Guid authUserId, CancellationToken cancellationToken = default)
        {
            if (ThrowOnFind)
                throw new InvalidOperationException("Simulated database failure.");
            return Task.FromResult(User);
        }

        public Task<bool> AdminExistsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task CreateBootstrapAdminAsync(
            BootstrapAdminCreation creation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

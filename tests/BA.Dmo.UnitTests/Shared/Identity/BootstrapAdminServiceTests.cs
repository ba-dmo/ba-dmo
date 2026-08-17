using BA.Dmo.Application.Shared.Identity;
using BA.Dmo.Domain.Shared.Kernel;

namespace BA.Dmo.UnitTests.Shared.Identity;

/// <summary>
/// U-05 bootstrap-admin service tests (Plan-V3 GLM-ACC-13, 06_DATA §15,
/// PV-08): one-shot, explicit, idempotent, auditable, no defaults, no
/// fictitious users. All collaborators are fakes — no live system is
/// touched in U-05.
/// </summary>
public class BootstrapAdminServiceTests
{
    private static readonly Guid ProvisionedAuthUserId =
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private readonly FakeProvisioningAdapter _provisioning = new();
    private readonly FakeInternalUserRepository _repository = new();
    private readonly BootstrapAdminService _service;

    public BootstrapAdminServiceTests()
    {
        _service = new BootstrapAdminService(
            _provisioning, _repository, new FixedClock(
                new DateTimeOffset(2026, 8, 17, 15, 0, 0, TimeSpan.Zero)));
    }

    private static BootstrapAdminOptions ValidOptions =>
        new("admin@ba-dmo.example", "explicit-password", "Primeiro Admin");

    [Fact]
    public async Task Success_CreatesMinimalAdminTemplateAndActiveUser()
    {
        var result = await _service.RunAsync(ValidOptions);

        Assert.True(result.IsSuccess);
        Assert.Equal(BootstrapAdminOutcome.Created, result.Value);

        // Provisioning received the explicit credentials (never defaults).
        var call = Assert.Single(_provisioning.Calls);
        Assert.Equal("admin@ba-dmo.example", call.Email);
        Assert.Equal("explicit-password", call.Password);

        // The persisted creation is minimal and exact (GLM-ACC-13).
        var creation = Assert.Single(_repository.Creations);
        Assert.Equal(ProvisionedAuthUserId.ToString(), creation.ActorId);
        Assert.Equal(ProvisionedAuthUserId, creation.AuthUserId);
        Assert.Equal("Primeiro Admin", creation.DisplayName);
        Assert.Equal(BootstrapAdminService.BootstrapTemplateId, creation.TemplateId);
        Assert.Equal(BootstrapAdminService.BootstrapModulesJson, creation.ModulesJson);
        Assert.Contains("admin.gerir", creation.ModulesJson, StringComparison.Ordinal);
        // No functional modules granted automatically.
        Assert.DoesNotContain("peso", creation.ModulesJson, StringComparison.Ordinal);
        Assert.DoesNotContain("boquilhas", creation.ModulesJson, StringComparison.Ordinal);
        Assert.Equal(new DateTimeOffset(2026, 8, 17, 15, 0, 0, TimeSpan.Zero), creation.CreatedAtUtc);
    }

    [Fact]
    public async Task ExistingValidAdmin_IsIdempotent_NoWrites()
    {
        _repository.AdminExists = true;

        var result = await _service.RunAsync(ValidOptions);

        Assert.True(result.IsSuccess);
        Assert.Equal(BootstrapAdminOutcome.AlreadyExists, result.Value);
        Assert.Empty(_provisioning.Calls);   // no duplicate user creation
        Assert.Empty(_repository.Creations); // no duplicate rows
    }

    [Theory]
    [InlineData("", "password", "Nome")]
    [InlineData("email@x.example", "", "Nome")]
    [InlineData("email@x.example", "password", "")]
    public async Task MissingExplicitConfiguration_FailsValidation(
        string email, string password, string displayName)
    {
        var result = await _service.RunAsync(new BootstrapAdminOptions(email, password, displayName));

        Assert.True(result.IsFailure);
        Assert.Equal("BOOTSTRAP_CONFIGURATION_MISSING", result.Error.Code);
        Assert.Empty(_provisioning.Calls);
        Assert.Empty(_repository.Creations);
    }

    [Fact]
    public async Task ProvisioningFailure_Propagates_AndNothingIsPersisted()
    {
        _provisioning.Failure = DomainError.BackendUnavailable(
            "AUTH_PROVIDER_UNAVAILABLE", "Provider down.");

        var result = await _service.RunAsync(ValidOptions);

        Assert.True(result.IsFailure);
        Assert.Equal("AUTH_PROVIDER_UNAVAILABLE", result.Error.Code);
        Assert.Empty(_repository.Creations);
    }

    [Fact]
    public async Task PersistenceFailure_Propagates()
    {
        _repository.ThrowOnCreate = true;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RunAsync(ValidOptions));
    }

    private sealed class FakeProvisioningAdapter : IAdminProvisioningAdapter
    {
        public List<(string Email, string Password)> Calls { get; } = [];

        public DomainError? Failure { get; set; }

        public Task<Result<AuthUser, DomainError>> EnsureAuthUserAsync(
            string email, string password, CancellationToken cancellationToken = default)
        {
            Calls.Add((email, password));
            return Failure is not null
                ? Task.FromResult(Result<AuthUser, DomainError>.Failure(Failure))
                : Task.FromResult(Result<AuthUser, DomainError>.Success(
                    new AuthUser(ProvisionedAuthUserId, email)));
        }
    }

    private sealed class FakeInternalUserRepository : IInternalUserRepository
    {
        public bool AdminExists { get; set; }

        public bool ThrowOnCreate { get; set; }

        public List<BootstrapAdminCreation> Creations { get; } = [];

        public Task<InternalUserRecord?> FindByAuthUserIdAsync(
            Guid authUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<InternalUserRecord?>(null);

        public Task<bool> AdminExistsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AdminExists);

        public Task CreateBootstrapAdminAsync(
            BootstrapAdminCreation creation, CancellationToken cancellationToken = default)
        {
            if (ThrowOnCreate)
                throw new InvalidOperationException("Simulated persistence failure.");
            Creations.Add(creation);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset fixedUtcNow) : IClock
    {
        public DateTimeOffset UtcNow => fixedUtcNow;
    }
}

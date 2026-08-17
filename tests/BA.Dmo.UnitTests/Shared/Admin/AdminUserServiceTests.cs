using BA.Dmo.Application.Modules.Admin;
using BA.Dmo.Application.Shared.Identity;
using BA.Dmo.Domain.Shared.Access;
using BA.Dmo.Domain.Shared.Kernel;

namespace BA.Dmo.UnitTests.Shared.Admin;

/// <summary>
/// U-06 Admin user-management tests (Plan-V3 04_ACC §9–12, GLM-ACC-10/11/12).
/// High-value coverage: capability gate on every mutation, provisioning
/// happy/error paths, duplicate handling, concurrency conflict, self-lockout,
/// audit facts without secrets, fail-closed authorization. All collaborators
/// are fakes — no live Supabase/DB.
/// </summary>
public class AdminUserServiceTests
{
    private static readonly Guid NewAuthUserId =
        Guid.Parse("99999999-8888-7777-6666-555555555555");

    private readonly FakeAdminRepository _repository = new();
    private readonly FakeProvisioning _provisioning = new();
    private readonly FakeCurrentUserAccessor _identity = new();
    private readonly AdminUserService _service;

    public AdminUserServiceTests()
    {
        var gate = new AdminAuthorizationGate(_identity);
        _service = new AdminUserService(
            gate, _repository, _provisioning, new FixedClock(
                new DateTimeOffset(2026, 8, 17, 18, 0, 0, TimeSpan.Zero)));

        _repository.Templates["tpl-active"] = new AdminTemplateRow(
            "tpl-active", "Template ativo", "[]", Active: true,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _repository.Templates["tpl-inactive"] = new AdminTemplateRow(
            "tpl-inactive", "Template inativo", "[]", Active: false,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        _repository.Users["user-1"] = new AdminUserRow(
            "user-1", Guid.Parse("11111111-2222-3333-4444-555555555555"),
            "Utilizador Um", "Metrologia", "tpl-active", Active: true,
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        _identity.GrantAdmin();
    }

    // ---- authorization gate (fail closed, capability only) ----------------

    [Fact]
    public async Task Mutation_WithoutCapability_IsDenied_AndWritesNothing()
    {
        _identity.GrantNone();

        var result = await _service.SetActiveAsync(
            new SetUserActiveRequest("user-1", false, Version("user-1")));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.Forbidden, result.Error.Category);
        Assert.Empty(_repository.Writes);
        Assert.Empty(_repository.Audits);
    }

    [Fact]
    public async Task Mutation_WithoutResolvedIdentity_IsDenied()
    {
        _identity.User = null;

        var result = await _service.CreateUserAsync(new CreateAdminUserRequest(
            "novo@ba-dmo.example", "password", "Novo", null, "tpl-active"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.Forbidden, result.Error.Category);
        Assert.Empty(_provisioning.Calls);
    }

    // ---- create user (provisioning boundary) ------------------------------

    [Fact]
    public async Task CreateUser_HappyPath_ProvisionsPersistsAndAudits_WithoutSecrets()
    {
        var result = await _service.CreateUserAsync(new CreateAdminUserRequest(
            "novo@ba-dmo.example", "secret-password-value", "Novo Utilizador",
            "Metrologia", "tpl-active"));

        Assert.True(result.IsSuccess);
        Assert.Equal("create", Assert.Single(_repository.Audits).ActionCode);
        Assert.Equal("internal_user", _repository.Audits[0].EntityType);
        Assert.Equal("admin", _repository.Audits[0].ModuleId);
        // Secrets never reach audit entries or results (GLM-DATA-06/U-06 rule).
        Assert.DoesNotContain(_repository.Audits, a =>
            (a.Reason ?? string.Empty).Contains("secret-password-value", StringComparison.Ordinal)
            || (a.EntityLabelSnapshot ?? string.Empty).Contains("secret-password-value", StringComparison.Ordinal));
        Assert.Equal(1, _provisioning.Calls.Count);
    }

    [Fact]
    public async Task CreateUser_InactiveTemplate_IsRejected_BeforeProvisioning()
    {
        var result = await _service.CreateUserAsync(new CreateAdminUserRequest(
            "novo@ba-dmo.example", "password", "Novo", null, "tpl-inactive"));

        Assert.True(result.IsFailure);
        Assert.Equal("ADMIN_TEMPLATE_INVALID", result.Error.Code);
        Assert.Empty(_provisioning.Calls);
        Assert.Empty(_repository.Writes);
    }

    [Fact]
    public async Task CreateUser_ProviderFailure_PersistsNothing()
    {
        _provisioning.FailEnsure = DomainError.BackendUnavailable(
            "AUTH_PROVIDER_UNAVAILABLE", "Provider down.");

        var result = await _service.CreateUserAsync(new CreateAdminUserRequest(
            "novo@ba-dmo.example", "password", "Novo", null, "tpl-active"));

        Assert.True(result.IsFailure);
        Assert.Equal("AUTH_PROVIDER_UNAVAILABLE", result.Error.Code);
        Assert.DoesNotContain(_repository.Writes, w => w.StartsWith("create:", StringComparison.Ordinal));
        Assert.Empty(_repository.Audits);
    }

    [Fact]
    public async Task CreateUser_DuplicateRegistration_IsExplicitConflict()
    {
        _provisioning.ProvisionedAuthUserId = _repository.Users["user-1"].AuthUserId;

        var result = await _service.CreateUserAsync(new CreateAdminUserRequest(
            "existente@ba-dmo.example", "password", "Duplicado", null, "tpl-active"));

        Assert.True(result.IsFailure);
        Assert.Equal("ADMIN_USER_ALREADY_REGISTERED", result.Error.Code);
    }

    // ---- edit / concurrency ------------------------------------------------

    [Fact]
    public async Task UpdateUser_PersistsAndAudits()
    {
        var result = await _service.UpdateUserAsync(new UpdateAdminUserRequest(
            "user-1", "Nome Novo", "Chefe", Version("user-1")));

        Assert.True(result.IsSuccess);
        Assert.Equal("Nome Novo", _repository.Users["user-1"].DisplayName);
        Assert.Equal("update", Assert.Single(_repository.Audits).ActionCode);
    }

    [Fact]
    public async Task UpdateUser_StaleVersion_IsConcurrencyConflict_WithReloadMessage()
    {
        _repository.ConcurrencyNextWrite = true;

        var result = await _service.UpdateUserAsync(new UpdateAdminUserRequest(
            "user-1", "Nome Novo", null, Version("user-1")));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.ConcurrencyConflict, result.Error.Category);
        Assert.Contains("Recarregue", result.Error.Message, StringComparison.Ordinal);
        Assert.Empty(_repository.Audits);
    }

    // ---- self-lockout (GLM-ACC-10) -----------------------------------------

    [Fact]
    public async Task DeactivateLastAdmin_IsRejected_AsSelfLockout()
    {
        _repository.LockoutNextWrite = true;

        var result = await _service.SetActiveAsync(
            new SetUserActiveRequest("user-1", false, Version("user-1")));

        Assert.True(result.IsFailure);
        Assert.Equal("ADMIN_SELF_LOCKOUT", result.Error.Code);
        Assert.True(_repository.Users["user-1"].Active); // unchanged
    }

    [Fact]
    public async Task DeactivateAdmin_WhenAnotherAdminRemains_IsAllowed_AndAudited()
    {
        var result = await _service.SetActiveAsync(
            new SetUserActiveRequest("user-1", false, Version("user-1")));

        Assert.True(result.IsSuccess);
        Assert.False(_repository.Users["user-1"].Active);
        Assert.Equal("deactivate", Assert.Single(_repository.Audits).ActionCode);
    }

    [Fact]
    public async Task ChangeTemplate_LockoutRejected_LeavesUserUnchanged()
    {
        _repository.LockoutNextWrite = true;

        var result = await _service.ChangeTemplateAsync(
            new ChangeUserTemplateRequest("user-1", "tpl-inactive", Version("user-1")));

        Assert.True(result.IsFailure);
        Assert.Equal("ADMIN_SELF_LOCKOUT", result.Error.Code);
        Assert.Equal("tpl-active", _repository.Users["user-1"].TemplateId);
    }

    // ---- password reset (privileged adapter path) ---------------------------

    [Fact]
    public async Task PasswordReset_GoesThroughPrivilegedAdapter_AndAuditsWithoutSecrets()
    {
        var result = await _service.RequestPasswordResetAsync("user-1");

        Assert.True(result.IsSuccess);
        var call = Assert.Single(_provisioning.ResetCalls);
        Assert.Equal(_repository.Users["user-1"].AuthUserId, call);

        var audit = Assert.Single(_repository.Audits);
        Assert.Equal("password_reset_request", audit.ActionCode);
        Assert.Equal("user-1", audit.EntityId);
        Assert.DoesNotContain("password", audit.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private DateTimeOffset Version(string actorId) => _repository.Users[actorId].UpdatedAtUtc;

    private sealed class FakeProvisioning : IAdminProvisioningAdapter
    {
        public Guid ProvisionedAuthUserId { get; set; } = NewAuthUserId;

        public List<(string Email, string Password)> Calls { get; } = [];

        public List<Guid> ResetCalls { get; } = [];

        public DomainError? FailEnsure { get; set; }

        public Task<Result<AuthUser, DomainError>> EnsureAuthUserAsync(
            string email, string password, CancellationToken cancellationToken = default)
        {
            Calls.Add((email, password));
            return FailEnsure is not null
                ? Task.FromResult(Result<AuthUser, DomainError>.Failure(FailEnsure))
                : Task.FromResult(Result<AuthUser, DomainError>.Success(
                    new AuthUser(ProvisionedAuthUserId, email)));
        }

        public Task<Result<bool, DomainError>> RequestPasswordResetAsync(
            Guid authUserId, CancellationToken cancellationToken = default)
        {
            ResetCalls.Add(authUserId);
            return Task.FromResult(Result<bool, DomainError>.Success(true));
        }
    }

    private sealed class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        public CurrentUser? User { get; set; }

        public CurrentUser? Current => User;

        public void GrantAdmin() => User = new CurrentUser(
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
            "Administrador",
            new[] { "admin" },
            new[] { "admin.gerir", "audit.view", "audit.export" });

        public void GrantNone() => User = new CurrentUser(
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
            "Operador",
            new[] { "boquilhas" },
            Array.Empty<string>());
    }

    private sealed class FixedClock(DateTimeOffset fixedUtcNow) : IClock
    {
        public DateTimeOffset UtcNow => fixedUtcNow;
    }
}

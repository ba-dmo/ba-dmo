using BA.Dmo.Application.Shared.Identity;
using BA.Dmo.Application.Shared.Persistence;
using BA.Dmo.Domain.Shared.Kernel;

namespace BA.Dmo.Application.Modules.Admin;

/// <summary>
/// Administration use cases for internal users (Plan-V3 04_ACC §9, U-06).
/// Every operation re-checks the canonical capability server-side through the
/// gate (hiding UI is not security — GLM-ACC-04), executes through the
/// repository port, and writes the global audit fact (GLM-ACC-11). Secrets
/// (passwords/tokens/service-role) never enter audit entries or results.
/// Self-lockout invariant: GLM-ACC-10. Concurrency: GLM-ACC-12/BT-06.
/// </summary>
public sealed class AdminUserService
{
    private readonly AdminAuthorizationGate _gate;
    private readonly IAdminRepository _repository;
    private readonly IAdminProvisioningAdapter _provisioning;
    private readonly IClock _clock;

    public AdminUserService(
        AdminAuthorizationGate gate,
        IAdminRepository repository,
        IAdminProvisioningAdapter provisioning,
        IClock clock)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _provisioning = provisioning ?? throw new ArgumentNullException(nameof(provisioning));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<IReadOnlyList<AdminUserRow>> ListAsync(
        string? search, CancellationToken cancellationToken = default)
    {
        var gate = _gate.Require(CanonicalCapabilities.AdminGerir);
        if (gate.IsFailure)
            return Task.FromResult<IReadOnlyList<AdminUserRow>>(Array.Empty<AdminUserRow>());

        return _repository.ListUsersAsync(search, cancellationToken);
    }

    public async Task<Result<AdminUserRow, DomainError>> GetAsync(
        string actorId, CancellationToken cancellationToken = default)
    {
        var gate = _gate.Require(CanonicalCapabilities.AdminGerir);
        if (gate.IsFailure)
            return Result<AdminUserRow, DomainError>.Failure(gate.Error);

        var user = await _repository.GetUserAsync(actorId, cancellationToken);
        return user is null
            ? Result<AdminUserRow, DomainError>.Failure(DomainError.NotFound(
                "INTERNAL_USER_NOT_FOUND", "Utilizador interno não encontrado."))
            : Result<AdminUserRow, DomainError>.Success(user);
    }

    /// <summary>
    /// Creates the Auth account through the PRIVILEGED adapter (TD-16), then
    /// the internal user. Provider failure persists nothing; duplicate
    /// registration is an explicit conflict. Retrying after a failed internal
    /// insert is safe (provisioning is idempotent). No default credentials.
    /// </summary>
    public async Task<Result<AdminUserRow, DomainError>> CreateUserAsync(
        CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var gate = _gate.Require(CanonicalCapabilities.AdminGerir);
        if (gate.IsFailure)
            return Result<AdminUserRow, DomainError>.Failure(gate.Error);

        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.DisplayName))
            return Result<AdminUserRow, DomainError>.Failure(DomainError.Validation(
                "ADMIN_USER_INVALID",
                "Email, palavra-passe e nome são obrigatórios."));

        var template = await _repository.GetTemplateAsync(request.TemplateId, cancellationToken);
        if (template is null || !template.Active)
            return Result<AdminUserRow, DomainError>.Failure(DomainError.Validation(
                "ADMIN_TEMPLATE_INVALID",
                "O template de acesso não existe ou está inativo."));

        var provisioned = await _provisioning.EnsureAuthUserAsync(
            request.Email, request.Password, cancellationToken);
        if (provisioned.IsFailure)
            return Result<AdminUserRow, DomainError>.Failure(provisioned.Error);

        if (await _repository.AuthUserIdAlreadyRegisteredAsync(
                provisioned.Value.AuthUserId, cancellationToken))
            return Result<AdminUserRow, DomainError>.Failure(DomainError.DomainConflict(
                "ADMIN_USER_ALREADY_REGISTERED",
                "Já existe um utilizador interno associado a esta conta de autenticação."));

        var now = _clock.UtcNow;
        var actorId = provisioned.Value.AuthUserId.ToString();
        await _repository.CreateInternalUserAsync(
            actorId,
            provisioned.Value.AuthUserId,
            request.DisplayName.Trim(),
            string.IsNullOrWhiteSpace(request.ProfileTitle) ? null : request.ProfileTitle.Trim(),
            request.TemplateId,
            now,
            cancellationToken);

        await AuditAsync(gate.Value, "create", "internal_user", actorId,
            request.DisplayName.Trim(), "succeeded", null, now, cancellationToken);

        return Result<AdminUserRow, DomainError>.Success(new AdminUserRow(
            actorId,
            provisioned.Value.AuthUserId,
            request.DisplayName.Trim(),
            string.IsNullOrWhiteSpace(request.ProfileTitle) ? null : request.ProfileTitle.Trim(),
            request.TemplateId,
            Active: true,
            now));
    }

    public async Task<Result<AdminUserRow, DomainError>> UpdateUserAsync(
        UpdateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var gate = _gate.Require(CanonicalCapabilities.AdminGerir);
        if (gate.IsFailure)
            return Result<AdminUserRow, DomainError>.Failure(gate.Error);

        var existing = await _repository.GetUserAsync(request.ActorId, cancellationToken);
        if (existing is null)
            return Result<AdminUserRow, DomainError>.Failure(DomainError.NotFound(
                "INTERNAL_USER_NOT_FOUND", "Utilizador interno não encontrado."));

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return Result<AdminUserRow, DomainError>.Failure(DomainError.Validation(
                "ADMIN_USER_INVALID", "O nome não pode ficar vazio."));

        var now = _clock.UtcNow;
        try
        {
            await _repository.UpdateUserAsync(
                request.ActorId,
                request.DisplayName.Trim(),
                string.IsNullOrWhiteSpace(request.ProfileTitle) ? null : request.ProfileTitle.Trim(),
                request.ExpectedUpdatedAt,
                now,
                cancellationToken);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Result<AdminUserRow, DomainError>.Failure(
                DomainError.ConcurrencyConflict("ADMIN_CONCURRENCY_CONFLICT", ex.Message));
        }

        await AuditAsync(gate.Value, "update", "internal_user", request.ActorId,
            request.DisplayName.Trim(), "succeeded",
            $"display_name={existing.DisplayName}; profile_title={existing.ProfileTitle}",
            now, cancellationToken);

        return Result<AdminUserRow, DomainError>.Success(existing with
        {
            DisplayName = request.DisplayName.Trim(),
            ProfileTitle = string.IsNullOrWhiteSpace(request.ProfileTitle)
                ? null
                : request.ProfileTitle.Trim(),
            UpdatedAtUtc = now
        });
    }

    public async Task<Result<AdminUserRow, DomainError>> ChangeTemplateAsync(
        ChangeUserTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var gate = _gate.Require(CanonicalCapabilities.AdminGerir);
        if (gate.IsFailure)
            return Result<AdminUserRow, DomainError>.Failure(gate.Error);

        var existing = await _repository.GetUserAsync(request.ActorId, cancellationToken);
        if (existing is null)
            return Result<AdminUserRow, DomainError>.Failure(DomainError.NotFound(
                "INTERNAL_USER_NOT_FOUND", "Utilizador interno não encontrado."));

        var template = await _repository.GetTemplateAsync(request.TemplateId, cancellationToken);
        if (template is null)
            return Result<AdminUserRow, DomainError>.Failure(DomainError.Validation(
                "ADMIN_TEMPLATE_INVALID", "O template de acesso não existe."));

        var now = _clock.UtcNow;
        bool applied;
        try
        {
            applied = await _repository.ChangeUserTemplateAsync(
                request.ActorId, request.TemplateId,
                request.ExpectedUpdatedAt, now, cancellationToken);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Result<AdminUserRow, DomainError>.Failure(
                DomainError.ConcurrencyConflict("ADMIN_CONCURRENCY_CONFLICT", ex.Message));
        }

        if (!applied)
            return Result<AdminUserRow, DomainError>.Failure(DomainError.DomainConflict(
                "ADMIN_SELF_LOCKOUT",
                "Operação recusada: deve permanecer pelo menos um administrador ativo " +
                "com template ativo que conceda admin.gerir."));

        await AuditAsync(gate.Value, "change_template", "internal_user", request.ActorId,
            existing.DisplayName, "succeeded",
            $"template_id={existing.TemplateId} → {request.TemplateId}",
            now, cancellationToken);

        return Result<AdminUserRow, DomainError>.Success(existing with
        {
            TemplateId = request.TemplateId,
            UpdatedAtUtc = now
        });
    }

    public async Task<Result<AdminUserRow, DomainError>> SetActiveAsync(
        SetUserActiveRequest request, CancellationToken cancellationToken = default)
    {
        var gate = _gate.Require(CanonicalCapabilities.AdminGerir);
        if (gate.IsFailure)
            return Result<AdminUserRow, DomainError>.Failure(gate.Error);

        var existing = await _repository.GetUserAsync(request.ActorId, cancellationToken);
        if (existing is null)
            return Result<AdminUserRow, DomainError>.Failure(DomainError.NotFound(
                "INTERNAL_USER_NOT_FOUND", "Utilizador interno não encontrado."));

        var now = _clock.UtcNow;
        bool applied;
        try
        {
            applied = await _repository.SetUserActiveAsync(
                request.ActorId, request.Active,
                request.ExpectedUpdatedAt, now, cancellationToken);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Result<AdminUserRow, DomainError>.Failure(
                DomainError.ConcurrencyConflict("ADMIN_CONCURRENCY_CONFLICT", ex.Message));
        }

        if (!applied)
            return Result<AdminUserRow, DomainError>.Failure(DomainError.DomainConflict(
                "ADMIN_SELF_LOCKOUT",
                "Operação recusada: deve permanecer pelo menos um administrador ativo " +
                "com template ativo que conceda admin.gerir."));

        await AuditAsync(gate.Value, request.Active ? "activate" : "deactivate",
            "internal_user", request.ActorId, existing.DisplayName, "succeeded", null,
            now, cancellationToken);

        return Result<AdminUserRow, DomainError>.Success(existing with
        {
            Active = request.Active,
            UpdatedAtUtc = now
        });
    }

    /// <summary>
    /// Composite save of the Admin user form: display/profile fields,
    /// template assignment and activation are applied as separate guarded
    /// use cases (each re-authorized and audited), refreshing the
    /// concurrency version between steps. Any failed step stops the flow
    /// and returns its explicit result.
    /// </summary>
    public async Task<Result<AdminUserRow, DomainError>> SaveUserAsync(
        string actorId,
        string displayName,
        string? profileTitle,
        string templateId,
        bool active,
        DateTimeOffset expectedUpdatedAt,
        CancellationToken cancellationToken = default)
    {
        var version = expectedUpdatedAt;

        var updated = await UpdateUserAsync(
            new UpdateAdminUserRequest(actorId, displayName, profileTitle, version),
            cancellationToken);
        if (updated.IsFailure)
            return updated;
        version = updated.Value.UpdatedAtUtc;

        if (updated.Value.TemplateId != templateId)
        {
            var changed = await ChangeTemplateAsync(
                new ChangeUserTemplateRequest(actorId, templateId, version),
                cancellationToken);
            if (changed.IsFailure)
                return changed;
            version = changed.Value.UpdatedAtUtc;
        }

        if (updated.Value.Active != active)
        {
            var activation = await SetActiveAsync(
                new SetUserActiveRequest(actorId, active, version),
                cancellationToken);
            if (activation.IsFailure)
                return activation;
            return activation;
        }

        return updated;
    }

    /// <summary>
    /// Admin-initiated password reset (04_ACC §9): explicit action, audited
    /// with executor/affected/result, privileged adapter only; the current
    /// password is never shown or recovered and no secret is audited.
    /// </summary>
    public async Task<Result<bool, DomainError>> RequestPasswordResetAsync(
        string targetActorId, CancellationToken cancellationToken = default)
    {
        var gate = _gate.Require(CanonicalCapabilities.AdminGerir);
        if (gate.IsFailure)
            return Result<bool, DomainError>.Failure(gate.Error);

        var target = await _repository.GetUserAsync(targetActorId, cancellationToken);
        if (target is null)
            return Result<bool, DomainError>.Failure(DomainError.NotFound(
                "INTERNAL_USER_NOT_FOUND", "Utilizador interno não encontrado."));

        var reset = await _provisioning.RequestPasswordResetAsync(
            target.AuthUserId, cancellationToken);
        if (reset.IsFailure)
            return Result<bool, DomainError>.Failure(reset.Error);

        await AuditAsync(gate.Value, "password_reset_request", "internal_user",
            targetActorId, target.DisplayName, "succeeded", null,
            _clock.UtcNow, cancellationToken);

        return Result<bool, DomainError>.Success(true);
    }

    private Task AuditAsync(
        AdminExecutor executor,
        string actionCode,
        string entityType,
        string entityId,
        string entityLabel,
        string result,
        string? detail,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _repository.InsertAuditEventAsync(new AuditEntry(
            now,
            executor.ActorId,
            executor.DisplayName,
            CanonicalCapabilities.AdminModuleId,
            actionCode,
            entityType,
            entityId,
            entityLabel,
            result,
            detail), cancellationToken);
}

/// <summary>Canonical capability ids used by Administration (modules/00).</summary>
public static class CanonicalCapabilities
{
    public const string AdminModuleId = "admin";
    public const string AdminGerir = "admin.gerir";
    public const string AuditView = "audit.view";
    public const string AuditExport = "audit.export";
}

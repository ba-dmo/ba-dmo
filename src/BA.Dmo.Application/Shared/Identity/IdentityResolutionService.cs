using BA.Dmo.Application.Shared.Access;
using BA.Dmo.Domain.Shared.Access;
using BA.Dmo.Domain.Shared.Kernel;

namespace BA.Dmo.Application.Shared.Identity;

/// <summary>
/// Result of a successful per-request identity resolution (GLM-ACC-01):
/// authoritative internal identity + effective access surface (U-04) +
/// first-page resolution. Grants are NEVER read from the cookie; this
/// structure is rebuilt server-side on every request.
/// </summary>
public sealed record ResolvedIdentity(
    CurrentUser User,
    string ActorId,
    string? ProfileTitle,
    EffectiveAccess Access,
    FirstPageResolution FirstPage);

/// <summary>
/// Server-side identity resolution pipeline (Plan-V3 GLM-ACC-01, U-05):
/// authenticated Supabase auth_user_id → internal_users → access template →
/// normalized grants → U-04 AccessResolver → CurrentUser/effective access.
/// Fail-closed: missing/inactive internal user → INTERNAL_USER_INACTIVE;
/// missing/inactive template → ACCESS_TEMPLATE_INACTIVE; both produce the
/// safe "session without access" state — never an Admin fallback, never a
/// silent grant (GLM-ARCH-18). No role-name branching anywhere.
/// </summary>
public sealed class IdentityResolutionService
{
    private readonly IInternalUserRepository _repository;
    private readonly AccessResolver _accessResolver;

    public IdentityResolutionService(
        IInternalUserRepository repository,
        AccessResolver accessResolver)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _accessResolver = accessResolver ?? throw new ArgumentNullException(nameof(accessResolver));
    }

    public async Task<Result<ResolvedIdentity, DomainError>> ResolveAsync(
        Guid authUserId,
        CancellationToken cancellationToken = default)
    {
        if (authUserId == Guid.Empty)
            return Result<ResolvedIdentity, DomainError>.Failure(
                DomainError.Unauthorized(
                    "INTERNAL_USER_INACTIVE",
                    "No authenticated internal user is resolved for this session."));

        InternalUserRecord? record;
        try
        {
            record = await _repository.FindByAuthUserIdAsync(authUserId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed on backend failure: no identity, no access.
            return Result<ResolvedIdentity, DomainError>.Failure(
                DomainError.BackendUnavailable(
                    "IDENTITY_RESOLUTION_UNAVAILABLE",
                    "Internal identity could not be resolved. Try again."));
        }

        if (record is null || !record.UserActive)
            return Result<ResolvedIdentity, DomainError>.Failure(
                DomainError.Unauthorized(
                    "INTERNAL_USER_INACTIVE",
                    "The internal user is not registered or is inactive."));

        if (!record.TemplateActive)
            return Result<ResolvedIdentity, DomainError>.Failure(
                DomainError.Unauthorized(
                    "ACCESS_TEMPLATE_INACTIVE",
                    "The access template is missing or inactive."));

        var parsed = AccessTemplateGrantsParser.Parse(record.ModulesJson);
        if (parsed.IsFailure)
            return Result<ResolvedIdentity, DomainError>.Failure(
                DomainError.Unauthorized(
                    "ACCESS_TEMPLATE_INACTIVE",
                    "The access template cannot grant access."));

        var template = new AccessTemplateDefinition(
            record.TemplateId,
            record.TemplateName,
            active: true,
            parsed.Value);

        var access = _accessResolver.Resolve(template);
        var firstPage = _accessResolver.ResolveFirstPage(access);

        var currentUser = new CurrentUser(
            record.AuthUserId,
            record.DisplayName,
            access.AuthorizedModuleIds,
            access.GrantedCapabilityIds);

        return Result<ResolvedIdentity, DomainError>.Success(new ResolvedIdentity(
            currentUser,
            record.ActorId,
            record.ProfileTitle,
            access,
            firstPage));
    }
}

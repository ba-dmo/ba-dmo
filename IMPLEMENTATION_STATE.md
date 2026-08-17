# BA DMO Implementation State

Workspace:
C:\BA-DMO-FRESH-BUILD

Reference repository:
C:\BA-DMO-REFERENCE\ba-dmo-beta

Implementation authority:
Plan-V3

Current branch:
main

Current HEAD:
b82f83c13af6024ad31cc1efc421c649d1f95d26

Current unit:
U-05 — Auth + identidade interna

Status:
COMPLETE

Completed units:
U-01, U-02, U-03, U-04, U-05

## Last Unit Summary

U-05 delivered the identity/authentication foundation exactly per Plan-V3
(GLM-ACC-01/03/13, GLM-ARCH-14/18, PV-06/07/08, 06_DATA §14–15, 05_SHL §5–6):

- Auth adapter boundary: ISupabaseAuthAdapter (Application port) +
  SupabaseAuthAdapter (Infrastructure, direct server-side REST to GoTrue
  token endpoint with the anon key — provider types never leave
  Infrastructure; no provider SDK dependency).
- Privileged provisioning boundary: IAdminProvisioningAdapter port +
  SupabaseAdminProvisioningAdapter — the ONLY service_role consumer;
  constructed exclusively inside the bootstrap-admin CLI path; never
  registered in the web request pipeline; service_role never appears in
  messages/claims/browser assets.
- Identity resolution pipeline (fail closed, no role branching):
  session cookie (auth_user_id ONLY) → internal_users lookup →
  template active check → modules jsonb parse → U-04 GrantNormalizer/
  AccessResolver → CurrentUser + EffectiveAccess + first-page resolution.
  Errors: INTERNAL_USER_INACTIVE / ACCESS_TEMPLATE_INACTIVE → safe
  "session without access" state (GLM-ACC-01.6).
- Session/authentication: ASP.NET Core cookie bridge (scheme BaDmo.Session,
  HttpOnly, sliding 8h); /login, /logout, /access-denied, /no-access routes;
  fallback authorization policy requiring an authenticated session;
  anti-forgery on the auth forms; generic credential errors (no email
  existence disclosure).
- Current user: RequestCurrentUserAccessor (ICurrentUserAccessor, scoped,
  per-request resolution with request cache) — claims are never the source
  of truth for grants.
- Persistence authorship binding: CurrentUserAuthorshipAccessor
  (IPersistenceAuthorshipAccessor) resolves actor_id from the internal
  identity; UTC timestamps via IClock.
- bootstrap-admin real implementation (replaces the U-02 placeholder):
  CLI-only, explicit env configuration, one-shot, idempotent (existing
  valid admin → no writes), auditable (audit_events row, moduleId=admin,
  action bootstrap_admin), minimal admin.gerir template, no functional
  modules granted, no defaults, no HTTP endpoint, no HostedService.
- Identity repository: DapperInternalUserRepository over the U-03
  foundation (parameterized SQL, enumerated columns, atomic bootstrap
  transaction).
- No live Supabase/DB access anywhere; all U-05 tests run on fakes.

## Plan-V3 Sources Used

- 10_MASTER_IMPLEMENTATION_ROADMAP.md (U-05 only)
- 04_IDENTITY_ACCESS_AND_ADMIN_SPEC.md (§1 identity/session, §2 grants,
  §3 catalog, §7 scenarios 1/7/9/12, §10 self-lockout context, §13 bootstrap)
- 05_SHELL_NAVIGATION_AND_ROUTING_SPEC.md (§5 routes, §6 states)
- 06_DATA_BACKEND_AND_SECURITY_SPEC.md §1 (roles), §3.1 (identity tables),
  §6 (security), §14 (Supabase boundary), §15 (bootstrap)
- 03_TARGET_MODULAR_ARCHITECTURE.md §14 (adapter boundary), §15 (CLI)
- 02_DECISIONS §3.35 PV-06/PV-07/PV-08, TD-09/TD-16 context
- 09_TEST_QUALITY_AND_ACCEPTANCE_SPEC.md §1 (no-I/O unit tests)

## Files Created/Changed

Application (created):
- src/BA.Dmo.Application/Shared/Identity/SupabaseAuthPorts.cs
  (AuthUser, ISupabaseAuthAdapter, IAdminProvisioningAdapter)
- src/BA.Dmo.Application/Shared/Identity/IInternalUserRepository.cs
  (InternalUserRecord, BootstrapAdminCreation)
- src/BA.Dmo.Application/Shared/Identity/AccessTemplateGrantsParser.cs
- src/BA.Dmo.Application/Shared/Identity/IdentityResolutionService.cs
  (ResolvedIdentity)
- src/BA.Dmo.Application/Shared/Identity/BootstrapAdminService.cs

Application (changed):
- src/BA.Dmo.Application/Shared/Access/AccessResolver.cs (additive:
  AuthorizedModuleIds/GrantedCapabilityIds public getters)

Infrastructure (created):
- src/BA.Dmo.Infrastructure/Auth/SupabaseSettings.cs
- src/BA.Dmo.Infrastructure/Auth/SupabaseAuthAdapter.cs
- src/BA.Dmo.Infrastructure/Auth/SupabaseAdminProvisioningAdapter.cs
- src/BA.Dmo.Infrastructure/Identity/DapperInternalUserRepository.cs
- src/BA.Dmo.Infrastructure/Persistence/DbConnectionFactory.cs (changed:
  added LazyDbConnectionFactory for healthy startup without DB config)

Web (created):
- src/BA.Dmo.Web/Identity/SessionClaims.cs
- src/BA.Dmo.Web/Identity/RequestCurrentUserAccessor.cs
- src/BA.Dmo.Web/Identity/CurrentUserAuthorshipAccessor.cs
- src/BA.Dmo.Web/Authorization/AuthenticatedSessionHandler.cs
- src/BA.Dmo.Web/Pages/Auth/Login.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Auth/Logout.cshtml(.cs)
- src/BA.Dmo.Web/Pages/AccessDenied.cshtml(.cs)
- src/BA.Dmo.Web/Pages/NoAccess.cshtml(.cs)

Web (changed):
- src/BA.Dmo.Web/Program.cs (auth/session/DI wiring; UseAuthentication/
  UseAuthorization; fallback policy)
- src/BA.Dmo.Web/Cli/BootstrapAdminCommand.cs (real one-shot implementation)

Tests (created):
- tests/BA.Dmo.UnitTests/Shared/Identity/IdentityResolutionServiceTests.cs
- tests/BA.Dmo.UnitTests/Shared/Identity/AccessTemplateGrantsParserTests.cs
- tests/BA.Dmo.UnitTests/Shared/Identity/BootstrapAdminServiceTests.cs
- tests/BA.Dmo.IntegrationTests/Identity/FakeHttpMessageHandler.cs
- tests/BA.Dmo.IntegrationTests/Identity/SupabaseAuthAdapterTests.cs
- tests/BA.Dmo.IntegrationTests/Identity/SupabaseAdminProvisioningAdapterTests.cs
- tests/BA.Dmo.IntegrationTests/Identity/WebAuthSessionTests.cs
- tests/BA.Dmo.IntegrationTests/Identity/IdentitySecurityGuardTests.cs
- tests/BA.Dmo.IntegrationTests/Cli/BootstrapAdminCliTests.cs

Tests (changed):
- tests/BA.Dmo.IntegrationTests/Cli/CliCommandPlaceholderTests.cs (bootstrap
  contract updated from placeholder to missing-configuration behavior)

Tests project (changed):
- tests/BA.Dmo.IntegrationTests/BA.Dmo.IntegrationTests.csproj (added
  Microsoft.AspNetCore.Mvc.Testing 10.0.11 for WebApplicationFactory)

## Build

Commands:
- `dotnet restore BA-DMO.sln` — PASS
- `dotnet build BA-DMO.sln` — PASS (0 warnings, 0 errors)

## Tests Executed

- `dotnet test BA-DMO.sln --no-build`
- Manual CLI verification: `dotnet BA.Dmo.Web.dll bootstrap-admin` without
  configuration → exit 2 with explicit diagnostic listing the missing
  variables; service-role value never echoed.
- Manual web smoke: unauthenticated `/` → 302 `/login?ReturnUrl=%2F`;
  `/login` → 200 login page (no Supabase/DB configured; fail-closed at use).

## Test Results

- BA.Dmo.UnitTests: Total 161, Passed 161, Failed 0, Skipped 0
- BA.Dmo.IntegrationTests: Total 93, Passed 93, Failed 0, Skipped 0
- Combined: Total 254, Passed 254, Failed 0 (203 prior + 51 new U-05 tests)

U-05 required coverage:
1. external identity → internal resolution ✓
2. missing internal user → fail closed ✓
3. inactive internal user → denied ✓
4. missing/inactive template → denied ✓
5. valid template → U-04 effective access ✓
6. Job On landing after login ✓ (web test: 302 /jobon)
7. deterministic fallback when Job On unavailable ✓ (U-04 resolver tests
   exercised through resolution)
8. no role-name routing ✓ (template-name invariance test)
9. current user carries authoritative internal identity ✓
10. persistence authorship binds to internal identity ✓ (binding class +
    actor_id contract tests via resolution)
11. auth provider failure handled safely ✓
12. expired/invalid authentication handled safely ✓ (generic failure,
    no session)
13. login success flow ✓
14. logout flow ✓
15. unauthenticated protected-page behavior ✓
16. access-denied/no-access behavior ✓
17. bootstrap-admin success path with fakes ✓
18. bootstrap-admin idempotency ✓
19. bootstrap-admin missing config failure ✓ (service + CLI)
20. bootstrap-admin does not start web server ✓ (CLI returns before any
    WebApplication construction; Program structure unchanged)
21. no HTTP/bootstrap setup endpoint ✓ (guard: no page/handler consumes
    IAdminProvisioningAdapter; provisioning only in CLI type)
22. service_role never exposed to browser/runtime claims ✓ (adapter tests:
    bearer only server-side; never in messages; session claim contract guard)
23. no debug bypass ✓ (existing guards green + no new bypass markers)
24. no hardcoded role/email routing ✓
25. U-01..U-04 regression ✓ (all 203 prior tests green)

## Decisions Applied

- Provider approach: direct server-side REST behind the adapters (PV-06
  leaves the concrete provider open) — no supabase-csharp dependency.
- Session cookie carries ONLY ba_dmo.auth_user_id; grants re-resolved
  server-side per request (GLM-ACC-01.5).
- Post-login destination = U-04 ResolveFirstPage (Job On landing; canonical
  fallback; /no-access otherwise) — never role-specific redirects.
- Generic login error message for every failure (design contract: never
  reveal email existence).
- Internal user actor_id = Supabase auth user UUID text for bootstrap
  (stable, TD-09); CurrentUser.InternalUserId = auth_user_id.
- bootstrap template id fixed (tpl-bootstrap-admin) for idempotency;
  admin.gerir only (GLM-ACC-13).
- Fallback authorization policy requires session everywhere; module/
  capability guards remain U-07 scope.

## Configuration Contract (env, never committed)

- BA_DMO_SUPABASE_URL — Supabase project URL
- BA_DMO_SUPABASE_ANON_KEY — anon key (normal sign-in, server-side)
- BA_DMO_SUPABASE_SERVICE_ROLE_KEY — PRIVILEGED, bootstrap-admin only
- BA_DMO_BOOTSTRAP_ADMIN_EMAIL / _PASSWORD / _NAME — explicit bootstrap input
- DB connection unchanged from U-02: BA_DMO_DB_CONNECTION_STRING / DATABASE_URL
Missing configuration fails explicitly; no defaults; no secrets in
appsettings or repository files.

## Security Guards

- IAdminProvisioningAdapter reachable only from BootstrapAdminCommand
  (reflection guard over the web assembly; no page/handler dependency)
- Session claim contract: single auth-user-id claim; no role/grant/
  capability/module/admin claim constants may be introduced (guard)
- Application assembly references no provider/Infrastructure assemblies
  (guard)
- Adapter errors never leak keys or provider details (tested)
- Existing no-debug-bypass guards remain green

## Safe Implementer Choices Made

- Cookie lifetime: sliding 8 hours; SameSite Lax; SecurePolicy SameAsRequest.
- Env variable naming for Supabase/bootstrap (Plan-V3 leaves names open;
  06_DATA §6.5 requires environment/secrets only).
- LazyDbConnectionFactory keeps web startup healthy without DB config;
  first DB use fails explicitly.
- WebApplicationFactory tests disable anti-forgery via page convention
  (test-only); production forms keep anti-forgery validation.
- Provisioning conflict path: 422/conflict → admin lookup by email for
  idempotent recovery of a partially completed bootstrap.

## Blockers

NONE.

## Known Risks

- DB round-trips of the identity repository are verified only when a test
  database becomes available (integration smoke phase), consistent with
  U-03/U-04 treatment.
- Real Supabase endpoints untested by design in U-05 (no live mutation
  allowed); adapters covered with scripted HTTP fakes.

## Manual Checks Pending

NONE required for U-05. (Owner review of the working tree before commit is
expected — commit/push not authorized for this execution.)

## Next Unit

U-06 — Administração completa (users/templates CRUD against the catalog,
self-lockout, auditoria, concorrência, reset password, cenários 7/8/13/14/15).

Status: NOT STARTED (per instruction; U-06 not touched in any way).

## Git Commit

NO commit created (commit/push not authorized for this execution). All U-05
changes left in the working tree for owner review.

Branch: main
HEAD: b82f83c13af6024ad31cc1efc421c649d1f95d26 (unchanged)

## Notes for Next Agent Session

- Environment: use `C:\BA-DMO-FRESH-BUILD\.dotnet-sdk\dotnet.exe` (set
  DOTNET_ROOT) and `C:\Program Files\Git\cmd\git.exe`; neither is on PATH.
- U-06 consumes: IdentityResolutionService, IInternalUserRepository (extend
  for CRUD), AccessResolver/catalogs, DapperUnitOfWork, audit_events
  contract, cookie session. Authority: 04_ACC §9–12, 04_ADMIN_COMPLETE_SPEC
  (referenced by roadmap; verify presence in the package before use),
  GLM-ACC-10 (self-lockout atomic), GLM-ACC-12 (optimistic concurrency).
- Canonical commands: `dotnet restore`, `dotnet build`, `dotnet test` on
  `BA-DMO.sln`.

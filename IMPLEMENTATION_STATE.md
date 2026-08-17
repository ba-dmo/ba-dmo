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
f356f325c4da0de2015dc3e24f8afe2291c93078

Current unit:
U-06 — Administração completa

Status:
COMPLETE

Completed units:
U-01, U-02, U-03, U-04, U-05, U-06

## Last Unit Summary

U-06 delivered the Administration module exactly per Plan-V3 (04_ACC §9–12,
GLM-ACC-06/09/10/11/12, UD-10/UD-17, TD-16/TD-19, modules/00 capabilities):

- Application use cases (Modules/Admin): AdminUserService (list, create via
  privileged provisioning, edit display/profile, template assignment,
  activate/deactivate, explicit password-reset initiation, composite save),
  AdminTemplateService (create/update with strict canonical-catalog
  validation — unknown modules/capabilities/area grants reject the whole
  write; templates deactivated, never deleted), AdminMirrorService (display
  order/activation of canonical modules only; mirror never grants access),
  AdminAuditService (audit.view query with canonical 20/40/60 pagination;
  audit.export CSV of factual columns only), AdminAuthorizationGate
  (server-side capability re-check on every operation; executor identity
  resolved from the session, never from posted forms).
- Authorization: CapabilityRequirement + CapabilityAuthorizationHandler;
  policies BaDmo.Admin.Gerir / BaDmo.Audit.View / BaDmo.Audit.Export built
  ONLY on canonical capabilities — no role names, emails or template names.
- Infrastructure: DapperAdminRepository (parameterized SQL, enumerated
  columns; optimistic concurrency via updated_at + ConcurrencyGuard
  (GLM-ACC-12); self-lockout invariant validated in the SAME transaction as
  the write (GLM-ACC-10); audit insert/query).
- Privileged operations: IAdminProvisioningAdapter extended with
  RequestPasswordResetAsync (admin lookup + recovery-link request); the
  adapter stays fail-closed without service-role configuration and is only
  reachable via admin.gerir-gated use cases or the bootstrap CLI; the
  service-role value never reaches messages, claims, audit or browser.
- Admin Razor Pages: /admin (dashboard, "Voltar ao Job On"), /admin/users
  (list/search), /admin/users/create, /admin/users/edit (save + template +
  activation + password reset), /admin/templates (list), /admin/templates/edit
  (canonical grants editor), /admin/applications (mirror order/activation),
  /admin/audit (filters, canonical pagination, CSV export). Page models call
  Application services; no business logic or SQL in Razor.
- Self-lockout (GLM-ACC-10): deactivation/template-change/template-update
  that would leave zero active admins with an active admin.gerir template
  are rolled back and rejected with ADMIN_SELF_LOCKOUT; self-exclusion
  allowed when another functional admin remains.
- Audit (GLM-ACC-11): actions create/update/activate/deactivate/
  change_template (internal_user), create/update/update_modules/activate/
  deactivate (access_template), password_reset_request, mirror_update —
  factual, no secrets.

## Plan-V3 Sources Used

- 10_MASTER_IMPLEMENTATION_ROADMAP.md (U-06 only)
- 04_IDENTITY_ACCESS_AND_ADMIN_SPEC.md (§6 matrix, §9 operations,
  §10 self-lockout, §11 audit, §12 concurrency)
- modules/00_MODULE_CATALOG.md (capabilities admin.gerir/audit.view/
  audit.export; GLM-CAT-02 rule 3 mirror order)
- 02_DECISIONS UD-02/UD-10/UD-17, TD-16, C16 (admin.consultar absent in V1)
- 06_DATA_BACKEND_AND_SECURITY_SPEC.md §3.1 (identity tables), §6 (security)
- 09_TEST_QUALITY_AND_ACCEPTANCE_SPEC.md §4 (scenarios 7/8/13/14/15/17)
- Design-Reference: not needed for behavior (functional pages only)

## Files Created/Changed

Application (created):
- src/BA.Dmo.Application/Modules/Admin/AdminModels.cs
- src/BA.Dmo.Application/Modules/Admin/IAdminRepository.cs
- src/BA.Dmo.Application/Modules/Admin/AdminAuthorizationGate.cs
- src/BA.Dmo.Application/Modules/Admin/AdminUserService.cs
  (incl. CanonicalCapabilities constants)
- src/BA.Dmo.Application/Modules/Admin/AdminTemplateService.cs
- src/BA.Dmo.Application/Modules/Admin/AdminMirrorService.cs
- src/BA.Dmo.Application/Modules/Admin/AdminAuditService.cs

Application (changed):
- src/BA.Dmo.Application/Shared/Identity/SupabaseAuthPorts.cs
  (IAdminProvisioningAdapter + RequestPasswordResetAsync)

Infrastructure (created/changed):
- src/BA.Dmo.Infrastructure/Access/DapperAdminRepository.cs
- src/BA.Dmo.Infrastructure/Auth/SupabaseAdminProvisioningAdapter.cs
  (changed: password-reset operation)

Web (created):
- src/BA.Dmo.Web/Authorization/CapabilityAuthorizationHandler.cs
  (CapabilityRequirement + AdminPolicies)
- src/BA.Dmo.Web/Pages/Admin/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Admin/Users/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Admin/Users/Create.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Admin/Users/Edit.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Admin/Templates/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Admin/Templates/Edit.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Admin/Applications/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Admin/Audit/Index.cshtml(.cs)

Web (changed):
- src/BA.Dmo.Web/Program.cs (Admin policies, handler and DI registrations;
  privileged adapter registered fail-closed; mirror repository registered)

Tests (created):
- tests/BA.Dmo.UnitTests/Shared/Admin/FakeAdminRepository.cs
- tests/BA.Dmo.UnitTests/Shared/Admin/AdminUserServiceTests.cs
- tests/BA.Dmo.UnitTests/Shared/Admin/AdminTemplateServiceTests.cs
- tests/BA.Dmo.UnitTests/Shared/Admin/AdminAuditAndMirrorTests.cs
- tests/BA.Dmo.IntegrationTests/Access/AdminWebAuthorizationTests.cs
- tests/BA.Dmo.IntegrationTests/Access/AdminSecurityGuardTests.cs

## Build

Commands:
- `dotnet restore BA-DMO.sln` — PASS
- `dotnet build BA-DMO.sln --no-restore` — PASS (0 warnings, 0 errors)

## Tests Executed

- `dotnet test BA-DMO.sln --no-build` (both projects)
- Targeted runs during development per owner guidance (unit and integration
  subsets); full regression at the final gate.
- Manual web smoke after composition changes: startup healthy without DB or
  Supabase config; `/login` 200; `/admin` 302 → /login (unauthenticated);
  `/no-access` 200.

## Test Results

- BA.Dmo.UnitTests: Total 187, Passed 187, Failed 0, Skipped 0
- BA.Dmo.IntegrationTests: Total 100, Passed 100, Failed 0, Skipped 0
- Combined: Total 287, Passed 287, Failed 0
  (254 prior U-01..U-05 tests + 33 new U-06 tests, all green)

U-06 required coverage (high-value tests, multiple requirements each):
1. admin capability required for page + mutations ✓ (unit gate tests + web
   policy tests)
2. mutations re-check capability server-side ✓ (gate on every service call)
3. user list/query mapping ✓
4. create-user happy path with fake provider ✓
5. provider creation failure ✓ (nothing persisted)
6. persistence failure/recovery semantics ✓ (duplicate registration
   conflict; retry-safe creation flow)
7. duplicate auth/email handling ✓
8. activate/deactivate user ✓
9. change user template ✓
10. profile/header label update ✓ (display-only; not used for auth)
11. template create/update ✓
12. template validation against canonical catalog ✓
13. unknown module/capability rejected ✓
14. capability ownership enforced ✓
15. inactive template behavior ✓ (validation rejects assignment)
16. self-lockout prevented ✓ (users + templates paths)
17. last-admin/administrative-path protection ✓
18. password reset privileged adapter path ✓
19. service_role never exposed ✓ (adapter message tests + guards)
20. administrative action audit written ✓
21. passwords/tokens never audited ✓ (asserted)
22. catalog mirror constrained to canonical modules ✓
23. Job On remains landing after admin login ✓ (web test)
24. no role-name routing/authorization ✓ (guards + behavior tests)
25. unauthorized forged POST denied ✓ (web test; no writes)
26. concurrency conflict behavior ✓ (reload message, GLM-ACC-12)
27. U-01–U-05 regression ✓ (254 tests green)
28. no debug bypass ✓ (existing guards green)

## Decisions Applied

- Authorization = canonical capabilities only (admin.gerir, audit.view,
  audit.export); audit tab requires audit.view, export requires audit.export
  (scenario 17).
- Executor identity for audits comes from the server-side gate (session),
  never from posted form fields (forged-mutation protection).
- Self-lockout validated by applying the write and counting surviving admin
  paths inside the same transaction; zero → rollback + ADMIN_SELF_LOCKOUT.
- Template grants validated with the U-04 GrantNormalizer discard report —
  any discard rejects the write (unknown/wrong-owner/area grants never
  silently granted).
- Audit page/export are factual listings only (no scores/rankings,
  UD-17); CSV separator sanitized.
- C16: admin.consultar does not exist in V1 — not implemented.

## Safe Implementer Choices Made

- Admin page markup is functional/semantic (design system tokens belong to
  U-08/U-09); no inline business logic in Razor.
- Composite user save applies guarded sub-operations sequentially with
  version refresh between steps.
- Audit CSV export limited to factual columns; before/after summaries
  excluded from export.
- Audit query filter values passed strictly as SQL parameters (dynamic SQL
  contains parameter names only).
- Privileged adapter registered fail-closed in web DI (rejects without
  service-role env config; reachable only via admin.gerir-gated use cases).

## Blockers

NONE.

## Known Risks

- DB round-trips of DapperAdminRepository verified only when a test database
  becomes available (integration smoke phase), consistent with U-03/U-04/U-05.

## Manual Checks Pending

NONE required for U-06. (Owner review of the working tree before commit is
expected — commit/push not authorized for this execution.)

## Next Unit

U-07 — Shell única + navegação derivada (per the canonical roadmap:
Layout/header/nav, tabs by grants, Controlo group, landing UD-04/UD-16,
deep links, /peso vs /peso/responsavel exclusivity guards).

Status: NOT STARTED (per instruction; U-07 not touched in any way).

## Git Commit

NO commit created (commit/push not authorized for this execution). All U-06
changes left in the working tree for owner review.

Branch: main
HEAD: f356f325c4da0de2015dc3e24f8afe2291c93078 (unchanged)

## Notes for Next Agent Session

- Environment: use `C:\BA-DMO-FRESH-BUILD\.dotnet-sdk\dotnet.exe` (set
  DOTNET_ROOT) and `C:\Program Files\Git\cmd\git.exe`; neither is on PATH.
- U-07 consumes: session (U-05), AccessResolver/EffectiveAccess + area rules
  (U-04), capability policies/handler (U-06), canonical catalog. Authority:
  05_SHELL_NAVIGATION_AND_ROUTING_SPEC.md, 04_ACC §5–6, GLM-CTR-02.
- Canonical commands: `dotnet restore`, `dotnet build`, `dotnet test` on
  `BA-DMO.sln`.

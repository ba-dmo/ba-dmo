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
0aa71eb985de29567397ec539f402134366754a8

Current unit:
U-04 — Catálogo de módulos + espelho DB

Status:
COMPLETE

Completed units:
U-01, U-02, U-03, U-04

## Last Unit Summary

U-04 delivered the canonical module/capability/page catalog and the
access-template resolution foundation exactly per Plan-V3 (modules/00
GLM-CAT-01..05, 04_ACC GLM-ACC-02/03/06, 05_SHL GLM-SHL-03/04, GLM-CTR-02,
UD-16/DS-01, TD-10):

- CanonicalModuleCatalog (Application/Shared/Access): the 12 modules/00
  entries with exact IDs, canonical order, initial routes and the 10
  canonical capabilities with exact ownership; Controlo modeled as
  FunctionalArea with children peso/pegamentos.
- PageCatalog + CanonicalPageCatalog: 12 module pages from 05_SHL §5 with
  canonical route grammar enforcement
  (^/[a-z][a-z0-9-]*(?:/[a-z][a-z0-9-]*)*$), unique ids/routes, required
  capability per page (jobon.view, peso.aprovar, admin.gerir), single global
  landing (/jobon, UD-16).
- AccessTemplateDefinition + ModuleGrant: template model of
  access_templates.modules jsonb ({moduleId, capabilities}); PreferredFirstPageId
  kept read-only and UNUSED in V1 (05_SHL §4).
- GrantNormalizer (normalizeModules, GLM-ACC-02/TD-10): discards unknown
  modules, capabilities not owned by the granted module, area grants and
  duplicates (first prevails); explicit discard report, nothing silent.
- AccessResolver + EffectiveAccess: navigation derivation in canonical
  order, Controlo area visibility (authorized children only), Peso
  experience exclusivity by capability (never role names), capability-gated
  pages, and first-page resolution: Job On landing for every active template
  (UD-16; universal jobon.view), deterministic canonical-order fallback only
  when Job On is genuinely unavailable, explicit NoAccess otherwise.
- CatalogValidator: invalid canonical configuration fails explicitly with
  the full violation list (never silently repaired); wired into the
  composition root.
- Catalog mirror: IModuleCatalogMirrorRepository port (Application) +
  DapperModuleCatalogMirrorRepository (Infrastructure, U-03 foundation,
  parameterized SQL, one unit of work) + ModuleCatalogMirrorSynchronizer
  (code → mirror sync rows, mirror validation discarding unknown modules,
  display-only merge honoring Admin order adjustments; the DB never
  redefines canonical values).
- No live DB access; no auth/login/Admin UI/shell UI/module functional work.

Landing rule verified against Plan-V3 (UD-16, DS-01, 05_SHL §4, 04_ACC §7
scenarios 1/7) and Design-Reference (PORTAL_LOGIN_ADMIN_HANDOFF, Design.md,
CODER_IMPLEMENTATION_HANDOFF, JOB_ON_DESIGN_BRIEF): Job On is the common
landing of every authenticated user; no role-name-specific landing; the
owner confirmation matches Plan-V3 exactly — implemented accordingly.

## Plan-V3 Sources Used

- 10_MASTER_IMPLEMENTATION_ROADMAP.md (U-04 only)
- modules/00_MODULE_CATALOG.md (canonical catalog, capabilities)
- modules/02_CONTROLO_SPEC.md (functional area semantics)
- 04_IDENTITY_ACCESS_AND_ADMIN_SPEC.md (§2 grants, §3 catalog, §5 Peso
  exclusivity, §6 access matrix, §7 scenarios)
- 05_SHELL_NAVIGATION_AND_ROUTING_SPEC.md (§3 tabs, §4 landing, §5 routes)
- 03_TARGET_MODULAR_ARCHITECTURE.md §2/§7 (Shared/Access placement,
  composition root, module extension contract)
- 06_DATA_BACKEND_AND_SECURITY_SPEC.md §3.1 (access_templates,
  module_catalog_mirror)
- 02_DECISIONS §2/§7.1 (UD-16/DS-01 landing substitution of UD-04)
- Design-Reference/portal-dmo-design-final (landing verification only)

## Files Created/Changed

Application (created):
- src/BA.Dmo.Application/Shared/Access/CanonicalModuleCatalog.cs
- src/BA.Dmo.Application/Shared/Access/PageCatalog.cs
- src/BA.Dmo.Application/Shared/Access/CanonicalPageCatalog.cs
- src/BA.Dmo.Application/Shared/Access/AccessTemplateDefinition.cs
- src/BA.Dmo.Application/Shared/Access/GrantNormalizer.cs
- src/BA.Dmo.Application/Shared/Access/AccessResolver.cs
- src/BA.Dmo.Application/Shared/Access/CatalogValidator.cs
- src/BA.Dmo.Application/Shared/Access/IModuleCatalogMirrorRepository.cs
- src/BA.Dmo.Application/Shared/Access/ModuleCatalogMirrorSynchronizer.cs

Infrastructure (created):
- src/BA.Dmo.Infrastructure/Access/DapperModuleCatalogMirrorRepository.cs

Web (changed):
- src/BA.Dmo.Web/Program.cs (canonical catalog validation at composition)

Tests (created):
- tests/BA.Dmo.UnitTests/Shared/Access/CanonicalModuleCatalogTests.cs
- tests/BA.Dmo.UnitTests/Shared/Access/CanonicalPageCatalogTests.cs
- tests/BA.Dmo.UnitTests/Shared/Access/CatalogValidatorTests.cs
- tests/BA.Dmo.UnitTests/Shared/Access/GrantNormalizerTests.cs
- tests/BA.Dmo.UnitTests/Shared/Access/AccessResolverTests.cs
- tests/BA.Dmo.UnitTests/Shared/Access/ModuleCatalogMirrorSynchronizerTests.cs
- tests/BA.Dmo.IntegrationTests/Access/CatalogCompositionGuardTests.cs

U-01/U-02/U-03 artifacts: UNCHANGED (regression green).

## Build

Commands:
- `dotnet restore BA-DMO.sln` — PASS
- `dotnet build BA-DMO.sln --no-restore` — PASS (0 warnings, 0 errors)

## Tests Executed

- `dotnet test BA-DMO.sln --no-build`
- Manual web verification after composition-root change: `/` → HTTP 200.

## Test Results

- BA.Dmo.UnitTests: Total 135, Passed 135, Failed 0, Skipped 0
- BA.Dmo.IntegrationTests: Total 68, Passed 68, Failed 0, Skipped 0
- Combined: Total 203, Passed 203, Failed 0

U-04 required coverage:
1. complete canonical module catalog ✓ (CanonicalModuleCatalogTests)
2. stable IDs ✓
3. canonical module order ✓
4. capability uniqueness ✓
5. page uniqueness ✓ (CanonicalPageCatalogTests)
6. route uniqueness ✓
7. route validation/grammar ✓ (accept + reject theories)
8. page → module consistency ✓
9. capability → module consistency ✓
10. required capability consistency ✓
11. invalid unknown references fail ✓ (CatalogValidatorTests)
12. inactive module/page behavior ✓ (inactive template, inactive page)
13. preferred-first-page success ✓ (landing /jobon; area first child)
14. preferred-first-page unavailable → deterministic fallback ✓
15. no accessible page → explicit NoAccess ✓
16. template ordering ✓ (canonical order of navigation modules)
17. template capabilities constrain access ✓
18. catalog mirror mapping/validation ✓ (synchronizer tests; DB round-trip
    deferred to integration phase with a test DB, as in U-03)
19. U-01/U-02/U-03 regression ✓ (full suite green)
20. no hardcoded role-name routing ✓ (same-grant templates named after
    roles resolve identically; resolver has no role branching)
21. no hardcoded Boquilhas/Peso/Admin landing shortcut ✓ (landing tests for
    BQ-only, admin-only and zero-grant templates all resolve /jobon)

## Decisions Applied

- Catalog code lives in Application/Shared/Access (roadmap U-04 scope);
  Domain keeps the U-01 ModuleCatalog/Capability primitives unchanged.
- Capability ownership = canonical catalog membership (GLM-CAT-03);
  audit.view/audit.export belong to the admin module; normalization
  validates ownership by membership, not by raw string prefix.
- Landing (owner-confirmed + UD-16): fixed global Job On for every active
  template via universal jobon.view; PreferredFirstPageId read-only/unused
  in V1; fallback = first accessible page in canonical display order only
  when the landing is genuinely unavailable; NoAccess explicit otherwise.
- Peso experience exclusivity resolved by capability in the resolver
  (GLM-ACC-05); route guards themselves arrive in U-07.
- Mirror: code → DB sync only; mirror order/active honored for Admin
  display; unknown mirror rows discarded with report; authorization never
  reads the mirror.

## Safe Implementer Choices Made

- Page IDs "{moduleId}.{name}"; page display order aligned with module
  canonical order; auth/session routes (/login, /logout, /access-denied,
  /no-access, /) deferred to U-05/U-07 as they are not module pages.
- CatalogValidator collects ALL violations before failing.
- Mirror upsert implemented as atomic delete-stale + upsert inside one unit
  of work.
- Fallback ordering tie-broken by DisplayOrder then PageId.

## Blockers

NONE.

## Known Risks

- Dapper row mapping of module_catalog_mirror against a real database is
  verified only when a test DB becomes available (roadmap integration smoke
  phase), as recorded in U-03.

## Manual Checks Pending

NONE required for U-04. (Owner review of the working tree before commit is
expected — commit/push not authorized for this execution.)

## Next Unit

U-05 — Auth + identidade interna (Supabase login + cookie bridge, per-request
identity/grant resolution via ICurrentUserAccessor, INTERNAL_USER_INACTIVE /
ACCESS_TEMPLATE_INACTIVE, bootstrap-admin CLI).

Status: NOT STARTED (per instruction; U-05 not touched in any way).

## Git Commit

NO commit created (commit/push not authorized for this execution). All U-04
changes left in the working tree for owner review.

Branch: main
HEAD: 0aa71eb985de29567397ec539f402134366754a8 (unchanged)

## Notes for Next Agent Session

- Environment: use `C:\BA-DMO-FRESH-BUILD\.dotnet-sdk\dotnet.exe` (set
  DOTNET_ROOT) and `C:\Program Files\Git\cmd\git.exe`; neither is on PATH.
- U-05 consumes: AccessResolver/EffectiveAccess (template → grants),
  ICurrentUserAccessor + CurrentUser (Domain), DbConnectionFactory /
  DapperUnitOfWork / Db helpers (U-03), internal_users/access_templates
  schema (N01). bootstrap-admin CLI verb exists as placeholder in
  BootstrapAdminCommand. Authority: 04_ACC §1, GLM-ACC-13, TD-09/TD-16,
  06_DATA §14–15, GLM-ARCH-14/15/18.
- Canonical commands: `dotnet restore`, `dotnet build`, `dotnet test` on
  `BA-DMO.sln`.

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
30fddbb573127cbc504989396650d98f735182d3

Current unit:
U-07 — Shell única + navegação derivada

Status:
COMPLETE

Completed units:
U-01, U-02, U-03, U-04, U-05, U-06, U-07

## Last Unit Summary

U-07 delivered the single application shell and grant-derived navigation
exactly per Plan-V3 (05_SHL §1–9, 04_ACC §5–6, GLM-CTR-02, GLM-CAT-02,
UD-03..UD-06/UD-14..UD-16; GLM-ACC-07 scenarios 1–12 at route level):

- Navigation derivation (Application/Shared/Access/NavigationService):
  INavigationService builds tabs from EffectiveAccess ∩ canonical catalog
  in canonical order — never in markup (GLM-SHL-01.3). Controlo is a
  functional area: visible only with authorized children, showing ONLY
  authorized children, never empty (GLM-CTR-02). Peso renders ONE entry
  whose route resolves the Operador/Responsável experience via peso.aprovar
  (GLM-ACC-05 — no manual selector). Administração is a right-aligned entry
  existing only when admin.gerir is held. Zero-grant active users keep Job On
  (UD-16). No role-name branching anywhere.
- Shell state port + web implementation: IShellService/ShellState
  (Application/Shared/Shell) + RequestShellService (Web/Shell) — per-request
  server-side resolution from the session's auth user id only; null =
  fail-closed minimal frame (GLM-ARCH-18). IdentityResolutionService memoizes
  per request (scoped) so guard/shell/authorship resolve once per request;
  re-resolution across requests preserved (GLM-ACC-08).
- Single shell frame: Pages/_ViewStart + Pages/Shared/_Layout/_Header/
  _Navigation. Header shows display name + profile_title (presentation only,
  UD-02) + logout (GLM-SHL-07). Semantic markup only — design tokens arrive
  with U-08/U-09. Auth pages (login/logout) and safe states (no-access,
  access-denied) render outside the module shell.
- Route surface with server-side guards (05_SHL §5): /jobon (jobon.view —
  global landing placeholder until U-13), /boquilhas, /peso, /peso/responsavel,
  /pegamentos, /ferramentas, /armazem, /reparacao-interna, /reparacao-externa,
  /tampoes, /historia — module-entry policies; /admin pages keep admin.gerir/
  audit.view. ModulePolicies/CapabilityPolicies are built ONLY from canonical
  ids and registered catalog-driven at the composition root (GLM-ACC-03/04).
- Peso exclusivity guards (GLM-ACC-05.2): Operador hitting /peso/responsavel
  is redirected to /peso; Responsável hitting /peso is redirected to
  /peso/responsavel — server-side, capability-driven, both directions.
- Landing and deep links: "/" redirects to the fixed global landing (Job On;
  deterministic fallback / /no-access otherwise). Unauthorized deep links are
  denied server-side (403) and /access-denied redirects safely to an area
  still authorized with fixed adequate feedback (?acesso-negado=1 renders a
  server-defined message — the flag grants nothing). No redirect loops.
- Admin pages converted into the shell layout (Admin is a module of this
  shell — GLM-SHL-01.2); "Voltar ao Job On" preserved.

## Plan-V3 Sources Used

- 10_MASTER_IMPLEMENTATION_ROADMAP.md (U-07 only)
- 05_SHELL_NAVIGATION_AND_ROUTING_SPEC.md (§1–9)
- 04_IDENTITY_ACCESS_AND_ADMIN_SPEC.md (§5 Peso separation, §6 matrix,
  §7 scenarios 1–12, §8 grant change during session)
- modules/00_MODULE_CATALOG.md (GLM-CAT-02 rules 1–4)
- modules/02_CONTROLO_SPEC.md (GLM-CTR-01..06)
- 02_DECISIONS UD-03/UD-04/UD-05/UD-06/UD-14/UD-15/UD-16, DS-01
- 09_TEST_QUALITY_AND_ACCEPTANCE_SPEC.md §4 (route-level matrix)
- Design-Reference: not needed (functional shell; tokens belong to U-08/U-09)

## Files Created/Changed

Application (created):
- src/BA.Dmo.Application/Shared/Access/NavigationService.cs
  (NavigationItem/NavigationTab/NavigationArea/ShellNavigation,
  INavigationService, NavigationService)
- src/BA.Dmo.Application/Shared/Shell/IShellService.cs (ShellState + port)

Application (changed):
- src/BA.Dmo.Application/Shared/Identity/IdentityResolutionService.cs
  (per-request memoization; behavior per request unchanged)

Web (created):
- src/BA.Dmo.Web/Authorization/ModuleAuthorizationHandler.cs
  (ModuleRequirement + handler, ModulePolicies, CapabilityPolicies)
- src/BA.Dmo.Web/Shell/RequestShellService.cs
- src/BA.Dmo.Web/Pages/_ViewStart.cshtml
- src/BA.Dmo.Web/Pages/Shared/_Layout.cshtml, _Header.cshtml, _Navigation.cshtml
- src/BA.Dmo.Web/Pages/JobOn/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Boquilhas/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Peso/Index.cshtml(.cs) and Responsavel.cshtml(.cs)
  (exclusivity guards)
- src/BA.Dmo.Web/Pages/Pegamentos/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Ferramentas/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Armazem/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/ReparacaoInterna/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/ReparacaoExterna/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Tampoes/Index.cshtml(.cs)
- src/BA.Dmo.Web/Pages/Historia/Index.cshtml(.cs)

Web (changed):
- src/BA.Dmo.Web/Program.cs (catalog-driven module/capability policies,
  ModuleAuthorizationHandler, INavigationService/IShellService DI,
  AccessResolver instance refactor)
- src/BA.Dmo.Web/Pages/Index.cshtml(.cs) ("/" → landing redirect)
- src/BA.Dmo.Web/Pages/AccessDenied.cshtml(.cs) (safe redirect + feedback)
- src/BA.Dmo.Web/Pages/_ViewImports.cshtml (shell/navigation usings)
- src/BA.Dmo.Web/Pages/Auth/Login.cshtml, Auth/Logout.cshtml, NoAccess.cshtml,
  AccessDenied.cshtml (Layout = null opt-out)
- src/BA.Dmo.Web/Pages/Admin/** (8 pages converted to the shell layout)

Tests (created):
- tests/BA.Dmo.UnitTests/Shared/Access/NavigationServiceTests.cs (10)
- tests/BA.Dmo.IntegrationTests/Access/ShellRoutingTests.cs (13)

Tests (changed):
- tests/BA.Dmo.IntegrationTests/Identity/WebAuthSessionTests.cs
  ("/" now redirects to the landing per 05_SHL §5)

## Build

Commands:
- `dotnet restore BA-DMO.sln` — PASS (all projects up-to-date)
- `dotnet build BA-DMO.sln` — PASS (0 warnings, 0 errors)

## Tests Executed

- Targeted during development: NavigationServiceTests (unit),
  ShellRoutingTests + WebAuthSessionTests + AdminWebAuthorizationTests
  (integration).
- Final gate: `dotnet test BA-DMO.sln --no-build` (full regression).

## Test Results

- BA.Dmo.UnitTests: Total 197, Passed 197, Failed 0, Skipped 0
- BA.Dmo.IntegrationTests: Total 113, Passed 113, Failed 0, Skipped 0
- Combined: Total 310, Passed 310, Failed 0
  (287 prior U-01..U-06 tests + 23 new U-07 tests, all green)

U-07 required coverage (scenarios 1–12 at route level + shell rules):
1. Boquilhas-only user: landing Job On; own route OK; every other module
   route denied; tabs = Job On + Boquilhas only ✓
2. Peso Operador: /peso OK; /peso/responsavel redirects to /peso; single
   Peso entry points at /peso ✓
3. Peso Responsável: /peso redirects to /peso/responsavel; single entry
   points at /peso/responsavel ✓
4./5./6. Controlo shows only authorized children (Pegamentos only / Peso
   only / both); never empty ✓ (unit + integration)
7. Admin without operational modules: landing Job On; /admin reachable by
   navigation ✓
8. Admin with modules: both surfaces; admin grants no implicit functional
   access ✓
9. No internal identity: /no-access safe state, no loop, modules denied ✓
10. Deep link denied → /access-denied → safe redirect to authorized area
    with fixed feedback ✓
11. Grants removed mid-session: per-request re-resolution denies the lost
    area on the next request ✓
12. Template deactivated: authenticated without access; safe state ✓
+ unauthenticated module routes → /login; zero-grant user keeps Job On
  (unit); canonical tab order; inactive template → no navigation (unit).

## Decisions Applied

- Tabs derived ONLY from EffectiveAccess ∩ canonical catalog; unauthorized
  entries never exist in the model, so they can never render (GLM-SHL-03.6).
- Route guards for module entry = module policies; /jobon entry guard =
  jobon.view per the 05_SHL §5 route table.
- Peso exclusivity implemented as server-side redirects on both routes
  (GLM-ACC-05.2), after the module-entry guard.
- Deep-link denial chain: policy 403 → /access-denied → redirect to first
  accessible page with a FIXED server-defined feedback message triggered by
  a flag (`acesso-negado`); the flag can trigger the message, never its
  content, and grants nothing (05_SHL §5 rule 1).
- "/" is a pure redirect endpoint to the U-04 first-page resolution
  (landing Job On; canonical fallback; /no-access) — replaces the U-01
  skeleton page.

## Safe Implementer Choices Made

- Shell markup is semantic only (no CSS/design tokens — they belong to
  U-08/U-09); header/nav/area use stable class + data-testid hooks.
- Module route placeholders render only the module name and a pointer to the
  module's own unit; they carry zero module logic and will be replaced by
  U-10..U-19 content.
- Auth pages and safe-state pages opt out of the shell frame (pre-shell /
  no-identity states).
- IdentityResolutionService request-scoped memoization added so multiple
  per-request consumers resolve identity once; resolution still happens
  every request (GLM-ACC-08).
- Feedback carried via fixed message + query flag instead of TempData/
  session infrastructure (stateless; no new session contract).

## Blockers

NONE.

## Known Risks

- Module placeholder pages must be replaced by their module units (U-10..
  U-19) without weakening the route guards installed here.
- Shell visual responsiveness (GLM-SHL-08 breakpoints) is pending the design
  foundation (U-08/U-09); markup hooks are in place.

## Manual Checks Pending

NONE required for U-07. (Owner review of the working tree before commit is
expected — commit/push not authorized for this execution.)

## Next Unit

U-08 — Design tokens + componentes universais (07_DESIGN §1–4: tokens,
foundation/components CSS, P1 components, laboratório page).

Status: NOT STARTED (per instruction; U-08 not touched in any way).

## Git Commit

NO commit created (commit/push not authorized for this execution). All U-07
changes left in the working tree for owner review.

Branch: main
HEAD: 30fddbb573127cbc504989396650d98f735182d3 (unchanged)

## Notes for Next Agent Session

- Environment: use `C:\BA-DMO-FRESH-BUILD\.dotnet-sdk\dotnet.exe` (set
  DOTNET_ROOT) and `C:\Program Files\Git\cmd\git.exe`; neither is on PATH.
  PowerShell 5.1 misreads BOM-less UTF-8 scripts as ANSI — never generate
  accented content through .ps1 scripts; write files through the editor.
- U-08 consumes: the shell frame hooks in Pages/Shared (_Layout/_Header/
  _Navigation classes app-*/nav-*) and wwwroot (not yet created). Authority:
  07_DESIGN_SYSTEM_AND_COMPONENT_ARCHITECTURE.md §1–4 +
  DESIGN_IMPLEMENTATION_CONTRACT; Design-Reference portal-dmo-design-final.
- U-07 shell services for later units: INavigationService/IShellService
  (Application ports), ModulePolicies/CapabilityPolicies (Web.Authorization),
  RequestShellService (Web.Shell).
- Canonical commands: `dotnet restore`, `dotnet build`, `dotnet test` on
  `BA-DMO.sln`.

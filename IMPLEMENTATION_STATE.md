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
ec79501 (U-07 commit; U-08 + U-09 changes uncommitted in the working tree)

Current unit:
U-09 — Calendar único + Shell visual

Status:
COMPLETE

Completed units:
U-01, U-02, U-03, U-04, U-05, U-06, U-07, U-08, U-09

## Last Unit Summary (U-09)

U-09 completed the shell visual and the single canonical calendar exactly
per Plan-V3 (07_DESIGN §5–6, §8; GLM-DSN-05/06) and the ACTUAL
Design-Reference (owner instruction: reference 100% for CSS AND behavior):

- Canonical calendar BEHAVIOR (wwwroot/scripts/dmo-calendar.js) — the single
  implementation consumed by every future module: Monday-first grid with
  disabled leading blanks (reference exact), month label centered between
  prev/next controls, ISO data-date buttons, has-record dot hook, selected
  state, month navigation that NEVER auto-selects (GLM-DSN-05), arrow-key
  focus roving + native Enter/Space selection, aria-pressed. Selection
  click/aria contract stays in dmo-interactions.js (reference contract);
  pages consume via dmo:date-select (reference pattern). No second calendar
  exists anywhere (guarded by tests).
- Today indicator (GLM-DSN-05 "hoje com indicador próprio"): reference CSS
  is silent, so the smallest neutral marker was added — strong brand border
  + aria-current="date" (never color alone) — documented as Plan-V3-driven.
- Shell visual completion on the U-07/U-08 frame (no derivation/auth
  change): reference-exact module tab spacing (gap 25px, tab padding 0 2px,
  3px brand underline active rule, hover brand-050 per DMO §7), reference
  contextual sidebar (gradient brand-900→950, side-head with caps title +
  muted sub, translucent cards — reference one-off values tokenized),
  sidebar narrows to 235px at ≤980px and stacks below content at ≤900px
  (reference behavior; Plan-V3 keeps header identity visible on mobile per
  GLM-SHL-08, overriding the boquilhas mockup's user-hiding).
- Laboratory consumes the LIVE canonical calendar (month nav, record dots,
  selection readout via dmo:date-select, "Mostrar todas as datas" clearing
  only the date filter) — the U-09 acceptance "calendar consumido por
  página de teste".
- U-01..U-08 behavior untouched: auth, authorization, capability checks,
  Admin in-app user management + service_role isolation, audit, Job On
  landing, navigation derivation, deep-link handling, Peso exclusivity all
  verified by the full regression.

## Plan-V3 / Design Sources Used

- 10_MASTER_IMPLEMENTATION_ROADMAP.md (U-09 only; Gate E)
- 07_DESIGN_SYSTEM_AND_COMPONENT_ARCHITECTURE.md (§5 calendar, §6 shell
  visual, §8 visual regression)
- 05_SHELL_NAVIGATION_AND_ROUTING_SPEC.md §2/§8 (frame + responsive)
Design-Reference files INSPECTED (actual assets, read-only):
- portal-dmo-design-final/dmo-design-system.css (canonical calendar CSS,
  app-header rules — source-to-source verified against dmo-components.css)
- portal-dmo-design-final/dmo-interactions.js (calendar click contract)
- portal-dmo-design-final/peso-responsavel.html (canonical calendar markup
  + behavior: head/week/grid, blanks, ISO dates, dmo:date-select consumer,
  "Mostrar todas as datas")
- portal-dmo-design-final/armazem.html (canonical calendar variant, same
  contract)
- portal-dmo-design-final/boquilhas.html (shell/header/tabs/sidebar anatomy
  + responsive behavior 1200/980/720)
- job-on-v48-folha-producao.html (legacy local calendar variant — NOT
  carried forward; canonical component only)

## Files Created/Changed (U-09; U-08 set remains uncommitted underneath)

Created:
- src/BA.Dmo.Web/wwwroot/scripts/dmo-calendar.js
- tests/BA.Dmo.IntegrationTests/Design/ShellAndCalendarGuardTests.cs (3 guards)

Changed:
- src/BA.Dmo.Web/wwwroot/styles/dmo-tokens.css (sidebar tokens: gradient,
  card bg/border, muted, compact width 235px)
- src/BA.Dmo.Web/wwwroot/styles/dmo-components.css (calendar .is-today)
- src/BA.Dmo.Web/wwwroot/styles/dmo-layout.css (sidebar reference visuals,
  responsive sidebar behavior, reference-exact tab gap/padding)
- src/BA.Dmo.Web/Pages/Shared/_Layout.cshtml (loads dmo-calendar.js)
- src/BA.Dmo.Web/Pages/DesignLaboratorio/Index.cshtml (live calendar +
  dmo:date-select consumption + reference sidebar demo)
- tests/.../Design/DesignSystemGuardTests.cs (calendar marker updated:
  days are now JS-rendered)

## Build

Commands:
- `dotnet restore BA-DMO.sln` — PASS (up-to-date)
- `dotnet build BA-DMO.sln` — PASS (0 errors, 0 warnings)

**Final verification build:** PASS at resume point.

## Tests Executed

- Targeted during development: DesignSystemGuardTests +
  ShellAndCalendarGuardTests + Admin/Web session suites.
- Final gate: `dotnet test BA-DMO.sln --no-build` (full regression).

**Final verification tests:** All 322 tests passed at resume point.

## Test Results

- BA.Dmo.UnitTests: Total 197, Passed 197, Failed 0, Skipped 0
- BA.Dmo.IntegrationTests: Total 125, Passed 125, Failed 0, Skipped 0
- Combined: Total 322, Passed 322, Failed 0
  (319 prior U-01..U-08 tests + 3 U-09 guards, all green)

U-09 guard coverage:
1. single canonical calendar: CSS only in dmo-components.css; exactly one
   behavior script (dmo-calendar.js) with the reference contract markers
   (Monday-first, aria-pressed, keyboard roving); no page re-implements
   calendar rendering ✓
2. shell composition uses the design system: load order + calendar script,
   reference header anatomy + DMO §26 attributes, derived nav with active
   indication + right-aligned Administração, breakpoints 1200/980/720 +
   reference sidebar tokens ✓
3. laboratory page consumes the canonical calendar contract (data-calendar-*
   markup, dmo:date-select listener, clear action, both scripts) ✓

## Decisions Applied

- Reference-first rule (owner instruction): calendar visuals AND behavior
  transcribed from the actual reference (dmo-design-system.css,
  dmo-interactions.js, peso-responsavel/armazem markup). The job-on-v48 and
  boquilhas local `.calendar/.day` variants are legacy mockup residuals —
  not carried forward (contract §3.4/§18; canonical component only).
- Behavior split faithful to the reference: dmo-interactions.js keeps the
  click-select/aria-pressed/date-select contract; dmo-calendar.js adds
  rendering/month-nav/keyboard. Pages consume dmo:date-select — no consumer
  re-implements selection.
- Calendar month nav never auto-selects; selection survives only inside its
  own month; "Mostrar todas as datas" clears only the date selection
  (GLM-DSN-05 + reference allDates handler).
- Today indicator: Plan-V3 requires it; reference silent → smallest neutral
  marker (strong brand border + aria-current), isolated in one CSS rule.
- Mobile header identity stays visible (GLM-SHL-08) — overrides the
  boquilhas mockup's user-hiding at 720px.
- Visual regression baseline (07_DESIGN §8): V1 allows comparable manual
  screenshots. Automated screenshot tooling was attempted once and proved
  UNAVAILABLE in this environment — recorded, not retried (owner
  call-budget instruction). Baseline capture remains a manual check below.
- Admin user management stays 100% in-app (U-06/U-08 behavior preserved):
  create/edit/template/activate/reset through the privileged server-side
  adapter; service_role never leaves the server; nothing regressed (verified
  by AdminWebAuthorizationTests + full regression).

## Safe Implementer Choices Made

- Calendar record dates are supplied by consumers through
  data-record-dates (comma-separated ISO) — the component never invents
  data; modules (U-10+) provide real dates.
- dmo-calendar.js binds idempotently (_dmoBound flag) for progressive
  enhancement safety.
- Tab font-weight stays 700 (P0-1 weight restriction) although the boquilhas
  mockup uses 650 — Plan-V3 token decision outranks mockup-local values.

## Blockers

NONE.

## Known Risks

- Contrast note (accepted by owner fidelity instruction): with the exact
  Design-Reference values, success/warning/danger text on their soft
  surfaces measures 4.01/3.38/4.12:1 at small sizes (below WCAG AA 4.5:1);
  pending/info measure 5.88:1 (pass). Recorded for a future design review —
  no change made while the 100%-fidelity instruction stands.
- Observation (not changed — outside any reported deviation):
  --dmo-text-muted (#64778a, Design-Reference fixed) measures 4.62:1 on
  white/card surfaces (pass) but ≈4.37:1 directly on the page surface;
  muted roles sit on cards in current pages.
- Visual regression baselines (desktop/tablet/mobile) are NOT yet captured:
  automated browser/screenshot tooling unavailable in this environment
  (single attempt, canceled/unavailable — not retried per owner
  instruction). 07_DESIGN §8 accepts comparable manual screenshots in V1 →
  see Manual Checks Pending.
- Design-Reference SILENT (smallest neutral behavior, isolated): account
  menu beyond the visible logout link; admin users table lacks Email/Último
  acesso columns (data contract exposes neither); audit dblclick detail card
  (no detail data beyond the row); login test-env notice + mockup credit
  line omitted; admin sub-nav includes Templates (U-06 functionality exists
  but the reference mockup has no templates view); audit filter grid uses
  an auto-fit variant (fewer fields than the mockup).

## Manual Checks Pending

- Visual regression baseline capture (07_DESIGN §8): owner/agent with
  working browser tooling should capture comparable screenshots of
  /design-laboratorio and /login at desktop (~1440), tablet (~980) and
  mobile (~720/390). Note: authenticated shell pages need a test DB to
  render (identity resolution) — the test environment of U-20 will enable
  full-shell captures; /login captures are possible today.

## Next Unit

U-13 — Job On (contexto central de produção): per the canonical roadmap
phase order (Job On precedes Controlo because Peso/Pegamentos start in the
Job On context — DS-04/DS-05). Dependencies U-07/U-08/U-09 now satisfied;
U-23 audit events arrive alongside the phase D modules as modules land.

Status: NOT STARTED (per instruction; U-13 not touched in any way).

## Git Commit

NO commit created (commit/push not authorized for this execution). All
U-08 + U-09 changes left in the working tree for owner review.

Branch: main
HEAD: ec79501 (unchanged)

## Notes for Next Agent Session

- Environment: use `C:\BA-DMO-FRESH-BUILD\.dotnet-sdk\dotnet.exe` (set
  DOTNET_ROOT) and `C:\Program Files\Git\cmd\git.exe`; neither is on PATH.
  `dotnet run` uses launchSettings (port 5051) unless overridden.
  PowerShell 5.1 misreads BOM-less UTF-8 scripts as ANSI — never generate
  accented content through .ps1 scripts. Automated browser/screenshot
  tooling was unavailable in the U-09 session — do not retry repeatedly;
  manual baseline capture is the V1 path.
- U-13 consumes: the full design system (wwwroot/styles + scripts incl.
  dmo-calendar.js), the shell frame, Design-Reference modules/05 +
  JOB_ON_DESIGN_BRIEF/VERIFICACOES/DATA_MODEL; lookup stubs for
  Ferramentas/Boquilhas/Armazém contracts are acceptable until those
  modules exist.
- Guard files tests/BA.Dmo.IntegrationTests/Design/* must stay green on
  every later unit (single design system, single calendar, token-only
  values, reference fidelity locks).
- Canonical commands: `dotnet restore`, `dotnet build`, `dotnet test` on
  `BA-DMO.sln`.

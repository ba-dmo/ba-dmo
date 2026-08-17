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
2f460f5ffe70522517f959eb01808357b5ea5aac

Current unit:
U-03 — Persistence infrastructure

Status:
COMPLETE

Completed units:
U-01, U-02, U-03

## Last Unit Summary

U-03 delivered the persistence/data-access foundation exactly per Plan-V3
(roadmap U-03: `DbConnectionFactory`, `DapperUnitOfWork`, mappings base,
política de timestamps/autoria; authority 06_DATA §1–2, §5, §8):

- Application support ports (no driver dependencies): IDbConnectionFactory,
  IDbUnitOfWork, ConcurrencyGuard (+ ConcurrencyConflictException per
  06_DATA §8), PersistenceAuthorship + IPersistenceAuthorshipAccessor
  (timestamp/authorship policy, 06_DATA §2).
- Infrastructure implementation: DbConnectionFactory (Npgsql, independent
  connection per call, no global/static state, error translation without
  credential leakage), DapperUnitOfWork (explicit commit/rollback/dispose,
  managed RunAsync form, no ambient TransactionScope), Db helpers
  (parameterized Dapper CommandDefinition + cancellation; no SQL string
  concatenation), PersistenceMappings (snake_case ↔ PascalCase convention,
  configured once in the composition root).
- Connection configuration reuses the U-02 server-side contract
  (BA_DMO_DB_CONNECTION_STRING / DATABASE_URL) via DatabaseConnectionSettings
  (single source; MigrateCommand refactored onto it, behavior unchanged).
- No EF Core, no DbUp, no ORM framework, no Supabase RPC, no module logic,
  no browser/database coupling, no live SQL anywhere.

## Plan-V3 Sources Used

- 10_MASTER_IMPLEMENTATION_ROADMAP.md (U-03 section)
- 06_DATA_BACKEND_AND_SECURITY_SPEC.md §1–2, §5, §6 (security context), §8
- 03_TARGET_MODULAR_ARCHITECTURE.md §1–4, §7 (composition root)
- 09_TEST_QUALITY_AND_ACCEPTANCE_SPEC.md §1 (unit tests without I/O)
- 02_DECISIONS §3.35 PV-01/PV-06/PV-09 (technical build contract context)

## Files Created/Changed

Application (created):
- src/BA.Dmo.Application/Shared/Persistence/IDbConnectionFactory.cs
- src/BA.Dmo.Application/Shared/Persistence/IDbUnitOfWork.cs
- src/BA.Dmo.Application/Shared/Persistence/ConcurrencyGuard.cs
- src/BA.Dmo.Application/Shared/Persistence/PersistenceAuthorship.cs

Infrastructure (created/changed):
- src/BA.Dmo.Infrastructure/Persistence/DatabaseConnectionSettings.cs
  (shared env contract + DatabaseConnectionException)
- src/BA.Dmo.Infrastructure/Persistence/DbConnectionFactory.cs
- src/BA.Dmo.Infrastructure/Persistence/DapperUnitOfWork.cs
- src/BA.Dmo.Infrastructure/Persistence/Db.cs
- src/BA.Dmo.Infrastructure/Persistence/PersistenceMappings.cs
- src/BA.Dmo.Infrastructure/BA.Dmo.Infrastructure.csproj (changed: Dapper 2.1.79)

Web (changed):
- src/BA.Dmo.Web/Cli/MigrateCommand.cs (refactored onto shared connection
  contract; behavior unchanged)
- src/BA.Dmo.Web/Program.cs (PersistenceMappings.Configure() in composition
  root; web startup otherwise unchanged)

Tests (created/changed):
- tests/BA.Dmo.UnitTests/Shared/Persistence/ConcurrencyGuardTests.cs
- tests/BA.Dmo.UnitTests/Shared/Persistence/PersistenceAuthorshipTests.cs
- tests/BA.Dmo.IntegrationTests/Persistence/FakeDbConnection.cs
- tests/BA.Dmo.IntegrationTests/Persistence/DapperUnitOfWorkTests.cs
- tests/BA.Dmo.IntegrationTests/Persistence/DbConnectionFactoryTests.cs
- tests/BA.Dmo.IntegrationTests/Persistence/PersistenceMappingsTests.cs
- tests/BA.Dmo.IntegrationTests/Persistence/PersistenceArchitectureGuardTests.cs

Migration runner: UNCHANGED (U-02 tests still green).
Business schema: UNCHANGED (no U-03 inconsistency found).

## Build

Commands:
- `dotnet restore BA-DMO.sln` — PASS
- `dotnet build BA-DMO.sln` — PASS (0 warnings, 0 errors, net10.0 all projects)

## Tests Executed

- `dotnet test BA-DMO.sln --no-build` (both test projects)
- Manual web verification: normal startup served `/` with HTTP 200 after the
  persistence bootstrap was added to the composition root.

## Test Results

- BA.Dmo.UnitTests: Total 63, Passed 63, Failed 0, Skipped 0
- BA.Dmo.IntegrationTests: Total 64, Passed 64, Failed 0, Skipped 0
- Combined: Total 127, Passed 127, Failed 0

U-03 coverage vs required areas:
1. factory creates configured Npgsql connections — DbConnectionFactoryTests ✓
2. missing configuration fails clearly — DbConnectionFactoryTests ✓
3. transaction commit path — DapperUnitOfWorkTests ✓
4. rollback on exception/failure — DapperUnitOfWorkTests ✓
5. connection/transaction disposal — DapperUnitOfWorkTests ✓
6. async/cancellation behavior — DapperUnitOfWorkTests + factory tests ✓
7. parameterized Dapper command/query behavior — Db helpers compile-time
   parameterized (CommandDefinition); live verification deferred to the
   integration smoke phase when a test DB is available (roadmap U-03) ✓/deferred
8. correct row mapping — PersistenceMappingsTests (underscore convention);
   DB row-level mapping deferred to integration smoke ✓/deferred
9. database error translation — DbConnectionFactoryTests (translated, no
   credential leakage) ✓
10. no direct browser/database coupling — guard: Web does not reference
    Npgsql; no JS/browser DB code exists ✓
11. no EF Core dependency — guard (EntityFramework/DbUp/NHibernate scan) ✓
12. U-01/U-02 regression — full suite green ✓
13. migration runner unchanged — U-02 tests green, files untouched ✓
14. normal web startup — HTTP 200 verified ✓

## Transaction Model

- Explicit DapperUnitOfWork: ONE connection + ONE transaction per scope.
- Commit only after successful completion; rollback on any failure;
  disposal without commit always rolls back.
- Managed form DapperUnitOfWork.RunAsync for deterministic boundaries.
- No ambient TransactionScope (guarded by assembly-reference test).
- No global/static connections (guarded by reflection test).
- Async disposal (IAsyncDisposable) verified in tests.

## Connection/Configuration Model

- BA_DMO_DB_CONNECTION_STRING (preferred) / DATABASE_URL fallback,
  centralized in DatabaseConnectionSettings; optional BA_DMO_MIGRATIONS_DIR
  for the migrate CLI.
- Missing configuration → explicit DatabaseConnectionException.
- Open failures translated to DatabaseConnectionException; messages never
  contain credentials.
- No secrets in the repository (test connection strings use obviously fake
  values against unreachable localhost ports).

## Decisions Applied

- Ports in Application (System.Data types only), implementations in
  Infrastructure (03_ARCH §1 separation).
- No repository interfaces beyond the generic support ports required by
  U-03; module repositories arrive with their units.
- IPersistenceAuthorshipAccessor contract only — concrete resolution belongs
  to U-05 identity; U-03 does not invent user claims.
- Dependency-graph guard reads csproj files (compilers prune unused
  references from IL, so reflection alone cannot prove the graph).

## Safe Implementer Choices Made

- Exit/exception shape: DatabaseConnectionException for configuration and
  connection failures; ConcurrencyConflictException with reload message
  (06_DATA §8 wording).
- PersistenceAuthorship rejects non-UTC timestamps at construction.
- Db helper returns IReadOnlyList<T>; QuerySingleOrDefaultAsync for optional
  single rows.
- FakeDbConnection/FakeDbTransaction doubles verify UoW semantics without a
  database (confined to tests/*).
- PersistenceMappings.Configure() idempotent with a lock.

## Blockers

NONE.

## Known Risks

- Parameterized-SQL and row-mapping behavior against a real database will be
  exercised in the integration smoke phase when a test DB is available
  (roadmap U-03 "integração smoke quando BD de teste disponível"); no such
  DB exists in this environment and no live DB may be used.

## Manual Checks Pending

NONE required for U-03. (Owner review of the working tree before commit is
expected — commit/push not authorized for this execution.)

## Next Unit

U-04 — Catálogo de módulos + espelho DB (canonical ModuleCatalog per
modules/00, module_catalog_mirror synchronization, server-side validation).

Status: NOT STARTED (per instruction; U-04 not touched in any way).

## Git Commit

NO commit created (commit/push not authorized for this execution). All U-03
changes left in the working tree for owner review.

Branch: main
HEAD: 2f460f5ffe70522517f959eb01808357b5ea5aac (unchanged)

## Notes for Next Agent Session

- Environment: use `C:\BA-DMO-FRESH-BUILD\.dotnet-sdk\dotnet.exe` (set
  DOTNET_ROOT) and `C:\Program Files\Git\cmd\git.exe`; neither is on PATH.
- U-04 scope: Application/Shared/Access canonical ModuleCatalog entries
  (modules/00 GLM-CAT-02/03), module_catalog_mirror synchronization +
  validation (normalização: duplicados, prefixos, entradas inválidas
  descartadas), acceptance "catálogo novo módulo dispensa alterações de
  navegação (verificado por teste)". Persistence is ready via
  DbConnectionFactory/DapperUnitOfWork/Db; authorship port awaits U-05.
- Canonical commands: `dotnet restore`, `dotnet build`, `dotnet test` on
  `BA-DMO.sln`.

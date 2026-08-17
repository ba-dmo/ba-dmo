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
f2245950ecd9617c1cf8868435d411dffd12e3a3

Current unit:
U-02 — Schema fresh-build (migrations N01–N12, sem execução live)

Status:
COMPLETE

Completed units:
U-01, U-02

## Last Unit Summary

U-02 delivered the fresh-build database schema contract and migration
infrastructure exactly per Plan-V3 (06_DATA §2/§3/§6/§12–13, PV-04/PV-05,
BT-08, GLM-DATA-12):

- Complete migration family `database/migrations/N01_identity.sql … N12_rls.sql`
  (12 scripts, idempotent, forward-only, whole-script execution contract):
  identity/roles/audit, catalog mirror, Boquilhas, Ferramentas (tool types
  CM/MF/BQ/PU/CS per owner decision resolving GLM-FERR-13), Job On family,
  Peso, Pegamentos, Repair, Armazém, Tampões, shared settings, RLS/least privilege.
- `schema_migrations` tracking (version PK, filename, sha256, applied_at,
  execution_time_ms — the extra field is explicitly allowed by 06_DATA §12).
- Custom Npgsql full-script runner in `Infrastructure/Persistence/Migrations`:
  deterministic discovery, SHA-256 over raw bytes, skip on same checksum,
  explicit failure on checksum mismatch, record-only-after-success, no later
  migration after failure, NO statement splitting/parsing, no EF Core, no DbUp.
- CLI `migrate` implemented (replaces the U-01 placeholder), CLI only, never
  starts the web server, exit 0 on success / non-zero on failure; explicit
  failure when connection configuration is missing (env vars
  `BA_DMO_DB_CONNECTION_STRING` or `DATABASE_URL`; optional
  `BA_DMO_MIGRATIONS_DIR`). bootstrap-admin remains a placeholder until U-05.
- Migrations ship with the Web build output (`database/migrations` copied via
  csproj Content) for the Render pre-deploy command.
- NO live SQL executed anywhere (no Supabase, no local DB).

Owner clarifications incorporated during U-02:
1. Ferramentas tool types = CM, MF, BQ, PU, CS (resolves GLM-FERR-13
   UNRESOLVED item); the Boquilhas operational module (bq_*, N03) stays
   separate from the BQ tool type — no cross-identity FKs, separate ownership.
2. Peso/Pegamentos historical attribution to the Ferramenta: guaranteed via
   the immutable `job_on_revision_id` (mandatory FK in peso_controlos and
   pegamento_controlos) whose job_on_component rows identify the tools
   (source_tool_id/source_lot_id). Bidirectional navigation verified
   (revision → tools → records, and record → revision → tools); navigation
   indexes added on job_on_revision_id in both tables. No redundant direct
   tool FKs added (owner instruction).

## Files Created/Changed

Migrations (created):
- database/migrations/N01_identity.sql (roles, append-only guard function,
  access_templates, internal_users, audit_events)
- database/migrations/N02_catalog.sql (module_catalog_mirror)
- database/migrations/N03_bq.sql (bq_lotes/traces/movements/discrepancies/
  lifecycle_history/utilisation_readings)
- database/migrations/N04_ferramentas.sql (tool_references [CM/MF/BQ/PU/CS],
  tool_lotes, physical_pieces, tool_check_rules, tool_check_occurrences)
- database/migrations/N05_jobon.sql (job_on, job_on_revision, job_on_component,
  job_on_component_field, job_on_component_row, job_on_verification_occurrence,
  job_on_audit_event, job_on_field_option)
- database/migrations/N06_peso.sql (peso_references/lotes/controlos/leituras/
  comparacao_anterior/day_approvals/settings)
- database/migrations/N07_pegamentos.sql (pegamento_controlos/medicoes)
- database/migrations/N08_reparacoes.sql (repairers, line_repairer_defaults,
  repair_exits, repair_exit_items, repair_events, internal_repair_records)
- database/migrations/N09_armazem.sql (warehouse_locations/stock/movements)
- database/migrations/N10_tampoes.sql (tampao_field_defs/field_values/
  configurations/saldos/movements/planos)
- database/migrations/N11_partilhado.sql (app_settings)
- database/migrations/N12_rls.sql (RLS on all tables, anon/authenticated
  revoked, ba_dmo_app technical CRUD + single technical policy per table,
  schema_migrations migrate-only)
- database/migrations/.gitkeep removed (family now present)

Infrastructure (created):
- src/BA.Dmo.Infrastructure/Persistence/Migrations/MigrationFile.cs
  (MigrationFile + AppliedMigration records)
- src/BA.Dmo.Infrastructure/Persistence/Migrations/MigrationExceptions.cs
- src/BA.Dmo.Infrastructure/Persistence/Migrations/MigrationChecksum.cs
- src/BA.Dmo.Infrastructure/Persistence/Migrations/MigrationDiscovery.cs
- src/BA.Dmo.Infrastructure/Persistence/Migrations/IMigrationScriptGateway.cs
- src/BA.Dmo.Infrastructure/Persistence/Migrations/MigrationRunner.cs
- src/BA.Dmo.Infrastructure/Persistence/Migrations/NpgsqlMigrationScriptGateway.cs
- src/BA.Dmo.Infrastructure/BA.Dmo.Infrastructure.csproj (changed: Npgsql 10.0.3)

Web (changed/created):
- src/BA.Dmo.Web/Cli/MigrateCommand.cs (replaced U-01 placeholder with real CLI)
- src/BA.Dmo.Web/BA.Dmo.Web.csproj (changed: ships database/migrations to output)

Tests (created/changed):
- tests/BA.Dmo.IntegrationTests/Migrations/FakeMigrationGateway.cs
- tests/BA.Dmo.IntegrationTests/Migrations/MigrationDiscoveryTests.cs
- tests/BA.Dmo.IntegrationTests/Migrations/MigrationChecksumTests.cs
- tests/BA.Dmo.IntegrationTests/Migrations/MigrationRunnerTests.cs
- tests/BA.Dmo.IntegrationTests/Migrations/MigrationArchitectureGuardTests.cs
- tests/BA.Dmo.IntegrationTests/Cli/MigrateCliTests.cs
- tests/BA.Dmo.IntegrationTests/Cli/CliCommandPlaceholderTests.cs (changed:
  now CliCommandContractTests; bootstrap-admin placeholder retained)

## Build

Commands:
- `dotnet restore BA-DMO.sln` — PASS
- `dotnet build BA-DMO.sln` — PASS (0 warnings, 0 errors, net10.0 all projects)

## Tests Executed

- `dotnet test BA-DMO.sln --no-build` (both test projects)
- Manual CLI verification: `dotnet BA.Dmo.Web.dll migrate` without connection
  config → exit 2 with explicit diagnostic; `bootstrap-admin` → exit 1
  (placeholder until U-05); web startup normal mode → HTTP 200 skeleton page.
- Verified 12 migration scripts copied to `src/BA.Dmo.Web/bin/…/database/migrations`.

## Test Results

- BA.Dmo.UnitTests: Total 55, Passed 55, Failed 0, Skipped 0 (U-01 regression green)
- BA.Dmo.IntegrationTests: Total 37, Passed 37, Failed 0, Skipped 0
- Combined: Total 92, Passed 92, Failed 0, Duration ~125 ms per project

U-02 required test coverage:
1. deterministic discovery/order — MigrationDiscoveryTests ✓
2. SHA-256 calculation — MigrationChecksumTests (FIPS vector) ✓
3. unapplied migration execution — MigrationRunnerTests ✓
4. successful migration recording — MigrationRunnerTests ✓
5. same-checksum skip — MigrationRunnerTests ✓
6. checksum mismatch failure — MigrationRunnerTests ✓
7. failed SQL not recorded — MigrationRunnerTests ✓
8. no later migrations after failure — MigrationRunnerTests ✓
9. whole-script execution — MigrationRunnerTests (byte-for-byte identity) ✓
10. no SQL splitting/parser — MigrationRunnerTests (semicolons in data intact)
    + MigrationArchitectureGuardTests ✓
11. CLI routing to migrate — CliRoutingTests (U-01, still green) ✓
12. migrate CLI non-zero on configuration/migration failure — MigrateCliTests ✓
13. web startup remains web startup — CliRoutingTests + manual HTTP 200 ✓
14. bootstrap-admin separate/not implemented — CliCommandContractTests ✓

## Decisions Applied

- Migration numbering follows the names fixed by 06_DATA §2 (N01_identity,
  N02_catalog, N03_bq, … N12_rls); remaining slots ordered by FK dependency
  (Ferramentas → Job On → Peso/Pegamentos → Repair → Armazém → Tampões →
  shared).
- schema_migrations is created by the runner itself (embedded DDL) — avoids
  the bootstrap chicken-and-egg and is not itself a tracked migration.
- RLS: RLS enabled on every table; anon/authenticated revoked (guarded for
  plain PostgreSQL); ba_dmo_app gets technical CRUD + one technical policy
  per table; NO per-user/per-module policies in V1 (GLM-DATA-06.3);
  schema_migrations has RLS but no app policy (migrate-only).
- Cross-module physical FKs only where Plan-V3 makes them mandatory
  (peso/pegamento → job_on/job_on_revision); forward references resolved
  within the same script via guarded ADD CONSTRAINT (job_on.current_revision_id,
  repair_events.internal_repair_record_id). Logical links kept as plain uuid
  where Plan-V3 keeps them denormalized (audit_events.job_on_id,
  tool_check_occurrences job_on links, internal_repair_records.job_on_id,
  job_on.article_reference_id).
- Ferramentas tool types CM/MF/BQ/PU/CS (owner decision; GLM-FERR-13 was
  UNRESOLVED in Plan-V3). Repair types stay BQ/CM/MF (06_DATA §3.7).
- Historical Ferramenta attribution for Peso/Pegamentos = immutable
  job_on_revision_id anchor (TD-18) + TD-26 Peso-lot identity; no redundant
  direct tool FKs (owner confirmation).

## Safe Implementer Choices Made

- Connection env contract: BA_DMO_DB_CONNECTION_STRING primary, DATABASE_URL
  fallback (Render convention); BA_DMO_MIGRATIONS_DIR override; missing
  config → exit 2 explicit. No connection string in any repository file.
- Exit codes: 0 success, 1 migration/connection failure, 2 configuration error.
- Migration family filename pattern enforced (`N##_<name>.sql`); discovery
  ordinal; duplicate versions rejected.
- Checksum computed over raw file bytes; lowercase hex canonical form.
- One transaction per migration (atomic success/failure).
- execution_time_ms column added to schema_migrations (explicitly allowed by
  06_DATA §12 "ex.: execution time").
- Append-only fact tables protected by a shared trigger function
  (ba_dmo_guard_append_only) created in N01.
- warehouse_stock 1:1 occupation via partial unique index (released rows kept
  as facts).
- Internal CLI overload accepts env-reader/TextWriter for deterministic tests
  without mutating real environment variables.

## Blockers

NONE.

## Known Risks

- Migration scripts are validated by review per U-02 acceptance ("validados
  por revisão"); application against a real database is only authorized in
  U-20/live phases with owner approval (06_DATA §17 verification items).
- CliCommandContractTests keeps the bootstrap-admin placeholder assertion;
  U-05 replaces it.

## Manual Checks Pending

NONE required for U-02. (Owner review of the working tree before commit is
expected — commit/push not authorized for this execution.)

## Next Unit

U-03 — Persistence infrastructure (DbConnectionFactory, DapperUnitOfWork,
mappings base, timestamp/authorship policy).

Status: NOT STARTED (per instruction; U-03 not touched in any way).

## Git Commit

NO commit created (commit/push not authorized for this execution). All U-02
changes left in the working tree for owner review.

Branch: main
HEAD: f2245950ecd9617c1cf8868435d411dffd12e3a3 (unchanged)

## Notes for Next Agent Session

- Environment: use `C:\BA-DMO-FRESH-BUILD\.dotnet-sdk\dotnet.exe` (set
  DOTNET_ROOT) and `C:\Program Files\Git\cmd\git.exe`; neither is on PATH.
- U-03 scope: Infrastructure/Persistence — DbConnectionFactory, DapperUnitOfWork,
  base mappings, timestamp/authorship policy; ports generic repositories;
  authority 06_DATA §1–2/§5/§8. No Supabase RPC. Dapper package will be
  needed (approved mechanism per GLM-DATA-01).
- Migration CLI is ready: `dotnet BA.Dmo.Web.dll migrate` (needs
  BA_DMO_DB_CONNECTION_STRING / DATABASE_URL). Live SQL only with explicit
  owner authorization.
- Canonical commands: `dotnet restore`, `dotnet build`, `dotnet test` on
  `BA-DMO.sln`.

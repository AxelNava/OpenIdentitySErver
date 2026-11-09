# Feature: Unit 1 — Production persistence and migration runner

Transform the single SQLite `ApplicationDbContext` into production persistence per the accepted ADRs: engine-selectable data projects (SQL Server + PostgreSQL, ADR 0006), separated stores (control/protocol/session/audit), a gated migration job (startup replicas never race migrations), and health/readiness wiring. SQLite remains local-development only.

## Specs

- S1 (user, verbatim): "Hoy SQL Server, pero al igual que identity server, lo vamos a hacer compatible con postgre, dependiendo de que use el que implementará esto, puede usar tanto sql server como postgres, así que puede ser librerías (proyectos) separados para que pueda elegir al inicio (antes de instalar el template)"
- S2 (user, verbatim, ADR 0007): "sería una sola celda, la provisional me sirve, backups diarios y recuperar en 4h" — RPO ≤ 24h, RTO ≤ 4h; restore drill is part of go-live acceptance.
- S3 (plan unit 1 deliverable): "Select DB engine; separate control/protocol/session/audit stores as decided; gated migration job; health/readiness."
- S4 (plan unit 1 acceptance): "Real-engine integration tests, backup/restore rehearsal, rolling-compatible migration proof, package gate. Startup replicas no longer race migrations."
- S5 (ADR 0002/0009 constraint): shared tables with tenant discriminator land with the directory work (unit 3/6); unit 1 only prepares store separation, not tenant filtering.
- S6 (plan convention): NuGet changes only via `dotnet add package`; after changes run `dotnet restore`, `dotnet list package --include-transitive`, and `dotnet list package --vulnerable --include-transitive`, archiving outputs as evidence.

## Environment constraints

- Solution: `OwnIdentity_openDict.sln`; SDK 10.0.113 (global.json).
- IdentityService currently: one `ApplicationDbContext` (Sqlite, `UseOpenIddict`), startup `MigrateAsync` + `DatabaseSeeder`, health checks at `/health/ready`.
- Docker available for real-engine integration tests (lab mssql on 14330 exists; prefer ephemeral containers on unique ports).

## Tasks

- T1 (S1,S3): Create data projects: `src/OwnIdentity.Data.Core` (entities/shared model conventions + store context base) and provider projects `src/OwnIdentity.Data.SqlServer`, `src/OwnIdentity.Data.PostgreSQL` (EF provider wiring + migrations ownership), `src/OwnIdentity.Data.Sqlite` (dev-only) — each provider owns its migrations.
- T2 (S3): Split stores: `ControlDbContext` (users/roles/admin data), `ProtocolDbContext` (OpenIddict core), `SessionDbContext` (server-side auth sessions), `AuditDbContext` (append-only audit events). IdentityService registration becomes provider-selected (config `Data:Engine` = SqlServer|PostgreSql, Sqlite fallback dev).
- T3 (S4): Gated migration job: a hosted/CLI migration runner that takes an exclusive lock (sp_getapplock / pg_advisory_lock) before migrating; replicas detect completed migrations and skip; startup never races. `Program.cs` stops migrating inline in production mode.
- T4 (S3): Health/readiness: readiness reflects migration-complete state; health checks per store/engine.
- T5 (S4): Real-engine integration tests: ephemeral docker SQL Server + PostgreSQL; run migrations + a rolling-compatible second migration; prove replicas skip.
- T6 (S2,S4): Backup/restore rehearsal scripts + evidence for both engines (RPO/RTO documentation).
- T7 (S6): Package gate: NuGet additions via CLI; archive `dotnet list package --vulnerable` outputs.
- T8: Update plan/progress docs and roadmap status.

## Log

- L1 (user, verbatim): "Vale, pues empieza a trabajar ahora si conlos primeros pasos" — start Unit 1 after roadmap approval.
- L2 (context): Existing state read 2026-11-08: single ApplicationDbContext/Sqlite/OpenIddict, startup migrate + seed (Program.cs:16-18, 127).

# ADR 0006: Database engine

Status: Accepted (2026-11-08, adjusted by operator)
Related: plan section 12.6; section 4.8/ADR 0002 (RLS dependency); section 8 (test strategy)

## Context

SQLite stays local-development only (standing plan constraint). The production engine must
satisfy: HA/backup/restore, RLS capability (feeds ADR 0002), EF migration ownership, encryption,
collation/case behavior, retention.

## Options

1. **PostgreSQL** — native RLS, strong EF Core support, simple HA ( Patroni), cost.
2. **SQL Server** — RLS + Always Encrypted; licensing cost; matches legacy IS4 lab (mssql).
3. **MySQL/MariaDB** — weaker RLS story; only if operator-standard.

## Recommendation

Option 1 (PostgreSQL) for RLS + cost, unless the operator runs SQL Server estates already
(in which case 2 keeps the migration lab representative).

## Decision (operator, 2026-11-08)

"Hoy SQL Server, pero al igual que identity server, lo vamos a hacer compatible con postgre..."
— **Multi-engine by design**: SQL Server first (matches the operator's current estate and the
lab), PostgreSQL supported in parallel. Implementation strategy:

- The data model (entities, migrations intent, policies) lives engine-agnostic in a core
  project; **separate library projects per provider** (e.g. `OwnIdentity.Data.SqlServer`,
  `OwnIdentity.Data.PostgreSQL`) own their EF provider specifics.
- The **template installer selects the engine** before deployment; the chosen provider project
  is wired in, the other is not referenced.
- SQLite remains local-development only (standing plan constraint).
- Both engines support Row-Level Security; RLS policies are provider-specific implementations
  of one shared policy definition (feeds ADR 0002).
- Collation/case behavior differs between engines (client_id, scope names): tests must pin
  behavior per engine.

## Consequences (of the decision)

- EF migrations owned by the provider projects; RLS policies versioned and tested per engine.
- Collation/case behavior (client_id, scope names) pinned in tests per engine.
- The lab's SQL Server stays representative of the primary target engine.

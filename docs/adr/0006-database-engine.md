# ADR 0006: Database engine

Status: Proposed
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

## Consequences (if adopted)

- EF migrations owned by the platform repo; RLS policies versioned and tested.
- Collation/case behavior (client_id, scope names) pinned in tests.
- The lab's SQL Server remains a legacy-source stand-in; it does not constrain this choice.

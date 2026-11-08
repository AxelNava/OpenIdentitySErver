# ADR Index — OwnIdentity identity platform modernization

Each ADR corresponds to one decision in section 12 of `docs/identity-platform-modernization-plan.md`.
Status is `Proposed` until the operator accepts or rejects the recommendation.

| ADR | Topic | Status |
| --- | --- | --- |
| [0001](0001-secret-key-provider.md) | Secret and key provider | Accepted — Vault (OSS) deferred, adapter boundary from day one |
| [0002](0002-tenant-isolation.md) | Tenant isolation | Accepted — shared tables + RLS; physical isolation only on demand |
| [0003](0003-saml-directions-and-product.md) | SAML directions and product | Accepted — no SAML product now; Entra ID federation is OIDC (section 4.4) |
| [0004](0004-compliance-target.md) | Compliance target (FAPI profile) | Accepted — FAPI 2.0 Final |
| [0005](0005-migration-downtime-coexistence.md) | Migration downtime/coexistence | Accepted — new issuer, blue/green (reauthentication tolerable) |
| [0006](0006-database-engine.md) | Database engine | Accepted — multi-engine SQL Server + PostgreSQL, selectable at install |
| [0007](0007-deployment-topology.md) | Deployment topology | Accepted — single cell (shared-state ready), RPO 24h / RTO 4h |
| [0008](0008-bff-commercial-boundary.md) | BFF commercial boundary | Accepted — OSS YARP + Apache ATM |
| [0009](0009-user-namespace.md) | User namespace | Accepted — tenant-local users (overrides recommendation) |
| [0010](0010-audit-and-privacy.md) | Audit and privacy | Accepted — in-platform append-only, SIEM export path, 7-year retention |

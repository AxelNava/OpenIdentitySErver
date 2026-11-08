# ADR 0002: Tenant isolation

Status: Accepted (2026-11-08)
Related: plan section 12.2; section 4.8 (multi-issuer and multi-tenancy); ADR 0009 (tenant-local users)

## Context

OpenIddict entities are extensible; the platform must decide how client/tenant data is isolated:
per-tenant database, per-tenant schema, or shared tables with tenant-aware entities plus RLS.

## Options

1. **Database per tenant** — strongest blast-radius isolation; highest operational cost at scale.
2. **Schema per tenant** — middle ground; migration fan-out per schema.
3. **Shared tables + tenant column + RLS + tenant-aware OpenIddict entities** — cheapest at
   scale; depends on engine RLS support (ADR 0006); client-ID uniqueness strategy required.

## Recommendation

Option 3 (shared tables + RLS) unless a named tenant contractually requires physical
isolation, in which case 1 for those tenants only (hybrid).

## Decision (operator, 2026-11-08)

"Si, que sea así hasta que alguien pida aislamiento físico" — **Option 3 accepted**: shared
tables with a tenant discriminator + RLS, per ADR 0006's per-engine RLS implementations and
ADR 0009's tenant-local users. Physical (database-per-tenant) isolation is added **only when a
specific tenant contractually requires it**, as a documented exception.

## Consequences (of the decision)

- Custom OpenIddict entities carry a tenant discriminator; every query path is tenant-scoped.
- Client-ID uniqueness decided up front (global vs per-tenant-prefixed).
- RLS policies become part of migration ownership and of the test matrix.

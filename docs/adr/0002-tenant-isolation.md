# ADR 0002: Tenant isolation

Status: Proposed
Related: plan section 12.2; section 4.8 (multi-issuer and multi-tenancy)

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

## Consequences (if adopted)

- Custom OpenIddict entities carry a tenant discriminator; every query path is tenant-scoped.
- Client-ID uniqueness decided up front (global vs per-tenant-prefixed).
- RLS policies become part of migration ownership and of the test matrix.

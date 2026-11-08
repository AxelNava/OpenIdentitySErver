# ADR 0009: User namespace

Status: Accepted (2026-11-08, adjusted by operator — overrides recommendation)
Related: plan section 12.9; section 4.5 (users, roles, claims, lifecycle)

## Context

Decide whether a person is a global subject with tenant memberships or tenant-local users:
email uniqueness, subject stability, account linking, cross-tenant SSO policy.

## Options

1. **Global person + tenant memberships** — one subject per human; linking/matching logic;
   simpler cross-tenant SSO.
2. **Tenant-local users** — subject per tenant; no global identity; harder linking, stronger
   isolation defaults.

## Recommendation

Option 1 (global person with memberships), with subject stability guaranteed by an immutable
internal ID; email uniqueness global, tenant roles per membership.

## Decision (operator, 2026-11-08)

"Usuarios por tenant" — **Option 2 accepted, overriding the recommendation**: users are
**tenant-local**. Consequences of the override:

- One subject per tenant; the same human logging into two tenants is two rows with two subjects.
- Email/user-name uniqueness is **per tenant**, not global.
- Account linking and cross-tenant SSO are **not platform features initially**; enabling them
  later (e.g. a global directory layer) is a new product decision with its own migration of the
  user namespace.
- Tenant context is established at authentication time (tenant-aware login), not inferred after.
- Pairs with ADR 0002: shared tables + tenant discriminator/RLS; `Users`/`Roles` rows carry the
  tenant scope, so isolation is enforced in data, not by separate databases by default.

## Consequences (of the decision)

- Tenant-local subject stability: each tenant's user rows are independent; subject IDs never
  cross tenants.
- Password reset/flows are tenant-scoped (no global account recovery without a tenant context).
- RLS tenancy (ADR 0002) filters users directly, not memberships.

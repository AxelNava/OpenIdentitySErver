# ADR 0009: User namespace

Status: Proposed
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

## Consequences (if adopted)

- Account linking (email, verified contact) becomes a lifecycle feature with its own tests.
- Cross-tenant SSO follows session policy from plan section 4.2.
- RLS tenancy (ADR 0002) filters memberships, not users.

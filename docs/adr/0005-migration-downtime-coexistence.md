# ADR 0005: Migration downtime/coexistence

Status: Proposed
Related: plan section 12.5; section 5 (IdentityServer4 to OpenIddict migration)

## Context

Moving from IdentityServer4 (Skoruba Admin 1.0.0-beta7 legacy lab as source baseline) to the
OpenIddict target must define issuer identity and acceptable downtime/coexistence.

## Options

1. **New issuer, blue/green** — clean config; users reauthenticate; old grants drained
   progressively; rollback = re-point edge.
2. **Same issuer, side-by-side migration** — shared signing key material; grant/configuration
   write freeze during cutover; reverse journal for rollback.
3. **Same issuer, long coexistence** — both stacks live, traffic split; higher running cost and
   drift risk.

## Recommendation

Option 2 (same issuer, side-by-side with write freeze) when user reauthentication on cutover is
unacceptable; option 1 when it is acceptable (simpler, safer). Decide after sizing the active
grant population from the legacy database inventory.

## Consequences (if adopted)

- A reverse journal and rollback-duration budget are mandatory pre-cutover artifacts.
- DNS/edge ownership named per ADR 0007.
- Key material continuity (same issuer) implies shared JWKS strategy with ADR 0001.

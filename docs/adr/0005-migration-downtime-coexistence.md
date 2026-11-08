# ADR 0005: Migration downtime/coexistence

Status: Accepted (2026-11-08, adjusted by operator)
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

## Decision (operator, 2026-11-08)

"Si es tolerable re autenticar" — user reauthentication is acceptable → **Option 1: new issuer,
blue/green**. Target runs under a new issuer with clean configuration; the legacy authority
stays side-by-side only to redeem/drain its own grants; rollback is re-pointing the edge.

## Consequences (of the decision)

- APIs temporarily trust an explicit allowlist of old and new issuers/audiences (plan section 5.6).
- No shared signing-key continuity work is needed (unlike same-issuer strategies).
- Cutover acceptance: zero unresolved ROPC dependencies, then maximum old grant/token/session
  lifetimes, contractual rollback period, and audit retention gates.
- The reverse-journal requirement of same-issuer strategies does not apply; database snapshots
  remain sufficient until directory writes move (plan section 5 rollback boundaries).

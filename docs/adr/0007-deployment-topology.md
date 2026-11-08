# ADR 0007: Deployment topology

Status: Proposed
Related: plan section 12.7; sections 3 (trust boundaries), 9 (observability)

## Context

Topology must fix: issuer cells/replicas/regions, trusted edge and mTLS termination, BFF and
control-plane placement, bridge isolation, session/cache/pub-sub, data residency, RPO/RTO,
active-active constraints.

## Options

1. **Single-region, active-passive DR** — simplest; RPO/RTO bounded by backup/restore.
2. **Single-region active-active + DR region** — needed when session state is shared
   (cache/pub-sub) and mTLS terminates at a trusted edge.
3. **Multi-region active-active** — data residency and latency driver; highest complexity.

## Recommendation

Option 2 as target (cells behind a trusted edge, per-cell state stores); start with a
single cell and grow. mTLS termination at the edge, issuer cells stateless.

## Consequences (if adopted)

- Session/cache/pub-sub become required platform services (cell-scoped).
- Bridge (SAML) runs isolated from issuer cells.
- RPO/RTO and residency recorded here; couples with ADRs 0001 and 0005.

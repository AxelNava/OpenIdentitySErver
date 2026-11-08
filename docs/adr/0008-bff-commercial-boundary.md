# ADR 0008: BFF commercial boundary

Status: Proposed
Related: plan section 12.8; section 4.9 (Vue administration BFF with YARP)

## Context

The Vue admin's BFF can be OSS (YARP + Apache Access Token Management + custom integration) or
licensed (Duende BFF/YARP). Support expectations and redistribution terms differ.

## Options

1. **OSS YARP + Apache Access Token Management** (Apache-2.0) + custom glue.
2. **Duende BFF/YARP** (commercial license) — integrated token management and antiforgery.

## Recommendation

Option 1 unless the operator values Duende's support contract; OSS stack already fits the plan's
own-repo policy (`src/ownidentity-admin/bunfig.toml` lifecycle-script policy stands independent
of this choice).

## Consequences (if adopted)

- Custom glue code owns CSRF, session refresh concurrency, and proxy allowlisting (plan
  section 6 test matrix).
- License and redistribution terms recorded here for future audit.

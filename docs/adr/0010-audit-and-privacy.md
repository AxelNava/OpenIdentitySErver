# ADR 0010: Audit and privacy

Status: Proposed
Related: plan section 12.10; section 9 (observability, SLOs, audit); section 7 (threat model)

## Context

Audit trail requirements: retention, immutable sink, SIEM integration, PII classification,
regional deletion/legal hold, operator access control, evidence export.

## Options

1. **Append-only DB sink + SIEM forwarder** — retained in-platform, exported on demand.
2. **External immutable store (object lock/WORM) as primary sink** — strongest legal evidence,
   added moving part.
3. **SIEM-primary** — depends entirely on the operator's SIEM estate.

## Recommendation

Option 1 initially, with PII classification and redaction rules from the extractor boundary
(no secrets/PII in manifests), plus a defined export path so migrating to 2/3 later is
mechanical.

## Consequences (if adopted)

- Retention periods and legal-hold workflow need operator values before go-live.
- Operator access to audit data is role-gated and itself audited.
- Deletion requests honor regional rules recorded here.

# ADR 0003: SAML directions and product

Status: Proposed
Related: plan section 12.3; section 4.3 (SAML 2.0 bridging)

## Context

SAML bridging has a direction problem: inbound SAML (partners as IdPs into our OP) and outbound
SAML (we act as IdP for legacy SPs) have very different cost. Buying/building outbound
capability when only inbound is required is waste.

## Options

1. **Inbound only, Sustainsys.Saml2** (Apache-2.0, actively maintained, per-scheme IdP config)
   as the bridge for partner IdPs.
2. **Outbound only or both, ITfoxtec Identity Saml2** (BSD-3-Clause) acting as IdP for SPs.
3. **Commercial SAML gateway** (e.g., a dedicated bridge product) with support contract.

## Recommendation

Start with option 1 (inbound) if no outbound SP is named today; record each partner/SP with
its SLO requirements before choosing 2 or 3.

## Consequences (if adopted)

- One authentication scheme per inbound IdP per Sustainsys guidance.
- SLO semantics (IdP-initiated logout) documented per partner before onboarding.
- Library license/support recorded in this ADR.

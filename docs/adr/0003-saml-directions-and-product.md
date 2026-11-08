# ADR 0003: SAML directions and product

Status: Accepted (2026-11-08, adjusted by operator — no SAML product now)
Related: plan section 12.3; section 4.3 (SAML 2.0 bridging); section 4.4 (database-driven external authentication providers)

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

## Decision (operator, 2026-11-08)

"Por ahora no conozco" — **no SAML product is selected or built now**. Clarification the
operator raised: partners authenticating with their **Microsoft / Entra ID (Azure AD) account**
at our login page is **OIDC federation, not SAML** — we act as the OIDC relying party of their
IdP, receive their id/access tokens, authenticate the user locally, and issue our own tokens.
That flow belongs to section 4.4 (database-driven external authentication providers), not to
this ADR. This ADR activates only when a partner IdP or SP actually speaks SAML.

If/when the first SAML partner appears: inbound-only bridge (Sustainsys.Saml2, option 1) is the
pre-selected default; each partner's SLO requirements are recorded before onboarding.

## Consequences (of the decision)

- One authentication scheme per inbound IdP per Sustainsys guidance.
- SLO semantics (IdP-initiated logout) documented per partner before onboarding.
- Library license/support recorded in this ADR.

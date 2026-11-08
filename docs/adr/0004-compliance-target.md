# ADR 0004: Compliance target

Status: Proposed
Related: plan section 12.4; section 4.6 (financial-grade security and FAPI)

## Context

The plan pins mTLS as the sender constraint and excludes DPoP. The remaining choice is the
certification profile: FAPI 2.0 Final, FAPI 1.0 Advanced, or a named ecosystem profile.

## Options

1. **FAPI 2.0 Security Profile Final** (2025) — current standard; simpler than 1.0 Advanced;
   requires PAR, PKCE, mTLS; OpenIddict 7.6.0 configuration boundary already verified against it.
2. **FAPI 1.0 Advanced** — older; required only by specific ecosystems/regulators.
3. **Named ecosystem profile** (e.g., UK Open Banking, BR Open Banking) — pick only when a
   regulator mandates it.

## Recommendation

Option 1, unless a regulator requires 2 or 3. Certification evidence effort is deferred until
operator/regulator demand exists.

## Consequences (if adopted)

- Conformance suite runs become the acceptance bar for flow policy changes (plan section 8).
- Message signing / response-mode requirements come from the profile, not ad hoc.
- DPoP remains out by standing decision; reopening needs a new product decision (plan section 11).

# ADR 0001: Secret and key provider

Status: Proposed
Related: plan section 12.1; sections 4.6 (FAPI/mTLS), 4.7 (signing keys and rotation)

## Context

The target platform needs signing keys (JWT/IdP), token-encryption keys, client secrets, and
mTLS certificates, with rotation, audit, and (per FAPI) non-exportable private keys. Options:
Azure Key Vault / Managed HSM, AWS KMS / Secrets Manager, HashiCorp Vault, on-prem HSM, or
software keys managed by the app.

## Options

1. **Azure Key Vault (+ Managed HSM for FAPI tiers)** — managed, hardware-backed, workload
   identity, good .NET SDK and KeyVault signing adapter; regional DR via key replication; cost
   per operation.
2. **AWS KMS / Secrets Manager** — equivalent AWS-native story.
3. **HashiCorp Vault** — cloud-neutral, transit engine for remote signing; self-operated.
4. **On-prem HSM** — strongest isolation, highest operational cost.
5. **Software keys in app storage** — simplest, fails the non-exportability expectation.

## Recommendation

Option 1 if the operator already runs Azure; otherwise 3. Decide with the deployment
topology (ADR 0007) and compliance target (ADR 0004) in the same batch.

## Consequences (if adopted)

- Key material never touches app configuration; remote-signing adapter in front of OpenIddict.
- Rotation/emergency-revocation runbook implemented against the provider API.
- DR/region choice couples to ADR 0007.

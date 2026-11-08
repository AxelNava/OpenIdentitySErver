# ADR 0001: Secret and key provider

Status: Accepted (2026-11-08, adjusted by operator)
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

## Decision (operator, 2026-11-08)

"De momento no opero ninguna nube... lo podemos integrar después pero con la arquitectura
preparada, para tener compatibilidad con y sin el vault."

- **HashiCorp Vault (OSS)** is the target provider, integration **deferred** — not required at start.
- The platform must ship a **key/secret adapter boundary** from day one: an internal interface
  through which signing/encryption credentials and client secrets are obtained. Two
  implementations: (1) software/file-based keys (default, no vault), (2) Vault-backed. Swapping
  one for the other must not touch protocol code.
- Non-exportability guarantees start only when Vault is integrated; until then keys are
  software-held and the risk is recorded in the threat model.

## Consequences (of the decision)

- Key material is accessed only through the adapter interface; never directly from app configuration.
- Rotation/emergency-revocation runbook implemented against the adapter (software mode first,
  Vault mode later).
- Vault integration becomes a bounded future work unit with its own security review.

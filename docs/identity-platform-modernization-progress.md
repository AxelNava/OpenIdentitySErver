# Identity platform modernization progress

This ledger is the durable resume point for implementation of the [identity platform modernization plan](identity-platform-modernization-plan.md). Unit 0 remains **IN PROGRESS**. Units 0A, 0B, and 0C are **COMPLETE** within their bounded contracts; no unit proves a real source deployment, migration readiness, data application, or full ADR closure.

## Current status

| Scope | Status | Evidence |
| --- | --- | --- |
| Unit 0: Legacy inventory and ADR closure | **IN PROGRESS** | The validation contract exists, but real source evidence, owner assignments, and ADR decisions remain outstanding. |
| Unit 0A: Legacy inventory manifest validator and ROPC blocker gate | **COMPLETE** | Offline CLI, typed manifest validation, samples, focused tests, and runtime checks pass. |
| Unit 0B: User-initiated stateless IdentityServer4 manifest preflight | **COMPLETE** | Authorized API boundary, sanitized Vue upload journey, bounded safe response, extraction to package-free Core, tests, build, CLI regression, and unauthenticated runtime policy smoke pass. |
| Unit 0C: Source-side SQL Server/IdentityServer4 EF 4.1 extractor | **COMPLETE** | Independent operator CLI, exact schema fingerprint extended to the Skoruba Admin 1.0.0-beta7 variant (quarantined extension evidence), stdin-only credential boundary, fixed read-only metadata projections, sanitized schema-v1 output, and behavior tests pass. Live SQL Server execution proven against the docker Skoruba beta7 lab (TLS-validated, stdin-only credential); production live execution remains an operator run. |

## Plan alignment

- Section 2, baseline-to-target gaps: provides a safe contract for evidence that the repository cannot discover itself.
- Section 5, side-by-side strategy step 1: classifies clients and records dispositions without reading a legacy database.
- Section 5, side-by-side strategy steps 1 and 3: adds the first bounded direct source projection without target runtime coupling or ETL/apply behavior.
- Section 10, Work Unit 0: enforces disposition, ROPC remediation ownership, and redacted evidence rules.
- Section 12, ADR ownership: requires an owner for each ADR still marked `unresolved` without selecting or closing a decision.

## Unit 0A boundary

**In scope:** validate one local JSON manifest; require explicit dispositions for declared legacy tables, custom extensions, credentials, and keys; require client migration classifications; quarantine custom grants; enforce owned non-ROPC remediation for ROPC blockers; require owners for unresolved ADRs; reject unknown or secret-bearing fields; emit deterministic redacted counts and diagnostics.

**Explicit non-goals:** connect to a database or service; collect real legacy inventory; validate source row truth; migrate or mutate data; activate any OAuth grant; choose database, topology, key provider, SAML product, compliance target, BFF boundary, tenancy, namespace, audit, or coexistence decisions; close an ADR; claim Unit 0 complete.

## Delivered behavior

| Area | Delivered |
| --- | --- |
| CLI | `OwnIdentity.LegacyInventory` accepts exactly one JSON path and performs offline read-only validation. Exit codes are `0` for a complete contract, `2` for validation failures, and `1` for usage, path, I/O, or malformed JSON failures. |
| Evidence contract | Typed sections cover legacy tables, custom extensions, credential metadata, signing/encryption key metadata, clients, custom grants, and ADRs. Every inventory section must be present, including empty arrays. |
| ROPC gate | Password-grant clients can exist only as `ropc-blocker` evidence. They require an owner and one plan-defined non-ROPC migration target. The schema has no activation or exception field. A complete ROPC record remains visible as `cutoverBlocked: True`. |
| Quarantine and ownership | Custom grants require `quarantine`; unresolved ADRs require an owner. |
| Redaction | Diagnostics contain codes and structural JSON paths, never field values. Unknown fields and suspicious password, client-secret, private-key, token, code, assertion, and key-material fields fail validation. |
| Samples and tests | Safe representative complete and missing-ROPC-remediation manifests plus eleven behavior-focused xUnit tests. Samples contain metadata only, not usable credentials or key material. |

## Verification evidence

Executed from the repository root on 2026-08-06 after formatting/normalization:

| Command | Exact outcome |
| --- | --- |
| `dotnet restore "OwnIdentity_openDict.sln"` | Exit `0`; tool and test projects restored, with the other two projects already up to date. |
| `dotnet build "OwnIdentity_openDict.sln" --no-restore` | Exit `0`; build succeeded with `0` warnings and `0` errors. |
| `dotnet test "tests/OwnIdentity.IdentityService.Tests/OwnIdentity.IdentityService.Tests.csproj" --no-restore --filter "FullyQualifiedName~LegacyInventory"` | Exit `0`; `11` passed, `0` failed, `0` skipped. |
| `dotnet run --project "tools/OwnIdentity.LegacyInventory/OwnIdentity.LegacyInventory.csproj" --no-build -- "tools/OwnIdentity.LegacyInventory/Samples/complete.inventory.json"` | Exit `0`; `status: VALID`, `ropcBlockers: 1`, `cutoverBlocked: True`, `diagnostics: 0`. |
| `dotnet run --project "tools/OwnIdentity.LegacyInventory/OwnIdentity.LegacyInventory.csproj" --no-build -- "tools/OwnIdentity.LegacyInventory/Samples/ropc-without-remediation.inventory.json"` | Exit `2`; stable diagnostics `LIV122 $.clients[0].owner` and `LIV123 $.clients[0].migrationTarget`; no field values emitted. |
| Sensitive-literal and stale-claim scan over Unit 0A files | Passed; only schema names, synthetic metadata, redaction tests, and explicit non-completion statements matched review terms. No usable secret, private key, password, token, or claim that Unit 0 is complete was found. |

## Runtime result

The runtime boundary is the offline CLI, not Aspire or an identity service. Both representative CLI scenarios executed successfully as recorded above. Aspire is **N/A** because Unit 0A has no network, database, service, schema, protocol, or persistent-data integration.

## Rollback boundary

Unit 0A is now a prerequisite of Unit 0B and cannot be rolled back while Unit 0B remains. First apply the Unit 0B rollback below, which moves `LegacyInventoryManifest.cs` and `LegacyInventoryValidator.cs` from Core back into the CLI and removes the Core project and its CLI, IdentityService, and test references. Then delete `tools/OwnIdentity.LegacyInventory`, including the restored validator files and its two samples, `tests/OwnIdentity.IdentityService.Tests/LegacyInventoryValidatorTests.cs`, and this ledger; remove the tool project from `OwnIdentity_openDict.sln`; remove the test-to-tool project reference; and remove the progress link from the plan. No schema, runtime protocol, frontend dependency, service data, or persistent data requires rollback.

## Deferred blockers

- The real IdentityServer4 schema, versions, stores, extensions, clients, grants, credential metadata, key sources, issuer, deployment, validators, and volumes have not been inventoried.
- The ten section 12 ADRs remain unresolved until accountable product/operator owners provide evidence and decisions.
- Every real ROPC dependency remains unknown. Any discovered dependency blocks cutover until eliminated; documenting an owner and target does not make ROPC executable or migration-ready.
- Product, topology, compliance, tenancy, persistence, BFF, SAML, key custody, migration downtime, user namespace, and audit/privacy decisions remain deferred.

## Next resume point

Run Unit 0C against an authorized disposable or sanitized SQL Server database created from the canonical IdentityServer4 EF 4.1 configuration migration, using a short-lived read-only principal and protected output path. Then preflight the emitted manifest through Unit 0B and assign real owners/targets to any ROPC blocker. Authenticated manual browser smoke for Unit 0B remains separate. Do not add durable jobs, target ETL/apply behavior, or claim Unit 0 complete until database, audit/privacy, remaining source inventory, reconciliation, and rollback scopes have their own decisions and acceptance evidence.

## Unit 0B boundary

**In scope:** an authorized administrator explicitly opens `/migrations/identityserver4`, selects one `.json` file up to 1 MiB, and sends the original file as an `application/json` body to a stateless preflight endpoint. The endpoint reuses the schema-v1 validator, returns only redacted counts, cutover-blocked state, and at most 100 allowlisted safe diagnostics, and stores nothing.

**Explicit non-goals:** source database connection or credentials; filename upload; automatic startup migration; durable run/history/progress/cancel/resume; ETL, apply, reconciliation, or rollback execution; Identity/OpenIddict row creation; grant/token/private-key/client-secret handling; credential re-registration; DPoP; ROPC activation or exception; source-truth or migration-readiness claims; Unit 0 completion.

## Unit 0B delivered behavior

| Area | Delivered |
| --- | --- |
| Dependency boundary | `OwnIdentity.LegacyInventory.Core` is a package-free class library containing the single typed manifest and validator implementation. The CLI, IdentityService, and tests reference Core; production does not reference the executable project. |
| HTTP contract | `POST /api/admin/migrations/identityserver4/preflight` requires `IdentityAdministration`, accepts only UTF-8 `application/json`, reads at most 1,048,576 bytes independently of `Content-Length`, returns 415/413 as applicable, and sets `Cache-Control: no-store` even when authorization short-circuits. |
| Safe result | Semantic and JSON preflights return 200. Malformed JSON and invalid UTF-8 return only `JSON001` with zero counts. Diagnostics use allowlisted paths/messages, are capped at 100, and never return manifest values. ROPC remains a cutover blocker with no activation or exception path. |
| Vue journey | Navigation and route expose **Migrate from IdentityServer4**. The view has initial, selected, local rejection, validating/live, valid, ROPC-blocked, invalid, retryable transport-error, and replacement states. It never parses or previews content; reload retains nothing. |
| Persistence boundary | No schema, database row, uploaded file, preflight result, job, target configuration, or source-system state is created by the work unit. |

## Unit 0B files

- `src/OwnIdentity.LegacyInventory.Core/*` - extracted package-free typed manifest and validator.
- `tools/OwnIdentity.LegacyInventory/OwnIdentity.LegacyInventory.csproj` - CLI-to-Core reference; CLI behavior and samples remain unchanged.
- `src/OwnIdentity.IdentityService/Controllers/Admin/IdentityServer4MigrationPreflightController.cs` - bounded raw-body endpoint and safe response mapping.
- `src/OwnIdentity.IdentityService/Program.cs` - endpoint-specific `no-store` header before authorization.
- `tests/OwnIdentity.IdentityService.Tests/{LegacyInventoryValidatorTests,IdentityServer4MigrationPreflightTests}.cs` - retained validator/CLI coverage and endpoint boundary coverage.
- `src/ownidentity-admin/src/{api/client.ts,router/index.ts,components/AdminShell.vue,views/IdentityServer4MigrationView.vue,styles.css}` - File-as-body API, route/navigation, responsive UI, and states.
- `src/ownidentity-admin/src/__tests__/{apiClient,IdentityServer4MigrationView}.spec.ts` - API-body and UI behavior coverage.
- `OwnIdentity_openDict.sln` and the three referencing `.csproj` files - Core project registration and dependency direction.
- `docs/identity-platform-modernization-{plan,progress}.md` - greenfield side-by-side decision and bounded evidence.

## Unit 0B verification evidence

Executed from the repository root on 2026-08-06 after `dotnet format "OwnIdentity_openDict.sln" --no-restore` and `bun run --cwd "src/ownidentity-admin" lint`. The .NET formatter exited `0` with its generic workspace-load warning; both frontend fixers exited `0`.

| Command | Exact outcome |
| --- | --- |
| `dotnet restore "OwnIdentity_openDict.sln"` | Exit `0`; all five projects up to date. |
| `dotnet build "OwnIdentity_openDict.sln" --no-restore` | Exit `0`; build succeeded with `0` warnings and `0` errors. |
| `dotnet test "tests/OwnIdentity.IdentityService.Tests/OwnIdentity.IdentityService.Tests.csproj" --no-restore --filter "FullyQualifiedName~LegacyInventory|FullyQualifiedName~IdentityServer4MigrationPreflight"` | Exit `0`; `21` passed, `0` failed, `0` skipped. |
| `dotnet test "tests/OwnIdentity.IdentityService.Tests/OwnIdentity.IdentityService.Tests.csproj" --no-restore` | Exit `0`; `25` passed, `0` failed, `0` skipped. |
| `bun run --cwd "src/ownidentity-admin" test:unit --run "src/__tests__/apiClient.spec.ts" "src/__tests__/IdentityServer4MigrationView.spec.ts"` | Exit `0`; `2` files and `12` tests passed. |
| `bun run --cwd "src/ownidentity-admin" test:unit --run` | Exit `0`; full suite `2` files and `12` tests passed. |
| `bun run --cwd "src/ownidentity-admin" type-check` | Exit `0`; `vue-tsc --build` completed without diagnostics. |
| `bun run --cwd "src/ownidentity-admin" build` | Exit `0`; type-check and Vite production build completed, including the lazy migration view bundle. |
| `dotnet run --project "tools/OwnIdentity.LegacyInventory/OwnIdentity.LegacyInventory.csproj" --no-build -- "tools/OwnIdentity.LegacyInventory/Samples/complete.inventory.json"` | Exit `0`; valid exact counts `2/1/1/2/2/1/2/1`, one ROPC blocker, cutover blocked, zero diagnostics. |
| `dotnet run --project "tools/OwnIdentity.LegacyInventory/OwnIdentity.LegacyInventory.csproj" --no-build -- "tools/OwnIdentity.LegacyInventory/Samples/ropc-without-remediation.inventory.json"` | Expected exit `2`; only `LIV122` and `LIV123`, one ROPC blocker, cutover blocked. |
| Changed/new-file safety scans | One validator/manifest implementation exists only in Core; production has no executable-project reference; no source persistence API, source connection, uploaded artifact, SDD artifact, lifecycle exception, or leaked sentinel was found. Test-only synthetic sentinels remain solely in negative assertions. Direct-table statements are prohibitions. |

## Unit 0B runtime result

A PowerShell 7 harness launched the real built IdentityService DLL at `http://127.0.0.1:5187` with a uniquely named temporary SQLite database outside the workspace and an ephemeral generated Development bootstrap identity. After `/health/ready` returned 200, an unauthenticated `POST` of `{}` with `Content-Type: application/json` to the real preflight route returned `401` through the OpenIddict validation challenge and included `Cache-Control: no-store`. The process was stopped in `finally`; the temporary database and sidecars were deleted, and a follow-up glob found no artifacts.

Authenticated administrator execution and browser automation were not fabricated and remain unverified manually. Controller, policy metadata, raw-body, redaction, size, media-type, and Vue interaction behavior are covered by the automated tests above.

## Unit 0B rollback boundary

Remove the endpoint, endpoint-specific `Program.cs` header middleware, Vue page/route/navigation/API contracts/tests/styles, endpoint tests, and this Unit 0B ledger section. Move `LegacyInventoryManifest.cs` and `LegacyInventoryValidator.cs` back into the CLI, remove the Core references/project from the three projects and solution, and remove the Unit 0B product-direction additions from the plan. No schema, database row, uploaded file, migration job, target configuration, or source-system state requires rollback.

## Unit 0B deferred decisions

- At Unit 0B completion, source connector placement, credential custody, approved network path, schema/version coverage, and extraction contract were unresolved. Unit 0C resolves only the operator-run SQL Server/IdentityServer4 EF 4.1 configuration-store slice; other engines, schema variants, source domains, and server-side execution remain unimplemented.
- Durable migration-run ownership, audit, concurrency, progress, cancellation, resume, reconciliation, apply, and rollback execution remain future work.
- Identity row/hash compatibility, protocol configuration transformation, credential re-registration, active-grant draining, key continuity, and cutover topology still require separate rehearsals and decisions.
- Every discovered ROPC dependency remains a hard blocker. Unit 0B adds no execution, activation, approval, expiry, or exception path.

## Unit 0C boundary

**In scope:** an operator runs `OwnIdentity.IdentityServer4Extractor` beside or network-adjacent to a dedicated SQL Server configuration database matching the canonical `IdentityServer4.EntityFramework` 4.1 schema. A short-lived connection string arrives only through redirected standard input. The CLI validates exact schema/migration evidence, reads fixed non-secret configuration metadata, and emits one deterministic schema-v1 manifest to standard output.

**Explicit non-goals:** IdentityService or Vue source connectivity; argv, environment, file, appsettings, or interactive credential input; credential persistence; broad database discovery; shared databases; custom schemas/table names; Skoruba/custom extensions; other engines/IdentityServer versions; ASP.NET Identity users; redirect URIs, claims, property values, or resource/scope names; secret values; certificate/key material; persisted grants/device codes/tokens; durable jobs/history/audit; target transform/load/reconciliation/apply; source or target writes; source-truth, migration-readiness, or Unit 0 completion claims.

## Unit 0C delivered behavior

| Area | Delivered |
| --- | --- |
| Process boundary | `tools/OwnIdentity.IdentityServer4Extractor` is an independent console project referencing only manifest Core plus `Microsoft.Data.SqlClient` 7.0.2. IdentityService and Vue do not reference the extractor or SQL client. |
| Credential custody | The CLI accepts no arguments and rejects interactive input. It reads exactly one redirected stdin line, requires server/database, rejects attach-file/user-instance modes, and forces `Encrypt=Mandatory`, certificate validation, `ApplicationIntent=ReadOnly`, and a fixed application name. The source login still requires database-enforced read-only permissions. |
| Baseline gate | The dedicated database must contain exactly the canonical 4.1.2 SQL Server configuration migration's `dbo` table/column set and sole `20200522172542_Config` / `3.1.0` history row. Missing, renamed, additional, duplicated, or ambiguous schema evidence returns redacted exit `2`. This fingerprints the 4.1 schema, not the deployed assembly patch. |
| Projection | Fixed SQL reads schema/migration metadata, client IDs, PKCE flags, grant names, and credential row/source/type metadata. It never selects credential values or operational-store data. Unknown credential types are normalized without echo; custom grants are quarantined. |
| ROPC gate | Any password grant emits `migrationClassification: ropc-blocker` without invented owner/target. The manifest is still emitted, existing validation reports `LIV122`/`LIV123`, and the CLI exits `2`. |
| Output | Standard output contains only indented schema-v1 JSON and is capped at 1 MiB. Standard error contains fixed codes/messages without provider exception details, SQL text, connection values, client IDs, or source field values. No file or database is written by the tool. |

## Unit 0C files

- `tools/OwnIdentity.IdentityServer4Extractor/*` - stdin-only CLI, exact baseline gate, fixed SQL reader, projector, and SQL client dependency.
- `tests/OwnIdentity.IdentityService.Tests/IdentityServer4SourceExtractorTests.cs` - schema, projection, credential-boundary, ROPC, SQL-safety, and redaction behavior.
- `tests/OwnIdentity.IdentityService.Tests/OwnIdentity.IdentityService.Tests.csproj` and `OwnIdentity_openDict.sln` - CLI test reference and tool registration, both added through .NET CLI.
- `docs/identity-platform-modernization-{plan,progress}.md` - operational contract, limitations, evidence, and resume point.

## Unit 0C verification evidence

Executed from the repository root on 2026-08-06 after source-mutating formatting:

| Command | Exact outcome |
| --- | --- |
| `dotnet format "OwnIdentity_openDict.sln" --no-restore` | Exit `0`; formatting completed with the repository's generic workspace-load warning and no reported formatting failure. |
| `dotnet restore "OwnIdentity_openDict.sln"` | Exit `0`; all projects were up to date. |
| `dotnet build "OwnIdentity_openDict.sln" --no-restore` | Exit `0`; all six .NET projects built with `0` warnings and `0` errors. |
| `dotnet test "tests/OwnIdentity.IdentityService.Tests/OwnIdentity.IdentityService.Tests.csproj" --no-restore --filter "FullyQualifiedName~IdentityServer4SourceExtractor"` | Exit `0`; `7` passed, `0` failed, `0` skipped. |
| `dotnet test "tests/OwnIdentity.IdentityService.Tests/OwnIdentity.IdentityService.Tests.csproj" --no-restore` | Exit `0`; full suite `32` passed, `0` failed, `0` skipped. |
| `dotnet list "tools/OwnIdentity.IdentityServer4Extractor/OwnIdentity.IdentityServer4Extractor.csproj" package --include-transitive` | Exit `0`; `Microsoft.Data.SqlClient` requested/resolved `7.0.2`, with `22` resolved transitive packages reported. |
| `dotnet list "tools/OwnIdentity.IdentityServer4Extractor/OwnIdentity.IdentityServer4Extractor.csproj" package --vulnerable --include-transitive` | Exit `0`; no vulnerable packages reported from nuget.org and the configured Visual Studio offline source. |
| `rg -n --glob "*.cs" "SELECT[^;]*(Password|Secret|Value|Token|Code|Certificate|Key)|FROM[^;]*(PersistedGrants|DeviceCodes)|Environment\\.GetEnvironmentVariable|File\\.(Read|Write)|ConnectionStrings" "tools/OwnIdentity.IdentityServer4Extractor"` | No matches; fixed production SQL does not project listed sensitive/operational fields, and the tool has no environment/config/file credential path. |
| `rg -n --glob "*.{cs,csproj,ts,vue,json}" "IdentityServer4Extractor|Microsoft\\.Data\\.SqlClient" "src/OwnIdentity.IdentityService" "src/ownidentity-admin"` | No matches; OwnIdentity runtime and Vue remain uncoupled from the source extractor and SQL client. |

## Unit 0C runtime result

A live SQL Server baseline is unavailable in the workspace/environment, so database runtime execution is **UNAVAILABLE**, not fabricated. Deterministic tests execute the schema-fingerprint, manifest-projection, stdin/connection hardening, ROPC, SQL allowlist, and redacted-failure contracts without pretending to validate SQL Server connectivity or permissions.

Future smoke prerequisite: provision an isolated SQL Server database from the canonical IdentityServer4 4.1.2 `ConfigurationDb.sql`, seed non-secret representative configuration, create a short-lived principal restricted to metadata and the four fixed projections, and provide its connection through an approved secret provider. Then run the documented source-side pipeline and preflight its protected output through Unit 0B. Do not put the connection string in shell text, environment variables, files, logs, or test evidence.

```powershell
Get-Secret -Name "<approved-reference>" -AsPlainText |
    dotnet run --project "tools/OwnIdentity.IdentityServer4Extractor/OwnIdentity.IdentityServer4Extractor.csproj" --no-build `
    > "<approved-output-path>\identityserver4.inventory.json"
```

## Unit 0C rollback boundary

Delete `tools/OwnIdentity.IdentityServer4Extractor` and `tests/OwnIdentity.IdentityService.Tests/IdentityServer4SourceExtractorTests.cs`; remove the extractor from `OwnIdentity_openDict.sln` and its test project reference through .NET CLI; remove the Unit 0C plan/progress sections and current-status row. Unit 0A Core/CLI, Unit 0B API/Vue behavior, IdentityService packages/runtime/schema/data, frontend dependencies, and source/target databases are unaffected. No extracted artifact is created automatically; operators separately delete any explicitly redirected manifest according to evidence-retention policy.

## Unit 0C Skoruba beta7 lab evidence

- Lab: `lab/skoruba-beta7/` — docker compose (SQL Server 2017 with TLS cert + Skoruba Admin/Api/STS built from tag `1.0.0-beta7`, all non-default ports: 14330/19000/15000/15002), seeded one client (`skoruba_identity_admin`, hybrid) after deleting the seed's swagger-ui client, schema evidence in `lab/skoruba-beta7/evidence/` (30 tables, 188 columns, 4 migrations @ EF 2.2.6).
- Extraction run: `is4beta7-extractor` image (repo-built, lab CA trusted) executed with the connection string exclusively on stdin inside the lab network; exit 0, sanitized schema-v1 manifest with beta7 fingerprint, 19 config tables, 10 quarantined Skoruba extension tables, ADR `ADR-source-baseline-skoruba-identityserver4-admin-1.0.0-beta7`.
- Preflight (Unit 0B): `OwnIdentity.LegacyInventory lab/skoruba-beta7/lab-manifest.json` → status VALID, 0 ROPC blockers.
- Database compatibility baseline decision: the port target schema is Skoruba IdentityServer4.Admin 1.0.0-beta7 (user decision L2 in `odd/tasks/is4-rc2-source-lab.md`); IS4 runtime remains 2.4.0-era and clients/grants migrate through the plan section-5 mapping table, not by copying rows.

## Unit 0C known limitations

- No production authorized live SQL Server/IdentityServer4 EF 4.1 database was available; connectivity, TLS chain validation, and the full read-only pipeline were proven against the isolated Skoruba 1.0.0-beta7 docker lab (see above). Real operator runs still require the approved secret provider, short-lived least-privilege principal, and protected output path.
- The gate accepts only a dedicated canonical `dbo` configuration database or the Skoruba Admin 1.0.0-beta7 variant (whose Skoruba identity/log/operational tables are recorded as quarantined evidence, never inventoried as source truth). Shared databases beyond that, custom migration histories, renamed schemas/tables, and other variants intentionally fail closed.
- The database fingerprint proves the canonical 4.1 configuration schema, not the exact deployed IdentityServer4 assembly patch or surrounding deployment configuration.
- Schema v1 cannot represent resource/scope detail, redirect URIs, providers, ASP.NET Identity rows, deployment/issuer/key-source evidence, operational counts, or full ADR state. Later bounded contracts must add those domains without weakening redaction.
- Standard input avoids common persistence/disclosure surfaces but leaves plaintext in process memory and the OS pipe. The external secret provider, host isolation, process supervision, SQL principal lifecycle, and protected redirected output remain operator responsibilities.
- Durable runs, audit, cancellation/resume, transformation, target load, reconciliation, apply, and rollback execution remain blocked on their separate persistence/audit/product decisions.

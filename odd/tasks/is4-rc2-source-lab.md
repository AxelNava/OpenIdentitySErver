# Feature: Skoruba IS4 Admin 1.0.0-beta7 source lab + extractor compatibility + ADR kickoff

Database-level compatibility target is **Skoruba IdentityServer4.Admin `1.0.0-beta7`** (https://github.com/skoruba/IdentityServer4.Admin/tree/1.0.0-beta7): its EF schema (IS4 configuration + Skoruba identity tables) is the baseline the port must be database-compatible with. Reached through a docker legacy lab (Skoruba admin + legacy IS4 host, one client) whose schema feeds Unit 0C extraction and Unit 0B preflight. Supersedes the earlier IS4 `1.0.0-rc2` (DuendeArchive) baseline (L1 → L2).

## Specs

- S1: "Por cierto, la versión que queremos portar es esta de identity server admin 4 de skoruba https://github.com/skoruba/IdentityServer4.Admin/tree/1.0.0-beta7 Desde ahí vamos a ofrecer la compatibilidad de base de datos" — Baseline for source-schema work is the Skoruba IdentityServer4.Admin 1.0.0-beta7 EF schema (supersedes the earlier rc2-only baseline and the current canonical 4.1.2-only gate).
- S2: "Ahora mismo no hay un inventario, pero se puede crear uno en docker, podemos montar con contenedor con un proyecto legacy de identity server4 que use identity server admin de skoruba, con un solo cliente." The lab runs in docker, uses Skoruba IdentityServer4 Admin alongside a legacy IS4 host, and seeds exactly one client.
- S3: "teniendo eso en cuenta, podemos continuar destrabando los adr's (no se que sea)" — ADR = Architecture Decision Record; the 10 section-12 decisions of the modernization plan must each get an owner and a decision. User explicitly did not know the term; explanation provided.
- S4: "luego los build/tests y luego continumos con lo que continua del plan" — after ADR progress, run full build/tests, then resume remaining plan units.
- S5 (derived constraint, plan amendment): Unit 0C's baseline gate currently fingerprints only the canonical `IdentityServer4.EntityFramework` 4.1.2 schema and fails closed on other variants including Skoruba. Reaching S1 requires extending the gate to accept the Skoruba 1.0.0-beta7 schema fingerprint (IS4 EF tables of the IS4 version beta7 targets, plus quarantined Skoruba identity tables), without weakening redaction, stdin-only credentials, or the ROPC blocker gate.

## Environment constraints

- Docker 29.9.3 + Compose v5.6.1 available; unrelated containers already run on this host — avoid port collisions.
- `global.json` pins .NET SDK `10.0.302`; installed SDK is `10.0.113` → `dotnet` build/test currently FAILS until fixed. Blocker for T7.
- No live legacy database exists; the lab is the evidence source.

## Tasks

- T1 (S1,S5): Research — Skoruba IdentityServer4.Admin 1.0.0-beta7: pinned IS4/EF package versions, EF configuration schema (tables/columns/migrations), its docker-compose, delta vs 4.1.2 fingerprint; viable lab topology. Route: explore. (Earlier rc2-only research superseded; archived in Engram.)
- T2 (S2): Build docker lab: compose file with SQL Server, Skoruba Admin at tag 1.0.0-beta7 (admin + api + STS as its compose defines), legacy IS4 host; seed one client; keep off default ports. Route: writer or inline.
- T3 (S2): Verify lab boots, one client visible in Skoruba admin, configuration DB reachable; capture schema evidence (beta7 fingerprint).
- T4 (S5): Extend Unit 0C gate to Skoruba 1.0.0-beta7 fingerprint + Skoruba identity-table quarantine; tests first (RED→GREEN); redaction and ROPC gates unchanged. Route: inline (focused) or writer.
- T5 (S1): Run extractor against lab DB (stdin-only credential boundary) and preflight emitted manifest through Unit 0B endpoint.
- T6 (S3): ADR kickoff: present the 10 decisions with recommendation + alternatives; redact ADR stubs under `docs/adr/`; owners and decisions come from the user.
- T7 (S4): Full build + tests (unblocked L3: global.json pins 10.0.113).
- T8: Update plan/progress docs with lab evidence, gate change, and ADR state.

## Log

- L1 (user, verbatim): "Vamos a intentar tener una compatibilidad a nivel de base de datos con la versión https://github.com/DuendeArchive/IdentityServer4/tree/1.0.0-rc2 desde ahí vamos a trabajar. Ahora mismo no hay un inventario, pero se puede crear uno en docker, podemos montar con contenedor con un proyecto legacy de identity server4 que use identity server admin de skoruba, con un solo cliente. teniendo eso en cuenta, podemos continuar destrabando los adr's (no se que sea) y luego los build/tests y luego continumos con lo que continua del plan"
- L1 (continuation): "Continua"
- L2 (user, verbatim): "Por cierto, la versión que queremos portar es esta de identity server admin 4 de skoruba https://github.com/skoruba/IdentityServer4.Admin/tree/1.0.0-beta7 Desde ahí vamos a ofrecer la compatibilidad de base de datos" — Baseline changed from IS4 1.0.0-rc2 to Skoruba IdentityServer4.Admin 1.0.0-beta7. S1/S2/S5 and T1–T5 rewritten; rc2 research archived.
- L3 (user, verbatim): "ajusta a 113, siempre la última versión." — global.json SDK pin moved 10.0.302 → 10.0.113; T7 unblocked.
- L4 (evidence): Lab verified 2026-11-08: containers mssql/admin/api/sts up (ports 14330/19000/15000/15002), OIDC admin→STS flow OK, 1 client (`skoruba_identity_admin`), schema evidence in lab/skoruba-beta7/evidence/ (30 tables, 188 columns, 4 migrations @ EF 2.2.6). T2/T3 done.
- L5 (evidence, T4–T7): Gate extended to Skoruba beta7 fingerprint (`TryMatch`, quarantine of 10 Skoruba tables, baseline-aware credentials SQL and manifest projection); 9 new tests RED→GREEN, full suite 40/40 (global.json pinned to 10.0.113 per L3). Extraction against the live lab DB (stdin-only credential, TLS-validated via lab CA after fixing mssql key pair and healthcheck) produced a beta7-fingerprint manifest; Unit 0B preflight VALID (19 config tables, 10 quarantined, 0 ROPC blockers). Wiring defect found and fixed during live run: snapshot now carries the detected baseline to the projector. Docs plan/progress updated; ADR stubs 0001–0010 in docs/adr/ (all Proposed).

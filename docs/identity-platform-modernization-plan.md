# Modernize OwnIdentity into an operated identity platform

**Status:** Architecture research and implementation plan, not an implementation specification  
**Evidence date:** 2026-08-06  
**Decision owner:** Product, security, and platform operations  
**Scope:** Build OwnIdentity as a greenfield ASP.NET Core Identity, OpenIddict, and Vue platform with an explicit side-by-side migration path for existing IdentityServer4 installations, optional SAML bridging, and a BFF. This document does not claim FAPI conformance or select an unresolved commercial/cloud product.

Implementation evidence and resume state: [identity platform modernization progress](identity-platform-modernization-progress.md).

## 1. Executive verdict

Proceed with OpenIddict 7.6.0 for the core OAuth 2.0/OpenID Connect authority only if the product accepts its verified support boundary. It is a current Apache-2.0 release and the repository already has a useful .NET 10 baseline. It is not a drop-in IdentityServer4 database replacement, a SAML server, an automatic key manager, a first-class multi-issuer platform, or a FAPI-certified product.

The recommended target is:

1. Preserve ASP.NET Core Identity users, roles, IDs, and password hashes after a compatibility rehearsal.
2. Build a versioned control plane and Vue admin over issuer-isolated runtime cells. Prefer one immutable issuer, key set, and protocol store per cell; do not derive tenant trust from an arbitrary `Host` header.
3. Move Vue authentication to a same-origin confidential BFF. The browser receives only a hardened session cookie; access and refresh tokens remain in a server-side session store.
4. Use OpenIddict's built-in PKCE, PAR, `private_key_jwt`, mTLS client authentication, and mTLS certificate-bound token capabilities where they satisfy the selected profile.
5. Target **FAPI 2.0 Security Profile Final with mTLS sender-constrained tokens first**. FAPI 2.0 does not require JAR or JARM, but it requires substantially more than enabling PKCE and mTLS. OpenIddict defaults are not FAPI-safe without additional configuration and tests. Do not claim conformance before the OpenID Foundation suite passes and certification, if required, is complete.
6. Defer DPoP beyond the current roadmap. It is unavailable in stock OpenIddict 7.6.0 and is not a current profile, Admin choice, acceptance gate, or dependency. The current sender-constraint target is mTLS. Any later DPoP initiative must select a maintained product or deliver and conform-test the complete authorization-server, client, and resource-server behavior described in section 11.
7. Separate inbound SAML federation from outbound SAML issuance. Sustainsys.Saml2 is an SP library, not an IdP. Use it only as a deliberately accepted inbound-only option; use a separate federation bridge/product for outbound SAML unless the organization explicitly funds and owns a custom IdP implementation.
8. Use side-by-side ETL and coexistence. Do not run OpenIddict against IdentityServer4 tables and do not migrate active authorization codes, refresh tokens, device codes, or persisted-grant payloads as if their formats were compatible.
9. Govern OAuth/OIDC capability through reviewed client and security profiles. Authorization Code + PKCE is the normal interactive baseline; Client Credentials and Device Authorization are explicit supported profiles; Hybrid and Implicit are temporary legacy compatibility profiles. ROPC/password grant is prohibited by RFC 9700, permanently disabled, and a hard migration blocker with no target implementation. mTLS is the current sender-constraint choice; DPoP is deferred.

### Corrected assumptions

| Assumption | Verdict | Corrected decision |
| --- | --- | --- |
| OpenIddict can use the IdentityServer4 EF schema directly. | **False / unsafe** | The models and payloads differ. Create a new OpenIddict schema and use repeatable ETL with reconciliation and legacy-ID mapping. |
| ASP.NET Core Identity users and roles must be rebuilt. | **Usually false** | Preserve `AspNet*` rows, string IDs, password hashes, security stamps, claims, logins, roles, and role memberships when schema/key types and hasher compatibility are proven. Rehearse login and rehash behavior. |
| OpenIddict 7.6.0 is behind the current stable release. | **False as of evidence date** | The repo uses 7.6.0, released 2026-07-15 and still the latest stable package; 8.0 packages are previews and are not a production upgrade target. |
| `RequireProofKeyForCodeExchange()` makes the authority FAPI-compliant. | **False** | It globally requires PKCE, but OpenIddict 7.6.0 enables both `plain` and `S256` by default. FAPI 2 requires `S256`, confidential clients, PAR, sender-constrained tokens, short code/request lifetimes, prescribed algorithms, TLS controls, and many validation rules. |
| PAR, mTLS, and certificate-bound tokens imply FAPI conformance. | **False** | They are useful built-in primitives. OpenIddict documentation/source reviewed here makes no FAPI certification claim. Conformance is a property of the full deployment, including clients and resource servers. |
| FAPI always requires JAR and JARM. | **False** | FAPI 1.0 Advanced requires a signed request object and either hybrid response protection or JARM. FAPI 2.0 Final replaces JAR with authenticated PAR and JARM with code-only responses plus the RFC 9207 `iss` parameter. Ecosystem profiles can add requirements. |
| OpenIddict 7.6.0 provides DPoP because it has generic proof-of-possession code. | **False** | Source review found mTLS `cnf` proof validation but no DPoP header, metadata, `dpop_jkt`, nonce, token type, or resource-server implementation. DPoP is deferred beyond the current roadmap; see section 11. |
| OpenIddict 7.6.0 provides JAR/JARM. | **False for stock server behavior** | The server explicitly rejects the `request` parameter and only accepts its own PAR/cache `request_uri`; stock response modes are `query`, `fragment`, and `form_post`, not JWT response modes. Custom handlers would be a substantial protocol implementation. |
| A request-derived issuer is built-in multi-tenancy. | **False / unsafe** | OpenIddict falls back to the request base URI when no issuer is set, but this is not tenant isolation. It makes issuer integrity depend on trusted proxy/host processing, and mTLS endpoint aliases require a static issuer. Use immutable, allowlisted issuer cells. |
| Registering multiple credentials is automatic key rotation. | **False** | OpenIddict can publish and validate multiple credentials and chooses among them, but it does not generate, approve, distribute, activate, retire, or emergency-revoke keys. Built-in registration is options-driven; safe runtime orchestration is custom. |
| All settings, including root secrets and private keys, should be editable in Vue. | **Unsafe** | Vue may manage references, metadata, lifecycle requests, and published configuration. It must never read private key material or raw provider secrets. Vault bootstrap identity, database credentials, trusted proxy roots, break-glass access, and HSM policy remain deployment/operator controls. |
| Sustainsys.Saml2 can make OwnIdentity a SAML IdP. | **False** | Its official description and ASP.NET Core handler make the application a SAML SP to upstream IdPs. Stable v2 is MIT but maintenance-only; v3 is work in progress, unsupported, and dual licensed by source area. |
| Dynamic database providers always require an app restart. | **False, with conditions** | ASP.NET Core supports adding/removing schemes and invalidating named options at runtime. A safe database loader, immutable snapshots, cache invalidation, callback draining, and multi-node propagation are custom work. Startup-only `AddOpenIdConnect`/`AddSaml2` configuration does require restart. |
| `Duende.AccessTokenManagement.OpenIdConnect` requires a commercial license. | **False** | Current 4.2.0 is part of Duende's Apache-2.0 FOSS repository and is suitable for commercial use under that license. `Duende.BFF`/`Duende.BFF.Yarp` is different: production use requires a Duende license. |
| Adding Access Token Management alone guarantees tokens are server-side. | **False** | Its default OIDC pattern stores tokens in the authentication session. Without a server-side ticket/session store, `SaveTokens` can place encrypted tokens in the cookie ticket. The BFF must use a durable server-side session store and an opaque browser cookie. |
| Refresh-token rotation is universally required by FAPI. | **False** | FAPI 2.0 says authorization servers generally shall not rotate refresh tokens for confidential, sender-constrained clients except extraordinary migrations; clients must tolerate rotation. Choose behavior by profile rather than by slogan. |
| An OAuth grant determines how the user authenticates. | **False** | Local Identity, external OIDC/OAuth, inbound SAML, MFA/passkeys, and the SSO cookie authenticate the user. OAuth/OIDC grants authorize token issuance; `client_secret`, `private_key_jwt`, and mTLS authenticate the client. Model and audit these three dimensions separately. |
| A flow supported by OpenIddict is enabled in OwnIdentity today. | **False** | The current service configures only `AllowAuthorizationCodeFlow()` and global PKCE. Client Credentials, Device Authorization, Hybrid, Implicit, password grant, refresh tokens, PAR, and DPoP are not implemented by the current repository. Library APIs are evidence of technical boundaries, not approval: ROPC remains prohibited and DPoP remains deferred. |
| ROPC is acceptable when restricted to first-party clients. | **False / prohibited** | RFC 9700 says the Resource Owner Password Credentials grant **MUST NOT** be used. OwnIdentity will not configure, approve, import as executable, or temporarily carry forward ROPC. Every dependent legacy client must migrate to Authorization Code + PKCE, Device Authorization, Client Credentials when there is no human user, or external federation before cutover. |
| DPoP is another OAuth grant or a `cnf.jkt` claim switch. | **False / unsafe** | DPoP is a sender-constraint/token-binding profile, not a grant. It is deferred beyond the current roadmap. Any future initiative must bind authorization codes, access tokens, and refresh tokens and validate proofs, nonce, replay, `ath`, metadata, errors, clocks, and keys at every participating component. |

### Support legend

| Label | Meaning |
| --- | --- |
| **Built-in** | OpenIddict/ASP.NET Core provides and documents the protocol behavior; deployment configuration is still required. |
| **Custom** | Achievable only with OwnIdentity handlers, stores, orchestration, or policy code that the team must maintain and test. |
| **Product** | Prefer or require another maintained product/library and verify its license/support contract. |
| **Unsupported/unsafe** | Do not represent this as supported in the target without a new product decision and evidence. |

## 2. Current repository assessment

### What exists

| Area | Evidence | Assessment |
| --- | --- | --- |
| Runtime | `src/OwnIdentity.IdentityService/OwnIdentity.IdentityService.csproj` targets .NET 10 and references OpenIddict 7.6.0, EF Core SQLite, and ASP.NET Core Identity. `dotnet list ... package --include-transitive` confirmed the resolved versions; `dotnet list ... package --vulnerable --include-transitive` reported no known vulnerable packages for the service or test project from the configured sources on 2026-08-06. | Good development baseline, not a production topology. Direct EF packages are 10.0.7 while some transitive EF components resolve to 10.0.10; package policy should intentionally align patch levels. |
| Identity store | `Data/ApplicationDbContext.cs` derives from `IdentityDbContext<ApplicationUser>` and `Program.cs:22-31` registers Identity with roles. | Correct foundation for preserving Identity users and roles. No tenant key or partitioning model exists. |
| OpenIddict store | `Program.cs:16-20` calls `UseOpenIddict`; `Data/Migrations/20260805210456_InitialIdentity.cs` creates applications, scopes, authorizations, and tokens beside `AspNet*` tables. | Clean OpenIddict schema, but only SQLite and only one authority. No configuration revision, audit, tenant, provider, SAML, or key-lifecycle tables. |
| Protocol | `Program.cs:43-78` enables authorization code, global PKCE, authorization/token/end-session endpoints, development credentials, and local validation. | Useful core. No explicit issuer, PAR endpoint, refresh flow, userinfo, introspection/revocation, device flow, mTLS, DPoP, FAPI profile, production credentials, or protocol-specific lifetimes. |
| Production readiness | Credentials are registered only inside `builder.Environment.IsDevelopment()` (`Program.cs:64-68`). `appsettings.json` has `AllowedHosts: "*"`. Cookies use `SecurePolicy = SameAsRequest` (`Program.cs:33-41`). | Production startup has no signing/encryption credentials and should fail OpenIddict validation. Host, proxy, cookie, key, and deployment hardening are absent. |
| Authorization UI | `Controllers/AuthorizationController.cs` creates claims and signs in; Razor pages provide local login/logout. | No consent workflow, external-provider selection, MFA/passkey policy, session view, account recovery, or tenant context. Resource assignment is hard-coded to `identity.admin`. |
| Seed data | `Data/DatabaseSeeder.cs` creates one `Administrator` role, `identity.admin` scope, public Vue client, and development bootstrap admin. | Appropriate bootstrap only. It grants implicit consent and broad admin scope; production bootstrap and least-privilege delegation need redesign. |
| Admin API | `Controllers/Admin/{Users,Roles,Applications,Scopes}Controller.cs` uses OpenIddict/Identity managers and requires both `Administrator` role and `identity.admin` scope. | Four coarse management areas. Applications are create/list-only, public-code-only, and always receive the admin scope. No update/delete, pagination, concurrency, approvals, audit, secrets, grants, provider, tenant, key, or rollback APIs. |
| User operations | `UsersController.cs` creates confirmed users and returns a generated temporary password, toggles a lockout sentinel, and replaces roles. | Not an acceptable production invitation/reset process. It needs write-only activation, MFA policy, audit, self-protection, concurrency, and prevention of last-admin removal. |
| Vue admin | Vue 3/TypeScript/Vite/Pinia routes to Users, Roles, Applications, and Scopes (`src/ownidentity-admin/src/router/index.ts`; `components/AdminShell.vue`). | Useful shell, not yet an operated control plane. |
| Browser security | `src/ownidentity-admin/src/auth/userManager.ts` uses `oidc-client-ts` with authorization code and sessionStorage. `api/client.ts` reads the access token and sends it as a bearer token. | Must be replaced, not preserved. The target Vue application must not own OAuth tokens. |
| Tests | `tests/OwnIdentity.IdentityService.Tests/AdminAuthorizationPolicyTests.cs` tests one policy. `src/ownidentity-admin/src/__tests__/apiClient.spec.ts` tests bearer attachment and problem details. | No protocol, persistence, migration, browser, federation, multi-node, key rotation, or security tests. The frontend bearer-token test should disappear with the BFF migration. |
| Orchestration | `IdentityProjectsHost/AppHost.cs` starts the authority and Vite app, waits for health, and injects fixed localhost authority/API URLs. | Good local harness. Add BFF, chosen production database, cache/session store, secret-provider emulator, bridge, and test resources incrementally. |
| Database operations | `Program.cs:115-120` runs migrations and seeding at every startup. | Convenient locally; unsafe as a general multi-node production migration strategy. Use a gated migration job and backward-compatible rolling migrations. |

### Baseline-to-target gaps

The current solution is a single-process development authority, not an IdentityServer4 migration surface. It has no code that reads an IdentityServer4 database. The migration source schema, deployment topology, issuer URL, signing material, client inventory, secret types, custom grants, persisted-grant volume, external providers, and Skoruba extensions must be inventoried from the real legacy environment before ETL is finalized.

The target also needs a separation that the current project does not have:

- **Data plane:** authorization, token, discovery/JWKS, login, federation callbacks, and validation-critical reads.
- **Control plane:** Vue BFF, admin API, draft/publish workflow, validation jobs, approvals, audit, and rollout orchestration.
- **Secret/key plane:** vault/KMS/HSM and narrowly privileged workers. Admins see references and public metadata, never private material.

## 3. Target architecture and trust boundaries

```text
Untrusted browser
  |
  | HTTPS, opaque __Host- session cookie, CSRF header/token
  v
[Vue + Admin BFF] ------------------------------+
  | OIDC confidential client; tokens in         | control-plane calls
  | encrypted server-side session store         v
  |                                      [Admin/Control API]
  | YARP allowlisted routes                      | RBAC + approvals + audit
  v                                              v
[Admin API facade]                       [Versioned config DB]
                                                 |
                                                 | validate -> stage -> publish
                                                 v
                                         [Outbox/config distributor]
                                                 |
                       +-------------------------+------------------------+
                       |                                                  |
                       v                                                  v
             [Issuer cell: tenant A]                           [Issuer cell: tenant B]
             fixed issuer + own keys                           fixed issuer + own keys
             OpenIddict + Identity                             OpenIddict + Identity
             protocol DB/cache                                 protocol DB/cache
                       |                                                  |
                       +------------------+-------------------------------+
                                          |
                    +---------------------+----------------------+
                    |                                            |
                    v                                            v
          [External OIDC/SAML bridge]                    [Protected APIs]
          egress allowlist, metadata                     validate issuer/aud/scope,
          and partner trust                              mTLS where required

Secret/key boundary:
[Vault/KMS/HSM] <---- workload identity only ---- [issuer/key worker/bridge]
Vue stores only secretRef/keyRef, public certificate/JWK, state, and timestamps.

Ingress boundary:
[Trusted edge/LB] validates SNI/host, terminates or forwards mTLS by an authenticated
mechanism, replaces forwarded headers, and routes only allowlisted issuer hosts.
```

### Architecture decisions made here

- **Issuer cells over Host-derived tenancy.** A cell may contain multiple replicas, but it has one immutable issuer and one explicit tenant binding. This matches OpenIddict's static issuer and mTLS constraints and limits blast radius.
- **Shared control plane, isolated data planes.** The admin can operate many cells, but data-plane availability does not depend on a live admin UI or config database for every request. Published snapshots are locally cached.
- **Versioned configuration.** Editing never changes live protocol behavior directly. A draft is validated, approved where required, published as an immutable revision, distributed, acknowledged by nodes, and reversible to a previous compatible revision.
- **Secret references.** Provider secrets and private keys are supplied write-only to the vault or generated there. Database rows contain opaque references and fingerprints only.
- **BFF ownership.** Vue is a presentation layer. The BFF is the confidential OIDC client and the only browser-facing API origin.
- **Protocol extensions are explicit products.** SAML IdP, FAPI 1 JAR/JARM, broad multi-issuer support, or the explicitly deferred DPoP capability do not become implicit obligations hidden inside generic middleware.

## 4. Feature decisions

Each section uses the same review frame: architecture, data/config, admin experience, security, runtime behavior, failures, tests, observability, rollout, and support status.

### 4.1 OAuth/OIDC flow and sender-constraint policy

**Decision:** Authorization Code + PKCE remains the normal interactive baseline. Client Credentials and Device Authorization are supported opt-in profiles; Hybrid and Implicit are time-bounded legacy compatibility profiles. ROPC is prohibited and has no target profile or exception path. mTLS is the current sender-constraint target; DPoP is deferred outside this roadmap. Server capability, per-application endpoint/grant/response permissions, client security profile, user authentication, client authentication, and token sender constraint are independent controls.

#### Separate the three authentication and authorization dimensions

| Dimension | Answers | OwnIdentity choices |
| --- | --- | --- |
| User authentication and SSO | How did a human establish an authority session, and at what assurance? | Local ASP.NET Core Identity, external OIDC/OAuth, inbound SAML, MFA/passkeys, step-up, and the authority SSO cookie. These methods can feed an interactive authorization request; they are not OAuth grants. |
| OAuth/OIDC grant or response type | How may a client obtain tokens? | Authorization code, client credentials, device code, temporary hybrid/implicit compatibility, and refresh token where a profile permits it. The password grant is globally prohibited. The grant does not select the user's login method. |
| Client authentication | How does a confidential client prove its identity? | Prefer mTLS or `private_key_jwt`; allow `client_secret_basic`/`client_secret_post` only as governed transitional exceptions. Public clients use no client secret. Client authentication is not user authentication and is not a sender constraint by itself. |
| Token sender constraint | How is token use bound to a key or certificate? | mTLS certificate binding is the current target and the only sender-constraint choice in this roadmap. DPoP is deferred, unavailable, and never represented as a grant checkbox. |

#### Target profile matrix

| Profile | Business use | Client type | Grant / response | Client authentication | PKCE / PAR | Token sender constraint | Refresh policy | Default state | FAPI eligibility | Support status | Replacement / sunset path |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Standard interactive | Web/BFF, native, and other user-facing clients | Confidential BFF/web or public native | Authorization code; `response_type=code` | Confidential: mTLS or `private_key_jwt` preferred; governed secret exception. Public: none. | PKCE `S256` required; PAR optional unless a stronger profile requires it | Optional mTLS only where sender constraint is required | Allowed only when needed; public-client rotation/reuse controls and confidential-client policy are explicit | **Enabled profile; current implementation baseline is code + global PKCE only** | Not automatically FAPI | **Supported baseline** | Migrate browser SPAs to BFF or code + PKCE; remove non-PKCE clients |
| FAPI 2 interactive | Regulated user-authorized API access | Confidential | Authorization code; `response_type=code` | mTLS first; approved `private_key_jwt` where the selected ecosystem permits it | Authenticated PAR and PKCE `S256` required | mTLS | Profile-specific; sender-bound confidential clients generally do not rotate except extraordinary migration | **Opt-in, security approval, conformance gate** | **Target interactive FAPI profile** | **Built-in primitives, custom profile, external proof** | No downgrade path; disable client/profile if conformance fails |
| Machine-to-machine | Service/workload access with no human subject | Confidential only | Client credentials | Prefer mTLS or `private_key_jwt`; shared secret only as approved transitional exception | Not applicable | Prefer mTLS where sender constraint is required | No refresh token by default; re-authenticate the client | **Opt-in per client** | Separate financial M2M profile possible; not the FAPI interactive authorization profile | **Supported profile** | Rotate to asymmetric client auth and sender constraint; expire secret exceptions |
| Device Authorization | CLI, TV, and input-constrained devices requiring human authorization on a second device | Public device by default; confidential only when deployment can protect credentials | Device authorization plus device-code token grant | Public device: none and no embedded device/client secret. Confidential variant requires reviewed auth. | Not applicable; verification browser session receives normal phishing/session protections | Optional mTLS only where the device can securely hold a certificate and the profile permits it | Off by default; allowed only for a reviewed client and scope set | **Disabled unless client has explicit profile and permissions** | Not the FAPI interactive profile | **Supported opt-in profile** | Move ordinary browser/native clients to code + PKCE; disable unused device registrations |
| Hybrid legacy compatibility | Temporary migration of clients requiring `code id_token`, `code token`, or `code id_token token` | Confidential legacy client | Authorization code + implicit response types | Existing reviewed confidential method; no new shared-secret onboarding | PKCE where technically possible; no FAPI/PAR representation | None by default; no FAPI | Short and restricted; no expansion | **Disabled by default; explicit per-client time-bounded exception** | **Ineligible** | **Legacy compatibility only** | Mandatory migration deadline to code + PKCE; remove front-channel tokens first |
| Implicit legacy compatibility | Temporary migration of browser/client integrations that cannot yet redeem a code | Existing legacy client only | Implicit; `id_token`, `token`, or `id_token token` response | Usually public/none; no new client registrations | PKCE does not protect implicit access tokens; no FAPI | None | No refresh; shortest viable access/ID token lifetime | **Disabled by default; explicit per-client time-bounded exception; no new clients** | **Ineligible** | **Legacy compatibility only** | Mandatory migration to BFF or code + PKCE with telemetry-backed deadline |
| ROPC/password | None; imported registrations are inventory and remediation records only | No eligible target client | Password (`grant_type=password`) | Not applicable | Not applicable | None | None | **Permanently disabled; no exception or approval path** | **Ineligible** | **Unsupported/prohibited migration blocker; no target implementation** | Migrate before cutover to Authorization Code + PKCE, Device Authorization, Client Credentials where there is no human user, or external federation |
| DPoP sender constraint | Deferred future capability; no current business profile | No eligible current client | **Not a grant or response type** | Not available | Not available | DPoP is unavailable; current profiles use mTLS where sender constraint is required | Not available | **Deferred beyond the current roadmap; unavailable** | Not a current compliance choice | **Out of scope; stock OpenIddict 7.6.0 lacks complete support** | See section 11; any later initiative is separately approved and conform-tested |

Client Credentials tokens represent the client/workload, not a user: do not add user `sub`, roles, authentication methods, or delegated-user claims. Require typed scopes, resources, and audiences, and prove that APIs reject cross-audience and delegated-scope confusion. Device Authorization requires device authorization and end-user verification endpoints, a user-code verification UI, explicit consent, short device/user-code expiry, polling interval plus `slow_down` backoff, per-client/device/IP rate limits, one-time redemption, session and transaction binding, and phishing-resistant display/confirmation of the requesting client and requested access. Never enable it globally without per-client permission.

Every Hybrid or Implicit assignment records a reason and ticket, security approver, activation and mandatory expiry date, owner, restricted scope/resource set, migration target, warning acknowledgement, and telemetry baseline. These profiles cannot be assigned to newly created clients. Both require a documented front-channel code/token leakage analysis; Implicit uses no refresh token and the shortest viable token lifetime.

ROPC has no assignment, activation, approval, expiry, adapter, or execution path. Imported ROPC configuration is retained only as read-only evidence for inventory and remediation tracking. Because the client captures the user's password and bypasses the authority's interactive ceremony, it cannot provide equivalent federation, MFA, passkey, or step-up semantics. Cutover remains blocked until every ROPC dependency is eliminated, and credentials must never reach logs, traces, metrics, errors, audit diffs, or support tooling.

#### Verified OpenIddict 7.6.0 configuration boundary

| Capability | Exact server configuration API | Required endpoint and application permissions | Current repository state |
| --- | --- | --- | --- |
| Authorization code | `AllowAuthorizationCodeFlow()` | Authorization + token endpoints; `Permissions.Endpoints.Authorization`, `Permissions.Endpoints.Token`, `Permissions.GrantTypes.AuthorizationCode`, `Permissions.ResponseTypes.Code` | **Configured today**, with global `RequireProofKeyForCodeExchange()` |
| Client credentials | `AllowClientCredentialsFlow()` | Token endpoint; `Permissions.Endpoints.Token`, `Permissions.GrantTypes.ClientCredentials` | **Not configured today** |
| Device Authorization | `AllowDeviceAuthorizationFlow()`, `SetDeviceAuthorizationEndpointUris(...)`, `SetEndUserVerificationEndpointUris(...)` | Device authorization + end-user verification + token endpoints; client gets `Permissions.Endpoints.DeviceAuthorization`, `Permissions.Endpoints.Token`, and `Permissions.GrantTypes.DeviceCode`. OpenIddict has no application permission for the user-facing verification endpoint. | **Not configured today** |
| Hybrid | `AllowHybridFlow()` | Authorization + token endpoints; authorization-code and implicit grant permissions plus only the reviewed `CodeIdToken`, `CodeToken`, or `CodeIdTokenToken` response permission | **Not configured today; legacy only** |
| Implicit | `AllowImplicitFlow()` | Authorization endpoint; implicit grant permission plus only the reviewed `IdToken`, `Token`, or `IdTokenToken` response permission | **Not configured today; legacy only** |
| ROPC/password | `AllowPasswordFlow()` exists in the library but will deliberately never be configured | No password-grant endpoint/profile permission may be assigned or published | **Permanently unavailable; prohibited migration blocker, not an Admin option** |
| Refresh token | `AllowRefreshTokenFlow()` | Token endpoint; `Permissions.Endpoints.Token`, `Permissions.GrantTypes.RefreshToken`; `offline_access` is special-cased and needs no scope permission | **Not configured today; profile-dependent** |
| DPoP | No complete stock 7.6.0 API/implementation verified | Not expressible by a grant permission | **Deferred beyond the current roadmap; unavailable** |

Do not call `IgnoreEndpointPermissions()`, `IgnoreGrantTypePermissions()`, or `IgnoreResponseTypePermissions()` in production profiles. Enabling a server flow only makes the protocol capability available; the published client profile must still assign the minimum endpoint, grant, response-type, scope, resource, and audience permissions. Hybrid needs both authorization-code and implicit grant permissions because OpenIddict 7.6.0's verified `AllowHybridFlow()` registers both. The end-user verification endpoint is a human-facing device-flow endpoint and has no per-application endpoint permission constant in 7.6.0; authorization of the device transaction is instead anchored to the initiating client and device/user codes.

#### Flow rollout and rollback

Publish server capabilities before assigning any client, then canary one profile/client with deny-by-default permissions and endpoint probes. Device Authorization additionally requires the verification UI and anti-phishing operations to be ready before its endpoint is advertised. Hybrid and Implicit profiles activate only for imported allowlisted client IDs after evidence and expiry approval; rollback disables the assignment and endpoint/grant permissions without deleting audit or migration evidence. ROPC discoveries block migration and can never activate. Rollback for an mTLS sender-constraint profile keeps resource-server validation compatible with already-issued bound tokens through their drain period; it never silently accepts them as bearer tokens.

### 4.2 SSO, sessions, consent, and logout

| Concern | Decision |
| --- | --- |
| Architecture | ASP.NET Core Identity's authority cookie provides SSO among OIDC clients of one issuer. OpenIddict issues tokens; OwnIdentity implements login, consent, account policy, and session coordination. Cross-issuer SSO uses an explicit upstream home IdP/broker, not a parent-domain cookie shared across tenants. |
| Data/config | Add `Session`, `ConsentPolicy`, `ClientClaimPolicy`, `AuthenticationContextPolicy`, and optional `HomeRealm` records keyed by tenant and revision. Keep protocol authorizations/tokens in OpenIddict stores. |
| Vue/API | Sessions: search, inspect redacted metadata, revoke one/all. Consents: inspect and revoke; configure whether a client requires explicit/implicit/systematic/external consent. Client screens configure front/back-channel logout URIs and required session semantics. |
| Security | Require MFA/step-up for session revocation at scale and consent-policy changes. Never expose cookie/tokens. Bind sessions to tenant, subject, client, authentication method/reference, and security stamp. Rotate the session ID after login/privilege change. |
| Runtime/cache | User/role/security-stamp changes publish invalidation events. Existing access tokens remain valid until expiration unless APIs introspect or maintain revocation state. Session revocation is immediate at the authority/BFF but downstream JWT revocation needs short lifetimes or a denylist/introspection design. |
| Failure modes | Back-channel logout targets can be down; queue with bounded retries and a dead-letter view. Partial logout must be visible to operators. Never block local logout indefinitely on a downstream client. |
| Tests | Multi-client SSO, `prompt=login`, `max_age`, consent grant/revoke, local/global logout, front/back-channel failure, security-stamp invalidation, concurrent session revocation, and cross-tenant cookie isolation. |
| Observability | Login/consent/logout success and categorized failure counts, active/revoked sessions, back-channel lag/dead letters, authentication method, tenant, and client. Do not log passwords, cookies, codes, assertions, or tokens. |
| Rollout/rollback | Add session/consent stores and read-only admin first; then enable revocation and logout callbacks client-by-client. Roll back callback delivery without removing local session revocation data. |
| Support status | **Built-in:** OpenIddict code flow/end-session primitives and ASP.NET Identity cookie. **Custom:** consent UI/policy, coordinated sessions, back-channel delivery, admin, and revocation operations. |

### 4.3 SAML 2.0 bridging

Inbound and outbound SAML are different products and trust directions.

| Concern | Decision |
| --- | --- |
| Architecture | **Inbound:** OwnIdentity is an SP/RP and an external organization is IdP; convert the validated SAML identity into the local Identity/external-login model, then issue OIDC tokens. **Outbound:** OwnIdentity is IdP to legacy SPs; run a separately deployable federation bridge with an IdP-capable library/product. Do not put outbound SAML protocol code into the OpenIddict token pipeline. |
| Product boundary | Sustainsys.Saml2 v2.11 is an inbound SP option only. Its docs recommend one authentication scheme per upstream IdP. Stable v2 is MIT but receives security/critical compatibility fixes rather than feature development. Main/v3 is work in progress and unsupported; FOSS and commercial source areas have different licenses. ITfoxtec.Identity.Saml2 4.20.1 is BSD-3-Clause and advertises SP/RP/IdP primitives, but using primitives means OwnIdentity owns complete IdP behavior and interoperability. A maintained federation product/bridge such as FoxIDs or a commercial SAML component is the lower-risk outbound choice, subject to ADR and procurement. |
| Data/config | `SamlConnection`: direction, tenant, entity ID, metadata source/snapshot/fingerprint, ACS/SLO endpoints, bindings, NameID policy, requested/accepted AuthnContext, signing/encryption requirements, clock skew, attribute mappings, certificate refs, partner status, revision, and validity. Store parsed normalized metadata and the signed raw snapshot for audit; never blindly follow live metadata on every login. |
| Vue/API | Separate **Upstream IdPs** and **Downstream SPs**. Provide metadata import, signature/certificate display, dry-run parser, generated metadata preview, claim mapping simulator, certificate rollover, test-login/test-response in a non-production tenant, approval, publish, and rollback. Raw private keys are never accepted/read back by Vue; certificate public material is readable. |
| Security | Require signed responses/assertions per partner policy, destination/audience/recipient/InResponseTo/time validation, replay cache, strict XML parser/signature-wrapping defenses, algorithm allowlists, encrypted assertions where required, HTTPS metadata allowlists, egress restrictions, and tenant-bound entity IDs. Disable unsolicited IdP-initiated login unless explicitly approved. |
| Runtime/cache | Import metadata asynchronously and publish a validated snapshot. Refresh before expiry with jitter and retain the last-known-good snapshot. New connections can load without restart only if the selected bridge supports dynamic configuration safely; otherwise stage and rolling-restart that bridge, not the OIDC authority. Keep old and new verification certificates during partner rollover. |
| Failure modes | Metadata endpoint unavailable/compromised, expired or prematurely rotated partner cert, clock drift, duplicate response, unknown InResponseTo, oversized XML, mapping that omits stable subject, bridge outage, and logout asymmetry. Fail closed for authentication; retain last-known-good metadata within explicit expiry policy. |
| Tests | Signed/encrypted assertions, negative signature wrapping, audience/destination/recipient, replay, clock boundaries, metadata rollover, SP- and IdP-initiated policy, SLO, attribute mapping, partner-specific staging interop, and tenant isolation. External partner certification cannot be replaced by unit tests. |
| Observability | Connection/entity ID, direction, binding, result/error category, cert fingerprint/expiry, metadata age, replay rejection, and bridge latency. Assertions and mapped sensitive attributes are redacted. |
| Rollout/rollback | Pilot one inbound IdP, then one outbound SP in isolated staging. Publish metadata/cert overlap before traffic. Roll back routing to the old federation endpoint while retaining correlation/audit data. Never change an entity ID in place; introduce a new connection. |
| Support status | **Product/built-in library for inbound:** Sustainsys SP or bridge. **Product/custom for outbound:** IdP-capable bridge. **Unsupported:** representing Sustainsys.Saml2 as an IdP. |

### 4.4 Database-driven external authentication providers

| Concern | Decision |
| --- | --- |
| Architecture | Implement a custom `IAuthenticationSchemeProvider` plus named-options resolver backed by immutable published provider snapshots. Register handler types once; add/remove provider schemes and resolve provider-specific options at runtime. Prefer an external bridge for protocols whose library cannot safely support dynamic options. |
| Data/config | `ExternalProvider`: immutable provider GUID/scheme key, tenant, kind, authority/metadata URL, client ID, secret reference, callback path, scopes, response mode, claim mappings, icon/display/order, login policy, enabled/draining state, revision/ETag, validation result, and timestamps. Scheme names are internal IDs, never operator-editable display names. |
| Vue/API | List by tenant/type/status; wizard for OIDC/OAuth/SAML; metadata discovery; callback URI copy; write-only secret set/rotate/clear; claim mapping simulator; connectivity and nonce/state dry-run; draft diff; approve/publish/rollback; disable/drain/delete. Display only secret presence, version/fingerprint, and last rotation. |
| Security | Allow only approved provider kinds and HTTPS authorities. Protect metadata/JWKS fetches against SSRF, redirects, private/link-local/metadata IPs, DNS rebinding, oversized responses, and weak TLS. Exact callback allowlist, state/correlation/nonce/PKCE, issuer validation, algorithm allowlists, and tenant-bound account linking are mandatory. Never auto-link by unverified email. |
| Runtime/cache | ASP.NET Core supports runtime scheme add/remove. `IAuthenticationHandlerProvider` caches a handler per request and `IOptionsMonitorCache<T>.TryRemove(name)` can invalidate named options. Publish adds the new immutable snapshot before activating it, invalidates node caches through an outbox/pub-sub event, and waits for acknowledgements. In-flight requests keep their captured revision. Disable first; preserve old callbacks for a bounded drain period; delete later. No restart is required after this custom infrastructure exists. Startup-only registrations still require restart. |
| Failure modes | Cache split-brain, stale secret version, provider outage, discovery/JWKS rollover, callback to a removed scheme, invalid claim map, duplicate external account, and publish acknowledged by only some nodes. A publish that lacks quorum remains degraded and can automatically roll back. |
| Tests | Scheme add/update/disable/delete under concurrent requests, options invalidation, multi-node event loss/replay, stale revision, secret rotation, callback drain, SSRF/DNS-rebinding cases, issuer/nonce/state failures, account-linking collisions, and provider outage. |
| Observability | Published revision and node acknowledgement, cache age/hit/miss, discovery/JWKS age, challenge/callback result by provider, secret-ref resolution failure, and external latency. Do not use provider secrets or authorization responses as labels/log fields. |
| Rollout/rollback | Start with OIDC providers only and one pilot tenant. Dual-run static and dynamic registrations under different callback paths. Rollback selects the prior published revision; never reconstruct it from mutable rows. |
| Support status | **Built-in substrate:** ASP.NET Core scheme APIs/options cache. **Custom:** DB loader, lifecycle, cache, propagation, security validation, and UI. **Not built-in:** generic database-driven providers. |

### 4.5 Users, roles, claims, and lifecycle

| Concern | Decision |
| --- | --- |
| Architecture | Retain ASP.NET Core Identity as the canonical local directory. Add tenant partitioning only after the isolation ADR. Separate human admin permissions from token scopes and from tenant business roles. |
| Data/config | Preserve Identity rows and add explicit invitation, credential status, MFA/passkey state, risk state, last-login, lifecycle status, tenant memberships, and version fields as required. Avoid encoding all authorization as global `IdentityRole` rows if tenants can define the same role name. |
| Vue/API | Search/paginate users; view sessions, external logins, claims, tenant memberships, MFA state, lockout, consent, and audit. Invite rather than returning a temporary password. Support disable, unlock, reset link, revoke sessions, require step-up, role assignment, and external-login unlink with confirmation. |
| Security | Prevent self-demotion and removal/disablement of the last authorized platform administrator. Require step-up and two-person approval for platform-admin grants, bulk operations, or credential resets. Use email confirmation only after possession proof. Add rate limiting, breached-password policy where applicable, MFA/passkey policy, and PII minimization. |
| Runtime/cache | User disable/security-stamp changes revoke local sessions and publish invalidation. Role changes do not magically alter already-issued self-contained tokens; use short access-token lifetimes and force refresh/re-authentication for sensitive elevation. |
| Failure modes | Partial role replacement, duplicate normalized identifiers across tenants, email delivery outage, orphaned external login, race with last-admin rule, stale tokens after demotion, and migration hash incompatibility. Use transactions/concurrency tokens and compensating invitation cancellation. |
| Tests | Preserved hash login/rehash, all Identity relationships, tenant uniqueness, invitation expiry, last-admin invariants, concurrent role writes, disable/session revocation, MFA recovery, external unlink, and PII authorization. |
| Observability | Lifecycle changes, invitation outcomes, lockouts, MFA enrollment/recovery, privileged role changes, failed last-admin attempts, and security-stamp invalidations. Never log reset tokens, passwords, authenticator keys, or recovery codes. |
| Rollout/rollback | Import users read-only, authenticate a representative hash sample, then enable lifecycle writes. Keep a source snapshot and ID reconciliation. Rollback routing to IdentityServer4 while preventing writes or replaying an explicitly designed change journal. |
| Support status | **Built-in:** Identity storage/managers/password formats. **Custom:** tenant membership, production lifecycle, permissions, approval, UI, and migration validation. |

### 4.6 Financial-grade security and FAPI

**Selected starting profile:** FAPI 2.0 Security Profile Final, confidential clients, authorization code, authenticated PAR, PKCE `S256`, and mTLS sender-constrained access tokens. This is a recommendation pending the compliance ADR, not a conformance claim.

| Capability | OpenIddict 7.6.0 status | Required action |
| --- | --- | --- |
| Global PKCE | **Built-in** via `RequireProofKeyForCodeExchange()`; already enabled. | Remove `plain` from allowed challenge methods for FAPI clients/profile and prove only `S256` is advertised/accepted. |
| PAR | **Built-in** in 6.1+ via pushed endpoint and `RequirePushedAuthorizationRequests()`. | Add endpoint, client permission, global/profile requirement, and authenticated confidential-client policy. Default request-token lifetime is one hour; FAPI 2 requires `expires_in < 600`, so configure and test a shorter lifetime. |
| Authorization response issuer | **Built-in** `iss` response parameter behavior is present. | Set an explicit immutable issuer and test exact RFC 9207 behavior. |
| Authorization code lifetime | **Built-in configurable**, default five minutes. | FAPI 2 maximum is 60 seconds; configure at most 60 seconds and test reuse rejection. |
| `private_key_jwt` | **Built-in and enabled by default** with client public JWK sets. | Enforce approved algorithms/audience/time/replay policy and prefer this over shared secrets where mTLS client auth is not selected. |
| mTLS client auth | **Built-in** for self-signed and PKI methods. | Configure trusted ingress/SNI aliases, certificate lifecycle, exact client binding, and static issuer. Treat forwarded client-certificate headers as privileged proxy input. |
| mTLS certificate-bound access/refresh tokens | **Built-in** via certificate-bound token options and validation stack. | Enable per selected profile, deploy matching API validation, and run cross-client certificate substitution tests. |
| JAR | **Not stock-supported**; source rejects `request`. | Not required for FAPI 2 core. If FAPI 1 or an ecosystem requires it, select another product or a separately governed protocol project. |
| JARM | **Not stock-supported**; stock response modes are not JWT modes. | Not required for FAPI 2 core. Required for the FAPI 1 code-only branch; use another product if that profile is mandatory. |
| Algorithms | Multiple signing algorithms/credentials are possible. Defaults commonly use RS256. | Limit JWT creation/acceptance to profile-approved PS256, ES256, or permitted EdDSA variant; configure key sizes and reject `none`/weak algorithms. Confirm library/platform algorithm support. |
| Certification | No OpenIddict FAPI certification claim was found in official docs/release/source reviewed. | Run the OIDF FAPI 2 authorization-server and resource-server plans. Complete formal certification if market/regulator requires it. |

| Concern | Decision |
| --- | --- |
| Architecture | Isolate FAPI clients/scopes/resources in an explicit security profile. The interactive FAPI profile is authorization code + authenticated PAR + PKCE `S256`, with mTLS sender constraint first. A client-credentials financial M2M profile may exist separately but is not automatically the FAPI interactive authorization profile. Do not make the current public Vue client a FAPI client; the new BFF is confidential. Sender constraint is end-to-end: AS issues it and every protected API enforces it. |
| Data/config | `SecurityProfile` records allowed grants, client types/auth methods, PKCE method, PAR requirement/lifetime, code/token lifetimes, sender constraint, algorithms, TLS policy reference, scopes/resources, and conformance evidence revision. Clients reference an approved profile rather than independent unsafe toggles. |
| Vue/API | Offer reviewed profiles, not a wall of booleans. Display computed compliance checks, effective metadata, cert/JWK registration, test-client bundle, last conformance run, deviations, and expiry. Publishing a FAPI profile requires security approval. |
| Security | Enforce confidential clients, exact issuer/audience/redirect processing, no hybrid/implicit/password grants, least privilege, no open redirects, TLS 1.2+, approved cipher policy, one-time codes/request URIs, sender constraint, short lifetimes, synchronized clocks, JWT BCP rules, and resource-server revocation validation. FAPI 2 normally avoids AS refresh-token rotation for sender-constrained confidential clients. |
| Runtime/cache | Protocol profile changes are high risk and staged. Clients retain the prior revision until a coordinated activation time. Metadata/JWKS caches and mTLS aliases must converge before activation. Clock synchronization is a production dependency. |
| Failure modes | Partial sender constraint (AS only), mTLS termination losing authenticated certificate identity, stale JWKS, duplicate `kid`, clock skew, wrong PAR/code lifetime, bearer downgrade, algorithm confusion, and profile drift after admin edits. Fail closed for profile clients. |
| Tests | Automated negative matrices plus OIDF conformance. Test two clients/certs for mix-up/substitution, PAR-only flow, S256-only, code/request replay, TLS/cert forwarding, `private_key_jwt` replay/audience, algorithms, issuer, and protected API sender constraint. External OIDF certification remains external evidence. |
| Observability | Profile/client/tenant result, PAR/code/token latency, sender-constraint method, cert thumbprint hash, rejected downgrade/replay/issuer/algorithm categories, time drift, and conformance evidence. Never record proofs, assertions, codes, or tokens. |
| Rollout/rollback | Build a dedicated non-production FAPI cell and resource API first. Pass conformance before onboarding a production client. Rollback disables new client/profile traffic and restores the prior immutable revision; retain both validation keys and both issuer validators for issued-token drain. |
| Support status | **Built-in primitives, custom profile, external proof.** The current roadmap is mTLS-only for sender constraint. FAPI 1 JAR/JARM requires another product or major custom protocol work. |

### 4.7 Signing keys, encryption keys, and rotation

| Concern | Decision |
| --- | --- |
| Architecture | Use a key lifecycle service backed by the selected KMS/Key Vault/HSM. Keep signing and encryption keys separate and tenant/purpose scoped. The service controls state; issuer cells consume an immutable approved key set. Private keys are non-exportable where the provider supports it. |
| Data/config | `KeyDescriptor`: tenant/issuer, purpose, algorithm, provider/key reference and version, public JWK/certificate, `kid`, fingerprint, not-before/not-after, state (`generated`, `prepublished`, `active`, `retiring`, `retired`, `revoked`, `compromised`), approval, activation/retirement times, and audit revision. Never store private key bytes. |
| OpenIddict reality | Multiple credentials are **built-in**. OpenIddict sorts them; valid X.509 keys with the furthest expiry are preferred, future certificates are not preferred, and asymmetric public signing keys are emitted in JWKS. This supports overlap but is not a lifecycle engine. Credentials are supplied through server options. The transaction factory reads `IOptionsMonitor.CurrentValue`, so a custom atomic options-reload source is technically possible; mutating a shared options list in place is unsafe and undocumented as a hot-key API. |
| Vue/API | Generate/import-public/reference-existing key; show only public material and provider reference; request prepublish/activate/retire/emergency compromise; show JWKS propagation probes and dependent clients. Activation/compromise requires separate key-operator and key-approver identities. No endpoint exports a private key. |
| Security | Workload identity accesses private operations; admins do not. Require non-exportable keys, algorithm/size policy, per-purpose separation, HSM audit, quorum approval, and break-glass procedure. A remote cloud signing API needs a compatible `SecurityKey`/signature provider adapter; OpenIddict has no generic built-in KMS signer. HSM-backed local certificate providers may work if exposed through .NET cryptography and load-tested. |
| Runtime/cache | Normal rotation: generate -> prepublish public key -> wait for measured consumer JWKS cache horizon -> activate on all nodes -> retain old signing/validation key through maximum token lifetime + skew + cache horizon -> retire -> later remove. Prefer atomic config reload with acknowledgements only after a proof-of-concept; otherwise use a rolling deployment. Never let nodes choose different active keys merely from wall-clock expiry without coordinated state. |
| Failure modes | KMS outage/latency, active private key inaccessible, partial node activation, duplicate `kid`, wrong algorithm, premature JWKS removal, future key clock error, inability to decrypt old tokens, or compromised key. Issuance should fail closed rather than silently switch to an unapproved key. Discovery may remain available from cached public keys. |
| Emergency rotation | Mark compromised, activate prepared emergency key, publish/shorten JWKS caches where controllable, stop issuing with old key, revoke reference tokens/sessions as policy requires, alert API owners, and retain/remove the old public key according to the compromise response decision. Existing signed JWTs cannot be made cryptographically invalid everywhere without verifier action. |
| Tests | Key-selection boundaries, prepublication, overlapping JWKS, multi-node rolling activation, old-token validation, KMS timeout/rate limit, clock skew, duplicate `kid`, emergency compromise, consumer cache lag, and no-private-material API/log tests. |
| Observability | Active/prepublished key count, expiration lead time, signer latency/error, key version/fingerprint, node revision acknowledgement, JWKS probe age, tokens issued by `kid`, and emergency actions. |
| Rollout/rollback | First replace development certs with operator-provisioned static production credentials. Then add overlap and rolling rotation. Only then automate. Rollback reactivates an uncompromised prior key while it remains accessible and published; compromised keys are never rollback targets. |
| Support status | **Built-in:** multiple credentials/JWKS overlap and selection. **Custom/product:** automatic lifecycle, KMS adapter, hot reload, approvals, propagation, emergency workflow. |

### 4.8 Multi-issuer and multi-tenancy

| Concern | Decision |
| --- | --- |
| Architecture | Default to one issuer cell per tenant or regulated tenant group, with explicit `SetIssuer` and dedicated protocol database/key set. A trusted edge maps an allowlisted SNI/host to a cell. For lower-risk shared deployments, first prove a custom tenant-aware OpenIddict entity/store model and database row-level security; stock OpenIddict EF tables have no tenant key and globally unique client IDs. |
| Issuer immutability | Once tokens are issued, issuer URL is an identifier, not editable branding. Domain/path changes create a new issuer migration with dual validation and client re-registration. Discovery, JWKS, authorization response `iss`, token `iss`, client assertion `aud`, mTLS aliases, and API validators must agree exactly. |
| Data/config | `Tenant`, immutable `Issuer`, allowed public hosts, cell/deployment ID, data partition, key namespace, branding/localization, domain verification, security profile, status, and published revision. Tenant ID is carried in every control-plane object and audit event. |
| Vue/API | Tenant overview, immutable issuer display, domain verification, cell health, key/config revision, branding, enabled features, and migration status. Creating or retiring an issuer is a platform-admin workflow with dry-run, approval, and explicit impact; there is no simple edit field. |
| Security | Reject unknown `Host` before OpenIddict. Configure ASP.NET Core `AllowedHosts`, forwarded-header `AllowedHosts`, `KnownProxies`/`KnownNetworks`, and `ForwardLimit`; the edge replaces rather than appends privileged headers. Never trust public `X-Forwarded-Host` or client-certificate headers. Isolate DB rows, cache keys, queues, Data Protection keys, provider callbacks, subjects, clients, and signing keys. |
| Runtime/cache | Resolve tenant at ingress from an immutable allowlist and attach it once. Published tenant config is cached locally by cell. Do not query tenant routing from a mutable DB on each protocol request. OpenIddict's request-base fallback is disabled by setting the issuer. mTLS aliases remain fixed for the cell. |
| Failure modes | Host spoofing, wrong cell route, cross-tenant cache key, duplicate client ID in shared tables, issuer mismatch, stale discovery, key leakage, tenant deletion with live tokens, and forwarded-header bypass. Default response is reject/404 before protocol handling, never fallback tenant. |
| Tests | Unknown/malformed/punycode host, forged forwarded headers, SNI-host mismatch, cross-tenant client/subject/token/provider access, shared-cache collisions, issuer exactness, discovery/JWKS consistency, mTLS aliases, tenant backup/restore, and noisy-neighbor limits. |
| Observability | Tenant/cell/issuer on every structured event, unknown-host attempts, route mismatches, per-tenant saturation/error budget, config revision, data-partition checks, and cross-tenant authorization denials. Avoid tenant-controlled values as unbounded metric labels. |
| Rollout/rollback | Start with one fixed issuer cell. Add a second synthetic tenant and pass isolation tests before real multi-tenancy. Rollback removes routing to the new cell but retains its DB/keys until all tokens and audit retention expire. |
| Support status | **Built-in:** one static issuer and request-base fallback. **Custom architecture:** safe tenancy. **Unsupported/unsafe:** arbitrary per-request issuer derived directly from Host as multi-tenancy, especially with mTLS aliases. |

### 4.9 Vue administration BFF with YARP

| Concern | Decision |
| --- | --- |
| Architecture | Serve Vue and `/api` from the same BFF origin. BFF is a confidential authorization-code client with PKCE; it owns OIDC redirects, cookie session, access/refresh tokens, and YARP proxying. The browser no longer uses `oidc-client-ts` or sends bearer tokens. |
| Component choice | **Preferred OSS composition:** YARP 2.3.0 (MIT) + `Duende.AccessTokenManagement.OpenIdConnect` 4.2.0 (Apache-2.0) + a small reviewed YARP transform/token-forwarding layer + ASP.NET Core antiforgery. **Alternative:** licensed `Duende.BFF`/`Duende.BFF.Yarp`, which supplies integrated token and antiforgery behavior but requires a production license. Confirm procurement before choosing it. |
| Token storage | Configure a durable encrypted server-side authentication ticket/session store. Store only an opaque high-entropy session identifier in the cookie. Access Token Management can refresh and attach tokens, but `SaveTokens` without a server-side ticket store is insufficient for the stated server-side-token requirement. Namespace and encrypt sessions per environment/tenant; share Data Protection keys safely across BFF replicas. |
| Cookie/CSRF | Use a `__Host-` cookie, `Secure`, `HttpOnly`, `Path=/`, no `Domain`, and normally `SameSite=Lax` for OIDC top-level redirects; evaluate `Strict` only with end-to-end browser tests. Require an antiforgery token or custom non-safelisted header on mutating and proxied API calls, validate `Origin`/`Sec-Fetch-Site`, and do not enable credentialed wildcard CORS. Rotate session on login/privilege change. |
| Data/config | `BffRoutePolicy` exposes logical allowlisted API routes and required token type/scope. Backend destinations are deployment-approved service references, not arbitrary admin-entered URLs. Session policy covers idle/absolute lifetime, refresh behavior, logout/revocation, and concurrency. |
| Vue/API | Vue calls relative URLs with credentials and CSRF header. Session endpoint returns redacted user/permission data, never tokens. Admin may manage route enablement and policy within a preapproved destination catalog; raw YARP clusters, headers, and arbitrary destinations remain platform-controlled to prevent SSRF/confused-deputy abuse. |
| Security | Scope every proxied route, strip inbound authorization/cookie/forwarded headers before constructing downstream requests, prevent path traversal and unrestricted transforms, bind destination and audience, use least-privileged admin token, set CSP/Trusted Types where practical, pin dependencies, and prevent caching of authenticated responses. BFF reduces token exfiltration but does not make XSS harmless; injected script can still act as the user through the BFF. |
| Runtime/cache | Token refresh is serialized per session to avoid refresh races. Route/config revisions are immutable and atomically reloaded. A failed token refresh ends or marks the session reauthentication-required; it never forwards an expired/missing token to a token-required route. |
| Failure modes | Session-store outage, Data Protection key mismatch, refresh race/reuse, authority outage, YARP destination confusion, CSRF bypass, BFF/API timeout, logout revocation failure, and oversized cookie regression. Define fail-closed behavior and a user-visible reauthentication path. |
| Tests | Browser E2E proves no token in Web Storage, JS-readable cookies, DOM, responses, or logs; CSRF/cross-origin negatives; cookie flags; login/logout; refresh concurrency/rotation; server-side session revocation; multi-replica key/session behavior; route audience/scope; SSRF/header stripping; XSS/CSP smoke; and authority/API outage. |
| Observability | BFF session counts, login/refresh/logout outcomes, token expiry bucket without token value, refresh contention, YARP route/destination ID, downstream latency/status, CSRF rejection, session-store health, Data Protection key age, and correlation IDs. |
| Rollout/rollback | Add BFF beside the existing SPA client; register a separate confidential client/callback; run E2E; switch Vue API/auth calls; then revoke and remove the public admin client and `sessionStorage` code. Rollback routes users to the old client only during a short pre-decommission window; never maintain both indefinitely. |
| Support status | **Built-in products:** YARP and Access Token Management. **Custom:** secure composition. **Product:** Duende BFF/YARP integrated option. |

## 5. IdentityServer4 to OpenIddict migration

OwnIdentity is greenfield: it has no embedded legacy database or automatic startup migration. An authorized administrator explicitly starts **Migrate from IdentityServer4** in Identity Admin. The workflow begins with a sanitized schema-v1 inventory-manifest preflight, then later bounded slices may add source-side extraction, durable migration runs, transformation, reconciliation, and apply controls. Preflight validity proves only the manifest contract; it does not prove source truth, migration readiness, or successful data application.

Compatibility means transforming/importing into independent OwnIdentity stores while IdentityServer4 remains side by side for controlled coexistence and rollback. OpenIddict never operates directly on IdentityServer4 tables. Raw source database credentials, client secrets, active grant/token payloads, private keys, and arbitrary source values remain outside Vue responses and rendering. Credentials use separately governed write-only or operator-controlled channels when later migration slices require them.

### Implemented source extraction boundary

Unit 0C adds an operator-run source-side CLI at `tools/OwnIdentity.IdentityServer4Extractor`. It is deployed beside or network-adjacent to the legacy authority; OwnIdentity services never receive its source connection string and have no reference to the extractor or SQL Server client package.

| Boundary | Unit 0C decision |
| --- | --- |
| Supported source | A dedicated SQL Server configuration database matching one of two accepted fingerprints: the canonical `IdentityServer4.EntityFramework` 4.1 schema (exact `dbo` table/column names, sole migration row `20200522172542_Config` / EF product version `3.1.0`) or the Skoruba IdentityServer4.Admin 1.0.0-beta7 schema (IdentityServer4.EntityFramework 2.4 table names, exactly the four `2019061907*` migrations on an EF 2.2 product version, plus Skoruba identity/log/operational tables recorded as quarantined extension evidence in the manifest). Other engines, shared databases, renamed schemas/tables, added/missing columns, other migration histories, and any other Skoruba/custom variant fail closed. Matching a fingerprint does not prove the deployed runtime assembly patch version. |
| Credential input | Exactly one connection-string line through redirected standard input. Arguments and interactive input are rejected; no environment variable, appsettings, file, or repository configuration is read. The tool forces encrypted certificate-validated transport, `ApplicationIntent=ReadOnly`, and a fixed application name. The source principal must still be independently restricted to the required `SELECT`/metadata permissions because application intent does not enforce authorization. |
| Queried data | Schema and migration metadata; client ID, PKCE requirement, and configured grant names; credential row ID/source kind/type only. Fixed SQL never selects secret values, persisted grants, device codes, token payloads, signing keys, certificates, private material, redirect URIs, claims, property values, or operational-store rows. |
| Output | One deterministic schema-v1 sanitized manifest on standard output, capped at the existing 1 MiB preflight boundary. Errors use fixed redacted codes on standard error. ROPC is emitted as an unowned `ropc-blocker`, so validation and process exit `2` force an operator to add real remediation ownership and a non-ROPC target rather than fabricate evidence. |
| Persistence and runtime | The extractor does not write the source or target, persist credentials/results, call IdentityService, create a durable run, or apply/import data. Redirecting stdout is an explicit operator action; the resulting manifest must be handled as migration evidence even though secret values are absent. |

Automation pipes an approved secret provider directly to the process; never place the connection string in the command text:

```powershell
Get-Secret -Name "<approved-reference>" -AsPlainText |
    dotnet run --project "tools/OwnIdentity.IdentityServer4Extractor/OwnIdentity.IdentityServer4Extractor.csproj" --no-build `
    > "<approved-output-path>\identityserver4.inventory.json"
```

Standard input removes the credential from argv, shell history, process listings, environment snapshots, and persisted application configuration. It does not remove plaintext from the producer/extractor process memory or the operating-system pipe. Operators must use an approved secret provider, an isolated host and service identity, a short-lived least-privilege source principal, protected output storage, and process supervision that does not capture standard input.

### Schema and semantic mapping

| IdentityServer4 source | OpenIddict/target | Preserve/transform decision | Validation and caveats |
| --- | --- | --- | --- |
| ASP.NET Identity `AspNetUsers`, `AspNetRoles`, claims, logins, roles, tokens | Same ASP.NET Core Identity tables/custom target directory | Preserve IDs and rows where types/schema match. Copy with explicit column mapping, not `SELECT *`. | Test representative password hashes, normalized values, security stamps, external login keys, token-provider compatibility, lockout timestamps, and role uniqueness. Rehash on successful login if the current hasher marks a compatible hash for upgrade. |
| `Clients.ClientId`, name, enabled, URIs | `OpenIddictApplications.ClientId`, display names, redirect/logout URIs plus target control-plane client | Preserve stable `ClientId` where possible; generate a new OpenIddict row ID and store source integer ID in `LegacyIdMap`. | Exact URI normalization/comparison must be reviewed. Disabled/non-editable state belongs in control-plane policy. |
| Authorization-code clients and response behavior | Standard or FAPI interactive profile; application endpoint/grant/response permissions | Map to code + PKCE, preserving only reviewed redirect URIs and scopes. FAPI clients additionally require authenticated PAR, `S256`, confidential auth, sender constraint, and profile lifetimes. | Require `Authorization`, `Token`, `AuthorizationCode`, and `Code` permissions as applicable; test exact effective behavior rather than copying flags. |
| Client-credentials clients | Machine-to-machine profile; token endpoint and client-credentials grant permissions | Map only confidential clients with typed scopes/resources/audiences. Re-register mTLS or `private_key_jwt`; shared secrets are temporary governed exceptions. Do not map user claims or refresh-token permission by default. | Prove workload `sub`/client identity, audience, scope, no user claims, no refresh token, credential substitution rejection, and API enforcement. |
| Device Authorization clients | Device profile; device authorization/end-user verification/token endpoints and device-code grant permission | Map explicit CLI/TV/input-constrained clients disabled, then onboard after verification-UI, consent, polling/backoff, expiry, replay, phishing, and rate-limit gates pass. Do not copy active device/user codes. Public devices receive no secret. | Require `DeviceAuthorization`, `Token`, and `DeviceCode` application permissions; there is no per-application end-user-verification endpoint permission in OpenIddict 7.6.0. |
| Hybrid and implicit clients | Explicit temporary legacy profiles | Import each client disabled. Activation requires existing-client proof, reason/ticket, security approval, activation/expiry, restricted scopes, telemetry, warning, and migration owner/deadline. Never assign these profiles to a new client. | Both need front-channel leakage analysis and migration to code + PKCE/BFF. Both are FAPI-ineligible. |
| ROPC/password clients | Unsupported migration-blocker record; no executable target profile | Detect and preserve source configuration only as read-only inventory/remediation evidence. Do not assign endpoint/grant permissions, import credentials for execution, or expose activation/approval controls. | RFC 9700 says ROPC **MUST NOT** be used. Cutover is blocked until each dependency migrates to Authorization Code + PKCE, Device Authorization, Client Credentials where no human user exists, or external federation. |
| Custom grants | Import quarantine only | Preserve configuration as evidence, not executable behavior. Require protocol/security review and a separately approved replacement or maintained product before any implementation. | Unknown semantics, handlers, tokens, or permissions are a hard migration gate; never map a custom grant to a superficially similar standard grant. |
| `ClientScopes` | Application scope permissions; `OpenIddictScopes` | Preserve scope names, transform each to permission prefix. | Missing target scope is a hard ETL error. Separate identity claims from API audiences. |
| `RequirePkce`, `AllowPlainTextPkce`, `RequireRequestObject` | Application requirements + security profile | Map PKCE; force `S256` in secure profiles. `RequireRequestObject` has no stock OpenIddict equivalent. | JAR clients require profile/product migration, not a flag copy. |
| `ClientSecrets` (multiple type/value/expiry rows) | OpenIddict single obfuscated `ClientSecret` or client JWK set/mTLS certificate | **Do not copy hashes blindly.** OpenIddict uses its own PBKDF2 representation and one secret field. Issue a new secret through a write-only channel or migrate to `private_key_jwt`/mTLS. | IdentityServer4 SHA-256/SHA-512/shared-secret and X.509 secret validators are not format-compatible with stock OpenIddict storage. A temporary custom validator is possible but expands attack surface and should be time-boxed. |
| Client signing keys/request-object settings | Application `JsonWebKeySet` | Import public keys only after validating ownership, algorithm, use, `kid`, and expiry. | Required for `private_key_jwt`; it does not add JAR support. Never import client private keys. |
| CORS origins | BFF/client web-origin policy | Most browser clients should move to BFF and need no token-endpoint CORS. Preserve only reviewed exact origins where a public client remains. | No wildcards with credentials; normalize scheme/host/port. |
| Client token/session lifetimes, claims, IdP restrictions, properties | OpenIddict application settings/properties plus typed target policy tables | Transform known settings into typed profile fields; quarantine unknown/custom properties. | OpenIddict settings are not a one-to-one contract. Compare effective discovery/tokens/claims, not just rows. |
| `IdentityResources` + claims/properties | OpenIddict scopes plus `ScopeClaimPolicy` | Preserve names/display metadata; store claim release policy separately. | OpenIddict scope rows do not themselves implement the full IdentityServer4 profile/claim service behavior. |
| `ApiScopes` + claims/properties | `OpenIddictScopes`; `Resources` audiences; target scope policy | Preserve scope names. Map linked API resource names into `Resources`; migrate claims to typed release policy. | Verify resulting `aud`, `scope`, and claim destinations at each API. |
| `ApiResources`, scopes, secrets | Target `ApiResource` policy + OpenIddict scope resource names; introspection client applications where needed | Preserve resource/audience names. Convert API secrets into newly registered confidential introspection clients or remove if JWT validation is used. | No direct OpenIddict API-resource table. Re-register secrets. Validate audience behavior and introspection authorization. |
| `PersistedGrants` authorization code/refresh/reference token payloads | `OpenIddictTokens` and `OpenIddictAuthorizations` | **Do not migrate active payloads.** Allow old authority to redeem/drain them or force reauthentication. | Serialization, protection, keying, status, and redemption semantics differ. Copying rows creates invalid or unsafe artifacts. |
| `PersistedGrants` `user_consent` | Permanent/ad-hoc OpenIddict authorization + target consent audit | Default to re-consent. Migrate only if legal/product owners approve the semantic mapping and ETL can reconstruct client, subject, scopes, dates, and status. | Consent evidence may have legal retention requirements. Preserve source archive even when not activated. |
| `DeviceCodes` | OpenIddict tokens of device/user-code types | Do not migrate active codes; drain/expire on old authority. | Values and protected payload are incompatible and short-lived. New device clients re-register and test. |
| IdentityServer4 signing credentials | Target KMS/key descriptors + OpenIddict signing credentials | IdentityServer4 EF does not store signing keys by default. Inventory files, certificate stores, vaults, deployment secrets, and any Skoruba/custom key tables separately. Preserve old **public** keys in overlap JWKS/validators. Reuse private key only after security approval and only if required for same-issuer continuity. | Maintain old token validation until expiry. Key custody and issuer continuity are a separate cutover gate. |
| IdentityServer4 operational cleanup/audit | Target cleanup jobs and immutable audit | Rebuild cleanup for OpenIddict statuses/types; preserve source audit separately. | Do not run source cleanup against target tables. |
| Source primary keys and custom tables | `LegacyIdMap`, import quarantine, typed extension tables | Preserve externally referenced IDs only where required; otherwise map source ID/type to target ID. | Every custom Skoruba/tenant/provider table requires an explicit disposition before migration sign-off. |

### Side-by-side strategy

1. **Initiate and inventory explicitly.** An authorized administrator starts the migration workflow; nothing runs automatically at application startup. Validate a sanitized schema-v1 manifest before any source connection or durable run exists. Later read-only extraction captures exact IdentityServer4/EF/ASP.NET Identity versions, database engine/collation, migrations, custom stores/grants/claims, clients, scopes/resources, CORS, providers, users, persisted-grant counts, key sources, issuer, token lifetimes, and API validators. Classify each client by migration strategy, identify every ROPC dependency as a hard blocker, and assign a non-ROPC remediation owner and target.
2. **Create target databases independently.** Apply OpenIddict/Identity/control-plane migrations to new databases. Never rename IdentityServer4 tables into OpenIddict names.
3. **Build repeatable read-only ETL.** Extract to a versioned canonical manifest, validate/quarantine, transform, load idempotently, and produce row-level reconciliation with source IDs, target IDs, checksums, warnings, and non-migrated reasons. Secret values and private keys are absent from reports.
4. **Preserve directory IDs first.** Rehearse users/roles/claims/logins and representative hash authentication. Keep legacy authority as writer until the directory cutover strategy is chosen; avoid uncontrolled dual-write.
5. **Migrate static protocol configuration.** Load scopes/resources and eligible clients disabled. Preserve ROPC source configuration only in the inventory/remediation ledger; never create an executable target application profile for it. Re-register credentials through write-only channels. Run metadata and protocol contract tests per eligible client.
6. **Use a new issuer for coexistence when possible.** APIs temporarily trust an explicit allowlist of old and new issuers/audiences. Migrate clients in waves. Old grants remain redeemable only at the old authority; new grants are created only at the new authority.
7. **If the issuer URL must remain unchanged, schedule a controlled authority cutover.** Prepublish target/old public keys as required, lower DNS/cache TTLs, stop configuration writes, run final ETL, switch trusted edge routing, and retain the old service dark/read-only for rollback. Active opaque/reference grants still cannot move; plan reauthentication or a bounded compatibility proxy.
8. **Observe and drain.** Compare login/token/refresh/API failures, key discovery, and client traffic by source authority. Cutover and decommission require zero unresolved ROPC dependencies, then completion of maximum old grant/token/session lifetimes, contractual rollback period, and audit retention gates.

### Rollback boundaries

- **Before target token issuance:** route back to IdentityServer4 and discard/rebuild target protocol data.
- **After target issuance under a new issuer:** route affected clients back while APIs continue accepting both issuers until target tokens expire; preserve target DB/keys.
- **After target issuance under the same issuer:** rollback is not a database restore. The old authority must publish/validate required target keys or APIs must retain them, and target grants/tokens remain target-owned. This is the highest-risk strategy.
- **After directory writes move:** rollback requires a tested reverse change journal or a write freeze. Database snapshots alone lose acknowledged user/admin changes.

## 6. Vue admin information architecture and permissions

### Navigation

| Area | Capabilities |
| --- | --- |
| Overview | Cell/tenant health, unpublished changes, rollout acknowledgements, key/provider expiry, SLO and conformance alerts. |
| Directory | Users, invitations, external logins, MFA/passkeys, sessions, consents, tenant memberships, roles, role permissions. |
| OAuth/OIDC | Applications, credentials/public keys, redirect/logout URIs, reviewed client/security-profile assignment, scopes, resources/audiences, claims, sender constraints, active authorizations/tokens, and migration status. Grants, client authentication, and sender constraints are not independent toggle lists. |
| Federation | Upstream OIDC/OAuth IdPs, upstream SAML IdPs, downstream SAML SPs, metadata, claim mappings, partner certificates, test results. |
| Tenants and issuers | Immutable issuer/cell, verified domains, branding, isolation, enabled features, migration state. |
| Keys and secrets | Public JWKS/certificates, vault references, expiry, rotation state, pending approvals. No secret/private-key read path. |
| BFF and API access | Logical routes, required scopes/token type, session policy, allowed destination catalog. No arbitrary proxy destination editor. |
| Operations | Drafts, validations, diffs, approvals, publishes, node acknowledgements, rollback, migration runs, conformance evidence, audit export. |

### Permission model

Replace the single `Administrator` role with permissions scoped to platform or tenant. Initial permissions include `platform.tenant.manage`, `tenant.settings.manage`, `identity.user.read`, `identity.user.lifecycle`, `identity.role.manage`, `protocol.client.read`, `protocol.client.manage`, `protocol.credential.rotate`, `protocol.scope.manage`, `federation.manage`, `key.operate`, `key.approve`, `config.publish`, `audit.read`, and `session.revoke`. Roles are bundles, not authorization checks hard-coded in controllers.

Rules:

- Platform administrators do not automatically read tenant PII unless explicitly assigned.
- Key operator and key approver are separate; the initiator cannot approve activation/compromise.
- Secret rotation, issuer creation/retirement, platform-admin grants, FAPI-profile changes, outbound SAML trust, and bulk user actions require step-up and approval.
- Every mutating API uses optimistic concurrency (`ETag`/row version). A stale write returns `409`/`412` with a redacted diff.
- Draft validation includes schema, referential, URI, SSRF, algorithm, collision, dependency, and protocol-profile checks. Dry-run never contacts arbitrary targets from the web request; a restricted worker performs approved probes.
- Publish creates an immutable revision and audit event. Rollback republishes a previous compatible revision as a new event; history is never edited.
- Secret fields are write-only. Responses expose `isConfigured`, provider, reference alias/version, fingerprint, and timestamps. Private key material has no upload/read endpoint unless a narrowly governed one-time import ceremony is explicitly selected; normal operation generates keys inside KMS/HSM.
- Client creation starts from a reviewed profile: standard code + PKCE, FAPI 2 interactive, machine-to-machine, or Device Authorization. The profile computes compatible client type, endpoints, grant/response permissions, client authentication, PKCE/PAR, refresh policy, token lifetimes, scopes/resources/audiences, and mTLS policy; administrators cannot construct unsafe combinations from independent switches.
- Hybrid and Implicit profiles appear only on imported existing clients and cannot be assigned to a newly created client. Activation requires reason/ticket, security approval, activation and mandatory expiry dates, restricted scopes/resources, owner and migration deadline, a prominent risk warning, and telemetry. Expired profiles fail closed and cannot be renewed without a new approval record.
- Imported ROPC clients appear only in a read-only **Migration blockers** view with preserved evidence, remediation owner/status, and guidance for Authorization Code + PKCE, Device Authorization, Client Credentials without a human user, or external federation. Vue exposes no password-grant enable, approval, activation, expiry, or publish control.
- The current **Sender constraint** selector offers mTLS only where the selected profile supports it. DPoP is not selectable; at most, Vue may display a read-only **Future/unavailable** status linked to section 11.

### Configuration that must remain outside Vue

The requirement that relevant runtime configuration be manageable through Vue is accepted with a trust-root exception. Database connection credentials, workload identities, vault/KMS/HSM administrator policy, trusted proxy/network roots, edge mTLS/header configuration, break-glass identities, audit-store retention/immutability, initial platform-admin bootstrap, and executable/package deployment are infrastructure configuration. Vue may show their health/reference but cannot rewrite its own roots of trust.

## 7. Threat model and controls

| Threat | Principal controls |
| --- | --- |
| Host/forwarded-header issuer injection | Explicit static issuer, edge host/SNI allowlist, ASP.NET `AllowedHosts`, forwarded-header allowlist and known proxies/networks, reject unknown host before protocol middleware, no fallback tenant. |
| Cross-tenant access/data leakage | Issuer cells, tenant-scoped authorization, DB isolation/RLS or separate DB, tenant in cache/queue/audit keys, per-tenant keys/provider callbacks, automated cross-tenant negative tests. |
| SSRF through metadata, JWKS, SAML, or YARP | Approved HTTPS schemes/domains/ports, DNS/IP validation before and after connect, block private/link-local/metadata ranges unless explicitly private-managed, egress proxy/firewall, redirect limits, size/time limits, pinned logical YARP destinations. |
| Open redirect and callback abuse | Exact registered redirect/logout/callback URIs, no fragments/wildcards, loopback exception only for native clients, validated return URLs, tenant-bound callback state. |
| Provider/SAML confused deputy and account takeover | Issuer/entity/audience/destination/recipient validation, state/nonce/InResponseTo/replay, client/tenant binding, no auto-link by unverified email, explicit linking while authenticated, stable subject mapping. |
| Browser token theft | Same-origin BFF, no browser tokens, HttpOnly/Secure host-only cookie, server-side sessions, CSP, dependency hygiene. XSS still permits same-session actions, so CSRF/authorization and output encoding remain required. |
| Hybrid/implicit front-channel leakage | Disabled-by-default legacy profiles, no new clients, exact response-type permission, fragment/form-post and redirect/referrer/history/log analysis, shortest viable token lifetime, no implicit refresh token, leakage telemetry, and enforced migration expiry. Prefer code + PKCE/BFF. |
| ROPC credential theft or assurance bypass | Globally omit password-flow server configuration and application permissions; reject `grant_type=password`; expose no assignment, publish, approval, exception, adapter, or activation path; keep imported evidence read-only; block cutover while any dependency remains; and redact credentials at every telemetry/support boundary. RFC 9700 says ROPC **MUST NOT** be used. |
| Device-code phishing, polling abuse, and replay | Explicit per-client profile, recognizable user-code UI, requesting-client/scope confirmation, authority-session binding, consent, one-time codes, short expiry, polling interval/`slow_down`, per-client/device/IP limits, replay cache, and no embedded secret for public devices. |
| M2M user impersonation or audience confusion | Confidential clients only, asymmetric client authentication preferred, typed scope/resource/audience policy, client/workload subject semantics, no user claims or refresh token by default, credential substitution tests, and API-side audience/scope enforcement. |
| CSRF/CORS | SameSite cookie, antiforgery token or required custom header, exact Origin/Fetch Metadata validation, deny credentialed cross-origin access, protect login/logout and all mutating/proxied APIs. |
| Token/code/proof replay | Short one-time codes/request URIs, token/replay storage, mTLS sender constraint where required, assertion `jti`/time validation, and idempotency keys for financial actions. |
| Bearer downgrade/partial sender constraint | Profile-driven client registration; `cnf` issuance and API enforcement; reject Bearer at protected FAPI resources; integration tests spanning AS and RS. |
| Secret leakage | Vault references, write-only UI/API, redaction, no secrets in config/audit/telemetry, short-lived workload identity, scoped vault access, secret scanning, one-time credential display only when unavoidable. |
| Signing-key compromise | Non-exportable purpose/tenant keys, approval separation, pre-staged emergency key, overlap, signer audit, rapid stop-issue/revoke/notify runbook, verifier coordination. |
| Privilege escalation through admin | Fine-grained tenant permissions, deny by default, step-up, dual approval, last-admin invariant, immutable audit, no direct DB operations, concurrency controls, separate break-glass monitoring. |
| Malicious configuration publish | Typed profiles, validation/dry-run, diff, approvals, signed immutable revision, staged canary, node acknowledgements, automatic halt/rollback thresholds. |
| Migration corruption or privilege drift | Read-only source, repeatable manifests/checksums, quarantine, reconciliation, representative login/protocol tests, no active-grant copy, reversible waves, dual issuer validation. |
| Availability attack/noisy tenant | Per-tenant/client/IP rate limits, bounded queues/caches, circuit breakers, cell isolation, bulkhead provider fetches, capacity SLOs, safe degraded discovery from cached public data. |

Security-sensitive logs must never contain passwords, client/provider secrets, private keys, cookies, authorization codes, access/refresh/identity tokens, full SAML assertions, reset links, recovery codes, or unbounded claims.

## 8. Test and conformance strategy

### Automated in this repository

| Layer | Scope and suggested harness |
| --- | --- |
| Unit | Policy/profile validation, URI/host/SSRF rules, claim mapping, permission evaluation, lifecycle transitions, ETL transforms, last-admin invariant, cache keys, and redaction. Keep xUnit for .NET and Vitest for Vue. |
| Persistence integration | Real target database engine through an isolated test instance; migrations up/down where supported, Identity/OpenIddict managers, concurrency, tenant partition/RLS, outbox/idempotency, cleanup, and server-side BFF sessions. SQLite is insufficient evidence for the production engine. |
| Protocol integration | `WebApplicationFactory`/real HTTPS test host with OpenIddict client: discovery/JWKS, every approved and time-bounded legacy flow below, password-grant prohibition invariants, per-client endpoint/grant/response permissions, disabled-by-default behavior, code + PKCE S256, PAR, refresh, logout, consent, private-key JWT, mTLS aliases/cert binding, negative redirects/issuer/audience, lifetimes, replay, and key overlap. Use at least two clients and credentials to prove isolation. |
| Federation | Signed/encrypted SAML fixtures and local partner harness; OIDC provider stub; metadata/JWKS/cert rotation; malformed XML/JWT; replay and clock boundaries; claim/account-linking cases. Partner staging interop remains required. |
| Browser E2E | Playwright against Aspire: Vue+BFF login/logout, cookie/CSRF flags, permissions, draft/publish/rollback, no tokens in Web Storage/DOM/responses, provider flow, concurrent refresh, session revoke, CSP/XSS smoke, and accessibility. |
| Migration rehearsal | Sanitized production-shaped snapshot -> extract/transform/load -> reconciliation -> representative user login -> client protocol contract suite -> rollback. Repeat until deterministic and within measured cutover constraints. |
| Chaos/failure | Kill a replica during publish/key rotation, drop pub-sub events, stale cache, DB/session/vault/KMS/provider outage, JWKS lag, clock skew, cert rollover, back-channel dead letter, and partial edge route. Assert fail-closed and recovery behavior. |
| Security | SAST/dependency/secret scans, authorization matrix, SSRF/open redirect/Host/header attacks, CSRF/CORS, XML signature wrapping, JWT algorithm confusion, replay, tenant isolation, mass assignment, rate limits, and audit tamper checks. Use OWASP ZAP only against an isolated environment with approved test data. |
| Performance | k6 or equivalent for discovery/JWKS, authorize/login, PAR/token, admin list/search, BFF proxy, session store, and signer/KMS latency. Include rotation/cache churn and per-tenant noisy-neighbor scenarios. |

### Flow-policy end-to-end evidence

| Profile | Positive evidence | Mandatory negative evidence |
| --- | --- | --- |
| Authorization code + PKCE | Public and confidential clients complete code + `S256`; consent, user authentication method, client authentication, audience/scope, and refresh behavior match the assigned profile | Missing/wrong verifier, `plain` where forbidden, code replay/substitution, wrong redirect/client/audience, unassigned endpoint/grant/response/scope, and disabled client are rejected |
| Client credentials | Confidential workload obtains an audience- and scope-limited access token using mTLS or `private_key_jwt`; token and API expose client/workload subject semantics without user claims | Public/unauthenticated client, shared-secret use outside an approved exception, wrong credential/audience/resource/scope, delegated-user claim, refresh request/token, and cross-client certificate/JWK substitution are rejected |
| Device Authorization | Approved client obtains device/user codes; separate authenticated user confirms client/scopes and consents; compliant polling reaches one-time token issuance | Unapproved client/endpoint/grant, embedded secret expectation for public device, phishing/session mix-up, wrong/replayed/expired code, cross-client redemption, early/rapid polling without `authorization_pending`/`slow_down`, rate-limit bypass, and consent denial are handled safely |
| Hybrid legacy | One imported, unexpired, approved client completes only its exact response type and emits telemetry | New/default/expired/unapproved client, unpermitted hybrid response, FAPI assignment, code/token replay, and front-channel code/token leakage probes fail; migration warning and deadline remain visible |
| Implicit legacy | One imported, unexpired, approved client completes only its exact response type with shortest viable lifetime and telemetry | New/default/expired/unapproved client, refresh/offline access, excess lifetime, wrong audience/client, FAPI assignment, and fragment/form-post/history/referrer/log token leakage are rejected or proven absent as applicable |
| ROPC/password prohibition invariant | Inventory imports preserve only redacted blocker evidence and remediation status; no executable client profile or credential is created | Server configuration never enables password flow; no application can be assigned or publish password-grant permission; every token request with `grant_type=password` is rejected; credentials never appear in logs, traces, metrics, audit, errors, or support output; migration and cutover fail while any ROPC dependency remains |

Run a generated permission matrix for every profile proving an application receives only its required endpoint, grant, response-type, scope, resource, and audience permissions. The suite must prove the password grant is globally unavailable and cannot be assigned or published. It must first prove Client Credentials, Device Authorization, Hybrid, Implicit, and refresh are unavailable in the current baseline, then prove each approved target or time-bounded legacy capability remains disabled until its server feature, endpoints, application profile, and permissions are deliberately activated.

### Commands and package convention

All NuGet package installation, update, restore, listing, and audit operations **must use NuGet/.NET CLI**. Never instruct contributors to hand-edit `<PackageReference>` elements or use IDE package-management UI. Examples:

```powershell
dotnet restore "OwnIdentity_openDict.sln"
dotnet list "src/OwnIdentity.IdentityService/OwnIdentity.IdentityService.csproj" package --include-transitive
dotnet list "src/OwnIdentity.IdentityService/OwnIdentity.IdentityService.csproj" package --outdated
dotnet list "src/OwnIdentity.IdentityService/OwnIdentity.IdentityService.csproj" package --vulnerable --include-transitive
dotnet add "<project.csproj>" package <PackageId> --version <approved-version>
dotnet test "tests/OwnIdentity.IdentityService.Tests/OwnIdentity.IdentityService.Tests.csproj" --no-restore
dotnet run --project "IdentityProjectsHost/IdentityProjectsHost.csproj"
```

The frontend requires Bun 1.3.14, declared by `packageManager`, while retaining Vue, Vite, and Vitest. CI, Aspire, onboarding, and IDE builds use the committed `bun.lock` with `--frozen-lockfile`; intentional dependency updates omit that flag and regenerate the lockfile.

```powershell
bun install --cwd "src/ownidentity-admin" --frozen-lockfile --ignore-scripts
bun run --cwd "src/ownidentity-admin" dev
bun run --cwd "src/ownidentity-admin" type-check
bun run --cwd "src/ownidentity-admin" test:unit -- --run
bun run --cwd "src/ownidentity-admin" build
bun audit --cwd "src/ownidentity-admin"
```

`src/ownidentity-admin/bunfig.toml` sets `[install] ignoreScripts = true`, prohibiting root and dependency `preinstall`, `install`, and `postinstall` execution even outside the commands above. Do not add `trustedDependencies` or lifecycle-script exceptions without a future explicit security decision. A dependency that requires an install script is incompatible with this policy. Script blocking is defense in depth, not a trust decision: packages, transitive dependencies, and Vite plugins still require review, lockfile inspection, vulnerability auditing, and normal application-security controls.

Add test packages only after a work unit approves them, for example with `dotnet add "tests/...csproj" package Microsoft.AspNetCore.Mvc.Testing`; record the resolved graph using `dotnet list ... package --include-transitive` and audit it using `dotnet list ... package --vulnerable --include-transitive`.

### External evidence

- The OpenID Foundation FAPI conformance suite is external protocol evidence. Automate environment provisioning and evidence capture where possible, but do not replace it with internal tests.
- Formal OpenID certification, regulator/ecosystem onboarding, penetration tests, SAML partner certification, HSM assurance, and disaster-recovery exercises require external/operator participation.
- Passing a library's unit tests or an internal happy path does not authorize the phrase "FAPI compliant" or "certified."

## 9. Observability, SLOs, and audit

### Signals

- **Protocol:** request count/latency/result by endpoint, tenant/cell/client/profile, grant, response type, user-auth method, client-auth method, and sender constraint; categorized OAuth errors; permission/default-deny rejection; PAR/code/device-code/user-code/token reuse rejection; device polling/`slow_down`/expiry/rate limit; mTLS enforcement result; rejected password-grant attempts; signer `kid`.
- **Legacy flow and blockers:** Hybrid/Implicit activation and expiry, owner/ticket, last use, client/scope, front-channel leakage detections, migration-deadline alerts, unresolved ROPC dependency count, and rejected `grant_type=password` attempts. Never attach passwords, codes, or tokens. Expiry, unexpected use, password-grant requests, and unresolved cutover blockers page security operations.
- **M2M and device:** Client-credentials audience/resource/scope and workload-subject mismatch, credential-auth method, shared-secret exception age, device verification/polling/replay/phishing category, consent result, and abandoned/expired transactions using bounded labels.
- **Identity:** login/logout/consent/MFA/invitation/session outcomes and lockout/rate-limit events.
- **Federation:** provider/connection result, metadata/JWKS age, certificate expiry/fingerprint, replay rejection, callback latency, bridge health.
- **Control plane:** draft validation, approval time, publish/rollback, node acknowledgement/lag, stale revision, outbox/dead letters, audit-write health.
- **Keys/secrets:** expiry lead time, lifecycle state, KMS signer latency/error/rate limit, vault reference resolution, JWKS probe convergence.
- **BFF:** active sessions, login/refresh/logout, refresh serialization contention, CSRF rejection, route/downstream latency, session-store and Data Protection health.
- **Tenancy/capacity:** unknown hosts, route mismatch, cross-tenant denial, per-cell saturation, DB/cache/queue pool usage, noisy-neighbor limits.

Use OpenTelemetry traces, metrics, and structured logs with correlation IDs propagated through BFF, control plane, issuer, bridge, and APIs. Keep metric label cardinality bounded; hash or map external IDs where needed. Sample successful high-volume paths, never security failures or audit events.

### Candidate SLOs requiring operator approval

These are starting objectives, not contractual promises:

| Service indicator | Candidate objective |
| --- | --- |
| Discovery/JWKS and token data plane availability | 99.95% monthly per production issuer cell, excluding approved maintenance. |
| Interactive authorization success availability | 99.9% monthly excluding upstream IdP/user-decision failures; report upstream failures separately. |
| Token endpoint latency | p95 < 300 ms and p99 < 750 ms excluding external KMS/network outliers; establish real baselines before commitment. |
| BFF proxied admin API | 99.9% monthly; p95 overhead < 100 ms excluding downstream API time. |
| Critical config propagation | 99% of healthy nodes acknowledge within 60 seconds; no publish marked complete without required quorum. |
| Key readiness | Alert 60/30/14/7 days before expiry; at least one approved emergency signing key or documented provider equivalent available. |
| Audit durability | 100% of privileged mutations have an audit event; if durable audit write fails, privileged mutation fails closed. |
| Recovery objectives | RPO/RTO are unresolved deployment ADRs; prove them with restore/failover exercises before production. |

### Audit record

Write an append-only, access-controlled, retention-governed event containing UTC time, actor human/workload ID, authentication method/step-up, tenant, permission, action, target type/ID, request/correlation ID, source network context, before/after redacted hashes and safe diff, draft/published revisions, approval actors, result, reason/ticket, and node rollout state. Secret changes record reference/fingerprint/version only. Key events record public fingerprint and state only. Export to an immutable/WORM-capable sink selected by operators and continuously alert on audit pipeline failure.

## 10. Implementation roadmap by reviewable work unit

### Global implementation conventions and acceptance gate

Every work unit is behavior-complete: implementation, focused tests, operational/runtime harness, observability, security notes, migration/rollback instructions, and user/operator documentation land together. Each unit has one independently reversible behavior boundary.

For every unit:

- Record exact focused test commands and results.
- Record an Aspire/runtime scenario and result, or explicit `N/A` when no runtime boundary exists.
- State the exact rollback behavior/data boundary.
- Run relevant authorization, tenant-isolation, redaction, and failure-path tests.
- If NuGet changes are needed, use only `dotnet add <project> package ...` or the appropriate NuGet/.NET CLI command. Never manually edit a `.csproj` package reference and never use IDE package-management UI.
- After any NuGet change, run `dotnet restore`, `dotnet list <project> package --include-transitive`, and `dotnet list <project> package --vulnerable --include-transitive`; archive outputs with the work-unit evidence. Resolve or explicitly accept version drift and advisories before merge.
- Keep authored code, tests, docs, migrations, and package changes for one behavior in the same review unit.

Relative effort uses **S/M/L/XL** and risk uses **Low/Medium/High/Critical**; these are comparison tools, not calendar estimates.

| Unit | Deliverable and dependencies | Acceptance gate | Rollback boundary | Effort / risk |
| --- | --- | --- | --- | --- |
| 0. Legacy inventory and ADR closure | Offline validation, stateless Admin preflight, and source-side SQL Server/IdentityServer4 EF 4.1 configuration extraction are implemented as bounded slices; real source evidence, remaining inventory domains, ownership, and ADR closure remain. No dependency. | Every source table/custom extension/credential/key has disposition; every ROPC client is a hard blocker with owner and non-ROPC migration target; unresolved ADRs have owners; no secret values in artifact. | Delete generated inventory and remove the independent tools/UI slices; no source or target data rollback. | M / High |
| 1. Production persistence and migration runner | Select DB engine; separate control/protocol/session/audit stores as decided; gated migration job; health/readiness. Depends 0/DB ADR. | Real-engine integration tests, backup/restore rehearsal, rolling-compatible migration proof, package gate. Startup replicas no longer race migrations. | Restore previous app/schema within tested backward-compatible migration boundary. | L / High |
| 2. Fixed issuer and production trust roots | Explicit issuer, host/forwarded-header policy, production signing/encryption references, cookie/TLS baseline, proxy contract. Depends 0/1/topology/vault ADRs. | Unknown Host/header tests, production-like startup, discovery/JWKS exactness, old-token validation plan, key material absent from config/log. | Route back to prior non-production authority; production launch blocked until valid keys. | L / Critical |
| 3. Permission and audit foundation | Fine-grained permissions, tenant scope, step-up hooks, append-only audit, optimistic concurrency, redaction. Depends 1/2. | Authorization matrix and audit fail-closed tests; last-admin invariant; every mutation has redacted audit. | Disable new mutation endpoints; preserve audit records and old read path. | L / Critical |
| 4. Versioned config publication | Draft/validate/diff/approve/publish/outbox/node acknowledge/rollback framework. Depends 1/3. | Concurrent edits, failed validation, event loss/replay, quorum, canary and rollback tests. | Republish prior compatible revision; data-plane keeps last-known-good. | XL / Critical |
| 5. Vue BFF migration | Same-origin BFF, server-side sessions, YARP allowlisted proxy, CSRF, relative Vue API, removal of browser token ownership. Depends 2/3. | Browser proves no token exposure; cookie/CSRF, refresh concurrency, multi-replica session, proxy header/audience/SSRF tests; license decision recorded. | Route temporarily to old public client before its credential is revoked; BFF data isolated. | L / High |
| 6. Directory lifecycle | Production user/role/search/invite/disable/MFA/session operations and Vue screens. Depends 3/5. | Identity hash compatibility, PII permissions, last-admin, concurrency, invitation and session-revoke tests. | Disable writes, retain read-only directory; reverse journal if cut over. | L / High |
| 7. OIDC profile and permission foundation | Reviewed profile schema/UI/API for client type, endpoints, grants/response types, client auth, PKCE/PAR, refresh, scopes/resources/audiences, lifetimes, and mTLS sender constraint. Depends 3/4/5. | Generated allow/deny matrix proves exact permissions and disabled-by-default behavior; unsafe combinations and password-grant permissions cannot be assigned or published; mTLS is the only current sender-constraint choice; secrets remain write-only. | Republish prior config; disable newly created/profile-changed clients. | XL / Critical |
| 8. Client Credentials M2M profile | Confidential-only token flow, mTLS/`private_key_jwt` first, typed scope/resource/audience policy, shared-secret exception lifecycle, workload claims, protected test API. Depends 7 and credential/key readiness from 2/14 as applicable. | Positive/negative E2E proves authentication, subject/audience/scope correctness, no user claims or refresh token by default, cross-client credential rejection, rate limits, and API enforcement. | Disable M2M profile assignments and endpoint/grant permissions; drain issued tokens without bearer downgrade. | L / High |
| 9. Device Authorization profile | Device and verification endpoints, user-code UI, consent/session binding, polling/backoff/expiry/rate limits, public-client no-secret policy, Vue profile controls. Depends 6/7 and session/consent readiness. | CLI/TV harness passes positive flow plus phishing, session/client mix-up, replay, expiry, wrong code, `authorization_pending`, `slow_down`, rate-limit, permission, and default-deny negatives. | Disable profile assignments and device endpoint permission; allow issued short-lived tokens to drain; retain transaction/audit evidence. | L / Critical |
| 10. Hybrid/Implicit legacy flow quarantine | Import-only Hybrid and Implicit profiles with ticket/approval/activation/expiry/restricted scopes/warnings/telemetry/migration target. Depends 0/3/4/7 and explicit security acceptance. | No new client can receive a legacy profile; expired/default clients fail; exact response/grant permissions, front-channel leakage, and FAPI-ineligibility gates pass. ROPC is absent from executable profiles. | Disable the individual imported client/profile and route it back to IdentityServer4 during its approved coexistence window; never broaden the profile. | L / Critical |
| 11. Dynamic upstream OIDC providers | DB model, secure scheme/options loader, cache propagation, Vue wizard/dry-run/drain. Depends 4/5. | Concurrent reload, multi-node convergence, provider/JWKS outage, SSRF, callback drain, account-linking tests. | Revert revision or route login to static provider during bounded transition. | XL / Critical |
| 12. SAML inbound pilot | Selected SP/bridge, metadata snapshot, mappings, cert rollover, Vue upstream screen. Depends SAML/vault ADRs and 4/6. | Negative XML/signature/replay/audience tests and one partner staging sign-off; license/support recorded. | Disable connection and route partner to legacy federation endpoint. | L / Critical |
| 13. Outbound SAML bridge | Separately deployed IdP-capable bridge/product, downstream SP management and lifecycle. Depends SAML-direction/product ADR and 4/12 learnings. | Product support/license, partner interop, metadata/cert overlap, SLO/DR, negative protocol suite. | Route SP metadata/traffic back to legacy IdP; preserve entity/key overlap. | XL / Critical |
| 14. Key lifecycle v1 | Static KMS-backed credentials, descriptors, public JWKS overlap, expiry alerts, operator runbook. Depends vault/topology ADR and 2/4. | Old/new token validation, KMS failure, no private read path, multi-node rolling deployment, emergency drill. | Reactivate prior uncompromised key while published; never rollback to compromised key. | XL / Critical |
| 15. Key automation | Generate/prepublish/activate/retire scheduler, approvals, atomic reload or rolling controller, propagation probes. Depends 14. | Repeated accelerated rotation/chaos, consumer-cache horizon, duplicate `kid` and partial activation tests. | Pause automation and operate v1 manual lifecycle. | XL / Critical |
| 16. Second issuer cell and isolation | Cell template, trusted routing, tenant data/key/cache isolation, Vue tenant health. Depends tenancy/topology/DB ADRs and 2-4/14. | Full cross-tenant negative suite, host/SNI/forwarded headers, backup/restore and noisy-neighbor tests. | Remove edge route; retain isolated cell DB/keys until drain/retention. | XL / Critical |
| 17. FAPI 2 mTLS interactive profile | Authorization code, authenticated PAR, S256-only, <=60s code, <600s request URI, approved algorithms, confidential auth, mTLS-bound tokens, protected test API. Depends compliance ADR and 7/14/16 as applicable. Client Credentials, if needed for financial M2M, remains a separate profile. | Internal negatives and OIDF FAPI 2 AS/RS suite pass; Hybrid and Implicit are ineligible; every password-grant request is rejected; no conformance claim before evidence approval. | Disable profile clients and restore prior revision; validators retain issued keys/tokens through drain. | XL / Critical |
| 18. ETL and coexistence | Canonical manifest, idempotent loaders, explicit standard/M2M/device/Hybrid/Implicit/custom-grant mappings, read-only ROPC blocker evidence, reconciliation, dual-issuer API validation, wave dashboard. Depends 0, 6-10, and applicable destination features. | Sanitized production-scale rehearsals, user hash/client flow contracts, no silently accepted/dropped grant, no executable ROPC target profile, zero unexplained row drift, credential re-registration complete. | Re-run from source snapshot before writes; route eligible wave back while dual validation remains. | XL / Critical |
| 19. Production cutover and decommission | Final sync/freeze, edge/DNS switch, monitoring, grant/session drain, legacy-profile sunset, archive and destruction plan. Depends every required feature and all ADR gates. | Go/no-go evidence, rollback drill, error budget green, old/new key/issuer validation, no expired exception, zero unresolved ROPC dependencies, password grant globally unavailable, owner sign-off. | Follow issuer-specific rollback boundary in section 5; never improvise a DB swap or reactivate an expired legacy profile. | L / Critical |

## 11. Deferred future capability: DPoP

DPoP is explicitly outside the current implementation roadmap, Admin configuration, acceptance gates, dependencies, FAPI choice, and delivery estimates. It is a sender-constraint profile, not an OAuth grant. Current profiles use mTLS wherever sender constraint is required, and stock OpenIddict 7.6.0 does not provide complete DPoP support.

Any later DPoP initiative requires a new product and architecture decision. It must either select a maintained product or own a complete authorization-server, client, and resource-server implementation covering `dpop_jkt` authorization-request binding; proof JWT validation (`typ`, `alg`, public `jwk`, method, URI, time, and unique `jti`); authorization-code, access-token `cnf.jkt`, and refresh-token binding; resource-request `ath`; nonce and replay handling; discovery metadata and standard errors; bounded clock skew; key lifecycle; downgrade prevention; sensitive-proof redaction; and end-to-end conformance. A token-only `cnf.jkt` shortcut is incomplete and unsafe.

## 12. Architecture decisions still requiring product/operator input

Create one ADR for each before its dependent work unit starts:

1. **Secret and key provider:** Azure Key Vault/Managed HSM, AWS KMS/Secrets Manager, HashiCorp Vault, on-prem HSM, or other; non-exportability, remote-signing adapter, workload identity, region/DR, cost, and audit.
2. **Tenant isolation:** database/deployment per tenant, schema per tenant, or shared tables with custom tenant-aware OpenIddict entities and RLS; client-ID uniqueness and operational scale.
3. **SAML directions and product:** exact inbound partners, exact outbound SPs, IdP-initiated/SLO requirements, selected bridge/library, support contract, and license. Do not buy/build outbound capability if only inbound is required.
4. **Compliance target:** FAPI 2.0 Final vs FAPI 1.0 Advanced vs named ecosystem profile; mTLS as the current sender constraint; certification/regulator evidence; message signing; required algorithms and assurance level. DPoP is not a choice for this roadmap.
5. **Migration downtime/coexistence:** new issuer vs same issuer, acceptable user reauthentication, active grant drain, configuration/user write freeze, reverse journal, rollback duration, and DNS/edge ownership.
6. **Database engine:** production engine, HA/backup/restore, RLS capability, EF migration ownership, encryption, collation/case behavior, and retention. SQLite remains local-development only.
7. **Deployment topology:** issuer cells/replicas/regions, trusted edge and mTLS termination, BFF/control-plane placement, bridge isolation, session/cache/pub-sub, data residency, RPO/RTO, and active-active constraints.
8. **BFF commercial boundary:** OSS YARP + Apache Access Token Management + custom integration, or licensed Duende BFF/YARP; support expectations and redistribution terms.
9. **User namespace:** global person with tenant memberships vs tenant-local users; email uniqueness, subject stability, account linking, and cross-tenant SSO policy.
10. **Audit and privacy:** retention, immutable sink, SIEM, PII classification, regional deletion/legal hold, operator access, and evidence export.

## 13. References

All references accessed 2026-08-06. Official specifications, documentation, repositories, source, release records, and licenses are preferred.

### OpenIddict

- OpenIddict introduction, modular boundary, and event model: https://documentation.openiddict.com/introduction
- OpenIddict 7.6.0 release, 2026-07-15: https://github.com/openiddict/openiddict-core/releases/tag/7.6.0
- NuGet version index showing 7.6.0 stable and 8.0 previews: https://api.nuget.org/v3-flatcontainer/openiddict.aspnetcore/index.json
- OpenIddict flow selection, including code, client credentials, hybrid, implicit, and ROPC behavior: https://documentation.openiddict.com/guides/choosing-the-right-flow
- OpenIddict endpoint, grant, scope, and response-type application permissions: https://documentation.openiddict.com/configuration/application-permissions
- PKCE global/per-client configuration: https://documentation.openiddict.com/configuration/proof-key-for-code-exchange
- PAR support and requirements: https://documentation.openiddict.com/configuration/pushed-authorization-requests
- mTLS client authentication, endpoint aliases, static issuer requirement, and certificate-bound tokens: https://documentation.openiddict.com/configuration/mutual-tls-authentication
- `private_key_jwt` support: https://documentation.openiddict.com/configuration/assertion-based-client-authentication
- Multiple credentials and selection rules: https://documentation.openiddict.com/configuration/encryption-and-signing-credentials
- Apache-2.0 license: https://github.com/openiddict/openiddict-core/blob/7.6.0/LICENSE.md
- Server options (`Issuer`, credential collections, default methods/lifetimes): https://github.com/openiddict/openiddict-core/blob/7.6.0/src/OpenIddict.Server/OpenIddictServerOptions.cs
- Server builder flow and endpoint APIs, including `AllowClientCredentialsFlow()`, `AllowDeviceAuthorizationFlow()`, `AllowHybridFlow()`, `AllowImplicitFlow()`, `SetDeviceAuthorizationEndpointUris()`, and `SetEndUserVerificationEndpointUris()`; `AllowPasswordFlow()` is cited only to document the API OwnIdentity will deliberately never configure: https://github.com/openiddict/openiddict-core/blob/7.6.0/src/OpenIddict.Server/OpenIddictServerBuilder.cs
- OpenIddict 7.6.0 endpoint/grant/response permission constants and absence of an end-user-verification application permission: https://github.com/openiddict/openiddict-core/blob/7.6.0/src/OpenIddict.Abstractions/OpenIddictConstants.cs
- JAR `request` rejection and PAR-only `request_uri` handling: https://github.com/openiddict/openiddict-core/blob/7.6.0/src/OpenIddict.Server/OpenIddictServerHandlers.Authentication.cs
- Credential validation/sorting and static issuer requirement with mTLS aliases: https://github.com/openiddict/openiddict-core/blob/7.6.0/src/OpenIddict.Server/OpenIddictServerConfiguration.cs
- JWKS publication of registered signing credentials: https://github.com/openiddict/openiddict-core/blob/7.6.0/src/OpenIddict.Server/OpenIddictServerHandlers.Discovery.cs
- Options snapshot captured per server transaction: https://github.com/openiddict/openiddict-core/blob/7.6.0/src/OpenIddict.Server/OpenIddictServerFactory.cs
- OpenIddict PBKDF2 client-secret representation: https://github.com/openiddict/openiddict-core/blob/7.6.0/src/OpenIddict.Core/Managers/OpenIddictApplicationManager.cs

### FAPI and OAuth security

- RFC 6749, historical authorization-code, implicit, ROPC/password, and client-credentials grants: https://www.rfc-editor.org/rfc/rfc6749
- RFC 8628, OAuth 2.0 Device Authorization Grant: https://www.rfc-editor.org/rfc/rfc8628
- OpenID Connect Core 1.0, code/implicit/hybrid flows and response types: https://openid.net/specs/openid-connect-core-1_0.html
- FAPI 2.0 Security Profile Final, February 2025: https://openid.net/specs/fapi-security-profile-2_0.html
- FAPI 1.0 Part 2 Advanced Final: https://openid.net/specs/openid-financial-api-part-2-1_0-final.html
- OpenID Foundation FAPI OP conformance instructions: https://openid.net/certification/fapi_op_testing/
- OpenID Foundation certification portal: https://openid.net/certification/
- RFC 7636, PKCE: https://www.rfc-editor.org/rfc/rfc7636
- RFC 8705, OAuth mTLS and certificate-bound tokens: https://www.rfc-editor.org/rfc/rfc8705
- RFC 9101, JAR: https://www.rfc-editor.org/rfc/rfc9101
- RFC 9126, PAR: https://www.rfc-editor.org/rfc/rfc9126
- RFC 9207, authorization response issuer: https://www.rfc-editor.org/rfc/rfc9207
- RFC 9449, DPoP: https://www.rfc-editor.org/rfc/rfc9449
- RFC 9700, OAuth 2.0 Security Best Current Practice: https://www.rfc-editor.org/rfc/rfc9700

### SAML

- Sustainsys.Saml2 official README, SP role and branch maintenance boundaries: https://github.com/Sustainsys/Saml2/blob/main/README.md
- Sustainsys ASP.NET Core handler and one-scheme-per-IdP recommendation: https://saml2.sustainsys.com/en/v2/asp.net-core.html
- Sustainsys license boundaries: https://github.com/Sustainsys/Saml2/blob/main/LICENSE.txt
- Sustainsys security/support policy: https://github.com/Sustainsys/Saml2/blob/main/SECURITY.md
- Sustainsys v2.11.0 release: https://github.com/Sustainsys/Saml2/releases/tag/v2.11.0
- ITfoxtec Identity Saml2 SP/RP/IdP scope, supported frameworks, bridge option, and BSD-3-Clause statement: https://github.com/ITfoxtec/ITfoxtec.Identity.Saml2/blob/main/README.md
- ITfoxtec 4.20.1 release: https://github.com/ITfoxtec/ITfoxtec.Identity.Saml2/releases/tag/4.20.1

### ASP.NET Core, BFF, YARP, and token management

- ASP.NET Core runtime scheme provider source: https://github.com/dotnet/aspnetcore/blob/main/src/Http/Authentication.Core/src/AuthenticationSchemeProvider.cs
- Per-request authentication-handler cache source: https://github.com/dotnet/aspnetcore/blob/main/src/Http/Authentication.Core/src/AuthenticationHandlerProvider.cs
- `IOptionsMonitorCache<T>` invalidation APIs: https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.ioptionsmonitorcache-1
- Forwarded headers, trusted proxies, and host restrictions: https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0
- ASP.NET Core production secret guidance: https://learn.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-10.0
- YARP 2.3.0 getting started and CLI package instruction: https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/getting-started?view=aspnetcore-10.0
- YARP 2.3.0 release and MIT statement: https://github.com/dotnet/yarp/releases/tag/v2.3.0
- Duende Access Token Management features/license: https://docs.duendesoftware.com/accesstokenmanagement/
- Duende Access Token Management OIDC web-app behavior: https://docs.duendesoftware.com/accesstokenmanagement/web-apps/
- Duende Access Token Management 4.2.0 release: https://github.com/DuendeSoftware/foss/releases/tag/atm-4.2.0
- Duende FOSS Apache-2.0 license: https://github.com/DuendeSoftware/foss/blob/main/LICENSE
- Duende BFF production licensing statement and BFF threat model: https://docs.duendesoftware.com/bff/
- Duende BFF YARP token/antiforgery integration: https://docs.duendesoftware.com/bff/fundamentals/apis/yarp/

### IdentityServer4 migration evidence

- IdentityServer4 archived/end-of-support/security notice: https://github.com/DuendeArchive/IdentityServer4/blob/main/README.md
- IdentityServer4 4.1.2 final release: https://github.com/DuendeArchive/IdentityServer4/releases/tag/4.1.2
- IdentityServer4 Apache-2.0 license: https://github.com/DuendeArchive/IdentityServer4/blob/main/LICENSE
- Archived IdentityServer4 EF client entity: https://github.com/DuendeArchive/IdentityServer4/blob/archive/src/EntityFramework.Storage/src/Entities/Client.cs
- Archived persisted-grant entity: https://github.com/DuendeArchive/IdentityServer4/blob/archive/src/EntityFramework.Storage/src/Entities/PersistedGrant.cs
- Archived device-flow entity: https://github.com/DuendeArchive/IdentityServer4/blob/archive/src/EntityFramework.Storage/src/Entities/DeviceFlowCodes.cs
- Archived IdentityServer4 EF configuration migration: https://github.com/DuendeArchive/IdentityServer4/blob/archive/samples/Quickstarts/5_EntityFramework/src/IdentityServer/Data/Migrations/IdentityServer/ConfigurationDb/20200625203625_InitialIdentityServerConfigurationDbMigration.cs
- Archived IdentityServer4 EF operational migration: https://github.com/DuendeArchive/IdentityServer4/blob/archive/samples/Quickstarts/5_EntityFramework/src/IdentityServer/Data/Migrations/IdentityServer/PersistedGrantDb/20200625203357_InitialIdentityServerPersistedGrantDbMigration.cs

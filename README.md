# OwnIdentity administration baseline

OwnIdentity runs a .NET 10 Identity/OpenIddict authority and a Vue administration UI together through Aspire. Local persistence uses SQLite; Docker or Podman is not required.

## Quick start

Prerequisites:

- .NET SDK 10.0.302 or a later 10.0 patch
- Bun 1.3.14
- A trusted ASP.NET Core development HTTPS certificate

From the repository root, complete the first-time setup:

```text
dotnet dev-certs https --trust
dotnet restore OwnIdentity_openDict.sln
bun install --cwd src/ownidentity-admin --frozen-lockfile --ignore-scripts
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.com" --project src/OwnIdentity.IdentityService
dotnet user-secrets set "BootstrapAdmin:Password" "replace-with-a-strong-local-password" --project src/OwnIdentity.IdentityService
```

The password must satisfy the configured Identity policy: at least 12 characters with upper-case, lower-case, numeric, and non-alphanumeric characters. User secrets stay outside the repository and are for local development only.

Start both applications with one command:

```text
dotnet run --project IdentityProjectsHost/IdentityProjectsHost.csproj
```

Aspire starts the identity service first and starts Vite only after `https://localhost:7043/health/ready` succeeds. Before Vite starts, the AppHost runs `bun install --frozen-lockfile --ignore-scripts`, so later AppHost runs enforce `bun.lock` without executing package lifecycle scripts. The explicit first-time install above fails early if Bun or the lockfile is invalid.

Open `http://localhost:5173`, select **Sign in with OwnIdentity**, and use the bootstrap account.

## Local resources

| Resource | URL |
|---|---|
| Administration UI | `http://localhost:5173` |
| Identity service | `https://localhost:7043` |
| Identity service HTTP | `http://localhost:5043` |
| Readiness check | `https://localhost:7043/health/ready` |
| Authorization endpoint | `https://localhost:7043/connect/authorize` |
| Token endpoint | `https://localhost:7043/connect/token` |
| Logout endpoint | `https://localhost:7043/connect/logout` |
| Aspire dashboard | Printed by the AppHost at startup |

The AppHost passes `VITE_AUTHORITY` and `VITE_API_BASE_URL` explicitly as `https://localhost:7043`. The seeded public client requires the stable redirect URI `http://localhost:5173/auth/callback` and logout redirect URI `http://localhost:5173/auth/logout-callback`.

## Reset local data

Stop the AppHost, delete the SQLite data directory, and start it again. Migrations and seed data are recreated at startup.

PowerShell:

```powershell
Remove-Item -Recurse -Force src/OwnIdentity.IdentityService/App_Data
```

Bash:

```bash
rm -rf src/OwnIdentity.IdentityService/App_Data
```

The bootstrap account is recreated from user secrets. To change its password after it has already been seeded, reset the database after updating the secret.

## Development commands

```text
dotnet build OwnIdentity_openDict.sln
dotnet test OwnIdentity_openDict.sln
bun run --cwd src/ownidentity-admin type-check
bun run --cwd src/ownidentity-admin test:unit -- --run
bun run --cwd src/ownidentity-admin build
bun audit --cwd src/ownidentity-admin
```

Run services separately only when Aspire orchestration is not needed:

```text
dotnet run --project src/OwnIdentity.IdentityService --launch-profile https
bun run --cwd src/ownidentity-admin dev -- --port 5173 --strictPort
```

## Startup problems

| Symptom | Resolution |
|---|---|
| Port 5173, 5043, or 7043 is already in use | Stop the process using the port. Do not change these local ports without also updating CORS, AppHost variables, and the seeded OpenIddict redirect URIs. |
| The browser rejects `https://localhost:7043` | Run `dotnet dev-certs https --clean`, then `dotnet dev-certs https --trust`, and restart the AppHost. |
| Aspire reports that `bun` is missing or has the wrong version | Install Bun 1.3.14, open a new shell, and verify `bun --version`. |
| Identity service exits during bootstrap | Configure both user secrets shown in the quick start. A missing value or a password that fails the Identity policy intentionally stops startup. |
| Vite does not start | Check `identity-service` in the Aspire dashboard first. `admin-ui` waits for backend readiness by design. |

## Security and persistence

- SQLite data is created at `src/OwnIdentity.IdentityService/App_Data/identity.db`; database files and environment files are ignored by Git.
- The Vue client uses authorization code with PKCE and stores OIDC state and tokens in `sessionStorage`.
- The administration API requires the `Administrator` role and `identity.admin` scope.
- OpenIddict development certificates are enabled only in Development. Production requires managed signing and encryption certificates plus a managed secret provider.
- Do not place bootstrap passwords in `appsettings*.json`, `.env` files, command scripts, or source control.
- `src/ownidentity-admin/bunfig.toml` globally blocks root and dependency `preinstall`, `install`, and `postinstall` scripts. Deterministic paths also pass `--ignore-scripts` explicitly as defense in depth.
- CI, Aspire, IDE builds, and onboarding must install with `--frozen-lockfile`. Intentional dependency updates use Bun without that flag and regenerate `bun.lock`, while lifecycle scripts remain blocked.
- Do not add `trustedDependencies` or lifecycle-script exceptions without a future explicit security decision. If a package requires an install script, treat it as incompatible rather than silently trusting it.
- Blocking lifecycle scripts reduces one execution path; it does not make packages, transitive dependencies, or Vite plugins trustworthy. Continue review, lockfile review, and `bun audit` checks.
- NuGet operations remain separate and must use the .NET CLI; do not hand-edit `PackageReference` entries or use IDE package-management UI.

## Project structure

| Path | Responsibility |
|---|---|
| `IdentityProjectsHost` | Aspire orchestration for the backend and Vite UI |
| `src/OwnIdentity.IdentityService` | Identity, OpenIddict, Razor login/logout, admin API, and EF migrations |
| `src/ownidentity-admin` | Vue administration client and preserved JavaScript Project System integration |
| `tests/OwnIdentity.IdentityService.Tests` | Backend authorization policy tests |

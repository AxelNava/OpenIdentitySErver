# Runs the OwnIdentity IdentityServer4 extractor against the lab SQL Server with the lab CA
# trusted, keeping the extractor's TLS boundary intact. Credential still arrives on stdin.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY src/OwnIdentity.LegacyInventory.Core/ src/OwnIdentity.LegacyInventory.Core/
COPY tools/OwnIdentity.IdentityServer4Extractor/ tools/OwnIdentity.IdentityServer4Extractor/
RUN dotnet publish tools/OwnIdentity.IdentityServer4Extractor/OwnIdentity.IdentityServer4Extractor.csproj -c Release -o /publish

FROM mcr.microsoft.com/dotnet/sdk:10.0
COPY lab/skoruba-beta7/certs/ca.crt /usr/local/share/ca-certificates/is4beta7-lab-ca.crt
RUN update-ca-certificates
COPY --from=build /publish /app
ENTRYPOINT ["/app/OwnIdentity.IdentityServer4Extractor"]

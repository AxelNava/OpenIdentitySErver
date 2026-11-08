# Lab-only image for Skoruba IdentityServer4.Admin 1.0.0-beta7 (no official image exists for this tag).
# Builds one publish per app (Admin / Api / STS); APP_NAME selects which one ends up in the runtime image.
# syntax=docker/dockerfile:1
ARG APP_NAME=Skoruba.IdentityServer4.Admin

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
ARG APP_NAME
WORKDIR /repo
COPY . .
# Debug configuration on purpose: Skoruba beta7 gates RequireHttpsMetadata=false behind #if DEBUG,
# and the lab runs the STS over plain HTTP. Lab-only; never used for production images.
RUN dotnet publish src/${APP_NAME}/${APP_NAME}.csproj -c Debug -o /publish

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS runtime
ARG APP_NAME
ENV APP_NAME=${APP_NAME} \
    ASPNETCORE_ENVIRONMENT=Docker
WORKDIR /app
COPY --from=build /publish .
# STS must serve http only inside the lab (no https cert); ports come from compose.
# The extra "--" makes "$@" receive the container command (e.g. /seed) after the script.
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet /app/${APP_NAME}.dll \"$@\"", "--"]

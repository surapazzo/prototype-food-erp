# ==============================================================================
# AURA OS - Hardened Multi-Stage Dockerfile (Node 6: Release & DevOps Engineer)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY src/ ./src/
COPY tests/ ./tests/

# Runtime Stage: Non-root, Read-Only RootFS compatible, Minimal Attack Surface
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
RUN addgroup -g 1000 aura && adduser -u 1000 -G aura -s /bin/sh -D aura
WORKDIR /workspace
COPY --from=build --chown=1000:1000 /src /workspace
COPY --chown=1000:1000 web/ /workspace/web/

USER 1000:1000
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/healthz || exit 1
ENTRYPOINT ["dotnet", "--info"]

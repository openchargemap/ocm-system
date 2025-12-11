# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy source
COPY . ./

# Restore (limit to this project for better layer caching)
RUN dotnet restore "API/OCM.Net/OCM.API.Web/OCM.API.Web.csproj"

# Publish
RUN dotnet publish "API/OCM.Net/OCM.API.Web/OCM.API.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS="http://+:8080" \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Copy published output
COPY --from=build /app/publish ./

# Copy Docker-specific configuration files to replace defaults
COPY API/OCM.Net/OCM.API.Web/App.docker.config ./App.Config
COPY API/OCM.Net/OCM.API.Web/appsettings.docker.json ./appsettings.json

# Install curl for health check (as root before switching to appuser)
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user (compatible with Debian-based images)
RUN groupadd -r appuser && useradd -r -g appuser appuser \
    && chown -R appuser:appuser /app

USER appuser

# Expose HTTP (adjust if you enable HTTPS)
EXPOSE 8080

# Health check - queries the system status endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/v3/system/status?key=statuscake || exit 1

ENTRYPOINT ["dotnet", "OCM.API.Web.dll"]

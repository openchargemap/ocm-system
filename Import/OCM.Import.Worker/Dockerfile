# OCM.Import.Worker Dockerfile
# Multi-stage build for .NET 9 Worker Service

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files for restore
# API projects (dependencies)
COPY ["API/OCM.Net/OCM.API.Model/OCM.API.Common.Model.csproj", "API/OCM.Net/OCM.API.Model/"]
COPY ["API/OCM.Net/OCM.API.Core/OCM.API.Core.csproj", "API/OCM.Net/OCM.API.Core/"]
COPY ["API/OCM.Net/OCM.API.Client/OCM.API.Client.csproj", "API/OCM.Net/OCM.API.Client/"]

# Import projects
COPY ["Import/OCM.Import.Analysis/OCM.Import.Analysis.csproj", "Import/OCM.Import.Analysis/"]
COPY ["Import/OCM.Import.Common/OCM.Import.Common.csproj", "Import/OCM.Import.Common/"]
COPY ["Import/OCM.Import.Worker/OCM.Import.Worker.csproj", "Import/OCM.Import.Worker/"]

# Restore dependencies
RUN dotnet restore "Import/OCM.Import.Worker/OCM.Import.Worker.csproj"

# Copy all source code
COPY ["API/", "API/"]
COPY ["Import/", "Import/"]

# Build the worker project
WORKDIR "/src/Import/OCM.Import.Worker"
RUN dotnet build "OCM.Import.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OCM.Import.Worker.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install system dependencies if needed for shapefiles
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_ENVIRONMENT=Production

# Create non-root user for security
RUN useradd -m -u 1000 ocmworker && \
    chown -R ocmworker:ocmworker /app

USER ocmworker

# Health check (optional - adjust port/endpoint as needed)
# HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
#   CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "OCM.Import.Worker.dll"]
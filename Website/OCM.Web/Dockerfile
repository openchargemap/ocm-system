# Use the official .NET 9 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy everything and restore, then publish the Website project
COPY . ./

# Restore only the Website project to speed up caching
RUN dotnet restore "Website/OCM.Web/OCM.Web.csproj"

# Publish the project in Release configuration
RUN dotnet publish "Website/OCM.Web/OCM.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Environment for production
ENV ASPNETCORE_URLS="http://+:80"
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Copy published output from build stage
COPY --from=build /app/publish ./

# Create a non-root user and give ownership of the app folder
RUN groupadd -r appuser && useradd -r -g appuser appuser \
    && chown -R appuser:appuser /app

USER appuser

# Expose port 80 for HTTP
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "OCM.Web.dll"]

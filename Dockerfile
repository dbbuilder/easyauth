# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers
COPY ["src/EasyAuth.Framework.Core/EasyAuth.Framework.Core.csproj", "src/EasyAuth.Framework.Core/"]
COPY ["src/EasyAuth.Framework.Extensions/EasyAuth.Framework.Extensions.csproj", "src/EasyAuth.Framework.Extensions/"]
COPY ["Directory.Build.props", "."]
COPY ["nuget.config", "."]
COPY ["global.json", "."]

RUN dotnet restore "src/EasyAuth.Framework.Core/EasyAuth.Framework.Core.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/EasyAuth.Framework.Core"

# Build the application
RUN dotnet build "EasyAuth.Framework.Core.csproj" -c Release -o /app/build --no-restore

# Publish the application
RUN dotnet publish "EasyAuth.Framework.Core.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r easyauth && useradd -r -g easyauth easyauth

# Copy published application
COPY --from=build /app/publish .

# Set ownership
RUN chown -R easyauth:easyauth /app

# Switch to non-root user
USER easyauth

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "EasyAuth.Framework.Core.dll"]
# Base image
FROM mcr.microsoft.com/dotnet/nightly/aspnet:9.0-preview AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build image
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0-preview AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["SimpleIdentityServer/SimpleIdentityServer.csproj", "SimpleIdentityServer/"]
# Add any other project references here as needed
RUN dotnet restore "SimpleIdentityServer/SimpleIdentityServer.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/SimpleIdentityServer"

# Build the application
RUN dotnet build "SimpleIdentityServer.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SimpleIdentityServer.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleIdentityServer.dll"]

# Health check
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

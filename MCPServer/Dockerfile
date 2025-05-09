# Base image
FROM mcr.microsoft.com/dotnet/nightly/aspnet:9.0-preview AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build image
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0-preview AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["MCPServer/MCP.Server.csproj", "MCPServer/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
# Add any other project references here as needed
RUN dotnet restore "MCPServer/MCP.Server.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/MCPServer"

# Build the application
RUN dotnet build "MCP.Server.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "MCP.Server.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MCP.Server.dll"]

# Health check
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

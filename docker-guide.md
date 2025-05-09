# Docker Setup for SimpleIdentityServer, MCP.Server & MessageBroker.Kafka

This document provides instructions for setting up Docker containers for SimpleIdentityServer, MCP.Server, and MessageBroker.Kafka components.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- Git (to clone the repository)

## Project Structure

The project is already structured with Dockerfiles in their respective project directories:

```
/
├── SimpleIdentityServer/
│   └── Dockerfile
├── MCPServer/
│   └── Dockerfile
├── MessageBroker/
│   └── Dockerfile
├── .dockerignore
└── docker-compose.yml
```

## Setup Instructions

1. **Clone the repository**:
   ```bash
   git clone https://github.com/myonathanlinkedin/mcp-rag-agency-book-appointments.git
   cd mcp-rag-agency-book-appointments
   ```

2. **Build and run the containers**:
   ```bash
   docker-compose up --build
   ```

   Alternatively, you can build and run specific services:
   ```bash
   docker-compose up --build simple-identity-server mcp-server
   ```

3. **Access the services**:
   - SimpleIdentityServer: http://localhost:8080
   - MCP.Server: http://localhost:8081
   - MessageBroker.Kafka: http://localhost:8082
   - MailHog UI: http://localhost:8025
   - Elasticsearch: http://localhost:9200

## Configuration

### Environment Variables

You can customize the environment variables in the `docker-compose.yml` file to match your requirements:

- **For SimpleIdentityServer**:
  - `ASPNETCORE_ENVIRONMENT`: Set to `Production` for production environments

- **For MCP.Server**:
  - `ASPNETCORE_ENVIRONMENT`: Set to `Production` for production environments

- **For MessageBroker.Kafka**:
  - `ASPNETCORE_ENVIRONMENT`: Set to `Production` for production environments

### Ports

The default port mappings are:
- SimpleIdentityServer: 8080 (HTTP) and 8443 (HTTPS)
- MCP.Server: 8081 (HTTP) and 8444 (HTTPS)
- MessageBroker.Kafka: 8082 (HTTP)
- Kafka: 9092
- SQL Server: 1433
- Elasticsearch: 9200
- MailHog: 1025 (SMTP) and 8025 (Web UI)

You can modify these in the `docker-compose.yml` file as needed.

## Important Notes About Docker Configuration

### Central Package Management

The project uses central package management with:
- `Directory.Build.props` - Common build properties
- `Directory.Packages.props` - Centralized package versions

When building Docker images, these files must be copied to the build context before running `dotnet restore`:

```dockerfile
COPY ["MCPServer/MCP.Server.csproj", "MCPServer/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
RUN dotnet restore "MCPServer/MCP.Server.csproj"
```

### .dockerignore

The `.dockerignore` file is located at the root of the project and excludes:
- Visual Studio files (.vs/, *.vsidx, *.user, *.suo, *.cache)
- Git-related directories (.git/, .github/)
- Build output directories (bin/, obj/)

This helps keep Docker images smaller and build times faster.

## Troubleshooting

### Common Issues

1. **Restore failures**: If `dotnet restore` fails, ensure the central package management files are being copied correctly.

2. **Port conflicts**: If you have services already using the exposed ports, modify the port mappings in `docker-compose.yml`.

3. **Database connection issues**: Ensure the SQL Server container is running and the connection string is correct.

4. **Kafka connection issues**: Check if Zookeeper and Kafka containers are running correctly.

5. **Identity server connectivity**: Make sure SimpleIdentityServer is running and accessible from MCP.Server.

### Logs

To view logs for debugging:

```bash
# View logs for all services
docker-compose logs

# View logs for specific services
docker-compose logs simple-identity-server
docker-compose logs mcp-server
docker-compose logs message-broker-kafka
docker-compose logs kafka
```

## Additional Notes

- The SQL Server container uses a volume (`mssql-data`) to persist data between container restarts.
- Elasticsearch uses a volume (`elasticsearch-data`) to persist indices.
- Services are configured with appropriate dependencies to ensure correct startup order.
- For production deployments, consider using Docker Swarm or Kubernetes for better orchestration and management.
- The project is configured to use .NET 9.0 preview, which may show warnings during build.

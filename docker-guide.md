# Docker Setup for SimpleIdentityServer, MCP.Server & MessageBroker.Kafka

This document provides instructions for setting up Docker containers for SimpleIdentityServer, MCP.Server, and MessageBroker.Kafka components.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- Git (to clone the repository)

## Project Structure

Place the Dockerfiles in their respective project directories:

```
/
├── SimpleIdentityServer/
│   └── Dockerfile
├── MCP.Server/
│   └── Dockerfile
├── MessageBroker.Kafka/
│   └── Dockerfile
└── docker-compose.yml
```

## Setup Instructions

1. **Clone the repository**:
   ```bash
   git clone https://github.com/myonathanlinkedin/mcp-rag-agency-book-appointments.git
   cd mcp-rag-agency-book-appointments
   ```

2. **Copy the Dockerfiles**:
   - Copy `Dockerfile for SimpleIdentityServer` to the `SimpleIdentityServer` directory
   - Copy `Dockerfile for MCP.Server` to the `MCP.Server` directory
   - Copy `Dockerfile for MessageBroker.Kafka` to the `MessageBroker.Kafka` directory
   - Copy `docker-compose.yml` to the root directory of your project

3. **Build and run the containers**:
   ```bash
   docker-compose up --build
   ```

4. **Access the services**:
   - SimpleIdentityServer: http://localhost:8080
   - MCP.Server: http://localhost:8081
   - MessageBroker.Kafka: http://localhost:8082

## Configuration

### Environment Variables

You can customize the environment variables in the `docker-compose.yml` file to match your requirements:

- **For SimpleIdentityServer**:
  - `ASPNETCORE_ENVIRONMENT`: Set to `Production` for production environments
  - `ConnectionStrings__DefaultConnection`: Database connection string

- **For MCP.Server**:
  - `ASPNETCORE_ENVIRONMENT`: Set to `Production` for production environments
  - `ConnectionStrings__DefaultConnection`: Database connection string
  - `Kafka__BootstrapServers`: Kafka connection string
  - `IdentityServer__Authority`: URL of the identity server

- **For MessageBroker.Kafka**:
  - `ASPNETCORE_ENVIRONMENT`: Set to `Production` for production environments
  - `Kafka__BootstrapServers`: Kafka connection string

### Ports

The default port mappings are:
- SimpleIdentityServer: 8080 (HTTP) and 8443 (HTTPS)
- MCP.Server: 8081 (HTTP) and 8444 (HTTPS)
- MessageBroker.Kafka: 8082 (HTTP)
- Kafka: 9092
- SQL Server: 1433

You can modify these in the `docker-compose.yml` file as needed.

## Troubleshooting

### Common Issues

1. **Port conflicts**: If you have services already using the exposed ports, modify the port mappings in `docker-compose.yml`.

2. **Database connection issues**: Ensure the SQL Server container is running and the connection string is correct.

3. **Kafka connection issues**: Check if Zookeeper and Kafka containers are running correctly.

4. **Identity server connectivity**: Make sure SimpleIdentityServer is running and accessible from MCP.Server.

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
- Health checks are configured for all services to monitor their status.
- Services are configured with appropriate dependencies to ensure correct startup order.
- For production deployments, consider using Docker Swarm or Kubernetes for better orchestration and management.
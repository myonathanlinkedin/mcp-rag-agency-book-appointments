# Base image
FROM mcr.microsoft.com/dotnet/nightly/aspnet:9.0-preview AS base
WORKDIR /app
EXPOSE 80
EXPOSE 9092

# Build image
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0-preview AS build
WORKDIR /src

# Copy the entire solution to maintain project references
COPY . .

# Restore dependencies
RUN dotnet restore "BackgroundServices/MessageBroker/MessageBroker.Kafka.csproj"

# Build the application
WORKDIR "/src/BackgroundServices/MessageBroker"
RUN dotnet build "MessageBroker.Kafka.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "MessageBroker.Kafka.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MessageBroker.Kafka.dll"]

# Health check
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

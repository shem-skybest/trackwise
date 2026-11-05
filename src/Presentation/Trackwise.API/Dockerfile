FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /trackwise

# Copy solution and project files
COPY *.slnx ./
COPY src/Core/ src/Core/
COPY src/Infrastructure/ src/Infrastructure/
COPY src/Shared/ src/Shared/
COPY src/Presentation/Trackwise.HostDefaults/ src/Presentation/Trackwise.HostDefaults/
COPY src/Presentation/Trackwise.API/ src/Presentation/Trackwise.API/
COPY src/Presentation/Trackwise.McpServer/ src/Presentation/Trackwise.McpServer/
COPY src/Presentation/Trackwise.TelegramBot/ src/Presentation/Trackwise.TelegramBot/

# Restore dependencies
RUN dotnet restore src/Presentation/Trackwise.API/Trackwise.API.csproj

# Build and publish
WORKDIR /trackwise/src/Presentation/Trackwise.API
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 7000

# Npgsql dlopens libgssapi_krb5 for SASL/GSS negotiation even when auth is username/password only.
RUN apt-get update && apt-get install -y --no-install-recommends libkrb5-3 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["./Trackwise.API"]

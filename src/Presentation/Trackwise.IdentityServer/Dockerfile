FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /trackwise

# Copy solution and project files
COPY *.slnx ./
COPY src/Core/ src/Core/
COPY src/Shared/ src/Shared/
COPY src/Infrastructure/ src/Infrastructure/
COPY src/Presentation/Trackwise.HostDefaults/ src/Presentation/Trackwise.HostDefaults/
COPY src/Presentation/Trackwise.IdentityServer/ src/Presentation/Trackwise.IdentityServer/

# Restore dependencies
RUN dotnet restore src/Presentation/Trackwise.IdentityServer/Trackwise.IdentityServer.csproj

# Build and publish
WORKDIR /trackwise/src/Presentation/Trackwise.IdentityServer
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 7001

# Npgsql dlopens libgssapi_krb5 for SASL/GSS negotiation even when auth is username/password only.
RUN apt-get update && apt-get install -y --no-install-recommends libkrb5-3 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["./Trackwise.IdentityServer"]

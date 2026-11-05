FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /trackwise

# Copy solution and project files
COPY *.slnx ./
COPY src/Core/ src/Core/
COPY src/Shared/ src/Shared/
COPY src/Infrastructure/ src/Infrastructure/
COPY src/Presentation/Trackwise.DbMigrator/ src/Presentation/Trackwise.DbMigrator/

# Restore dependencies
RUN dotnet restore src/Presentation/Trackwise.DbMigrator/Trackwise.DbMigrator.csproj

# Build and publish
WORKDIR /trackwise/src/Presentation/Trackwise.DbMigrator
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Trackwise.DbMigrator.dll"]

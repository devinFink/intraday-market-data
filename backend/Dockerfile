# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore in its own layer so it is cached until a project or package version changes.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/MarketData.Application/MarketData.Application.csproj src/MarketData.Application/
COPY src/MarketData.Infrastructure/MarketData.Infrastructure.csproj src/MarketData.Infrastructure/
COPY src/MarketData.Api/MarketData.Api.csproj src/MarketData.Api/
RUN dotnet restore src/MarketData.Api/MarketData.Api.csproj

COPY src/ src/
RUN dotnet publish src/MarketData.Api/MarketData.Api.csproj \
    --configuration Release --no-restore --output /app -p:UseAppHost=false

# Chiseled images have no shell or package manager and run as a non-root user.
# The "-extra" variant adds ICU and tzdata, which exchange time zone lookups (e.g. America/New_York) need.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MarketData.Api.dll"]

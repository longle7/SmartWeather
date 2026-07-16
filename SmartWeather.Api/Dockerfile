# =========
# Build stage
# =========
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and projects
COPY SmartWeather.slnx ./
COPY SmartWeather.Api/SmartWeather.Api.csproj SmartWeather.Api/
COPY SmartWeather.Tests/SmartWeather.Tests.csproj SmartWeather.Tests/

# Restore dependencies
RUN dotnet restore SmartWeather.Api/SmartWeather.Api.csproj

# Copy everything and build
COPY ./ ./
WORKDIR /src/SmartWeather.Api
RUN dotnet publish SmartWeather.Api.csproj -c Release -o /app/publish

# =========
# Runtime stage
# =========
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published output
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartWeather.Api.dll"]
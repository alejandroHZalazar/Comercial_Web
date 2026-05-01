# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# NuGet config explícito para asegurar source correcto
COPY nuget.config .

# Copiar archivos de proyecto primero (layer caching de NuGet restore)
COPY Domain/Domain.csproj             Domain/
COPY Application/Application.csproj   Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY ComercialWeb/ComercialWeb.csproj  ComercialWeb/

RUN dotnet restore ComercialWeb/ComercialWeb.csproj --verbosity normal

# Copiar todo el código fuente y publicar
COPY . .
RUN dotnet publish ComercialWeb/ComercialWeb.csproj -c Release -o /app/publish

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Directorio para Data Protection keys (cookies de auth)
RUN mkdir -p /app/keys

ENV ASPNETCORE_ENVIRONMENT=Production

# Railway inyecta PORT en runtime; shell form permite expandir la variable
CMD dotnet ComercialWeb.dll --urls "http://0.0.0.0:${PORT:-8080}"

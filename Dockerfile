# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto primero (layer caching de NuGet restore)
COPY ["Domain/Domain.csproj",                     "Domain/"]
COPY ["Application/Application.csproj",           "Application/"]
COPY ["Infrastructure/Infrastructure.csproj",     "Infrastructure/"]
COPY ["Comercial Web/Comercial Web.csproj",       "Comercial Web/"]

RUN dotnet restore "Comercial Web/Comercial Web.csproj"

# Copiar todo el código fuente y publicar
COPY . .
RUN dotnet publish "Comercial Web/Comercial Web.csproj" -c Release -o /app/publish

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

# Railway inyecta la variable PORT en runtime.
# Usar shell form para que el shell expanda ${PORT} al arrancar.
CMD dotnet ComercialWeb.dll --urls "http://0.0.0.0:${PORT:-8080}"

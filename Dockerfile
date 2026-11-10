# Imagen única con la API y el frontend compilado: `docker compose up --build` y abrir http://localhost:5080

# ---------- 1. Frontend (Angular) ----------
FROM node:24-alpine AS frontend
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund --loglevel=error
COPY frontend/ ./
RUN npm run build

# ---------- 2. API (.NET) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
# Primero la configuración común y los .csproj: la restauración queda en caché mientras no cambien.
COPY backend/Directory.Build.props backend/Directory.Packages.props backend/
COPY backend/src/Practicas.Domain/Practicas.Domain.csproj backend/src/Practicas.Domain/
COPY backend/src/Practicas.Application/Practicas.Application.csproj backend/src/Practicas.Application/
COPY backend/src/Practicas.Infrastructure/Practicas.Infrastructure.csproj backend/src/Practicas.Infrastructure/
COPY backend/src/Practicas.Api/Practicas.Api.csproj backend/src/Practicas.Api/
RUN dotnet restore backend/src/Practicas.Api/Practicas.Api.csproj
COPY backend/src/ backend/src/
RUN dotnet publish backend/src/Practicas.Api -c Release -o /app --no-restore

# ---------- 3. Imagen final ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app ./
COPY --from=frontend /src/dist/frontend/browser ./web

# Development carga las credenciales de demo de appsettings.Development.json (analista / Seleccion2026*).
ENV ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_HTTP_PORTS=8080 \
    Frontend__Ruta=/app/web \
    ConnectionStrings__Practicas="Data Source=/datos/practicas.db" \
    Almacenamiento__RutaHojasDeVida=/datos/hojas-de-vida

RUN mkdir -p /datos && chown "$APP_UID" /datos
USER $APP_UID
VOLUME /datos
EXPOSE 8080
ENTRYPOINT ["dotnet", "Practicas.Api.dll"]

#!/usr/bin/env bash
# Compila y levanta la plataforma completa (API + frontend) con un solo comando en macOS o Linux.
#
#   ./iniciar.sh                 # http://localhost:5080
#   PUERTO=8080 ./iniciar.sh     # otro puerto
#   ./iniciar.sh --reiniciar     # borra compilación y datos y empieza desde cero
#
# Lo generado (compilación, node_modules, base de datos y hojas de vida) va a
# ~/.cache/plataforma-practicas, fuera de la carpeta del proyecto.
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUERTO="${PUERTO:-5080}"
SALIDA="${XDG_CACHE_HOME:-$HOME/.cache}/plataforma-practicas"
WEB="$SALIDA/frontend"
API="$SALIDA/api"
DATOS="$SALIDA/datos"

paso()   { printf '\n\033[36m==> %s\033[0m\n' "$1"; }
fallar() { printf '\n\033[31mERROR: %s\033[0m\n' "$1" >&2; exit 1; }

# ---------- 1. Requisitos ----------
paso "Verificando requisitos"

command -v dotnet >/dev/null || fallar "No se encontró .NET. Instala el SDK de .NET 10: https://dotnet.microsoft.com/download"
dotnet --list-sdks | grep -Eq '^(1[0-9]|[2-9][0-9])\.' \
  || fallar "Se necesita el SDK de .NET 10 o superior. Descarga: https://dotnet.microsoft.com/download"

command -v node >/dev/null && command -v npm >/dev/null \
  || fallar "No se encontró Node.js. Instala Node.js 22 o superior: https://nodejs.org/"
VERSION_NODE="$(node --version | sed 's/^v//')"
[ "${VERSION_NODE%%.*}" -ge 22 ] || fallar "Se necesita Node.js 22 o superior (tienes $VERSION_NODE)."

if command -v lsof >/dev/null && lsof -iTCP:"$PUERTO" -sTCP:LISTEN >/dev/null 2>&1; then
  fallar "El puerto $PUERTO ya está en uso. Ciérralo o usa otro: PUERTO=8080 ./iniciar.sh"
fi

echo "    .NET $(dotnet --version) · Node $VERSION_NODE · salida en $SALIDA"

if [ "${1:-}" = "--reiniciar" ]; then
  paso "Borrando compilación y datos anteriores"
  rm -rf "$SALIDA"
fi
mkdir -p "$WEB" "$DATOS"

# ---------- 2. Frontend ----------
paso "Compilando el frontend (Angular)"

# Copia el código (sin node_modules, dist ni .angular) para compilar fuera del proyecto.
(cd "$RAIZ/frontend" && tar --exclude=./node_modules --exclude=./dist --exclude=./.angular -cf - .) \
  | (cd "$WEB" && tar -xf -)

cd "$WEB"
HUELLA="$(cksum package-lock.json | cut -d' ' -f1)"
if [ "$(cat node_modules/.huella-lock 2>/dev/null || true)" != "$HUELLA" ]; then
  echo "    Instalando dependencias (solo la primera vez o si cambian; puede tardar unos minutos)..."
  npm ci --no-audit --no-fund --loglevel=error
  echo "$HUELLA" > node_modules/.huella-lock
fi
npm run build --silent

# ---------- 3. API ----------
paso "Compilando la API (.NET)"
dotnet publish "$RAIZ/backend/src/Practicas.Api" -c Release -o "$API" \
  --artifacts-path "$SALIDA/compilacion" --nologo -v q

# ---------- 4. Arranque ----------
URL="http://localhost:$PUERTO"
export ASPNETCORE_ENVIRONMENT=Development   # credenciales de demo de appsettings.Development.json
export ASPNETCORE_URLS="$URL"
export ConnectionStrings__Practicas="Data Source=$DATOS/practicas.db"
export Almacenamiento__RutaHojasDeVida="$DATOS/hojas-de-vida"
export Frontend__Ruta="$WEB/dist/frontend/browser"

paso "Iniciando en $URL"
printf '\033[32m
    Registro de practicantes:  %s/registro
    Panel de analista:         %s/analista
      usuario:     analista
      contraseña:  Seleccion2026*

    Datos guardados en: %s
    Para detener la aplicación pulsa Ctrl+C.
\033[0m\n' "$URL" "$URL" "$DATOS"

cd "$API"
exec dotnet Practicas.Api.dll

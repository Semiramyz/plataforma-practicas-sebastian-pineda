<#
.SYNOPSIS
    Compila y levanta la plataforma completa (API + frontend) con un solo comando.

.DESCRIPTION
    1. Verifica que estén instalados el SDK de .NET 10 y Node.js 22 o superior.
    2. Compila el frontend de Angular y publica la API.
    3. Arranca la API, que también sirve el frontend, en http://localhost:5080 y abre el navegador.

    Todo lo que se genera (compilación, node_modules, base de datos y hojas de vida) va a
    %LOCALAPPDATA%\PlataformaPracticas, fuera de la carpeta del proyecto. Así funciona aunque el
    proyecto esté en Documentos con el "Acceso controlado a carpetas" de Windows activado.

.PARAMETER Puerto
    Puerto HTTP de la aplicación. Por defecto 5080.

.PARAMETER SinNavegador
    No abre el navegador al terminar de arrancar.

.PARAMETER Reiniciar
    Borra la compilación y los datos (base de datos y hojas de vida) y empieza desde cero.

.EXAMPLE
    .\iniciar.ps1
.EXAMPLE
    .\iniciar.ps1 -Puerto 8080 -SinNavegador
#>
[CmdletBinding()]
param(
    [int]$Puerto = 5080,
    [switch]$SinNavegador,
    [switch]$Reiniciar
)

$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
$salida = Join-Path $env:LOCALAPPDATA 'PlataformaPracticas'
$carpetaWeb = Join-Path $salida 'frontend'
$carpetaApi = Join-Path $salida 'api'
$carpetaDatos = Join-Path $salida 'datos'

function Paso([string]$texto) { Write-Host "`n==> $texto" -ForegroundColor Cyan }
function Fallar([string]$texto) { Write-Host "`nERROR: $texto" -ForegroundColor Red; exit 1 }
function Ejecutar([string]$descripcion, [scriptblock]$comando) {
    & $comando
    if ($LASTEXITCODE -ne 0) { Fallar "$descripcion falló (código $LASTEXITCODE). Revisa los mensajes de arriba." }
}

# ---------- 1. Requisitos ----------
Paso 'Verificando requisitos'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Fallar 'No se encontró .NET. Instala el SDK de .NET 10: https://dotnet.microsoft.com/download'
}
$sdks = @(dotnet --list-sdks) -join "`n"
if ($sdks -notmatch '(?m)^(1\d|[2-9]\d)\.') {
    Fallar "Se necesita el SDK de .NET 10 o superior. SDK instalados:`n$sdks`nDescarga: https://dotnet.microsoft.com/download"
}

if (-not (Get-Command node -ErrorAction SilentlyContinue) -or -not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Fallar 'No se encontró Node.js. Instala Node.js 22 o superior: https://nodejs.org/'
}
$versionNode = (node --version).TrimStart('v')
if ([int]($versionNode.Split('.')[0]) -lt 22) {
    Fallar "Se necesita Node.js 22 o superior (tienes $versionNode). Descarga: https://nodejs.org/"
}

$ocupado = Get-NetTCPConnection -LocalPort $Puerto -State Listen -ErrorAction SilentlyContinue
if ($ocupado) {
    $proceso = (Get-Process -Id $ocupado[0].OwningProcess -ErrorAction SilentlyContinue).ProcessName
    Fallar "El puerto $Puerto ya está en uso (proceso: $proceso). Ciérralo o usa otro puerto: .\iniciar.ps1 -Puerto 8080"
}

Write-Host "    .NET $(dotnet --version) · Node $versionNode · salida en $salida"

if ($Reiniciar -and (Test-Path $salida)) {
    Paso 'Borrando compilación y datos anteriores'
    Remove-Item $salida -Recurse -Force
}
New-Item -ItemType Directory -Force $salida, $carpetaDatos | Out-Null

# ---------- 2. Frontend ----------
Paso 'Compilando el frontend (Angular)'

# Se copia el código fuera del proyecto para que npm y Angular escriban allí y no en la carpeta original.
robocopy (Join-Path $raiz 'frontend') $carpetaWeb /MIR /XD node_modules dist .angular /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { Fallar "No se pudo copiar el frontend a $carpetaWeb (robocopy $LASTEXITCODE)." }
$global:LASTEXITCODE = 0

Push-Location $carpetaWeb
try {
    # npm ci solo cuando cambian las dependencias: las siguientes ejecuciones son mucho más rápidas.
    $huellaLock = (Get-FileHash 'package-lock.json').Hash
    $archivoHuella = Join-Path $carpetaWeb 'node_modules\.huella-lock'
    $huellaInstalada = if (Test-Path $archivoHuella) { Get-Content $archivoHuella -Raw } else { '' }
    if ($huellaInstalada.Trim() -ne $huellaLock) {
        Write-Host '    Instalando dependencias (solo la primera vez o si cambian; puede tardar unos minutos)...'
        Ejecutar 'npm ci' { npm ci --no-audit --no-fund --loglevel=error }
        Set-Content -Path $archivoHuella -Value $huellaLock -Encoding ascii
    }
    Ejecutar 'La compilación del frontend' { npm run build --silent }
}
finally {
    Pop-Location
}
$carpetaWebCompilada = Join-Path $carpetaWeb 'dist\frontend\browser'

# ---------- 3. API ----------
Paso 'Compilando la API (.NET)'

# --artifacts-path envía obj/ y bin/ fuera del proyecto por la misma razón que arriba.
Ejecutar 'La compilación de la API' {
    dotnet publish (Join-Path $raiz 'backend\src\Practicas.Api') -c Release -o $carpetaApi `
        --artifacts-path (Join-Path $salida 'compilacion') --nologo -v q
}

# ---------- 4. Arranque ----------
$url = "http://localhost:$Puerto"
$env:ASPNETCORE_ENVIRONMENT = 'Development'   # credenciales de demo de appsettings.Development.json
$env:ASPNETCORE_URLS = $url
$env:ConnectionStrings__Practicas = "Data Source=$(Join-Path $carpetaDatos 'practicas.db')"
$env:Almacenamiento__RutaHojasDeVida = Join-Path $carpetaDatos 'hojas-de-vida'
$env:Frontend__Ruta = $carpetaWebCompilada

Paso "Iniciando en $url"
Write-Host @"

    Registro de practicantes:  $url/registro
    Panel de analista:         $url/analista
      usuario:     analista
      contraseña:  Seleccion2026*

    Datos guardados en: $carpetaDatos
    Para detener la aplicación pulsa Ctrl+C.

"@ -ForegroundColor Green

$abrirNavegador = $null
if (-not $SinNavegador) {
    # Abre el navegador en cuanto la API responda.
    $abrirNavegador = Start-Job -ArgumentList $url -ScriptBlock {
        param($url)
        for ($i = 0; $i -lt 60; $i++) {
            try { Invoke-WebRequest "$url/api/salud" -UseBasicParsing -TimeoutSec 2 | Out-Null; Start-Process $url; return }
            catch { Start-Sleep -Milliseconds 500 }
        }
    }
}

Push-Location $carpetaApi
try {
    dotnet Practicas.Api.dll
}
finally {
    Pop-Location
    if ($abrirNavegador) { Remove-Job $abrirNavegador -Force -ErrorAction SilentlyContinue }
}

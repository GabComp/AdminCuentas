param(
    [string]$Salida = (Join-Path $PSScriptRoot "publish\portal"),
    [string]$Configuracion = "Release"
)

$ErrorActionPreference = "Stop"

$proyecto = Join-Path $PSScriptRoot "AdministradorDeCuentas.csproj"

if (-not (Test-Path $proyecto)) {
    throw "No se encontró el proyecto: $proyecto"
}

Write-Host "Publicando portal ($Configuracion) en $Salida ..."

dotnet publish $proyecto -c $Configuracion -o $Salida --nologo

if ($LASTEXITCODE -ne 0) {
    throw "La publicación falló con código $LASTEXITCODE."
}

if (-not (Test-Path (Join-Path $Salida "AdministradorDeCuentas.dll"))) {
    throw "No se generó AdministradorDeCuentas.dll en $Salida."
}

# Nunca publicar secretos locales: el appsettings.Production.json se configura
# directamente en el servidor.
$produccion = Join-Path $Salida "appsettings.Production.json"

if (Test-Path $produccion) {
    Remove-Item $produccion -Force
    Write-Warning "Se excluyó appsettings.Production.json del paquete. Configúrelo en el servidor."
}

Write-Host ""
Write-Host "Listo: $Salida"
Write-Host "Copie el contenido al servidor SIN borrar el appsettings.Production.json existente."

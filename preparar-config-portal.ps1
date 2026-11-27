param(
    [string]$Proyecto = (Join-Path $PSScriptRoot "AdministradorDeCuentas.csproj"),
    [string]$Salida = (Join-Path $PSScriptRoot "publish\appsettings.Production.portal.json"),
    [string]$ApiBase = "http://192.168.0.199:5058/",
    [string]$PortalPublico = "https://192.168.0.199:5199",
    [string]$ResendApiKey = "",
    [string]$ResendFrom = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Proyecto)) {
    throw "No se encontró el proyecto: $Proyecto"
}

$secretos = @{}
$lineas = & dotnet user-secrets list --project $Proyecto 2>$null

foreach ($linea in $lineas) {
    if ($linea -match '^(?<clave>[^=]+?)\s*=\s*(?<valor>.*)$') {
        $secretos[$matches.clave.Trim()] = $matches.valor
    }
}

# Valores de prueba de EVO (no son secretos; el password viene de user-secrets).
$evoMerchant = "TEST1114864"
$evoUser = "merchant.$evoMerchant"
$evoBase = "https://evopaymentsmexico.gateway.mastercard.com/api/rest/version/100/merchant/$evoMerchant"

function PrimerValor {
    param([object[]]$Valores)

    foreach ($valor in $Valores) {
        if (-not [string]::IsNullOrWhiteSpace([string]$valor)) {
            return [string]$valor
        }
    }

    return ""
}

$apiKey = $secretos["ApiSettings:RecibosApiKey"]
$loginUser = $secretos["ApiSettings:RecibosLoginUser"]
$loginPassword = $secretos["ApiSettings:RecibosLoginPassword"]
$evoPassword = $secretos["Evo:Password"]

$obligatorios = [ordered]@{
    "ApiSettings:RecibosApiKey"      = $apiKey
    "ApiSettings:RecibosLoginUser"   = $loginUser
    "ApiSettings:RecibosLoginPassword" = $loginPassword
    "Evo:Password"                   = $evoPassword
}

foreach ($par in $obligatorios.GetEnumerator()) {
    if ([string]::IsNullOrWhiteSpace($par.Value)) {
        Write-Warning "Falta el valor para '$($par.Key)'."
    }
}

$permitirPago = $true

if (-not [string]::IsNullOrWhiteSpace($secretos["ApiSettings:PermitirAplicarPago"])) {
    $permitirPago = [bool]::Parse($secretos["ApiSettings:PermitirAplicarPago"])
}

$configuracion = [ordered]@{
    Logging           = [ordered]@{
        LogLevel = [ordered]@{
            Default              = "Information"
            "Microsoft.AspNetCore" = "Warning"
        }
    }
    AllowedHosts      = "192.168.0.199;comapa-test.local;localhost"
    ApiSettings       = [ordered]@{
        BaseUrl                 = $ApiBase
        Recibos                 = ($ApiBase.TrimEnd('/') + "/api/")
        RecibosApiKey           = $apiKey
        RecibosLoginUser        = $loginUser
        RecibosLoginPassword    = $loginPassword
        RecibosUsuario          = "U0293"
        RecibosFormaPago        = 4
        PermitirAplicarPago     = $permitirPago
    }
    Evo               = [ordered]@{
        Merchant           = $evoMerchant
        User               = $evoUser
        Password           = $evoPassword
        InitiateCheckoutUrl = "$evoBase/session"
        RetrieveOrderUrl    = "$evoBase/order"
        CheckoutScriptUrl   = "https://evopaymentsmexico.gateway.mastercard.com/static/checkout/checkout.min.js"
        MerchantName       = "COMAPA VICTORIA"
        MerchantLogo       = ""
        MerchantUrl        = ""
        ReturnUrl          = ""
        CancelUrl          = ""
    }
    Resend            = [ordered]@{
        ApiKey = $ResendApiKey
        From   = $ResendFrom
    }
    Portal            = [ordered]@{
        PublicBaseUrl = $PortalPublico
    }
    ForwardedHeaders  = [ordered]@{
        Enabled = $false
    }
}

$json = $configuracion | ConvertTo-Json -Depth 10
$carpeta = Split-Path -Parent $Salida

if (-not (Test-Path $carpeta)) {
    New-Item -ItemType Directory -Force -Path $carpeta | Out-Null
}

[IO.File]::WriteAllText($Salida, $json, (New-Object Text.UTF8Encoding($false)))

Write-Host ""
Write-Host "Archivo generado: $Salida"
Write-Host "API: $ApiBase"
Write-Host "Portal: $PortalPublico"
Write-Host "Evo merchant: $($configuracion.Evo.Merchant)"
Write-Host "PermitirAplicarPago: $permitirPago"
Write-Host ""
Write-Host "Cópielo por RDP al servidor como:"
Write-Host "  C:\inetpub\comapa-portal\appsettings.Production.json"

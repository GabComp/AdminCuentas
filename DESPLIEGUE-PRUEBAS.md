# Despliegue de pruebas en Windows Server 2025 (192.168.0.199)

Guía para publicar `ComapaInternet.Api` en el servidor de pruebas y apuntar el
portal a esa API. Está pensada para **IIS + .NET 10** en Windows Server 2025
Standard, con los dos escenarios habituales:

- **A. Solo la API en el servidor** y cada desarrollador corre el portal en su
  equipo apuntando a `192.168.0.199`.
- **B. API y portal en el servidor** para que el equipo pruebe el sitio completo
  desde su navegador.

> Los archivos `appsettings.Production.json` están en `.gitignore`. Los secretos
> se configuran en el servidor, nunca en el repositorio.

---

## 1. URLs propuestas

| Servicio | URL de pruebas |
|---|---|
| API (escenario A y B) | `http://192.168.0.199:5058` |
| API con HTTPS (recomendado) | `https://192.168.0.199:5058` |
| Portal local (escenario A) | `http://localhost:5199` |
| Portal en el servidor (escenario B) | `https://192.168.0.199:5199` |

Si usan un nombre interno (recomendado para el certificado), agreguen en el DNS
o en `C:\Windows\System32\drivers\etc\hosts` de cada equipo:

```
192.168.0.199  comapa-test.local
```

---

## 2. Requisitos en el servidor

Ejecutar en PowerShell **como administrador**:

```powershell
Install-WindowsFeature Web-Server -IncludeManagementTools
Install-WindowsFeature Web-Http-Logging, Web-Stat-Compression, Web-Filtering, Web-Mgmt-Console
```

1. Instalar el **.NET 10 Hosting Bundle** (ASP.NET Core Runtime 10.x):
   <https://dotnet.microsoft.com/download/dotnet/10.0>
   El bundle instala el módulo `AspNetCoreModuleV2` que IIS necesita.
2. Reiniciar IIS después de instalarlo:

```powershell
iisreset
```

3. Verificar conectividad con las bases y con la pasarela:

```powershell
# Comercial (mismo servidor)
Test-NetConnection 192.168.0.199 -Port 1433
# Portal / Hosting
Test-NetConnection server165.negox.com -Port 1433
# EVO (checkout de pruebas)
Test-NetConnection evopaymentsmexico.gateway.mastercard.com -Port 443
```

4. Abrir los puertos en el firewall (solo perfil de dominio/privado):

```powershell
New-NetFirewallRule -DisplayName "COMAPA API 5058" -Direction Inbound `
  -Action Allow -Protocol TCP -LocalPort 5058 -Profile Domain,Private

New-NetFirewallRule -DisplayName "COMAPA Portal 5199" -Direction Inbound `
  -Action Allow -Protocol TCP -LocalPort 5199 -Profile Domain,Private
```

> No expongan estos puertos a Internet. Para pruebas internas basta con la LAN.

5. Ajustar la zona horaria a la de Victoria para que fechas, pagos y la
   constancia salgan con la hora correcta:

```powershell
Set-TimeZone -Id "Central Standard Time (Mexico)"
Get-TimeZone
```

---

## 3. Publicar la API

En el equipo de desarrollo:

```powershell
cd C:\Users\Admin\source\repos\comapainternet

# La política de ejecución de PowerShell puede bloquear el .ps1:
powershell -NoProfile -ExecutionPolicy Bypass -File .\publicar-api.ps1 -Salida .\publish\api

# Alternativa:
# Set-ExecutionPolicy -Scope Process Bypass
# .\publicar-api.ps1 -Salida .\publish\api
```

El script genera la carpeta con `ComapaInternet.Api.dll`, `web.config` y la
carpeta `Resources` (logotipos y fuentes del PDF).

Copiar al servidor. Si tienen acceso administrativo por UNC:

```powershell
robocopy .\publish\api \\192.168.0.199\C$\inetpub\comapa-api /E /XO /R:2 /W:2
```

> **No usar `/MIR`**: borraría `appsettings.Production.json` y la carpeta
> `logs` que viven solo en el servidor. `/XO` conserva los archivos más nuevos
> del servidor.

El script **excluye** `appsettings.Production.json` del paquete para no
filtrar secretos locales; ese archivo se crea directamente en el servidor (ver
sección 5).

> Si `robocopy` devuelve **ERROR 53 (no se encontró la ruta de acceso de la
> red)**, el problema es SMB, no la carpeta: el share `C$` puede estar
> deshabilitado o el puerto 445 bloqueado. Alternativas en la sección 9.

Si el servidor no tiene el SDK, no hace falta: con el Hosting Bundle y los
archivos publicados es suficiente.

---

## 4. Configurar IIS

```powershell
Import-Module WebAdministration

$sitio = "ComapaApi"
$ruta  = "C:\inetpub\comapa-api"

New-Item -ItemType Directory -Force -Path $ruta | Out-Null
New-Item -ItemType Directory -Force -Path "$ruta\logs" | Out-Null

if (-not (Test-Path "IIS:\AppPools\$sitio")) {
    New-WebAppPool -Name $sitio
}

# ASP.NET Core se ejecuta sin runtime administrado.
Set-ItemProperty "IIS:\AppPools\$sitio" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$sitio" -Name startMode -Value "AlwaysRunning"

# Evita que el app pool se apague por inactividad (útil en pruebas).
Set-ItemProperty "IIS:\AppPools\$sitio" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)

if (-not (Get-Website -Name $sitio -ErrorAction SilentlyContinue)) {
    New-Website -Name $sitio -PhysicalPath $ruta -ApplicationPool $sitio -Port 5058 -Force
}

# Permisos para la identidad del app pool.
icacls $ruta /grant "IIS AppPool\${sitio}:(OI)(CI)(RX)" /T | Out-Null
icacls "$ruta\logs" /grant "IIS AppPool\${sitio}:(OI)(CI)(M)" /T | Out-Null
```

Comprobar que el sitio responde:

```powershell
Invoke-WebRequest http://localhost:5058/health -UseBasicParsing
```

### HTTPS (recomendado)

Con un certificado autofirmado para pruebas:

```powershell
# Un puerto no puede tener a la vez binding HTTP y HTTPS.
Remove-WebBinding -Name "ComapaApi" -Protocol http -Port 5058

$cert = New-SelfSignedCertificate `
  -Subject "CN=comapa-test.local" `
  -TextExtension @("2.5.29.17={text}DNS=comapa-test.local&IPAddress=192.168.0.199") `
  -CertStoreLocation "Cert:\LocalMachine\My" `
  -NotAfter (Get-Date).AddYears(2)

New-WebBinding -Name "ComapaApi" -Protocol https -Port 5058 -HostHeader "" -SslFlags 0
New-Item "IIS:\SslBindings\0.0.0.0!5058" -Value $cert | Out-Null
```

Si activan HTTPS, la URL de la API pasa a `https://192.168.0.199:5058` y el
portal debe usar esa base. Los equipos de desarrollo deben confiar en el
certificado o abrir `https://comapa-test.local:5058` y aceptar la advertencia
una vez.

### Logs de IIS

Editar `C:\inetpub\comapa-api\web.config` y dejar:

```xml
<aspNetCore processPath="dotnet"
            arguments=".\ComapaInternet.Api.dll"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess" />
```

La carpeta `logs` ya tiene permiso de escritura para el app pool. Un error de
arranque queda en `logs\stdout_*.log`; sin esto, IIS solo muestra el 500.

> Cada `dotnet publish` sobrescribe `web.config`. Si necesitan conservar
> `stdoutLogEnabled`, guarden una copia o vuelvan a aplicarlo después de
> publicar.

---

## 5. Configuración de la API

Crear `C:\inetpub\comapa-api\appsettings.Production.json` **reemplazando** el
que exista (el actual tiene `AllowedHosts: "*"`, que impide el arranque en
Production):

```json
{
  "AllowedHosts": "192.168.0.199;comapa-test.local;localhost",
  "ConnectionStrings": {
    "Comercial": "Server=192.168.0.199,1433;Database=ComapaVic_Comercial_Globales;User Id=USUARIO_SQL;Password=PASSWORD_SQL;Encrypt=True;TrustServerCertificate=True;",
    "Portal": "Server=server165.negox.com;Database=comapavictoria_TEST;User Id=USUARIO_SQL;Password=PASSWORD_SQL;Encrypt=True;TrustServerCertificate=True;",
    "Hosting": "Server=server165.negox.com,1433;Database=comapavictoria_web;User Id=USUARIO_SQL;Password=PASSWORD_SQL;Encrypt=True;TrustServerCertificate=True;"
  },
  "AppSettings": {
    "ApiKey": "GENERAR_UNA_CLAVE_LARGA_Y_UNICA"
  },
  "ServicioReportes": {
    "BaseUrl": "https://reportes.comapavictoria.org/",
    "ApiKey": "API_KEY_DE_REPORTES"
  },
  "Reportes": {
    "Generador": "local"
  },
  "Auth": {
    "Users": [
      {
        "Username": "portal",
        "PasswordHash": "GENERAR_CON_HASH-PASSWORD",
        "Role": "Admin"
      }
    ]
  },
  "Jwt": {
    "Key": "CLAVE_ALEATORIA_DE_AL_MENOS_64_CARACTERES",
    "Issuer": "https://192.168.0.199:5058",
    "Audience": "ComapaVictoriaEnLinea",
    "ExpireMinutes": 30
  },
  "Cors": {
    "AllowedOrigins": []
  }
}
```

Puntos que **impiden el arranque** si quedan mal:

- `AllowedHosts` no puede ser `*` en Production ni quedar vacío.
- `ConnectionStrings:Comercial` y `ConnectionStrings:Portal` son obligatorias.
- `Jwt:Key` debe tener al menos 32 bytes.
- `AppSettings:ApiKey` es obligatoria.
- `Auth:Users` debe tener al menos un usuario.
- `ServicioReportes:BaseUrl` debe ser una URL `https` y `ApiKey` no puede ir vacía
  (aunque el generador de PDF sea local).

> Para las bases usen un login SQL dedicado con los permisos mínimos. Si optan
> por autenticación de Windows, la identidad `IIS AppPool\ComapaApi` necesita
> login y permisos en ambas bases; para pruebas suele ser más simple el login
> SQL.

Generar el hash del usuario de servicio (en el equipo de desarrollo, que sí
tiene el SDK; copiar solo el hash al servidor):

```powershell
dotnet run --project C:\Users\Admin\source\repos\comapainternet\ComapaInternet.Api -- hash-password "ClaveDelPortal2026!"
```

Generar claves aleatorias (API Key y JWT):

```powershell
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 64
$rng.GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

Proteger el archivo de configuración:

```powershell
$archivo = "C:\inetpub\comapa-api\appsettings.Production.json"
icacls $archivo /inheritance:r
icacls $archivo /grant "*S-1-5-18:(F)" "*S-1-5-32-544:(F)" "IIS AppPool\ComapaApi:(R)"
```

> Si otros consumidores ya usan el usuario `alexis`, consérvenlo en
> `Auth:Users` además del usuario `portal`. El portal solo necesita uno.

> `*S-1-5-18` es SYSTEM y `*S-1-5-32-544` es Administradores; usar los SID
> evita problemas con servidores en español.

Reiniciar el sitio:

```powershell
Restart-WebAppPool -Name ComapaApi
```

---

## 6. Apuntar el portal a la API del servidor

### Escenario A: portal local de cada desarrollador

En el repo del portal, con user-secrets (no se versiona):

```powershell
dotnet user-secrets set "ApiSettings:BaseUrl" "http://192.168.0.199:5058/" --project AdministradorDeCuentas.csproj
dotnet user-secrets set "ApiSettings:Recibos" "http://192.168.0.199:5058/api/" --project AdministradorDeCuentas.csproj
dotnet user-secrets set "ApiSettings:RecibosApiKey" "LA_MISMA_AppSettings:ApiKey_DE_LA_API" --project AdministradorDeCuentas.csproj
dotnet user-secrets set "ApiSettings:RecibosLoginUser" "portal" --project AdministradorDeCuentas.csproj
dotnet user-secrets set "ApiSettings:RecibosLoginPassword" "ClaveDelPortal2026!" --project AdministradorDeCuentas.csproj
dotnet user-secrets set "ApiSettings:PermitirAplicarPago" "true" --project AdministradorDeCuentas.csproj
```

En Desarrollo, `Portal:PublicBaseUrl` puede quedar vacío: los retornos de EVO se
arman con `http://localhost:5199`. El equipo solo necesita que su equipo tenga
salida a Internet para cargar el checkout de EVO.

### Escenario B: portal también en el servidor

Publicar:

```powershell
cd C:\Users\Admin\source\repos\AdminCuentas
powershell -NoProfile -ExecutionPolicy Bypass -File .\publicar-portal.ps1 -Salida .\publish\portal
robocopy .\publish\portal \\192.168.0.199\C$\inetpub\comapa-portal /E /XO /R:2 /W:2
```

Crear el sitio en IIS igual que la API, pero en el puerto **5199** y con su
propio app pool (`ComapaPortal`). El portal **sí** requiere HTTPS cuando corre
en Production (las cookies de sesión y antiforgery son `Secure`), así que:

- Publicar con un certificado válido para `192.168.0.199` o `comapa-test.local`.
- Configurar `Portal:PublicBaseUrl` con la URL **https** que usarán los
  desarrolladores.

```powershell
Import-Module WebAdministration

$sitio = "ComapaPortal"
$ruta  = "C:\inetpub\comapa-portal"

New-Item -ItemType Directory -Force -Path $ruta | Out-Null
New-Item -ItemType Directory -Force -Path "$ruta\logs" | Out-Null

if (-not (Test-Path "IIS:\AppPools\$sitio")) { New-WebAppPool -Name $sitio }
Set-ItemProperty "IIS:\AppPools\$sitio" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$sitio" -Name startMode -Value "AlwaysRunning"
Set-ItemProperty "IIS:\AppPools\$sitio" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)

if (-not (Get-Website -Name $sitio -ErrorAction SilentlyContinue)) {
    New-Website -Name $sitio -PhysicalPath $ruta -ApplicationPool $sitio -Port 5199 -Force
}

# Certificado de pruebas (se reutiliza si ya existe, por ejemplo del sitio de la API).
$cert = Get-ChildItem Cert:\LocalMachine\My |
    Where-Object { $_.Subject -like "*comapa-test.local*" } |
    Select-Object -First 1

if (-not $cert) {
    $cert = New-SelfSignedCertificate `
      -Subject "CN=comapa-test.local" `
      -TextExtension @("2.5.29.17={text}DNS=comapa-test.local&IPAddress=192.168.0.199") `
      -CertStoreLocation "Cert:\LocalMachine\My" `
      -NotAfter (Get-Date).AddYears(2)
}

# HTTPS con el certificado de pruebas.
Remove-WebBinding -Name $sitio -Protocol http -Port 5199
New-WebBinding -Name $sitio -Protocol https -Port 5199 -HostHeader "" -SslFlags 0
New-Item "IIS:\SslBindings\0.0.0.0!5199" -Value $cert | Out-Null

icacls $ruta /grant "IIS AppPool\${sitio}:(OI)(CI)(RX)" /T | Out-Null
icacls "$ruta\logs" /grant "IIS AppPool\${sitio}:(OI)(CI)(M)" /T | Out-Null
```

Para diagnosticar errores de arranque del portal, editen
`C:\inetpub\comapa-portal\web.config` y habiliten `stdoutLogEnabled="true"` con
`stdoutLogFile=".\logs\stdout"`, igual que en la API.

`C:\inetpub\comapa-portal\appsettings.Production.json`:

```json
{
  "AllowedHosts": "192.168.0.199;comapa-test.local;localhost",
  "ApiSettings": {
    "BaseUrl": "http://192.168.0.199:5058/",
    "Recibos": "http://192.168.0.199:5058/api/",
    "RecibosApiKey": "LA_MISMA_AppSettings:ApiKey_DE_LA_API",
    "RecibosLoginUser": "portal",
    "RecibosLoginPassword": "ClaveDelPortal2026!",
    "RecibosUsuario": "U0293",
    "RecibosFormaPago": 4,
    "PermitirAplicarPago": true
  },
  "Evo": {
    "Merchant": "TEST1114864",
    "User": "merchant.TEST1114864",
    "Password": "PASSWORD_DE_PRUEBA_EVO",
    "InitiateCheckoutUrl": "https://evopaymentsmexico.gateway.mastercard.com/api/rest/version/100/merchant/TEST1114864/session",
    "RetrieveOrderUrl": "https://evopaymentsmexico.gateway.mastercard.com/api/rest/version/100/merchant/TEST1114864/order",
    "CheckoutScriptUrl": "https://evopaymentsmexico.gateway.mastercard.com/static/checkout/checkout.min.js",
    "MerchantName": "COMAPA VICTORIA",
    "MerchantLogo": "",
    "MerchantUrl": "",
    "ReturnUrl": "",
    "CancelUrl": ""
  },
  "Portal": {
    "PublicBaseUrl": "https://192.168.0.199:5199"
  },
  "ForwardedHeaders": {
    "Enabled": false
  }
}
```

`ForwardedHeaders:Enabled=false` es obligatorio si IIS recibe el tráfico
directo. El portal, en Production, exige proxies de confianza si los
forwarded headers están activos; con IIS en proceso no se usan.

> **Importante:** abran el portal con exactamente el mismo host que pusieron en
> `Portal:PublicBaseUrl`. La cookie de sesión es del host y EVO regresa a esa
> URL; si entran por `comapa-test.local` pero la configuración dice
> `192.168.0.199`, al volver del pago se pierde la sesión y el pago pendiente.
> Usen el mismo host (IP o nombre) en todo.

Si activaron HTTPS en la API, cambien `ApiSettings:BaseUrl` y
`ApiSettings:Recibos` a `https://192.168.0.199:5058/...` e instalen el
certificado de pruebas en **Entidades de certificación raíz de confianza** del
servidor del portal (y de cada equipo que use el portal). De lo contrario,
HttpClient rechazará el certificado autofirmado.

Si más adelante ponen un balanceador o ARR delante, cambien a:

```json
"ForwardedHeaders": {
  "Enabled": true,
  "KnownProxies": [ "192.168.0.10" ],
  "KnownNetworks": []
}
```

---

## 7. Cambios de rutas/URLs (resumen)

No hay que cambiar rutas en el código: la API conserva `/api/...` y el portal
`/HojaAdeudo`, `/pago-en-linea`, etc. Solo cambian las URLs base en configuración.

| Archivo | Clave | Valor local | Valor en el servidor |
|---|---|---|---|
| API | `AllowedHosts` | `*` | `192.168.0.199;comapa-test.local;localhost` |
| API | `ConnectionStrings:Comercial` | local | `Server=192.168.0.199,1433;Database=ComapaVic_Comercial_Globales;...` |
| API | `ConnectionStrings:Portal` | local | `Server=server165.negox.com;Database=comapavictoria_TEST;...` |
| API | `Jwt:Issuer` | `https://micuenta...` | `https://192.168.0.199:5058` (opcional) |
| Portal | `ApiSettings:BaseUrl` | `http://localhost:5058/` | `http://192.168.0.199:5058/` |
| Portal | `ApiSettings:Recibos` | `http://localhost:5058/api/` | `http://192.168.0.199:5058/api/` |
| Portal | `ApiSettings:RecibosApiKey` | secreto local | Igual a `AppSettings:ApiKey` de la API |
| Portal | `ApiSettings:RecibosLoginUser/Password` | usuario local | Usuario de `Auth:Users` de la API |
| Portal | `Portal:PublicBaseUrl` | vacío (Desarrollo) | `https://192.168.0.199:5199` (Production) |
| Portal | `ForwardedHeaders:Enabled` | n/a | `false` (IIS directo) |
| Portal | `Evo:ReturnUrl` / `CancelUrl` | vacío | vacío (se arman con `PublicBaseUrl`) |

La barra final importa: `ApiSettings:Recibos` debe terminar en `/api/`. El
código concatena `constancias/...`, `pagos/...`, etc.

Si en lugar de sitios separados publican la API como aplicación virtual
(`/api`) dentro del mismo sitio, entonces `ApiSettings:Recibos` tendría que ser
`https://host/api/api/` porque las rutas del controlador ya incluyen `/api`.
Para evitar confusiones, usen **sitios/puertos separados**.

---

## 8. Verificación

Desde otro equipo de la red:

```powershell
# 1. Salud de la API
Invoke-WebRequest http://192.168.0.199:5058/health -UseBasicParsing

# 2. Login del usuario de servicio
$login = Invoke-RestMethod -Method Post `
  -Uri "http://192.168.0.199:5058/api/auth/login" `
  -ContentType "application/json" `
  -Body (@{ username = "portal"; password = "ClaveDelPortal2026!" } | ConvertTo-Json)

# 3. Endpoint protegido (JWT + API Key)
$headers = @{
    Authorization = "Bearer $($login.token)"
    "X-API-KEY"   = "LA_MISMA_AppSettings:ApiKey_DE_LA_API"
}
Invoke-WebRequest -Uri "http://192.168.0.199:5058/api/constancias/ID/pdf" `
  -Headers $headers -OutFile "constancia.pdf"
```

Comprobar en el portal:

1. Login de un usuario de prueba.
2. Consulta de adeudo y recibos (usa la API del servidor).
3. Registro de constancia y pago con tarjeta de prueba EVO.
4. Descarga del PDF de la constancia.
5. Historial de pagos.

---

## 9. Problemas comunes

| Síntoma | Causa probable | Solución |
|---|---|---|
| `500.30` / `500.31` | Falta el runtime o el Hosting Bundle | Instalar .NET 10 Hosting Bundle y `iisreset` |
| `500.19` | Permisos o módulo ANCM | Revisar `logs\stdout_*.log`, dar lectura al app pool |
| `Configure 'AllowedHosts'...` | `*` o vacío en Production | Poner IP/hostnames reales |
| `Falta 'ConnectionStrings:Portal'` | Falta la cadena | Agregarla al JSON de producción |
| `Falta 'Portal:PublicBaseUrl'` | Portal en Production sin URL pública | Configurar la URL https del portal |
| `Configure 'ForwardedHeaders:KnownProxies'...` | IIS directo con forwarded headers activos | `"ForwardedHeaders": { "Enabled": false }` |
| Login entra y regresa al inicio | Cookies `Secure` sobre HTTP | Usar HTTPS o correr el portal en Development |
| El portal no conecta a la API | URL o API Key distintas | Revisar `ApiSettings:*` y firewall 5058 |
| `502` al pagar | El servidor no sale a Internet | Abrir HTTPS saliente a `evopaymentsmexico.gateway.mastercard.com` |
| PDF responde 500 | Faltan fuentes/recursos | Verificar `Resources\` y permisos de lectura |
| Swagger da 404 | En Production está deshabilitado | Es lo esperado; habilitarlo solo en Development |
| `robocopy` ERROR 53 | `C$` deshabilitado, SMB/445 bloqueado o no estás en la red | Copiar por RDP, publicar en el servidor o crear un share (ver abajo) |

### Si `\\192.168.0.199\C$` no responde

Primero confirmar si el comando se está ejecutando **en el servidor** o en el
equipo de desarrollo, y si SMB responde:

```powershell
hostname
Test-NetConnection 192.168.0.199 -Port 445
Get-Service LanmanServer
Get-SmbShare | Where-Object Name -eq 'C$'
```

- **Si estás en el servidor:** no uses la ruta UNC; copia a la ruta local:

  ```powershell
  robocopy .\publish\api C:\inetpub\comapa-api /E /XO /R:2 /W:2
  ```

- **Si estás en tu equipo y no hay SMB:** la opción más rápida es comprimir y
  copiar por RDP:

  ```powershell
  # Equipo de desarrollo
  Compress-Archive .\publish\api\* .\publish\api.zip -Force

  # En el servidor, después de copiar el zip por RDP
  Expand-Archive C:\Users\Admin\Downloads\api.zip C:\inetpub\comapa-api -Force
  ```

- **Si el servidor tiene el SDK**, publica directo ahí:

  ```powershell
  cd C:\Users\Admin\source\repos\comapainternet
  dotnet publish .\ComapaInternet.Api\ComapaInternet.Api.csproj -c Release -o C:\inetpub\comapa-api
  ```

  No borra `appsettings.Production.json` porque ya no se genera en el publish.

- **Si prefieren un share temporal** (en el servidor, como administrador):

  ```powershell
  New-Item C:\deploy -ItemType Directory -Force
  New-SmbShare -Name deploy -Path C:\deploy -FullAccess "$env:COMPUTERNAME\$env:USERNAME"
  ```

  Desde el equipo de desarrollo:

  ```powershell
  net use \\192.168.0.199\deploy /user:192.168.0.199\Admin "TU_CLAVE"
  robocopy .\publish\api \\192.168.0.199\deploy\comapa-api /E /XO
  net use \\192.168.0.199\deploy /delete
  ```

> En servidores endurecidos el share `C$` suele estar deshabilitado aunque
> tengas RDP. Copiar por RDP o publicar localmente evita depender de SMB.

---

## 10. Seguridad mínima para el servidor de pruebas

- No reutilizar las claves de desarrollo: generar `AppSettings:ApiKey`,
  `Jwt:Key` y la contraseña del usuario `portal`.
- Mantener `appsettings.Production.json` fuera del repositorio y con ACL
  restringida.
- No abrir 5058/5199 a Internet; solo LAN o VPN.
- Usar HTTPS cuando el portal corra en el servidor.
- Rotar el password del merchant de pruebas si estuvo en archivos compartidos.
- Recordar que `TEST1114864` es un merchant de pruebas: no procesa cargos
  reales y no debe usarse en producción.

---

## 11. Checklist para la reunión

Antes de la demostración, con el portal ya publicado:

1. **Probar desde el equipo que se usará en la reunión**, no solo desde el
   servidor:

   ```powershell
   Invoke-WebRequest https://192.168.0.199:5199/ -UseBasicParsing
   Invoke-WebRequest http://192.168.0.199:5058/health -UseBasicParsing
   ```

2. **Confiar en el certificado** en el equipo de la reunión (si usan
   autofirmado). En el servidor:

   ```powershell
   Export-Certificate -Cert $cert -FilePath C:\temp\comapa-test.cer
   ```

   En el equipo de la reunión, como administrador:

   ```powershell
   Import-Certificate -FilePath .\comapa-test.cer -CertStoreLocation Cert:\LocalMachine\Root
   ```

   Sin esto, el navegador mostrará una advertencia de seguridad al abrir el
   portal; en algunos equipos las políticas del navegador pueden bloquear el
   acceso hasta que el certificado sea de confianza.

3. **Calentar el sitio** antes de la reunión: abrir login, consulta y una
   constancia. El primer request después de un reciclado tarda más.

4. **Dejar el API y el portal en marcha**:

   ```powershell
   Set-ItemProperty "IIS:\AppPools\ComapaApi" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
   Set-ItemProperty "IIS:\AppPools\ComapaPortal" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
   Start-Website -Name ComapaApi
   Start-Website -Name ComapaPortal
   ```

5. **Probar el flujo completo** que se va a mostrar:
   - login con el usuario de pruebas,
   - consulta de adeudo/recibos,
   - constancia: registro, pago con tarjeta de prueba EVO, comprobante y
     descarga del PDF,
   - historial de pagos.

6. **Tener un plan B**: si el Internet del lugar falla, EVO no cargará. Tengan
   capturas o un video del flujo de pago; el resto del portal funciona en la
   LAN.

7. **No dejar datos QA** de las pruebas en la base antes de la demostración
   (constancias, ventas y vínculos temporales).

8. **Revisar el firewall** del servidor y que el equipo de la reunión esté en
   la misma red/VPN.

> Si no quieren usar certificado para la reunión, la alternativa es correr el
> portal con `ASPNETCORE_ENVIRONMENT=Development` sobre HTTP y configurar los
> secretos con variables de entorno. Es más rápido, pero no representa la
> configuración de producción y muestra errores detallados; úsenlo solo para la
> demo y regresen a Production/HTTPS después.

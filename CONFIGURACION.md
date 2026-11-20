# Configuración y rotación de secretos

Este portal ya no contiene credenciales de servicio en el código ni en JavaScript.
Los valores sensibles deben configurarse por entorno:

- **Desarrollo:** `dotnet user-secrets`.
- **Producción:** variables de entorno de App Service o Azure Key Vault.

## Claves requeridas

| Clave | Uso |
|---|---|
| `ApiSettings:BaseUrl` | `ComapaInternet.Api`: endpoint de compatibilidad `/API/Transaction` (login, registro y cuentas). En producción debe ser el mismo dominio que `ApiSettings:Recibos`. |
| `ApiSettings:Recibos` | URL base de `ComapaInternet.Api` (debe terminar en `/api/`). |
| `ApiSettings:RecibosApiKey` | API Key de `ComapaInternet.Api`. |
| `ApiSettings:RecibosLoginUser` | Usuario de servicio de `ComapaInternet.Api`. |
| `ApiSettings:RecibosLoginPassword` | Contraseña del usuario de servicio. |
| `ApiSettings:RecibosUsuario` | Usuario interno que se envía en `AplicarPago` (por defecto `U0293`). |
| `ApiSettings:RecibosFormaPago` | Forma de pago interna (por defecto `4`). |
| `ApiSettings:PermitirAplicarPago` | Interruptor de aplicación de pagos. Por defecto `false`. |
| `Evo:*` | Configuración de la pasarela EVO. Los valores de ejemplo provienen de `PrototipoWebSitioComapa` (pasarela de pruebas). |
| `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Password`, `Smtp:From` | Envío del correo de restablecimiento. |

## Ejemplo para desarrollo

```powershell
dotnet user-secrets set "ApiSettings:RecibosApiKey" "..." --project AdministradorDeCuentas.csproj
dotnet user-secrets set "ApiSettings:RecibosLoginUser" "..." --project AdministradorDeCuentas.csproj
dotnet user-secrets set "ApiSettings:RecibosLoginPassword" "..." --project AdministradorDeCuentas.csproj
```

## Rotación obligatoria

Estos valores estuvieron expuestos en el repositorio o en JavaScript y deben rotarse
en `ComapaInternet.Api`, no solo en el portal:

1. **API Key** de `ComapaInternet.Api` (`AppSettings:ApiKey`).
2. **Contraseña del usuario de servicio** `alexis` (el portal ya no la usa).
3. **Credenciales SMTP** que estaban en `EmailService`.
4. **Cadena de conexión SQL** que estaba en `appsettings.json` (el portal no la usa).
5. **Clave JWT** si se considera comprometida.

Después de rotar, actualizar las variables del App Service y reiniciar.

## Pago público (sin cuenta)

La página `/pago-en-linea` es pública, igual que en `PrototipoWebSitioComapa`:
no exige iniciar sesión ni administrar cuentas. Solo pide el número de cuenta y
muestra el adeudo para pagar con tarjeta.

Configuración de EVO tomada del sitio de pago de referencia. Los valores de
prueba viven en `appsettings.Development.json`; en producción **no** se versionan
y deben configurarse por variables de entorno / Key Vault:

- `Evo:Merchant`: merchant de pruebas `TEST1114864`.
- `Evo:User`: `merchant.TEST1114864`.
- `Evo:Password`: **secreto**, debe configurarse por user-secrets/variables.
- URLs base: `https://evopaymentsmexico.gateway.mastercard.com/...`.
- `Evo:CheckoutScriptUrl`: `.../static/checkout/checkout.min.js`.

En producción reemplace el merchant, usuario, URLs y contraseña por los
productivos. `Evo:ReturnUrl` y `Evo:CancelUrl` pueden quedar vacías: el portal
las construye con el host de la petición (`/pago-en-linea/retorno` y
`/pago-en-linea`). Si el dominio público no coincide con el host interno,
configúrelas explícitamente.

## API única del portal

El portal ya no consume `AppiPipasPruebas` para login, registro ni cuentas.
Todo el portal pasa por `ComapaInternet.Api`:

- `API/Transaction` (compatibilidad): `LoginServicios` y `CuentaUsuarios`.
- `comercial/*`: historial y PDF de recibos.
- `pagos/*`: consulta de adeudo y aplicación de pagos.

En la API, la compatibilidad usa `ConnectionStrings:Portal` (base
`comapavictoria_TEST`) y los mismos stored procedures del portal anterior:
`dbo.SPIUB_MAE_USUARIO_WEB`, `dbo.SPS_MAE_USUARIO_WEB_CUENTA` y
`dbo.SPIUB_MAE_USUARIO_WEB_CUENTA`. El hash PBKDF2 se conserva para no invalidar
las contraseñas existentes.

`AppiPipasPruebas` sigue existiendo para pipas, tandeo y otras aplicaciones,
pero el portal ya no depende de ella.

Pendiente de fase 2: sustituir `API/Transaction` por endpoints REST con JWT
ciudadano y retirar los modelos de transacción genéricos del portal.

## Entorno local de pruebas

Para probar el pago completo en la máquina local:

1. Arrancar `ComapaInternet.Api` en `http://localhost:5058`.
2. Configurar en la API (user-secrets, no se versionan):
   ```powershell
   dotnet user-secrets set "ConnectionStrings:Portal" "..." --project ComapaInternet.Api.csproj
   dotnet user-secrets set "ServicioReportes:ApiKey" "..." --project ComapaInternet.Api.csproj
   ```
   `ConnectionStrings:Portal` es la base `comapavictoria_TEST` que usaba
   `AppiPipasPruebas`.
3. Configurar en AdminCuentas (user-secrets, no se versionan):
   ```powershell
   dotnet user-secrets set "ApiSettings:BaseUrl" "http://localhost:5058/" --project AdministradorDeCuentas.csproj
   dotnet user-secrets set "ApiSettings:Recibos" "http://localhost:5058/api/" --project AdministradorDeCuentas.csproj
   dotnet user-secrets set "ApiSettings:RecibosApiKey" "..." --project AdministradorDeCuentas.csproj
   dotnet user-secrets set "ApiSettings:RecibosLoginUser" "test" --project AdministradorDeCuentas.csproj
   dotnet user-secrets set "ApiSettings:RecibosLoginPassword" "..." --project AdministradorDeCuentas.csproj
   dotnet user-secrets set "Evo:Password" "..." --project AdministradorDeCuentas.csproj
   dotnet user-secrets set "ApiSettings:PermitirAplicarPago" "true" --project AdministradorDeCuentas.csproj
   ```
4. Arrancar AdminCuentas y abrir `http://localhost:5199/pago-en-linea`.
5. El `estado` de la API debe devolver `suspendido: false`.
6. Usar una cuenta de prueba con adeudo. `PermitirAplicarPago=true` aplica el
   pago en la base de datos que use la API; no usar cuentas reales.

`appsettings.Development.json` ya contiene el merchant y las URLs de prueba de
EVO (`TEST1114864`). El `Evo:Password` y la `ApiSettings:RecibosApiKey` se toman
de user-secrets.

La contraseña del usuario de servicio `test` de `ComapaInternet.Api` se rotó
en el entorno local para poder probar recibos. Si otro consumidor usa ese
usuario, debe actualizar sus credenciales.

El login/registro ciudadano ahora usa `ComapaInternet.Api` a través de
`API/Transaction`; ya no depende de `AppiPipasPruebas`.

### Estado del PDF de recibos

El historial de recibos funciona. La generación del PDF está bloqueada por el
servicio externo `reportes.comapavictoria.org`: responde `500` con
`TypeInitializationException` de `CrystalDecisions.CrystalReports.Engine.ReportDocument`.
La API ahora devuelve `502` y el portal muestra un error controlado. Hay que
revisar el runtime de Crystal Reports en el servidor de reportes.

## Tarjetas de prueba EVO (México)

Documentación: `https://evopaymentsmexico.gateway.mastercard.com/api/documentation/integrationGuidelines/supportedFeatures/testAndGoLive.html`

Tarjetas de prueba de México:

| Tarjeta | Número | 3DS |
|---|---|---|
| Visa | `4012000033330026` | No* |
| Mastercard | `5424180279791732` | Sí |
| Mastercard | `5200030000000004` | Sí |
| Mastercard | `2223000000000007` | Sí |
| Visa | `4002260000000000` | No |
| Mastercard | `2223000000000023` | No |

\* En la prueba real, la Visa `4012000033330026` abrió el emulador 3DS.
Seleccionar `(Y) Authentication/Account Verification Successful` y enviar.

Respuestas por fecha de vencimiento:

| Vencimiento | Resultado |
|---|---|
| `01/39` | Aprobada |
| `04/28` | Rechazada |
| `02/30` | Tarjeta expirada |
| `03/31` | Error del emisor |
| `01/30` | Fondos insuficientes |

CVV de prueba: `100` (México no valida CSC/CVV, pero se puede enviar).

### Prueba realizada

- Cuenta: `772936`
- Importe: `$1,160.00`
- Tarjeta: Visa `4012000033330026`, vencimiento `01/39`, CVV `100`
- Folio: `P11141604`
- Orden: `772936_F-12216471`
- Resultado: pago aplicado y adeudo de la cuenta en `$0.00`.

La cuenta `772936` quedó pagada en la base de pruebas. Para repetir la prueba,
usar otra de las cuentas con adeudo.

### Prueba de rechazo

- Cuenta: `767656`
- Tarjeta: Visa `4012000033330026`, vencimiento `04/28`, CVV `100`
- Resultado EVO: **Transacción Declinada**
- Efecto: no se aplicó ningún pago; el adeudo permaneció en `$1,160.00`.

La pasarela muestra el mensaje de rechazo dentro del checkout y permite reintentar.
El portal no recibe un `resultIndicator` de éxito, por lo que no llama a
`AplicarPago`.

## Antiforgery y CSP

- Todos los POST/PUT/PATCH/DELETE de MVC pasan por `AutoValidateAntiforgeryToken`.
- El token se renderiza en `_AntiforgeryMeta.cshtml` y `site.js` lo envía en la
  cabecera `RequestVerificationToken` para todas las peticiones jQuery.
- La CSP se construye en `Program.cs` y permite los CDN usados por el portal
  (Bootstrap, Font Awesome, SweetAlert2, DataTables y Google Fonts), además de
  los orígenes de EVO.
- La CSP usa `'unsafe-inline'` para scripts y estilos porque las vistas todavía
  tienen bloques inline. El siguiente paso es migrar a nonces/hashes y quitarlo.
- `frame-src` permite `https:` porque el reto 3-D Secure puede abrir marcos de
  emisores distintos. `script-src` sigue restringido.
- La CSP incluye `https://*.gateway.mastercard.com`, que es el dominio real de
  EVO en el sitio de referencia (`evopaymentsmexico.gateway.mastercard.com`).
- Si EVO cambia de dominio o el checkout usa otro host para `connect`, agregar el
  origen en `Csp:AdditionalScriptOrigins`, `Csp:AdditionalConnectOrigins` o
  `Csp:AdditionalFrameOrigins` en `appsettings.json` / App Settings.

## Pendiente del lado de la API

- Verificar el estado real de la orden con EVO antes de `AplicarPago`.
- Persistir la orden en `RegistrarIntento` también en modo Comapa y exigirla en `AplicarPago`.
- Exigir JWT/rol en `PagosController`, no solo la API Key.
- Restringir la API por red (access restrictions / private endpoint / WAF).
- Enmascarar el nombre en `ObtenAdeudo` y limitar consultas por cuenta/IP.

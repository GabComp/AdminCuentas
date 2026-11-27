# Configuración y rotación de secretos

Este portal ya no contiene credenciales de servicio en el código ni en JavaScript.
Los valores sensibles deben configurarse por entorno:

- **Desarrollo:** `dotnet user-secrets`.
- **Producción:** variables de entorno de App Service o Azure Key Vault.

Para publicar la API y el portal en el servidor de pruebas de Windows Server
2025 (IIS, `192.168.0.199`), seguir `DESPLIEGUE-PRUEBAS.md`.

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
| `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Password`, `Smtp:From` | Envío de correo por SMTP (códigos, restablecimiento y comprobantes). |
| `Resend:ApiKey`, `Resend:From` | Envío por la API HTTP de Resend (HTTPS 443). Si `Resend:ApiKey` está definida, tiene prioridad sobre SMTP. |

## Ejemplo para desarrollo

```powershell
dotnet user-secrets set "ApiSettings:RecibosApiKey" "..." --project AdministradorDeCuentas.csproj
dotnet user-secrets set "ApiSettings:RecibosLoginUser" "..." --project AdministradorDeCuentas.csproj
dotnet user-secrets set "ApiSettings:RecibosLoginPassword" "..." --project AdministradorDeCuentas.csproj
```

### Correo transaccional: Resend o SMTP

El portal usa **Resend por HTTPS** si existe `Resend:ApiKey`; si no, usa SMTP
con MailKit. Ambos sirven para los códigos de registro, el restablecimiento y
los comprobantes.

Resend (recomendado: solo requiere el puerto 443):

```json
"Resend": {
  "ApiKey": "re_...",
  "From": "no-reply@tudominio-verificado.com"
}
```

SMTP:

```json
"Smtp": {
  "Host": "smtp.tudominio.com",
  "Port": 587,
  "User": "no-reply@tudominio.com",
  "Password": "...",
  "From": "no-reply@tudominio.com",
  "EnableSsl": true
}
```

En Resend el remitente debe pertenecer a un dominio verificado. Para pruebas
sin dominio propio, `onboarding@resend.dev` solo entrega al correo dueño de la
cuenta de Resend.

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

### Prueba del pago de constancia en línea

Con el merchant de pruebas `TEST1114864` se puede cerrar el flujo completo
(registro → pago EVO → constancia en estatus 2):

1. Vincular al usuario de prueba una cuenta elegible (sin adeudo y con medidor).
2. Registrar la constancia en **Constancia de no Adeudo** y pulsar
   **Pagar en línea**.
3. En el formulario de EVO usar una tarjeta de prueba de México
   ([documentación oficial](https://evopaymentsmexico.gateway.mastercard.com/api/documentation/integrationGuidelines/supportedFeatures/testAndGoLive.html?locale=en_US)):
   - Visa `4012000033330026` (sin 3DS), vencimiento `01/39`, CVV `100`.
   - Mastercard `5424180279791732` (con 3DS), vencimiento `01/39`, CVV `100`.
   - El emulador 3DS aparece en el mismo modal; dejar el resultado `(Y)` y
     enviar.
4. Al volver, el comprobante muestra folio de pago, folio de constancia e
   importe `$204.13`, y la tabla queda con `id_estatus = 2` e `id_pago`
   capturado.
5. Pulsar **Descargar constancia**: el PDF debe abrir una sola hoja y el QR debe
   resolver a `https://validador.comapavictoria.gob.mx/Validar?token=...`.
6. Borrar después la constancia y el vínculo de prueba, y la venta de "otros"
   que generó `usp_GuardarCobro` (`Ventanillas.Opr_Ventas` y `Opr_DetVentas`).

La contraseña del usuario de servicio `test` de `ComapaInternet.Api` se rotó
en el entorno local para poder probar recibos. Si otro consumidor usa ese
usuario, debe actualizar sus credenciales.

El login/registro ciudadano ahora usa `ComapaInternet.Api` a través de
`API/Transaction`; ya no depende de `AppiPipasPruebas`.

### Generación del PDF de recibos

El PDF se genera dentro de `ComapaInternet.Api`; ya no depende del servicio
externo de Crystal Reports.

- Librerías: `PDFsharp 6.1.1` (MIT), `SkiaSharp` (MIT) y `ZXing.Net` (Apache 2.0).
- Datos: `Internet.usp_BuscarRecibo` y después
  `Facturacion.usp_PrintRecibosV2` (recibo actual) o
  `Facturacion.usp_PrintRecibosFacturas` (histórico), con los mismos filtros
  que usaba Crystal.
- El PDF se arma en memoria y se transmite al portal: **no se guarda en disco,
  base de datos, blob ni caché**.
- `Reportes:Generador` en `appsettings.json`:
  - `local` (predeterminado): generador nuevo.
  - `crystal`: vuelve temporalmente al motor anterior (`ServicioReportes`).
- Códigos de barras Code128 para el pago y leyenda con la referencia; gráfica de
  consumo de los últimos 6 meses contra el año anterior.
- El recibo incluye: datos del titular/comercio (RFC, clase, ruta,
  localización, giro), datos del consumo (servicio, tarifa, medidor, lecturas,
  fechas, incumplimiento), estado de cuenta con subtotal/IVA/adeudo, resumen de
  documentos, historial de consumo y bloque de pago con cuenta y mes facturado.
  Se omiten los anuncios externos que trae la versión impresa oficial.
- El contrato `POST comercial/ObtnRecibo` no cambia.

Fuentes: en Windows se usa Arial del sistema. En Linux/contenedores instale
`fonts-liberation` (o configure `Reportes:FuenteRegular` y
`Reportes:FuenteNegrita` con rutas a archivos TTF). Si desea que no dependa del
sistema operativo, copie los TTF en `Resources/fonts` del proyecto.

El servicio es sin estado: puede escalarse horizontalmente sin coordinación
entre instancias.

## Constancias de no adeudo

- La página `/HojaAdeudo/HojaAdeudo` permite **pre-registrar** una solicitud
  para la cuenta vinculada del usuario.
- La API valida: sin adeudo, sin negociaciones, sin saldos congelados, sin
  constancia en trámite, sin sentencia y con medidor instalado o en trámite.
- El registro se inserta en `Ventanillas.Opr_ConstanciasNoAdeudo` con estatus
  **1 (Solicitado)**. El folio se genera por trigger de la base.
- El cobro y la impresión se concluyen en sucursal (costo $204.13 con IVA); el
  portal permite pagar la constancia en línea con EVO y la deja en estatus
  **2 (Cobrado)** con el folio de pago en `id_pago`. Al pagar, la API también
  fija la fecha de emisión (`fecha_genero`), el mes y el texto de la fecha, y
  genera la URL de validación con el `Token` de la constancia.
- Configuración API: `Constancias:UsuarioGenero` (por defecto `WEB01`) es el
  código que queda en `id_genero` para identificar solicitudes web;
  `Constancias:Sucursal`, `Constancias:Operador` y `Constancias:FormaPago`
  definen los datos del cobro. `Constancias:ValidadorUrl` (por defecto
  `https://validador.comapavictoria.gob.mx/Validar`) alimenta el QR;
  `Constancias:Firmante` y `Constancias:FirmanteCargo` se imprimen en la firma.
- Endpoints: `POST /api/constancias/validar`,
  `POST /api/constancias/solicitar`, `POST /api/constancias/preview-pago`,
  `POST /api/constancias/aplicar-pago` y
  `GET /api/constancias/{id}/pdf` (JWT + API Key).

### Constancia en PDF

- `ConstanciaPdfService` arma la constancia en memoria con PDFsharp/SkiaSharp y
  no la persiste. Una hoja tamaño carta con la misma estructura del sistema de
  ventanillas: encabezado con logotipos, fecha, `CONSTANCIA NÚMERO/{folio}`,
  titular, destinatario, párrafos justificados, firma y QR de validación.
- Tipo **1 (notaría)**: destinatario `ATENCION AL` + `dato_notaria` + `notaria`.
  Tipo **2 (usuario)**: `A QUIEN CORRESPONDA.`
- Solo se genera si el estatus es **2 (Cobrado)** o **3 (Impreso)**. En estatus
  **1** responde `409` y en constancias canceladas o inexistentes, error.
- El portal lo descarga con
  `GET /HojaAdeudo/DescargarConstancia?idConstancia=...&idCuenta=...`, validando
  que la cuenta pertenezca al usuario en sesión. El botón aparece en el
  comprobante de pago y en la página de la constancia ya pagada.
- La tipografía es Courier New (monoespaciada). Si no está instalada se usa
  Arial; se puede forzar con `Reportes:FuenteMonoRegular` y
  `Reportes:FuenteMonoNegrita`.

## Seguridad y despliegue

- Los endpoints sensibles de la API (`/API/Transaction`, `/api/pagos/*`,
  `/api/reportes/*`, `/api/comercial/*`) exigen **JWT de servicio + API Key**.
  El portal adjunta el token automáticamente con `ApiTokenHandler`.
- Los secretos (cadena de conexión, API Key, llave JWT, credenciales SMTP/EVO)
  se configuran por variables de entorno o Key Vault. No se versionan:
  `appsettings.Production.json` está en `.gitignore`.
- En producción el portal exige:
  - `AllowedHosts` con dominios reales (no `*`).
  - `Portal:PublicBaseUrl` absoluta `https`.
  - `ForwardedHeaders:KnownProxies` o `KnownNetworks` si está detrás de un
    proxy/balanceador. Sin estos valores el arranque falla para evitar
    falsificación de `X-Forwarded-For`.
- La API también exige `AllowedHosts` reales en producción.
- Rotar la API Key, la llave JWT y las contraseñas de BD si el archivo
  `appsettings.Production.json` se compartió o copió fuera del servidor.
- `/health` de la API responde un resumen mínimo y cachea el resultado 30 s.
  El detalle completo solo se muestra enviando `X-HEALTH-KEY` con
  `Health:DetalleKey`.
- La CSP usa nonces para scripts inline; no se permite `'unsafe-inline'` en
  `script-src`.
- El restablecimiento de contraseña responde siempre de forma genérica para no
  revelar si el correo existe.

Pendiente recomendado: rotar el identificador de sesión al iniciar sesión y
migrar el restablecimiento a un token de un solo uso con expiración forzada.

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
  (Bootstrap, Font Awesome, SweetAlert2 y Google Fonts), además de los orígenes
  de EVO.
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

## Monitoreo y operación

### Health checks

El portal expone:

- `GET /health` — liveness: responde `200` si el proceso está vivo.
- `GET /health/ready` — readiness: valida la API de compatibilidad y responde
  `200` o `503` según su estado.

Configurar en App Service **Health check** la ruta `/health` (o `/health/ready`).
El endpoint de readiness consulta `ApiSettings:BaseUrl + "health"`.

### Secretos en producción

- Usar **Azure Key Vault** (o variables de entorno) para `ApiSettings:*`,
  `Evo:*`, `Smtp:*` y `ConnectionStrings`. No versionar valores en `appsettings.json`.
- La API (`ComapaInternet.Api`) usa `ConnectionStrings:Comercial`,
  `ConnectionStrings:Portal`, `Jwt:Key` y `AppSettings:ApiKey` desde
  user-secrets / entorno.

### Telemetría (recomendado)

- Habilitar **Application Insights** para logs estructurados, trazas de
  dependencias y métricas de las solicitudes.
- Registrar en App Service las alertas de disponibilidad sobre `/health`.

### Pruebas y CI

- Proyecto de pruebas: `AdministradorDeCuentas.Tests` (xUnit).
  - Ejecutar: `dotnet test AdministradorDeCuentas.sln`.
- CI: `.github/workflows/ci.yml` (GitHub Actions): restaura, compila y ejecuta
  las pruebas en cada push/PR.


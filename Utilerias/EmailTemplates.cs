using System.Net;

namespace PrototipoComapa.Utilerias
{
    public static class EmailTemplates
    {
        private const string Guinda = "#691C32";
        private const string GuindaOscuro = "#4D1222";
        private const string Dorado = "#BC955C";

        public static string Restablecimiento(string usuario, string codigo, string urlCambio)
        {
            var cuerpo = $$"""
                <h2 style="margin-top:0;">Hola {{WebUtility.HtmlEncode(usuario)}},</h2>

                <p style="font-size:16px; line-height:1.6;">
                    Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.
                </p>

                <p style="font-size:16px; line-height:1.6;">
                    Si realizaste esta solicitud, la siguiente contraseña temporal te permitirá ingresar al portal:
                </p>

                <center><h1 style="letter-spacing:4px;">{{WebUtility.HtmlEncode(codigo)}}</h1></center>

                <div style="text-align:center; margin:35px 0;">
                    <a href="{{WebUtility.HtmlEncode(urlCambio)}}"
                       style="background:{{Guinda}}; color:#ffffff; text-decoration:none; padding:14px 30px; border-radius:5px; display:inline-block; font-size:16px; font-weight:bold;">
                        CAMBIAR CONTRASEÑA
                    </a>
                </div>

                <p style="font-size:14px; color:#666666;">
                    Por seguridad, cambia esta contraseña al iniciar sesión.
                </p>

                <p style="font-size:14px; color:#666666;">
                    Si no solicitaste este cambio, ignora este correo y contacta a la sucursal.
                </p>
                """;

            return Plantilla("Recuperación de contraseña", cuerpo);
        }

        public static string CodigoVerificacion(string codigo)
        {
            var cuerpo = $$"""
                <h2 style="margin-top:0;">Verifica tu correo</h2>

                <p style="font-size:16px; line-height:1.6;">
                    Usa el siguiente código para completar tu registro en el portal de COMAPA Victoria.
                    Es válido por 10 minutos.
                </p>

                <center>
                    <h1 style="letter-spacing:8px; background:#F7F3EC; border:1px dashed {{Dorado}}; padding:18px 24px; border-radius:8px; display:inline-block;">
                        {{WebUtility.HtmlEncode(codigo)}}
                    </h1>
                </center>

                <p style="font-size:14px; color:#666666;">
                    Si no solicitaste este registro, ignora este correo.
                </p>
                """;

            return Plantilla("Código de verificación", cuerpo);
        }

        public static string Bienvenida(string usuario, string correo)
        {
            var cuerpo = $$"""
                <h2 style="margin-top:0;">¡Hola {{WebUtility.HtmlEncode(usuario)}}!</h2>

                <p style="font-size:16px; line-height:1.6;">
                    Tu cuenta del portal de servicios de COMAPA Victoria se creó correctamente con el correo
                    <b>{{WebUtility.HtmlEncode(correo)}}</b>.
                </p>

                <p style="font-size:16px; line-height:1.6;">
                    Ya puedes iniciar sesión para vincular tus cuentas, consultar tus recibos y pagar en línea.
                </p>
                """;

            return Plantilla("Bienvenido al portal", cuerpo);
        }

        public static string CuentaVinculada(string usuario, int cuenta)
        {
            var cuerpo = $$"""
                <h2 style="margin-top:0;">Hola {{WebUtility.HtmlEncode(usuario)}},</h2>

                <p style="font-size:16px; line-height:1.6;">
                    Se vinculó la cuenta <b>{{WebUtility.HtmlEncode(cuenta.ToString())}}</b> a tu usuario del portal.
                    A partir de ahora podrás consultar sus recibos y realizar pagos.
                </p>

                <p style="font-size:14px; color:#666666;">
                    Si no reconoces esta acción, contacta de inmediato a la sucursal.
                </p>
                """;

            return Plantilla("Cuenta vinculada", cuerpo);
        }

        public static string PagoComprobante(
            string nombre,
            int cuenta,
            decimal importe,
            string folio,
            DateTime fecha)
        {
            var cuerpo = $$"""
                <h2 style="margin-top:0;">Hola {{WebUtility.HtmlEncode(nombre)}},</h2>

                <p style="font-size:16px; line-height:1.6;">
                    Confirmamos la recepción de tu pago en línea. Este correo es solo informativo y
                    <b>no sustituye el comprobante oficial</b>.
                </p>

                <table width="100%" cellpadding="12" cellspacing="0"
                       style="border:1px solid #e2e5e8; border-collapse:collapse; margin:20px 0;">
                    <tr>
                        <td style="background:#f8f9fa; border-bottom:1px solid #e2e5e8;"><b>Cuenta</b></td>
                        <td style="border-bottom:1px solid #e2e5e8;">{{WebUtility.HtmlEncode(cuenta.ToString())}}</td>
                    </tr>
                    <tr>
                        <td style="background:#f8f9fa; border-bottom:1px solid #e2e5e8;"><b>Importe</b></td>
                        <td style="border-bottom:1px solid #e2e5e8;">{{importe.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("es-MX"))}}</td>
                    </tr>
                    <tr>
                        <td style="background:#f8f9fa; border-bottom:1px solid #e2e5e8;"><b>Folio</b></td>
                        <td style="border-bottom:1px solid #e2e5e8;">{{WebUtility.HtmlEncode(folio)}}</td>
                    </tr>
                    <tr>
                        <td style="background:#f8f9fa;"><b>Fecha</b></td>
                        <td>{{fecha.ToString("dd/MM/yyyy HH:mm")}}</td>
                    </tr>
                </table>

                <p style="font-size:14px; color:#666666;">
                    El comprobante oficial está disponible en la sección "Consultar recibos" del portal.
                </p>
                """;

            return Plantilla("Comprobante de pago", cuerpo);
        }

        private static string Plantilla(string titulo, string cuerpo)
        {
            var tituloHtml = WebUtility.HtmlEncode(titulo);

            return $$"""
                <body style="margin:0; padding:0; background-color:#f4f4f4; font-family:Arial, Helvetica, sans-serif;">
                    <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#f4f4f4; padding:20px 0;">
                        <tr>
                            <td align="center">
                                <table width="600" cellpadding="0" cellspacing="0" border="0"
                                       style="background:#ffffff; border-radius:10px; overflow:hidden;">
                                    <tr>
                                        <td align="center"
                                            style="background:{{Guinda}}; color:#ffffff; padding:10px; border-bottom:3px solid {{Dorado}};">
                                            <h1 style="margin:0;">{{tituloHtml}}</h1>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:40px 30px; color:#333333;">
                                            {{cuerpo}}
                                        </td>
                                    </tr>
                                    <tr>
                                        <td align="center"
                                            style="background:#f8f9fa; padding:20px; color:#666666; font-size:12px;">
                                            © {{DateTime.Now.Year}} COMAPA Victoria. Todos los derechos reservados.
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                """;
        }
    }
}

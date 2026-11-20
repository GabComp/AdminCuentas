using System.Net;

namespace PrototipoComapa.Utilerias
{
    public static class EmailTemplates
    {
        public static string Restablecimiento(string usuario, string codigo, string urlCambio)
        {
            var usuarioHtml = WebUtility.HtmlEncode(usuario);
            var codigoHtml = WebUtility.HtmlEncode(codigo);
            var urlHtml = WebUtility.HtmlEncode(urlCambio);

            return $$"""
                <body style="margin:0; padding:0; background-color:#f4f4f4; font-family:Arial, Helvetica, sans-serif;">
                    <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#f4f4f4; padding:20px 0;">
                        <tr>
                            <td align="center">
                                <table width="600" cellpadding="0" cellspacing="0" border="0"
                                       style="background:#ffffff; border-radius:10px; overflow:hidden;">
                                    <tr>
                                        <td align="center"
                                            style="background:#AB0033; color:#ffffff; padding:10px;">
                                            <h1 style="margin:0;">Recuperación de Contraseña</h1>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:40px 30px; color:#333333;">
                                            <h2 style="margin-top:0;">Hola {{usuarioHtml}},</h2>

                                            <p style="font-size:16px; line-height:1.6;">
                                                Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.
                                            </p>

                                            <p style="font-size:16px; line-height:1.6;">
                                                Si realizaste esta solicitud, la siguiente contraseña temporal te permitirá ingresar al portal:
                                            </p>

                                            <center><h1>{{codigoHtml}}</h1></center>

                                            <div style="text-align:center; margin:35px 0;">
                                                <a href="{{urlHtml}}"
                                                   style="background:#AB0033;
                                                          color:#ffffff;
                                                          text-decoration:none;
                                                          padding:14px 30px;
                                                          border-radius:5px;
                                                          display:inline-block;
                                                          font-size:16px;
                                                          font-weight:bold;">
                                                    CAMBIAR CONTRASEÑA
                                                </a>
                                            </div>

                                            <p style="font-size:14px; color:#666666;">
                                                Por seguridad, cambia esta contraseña al iniciar sesión.
                                            </p>

                                            <p style="font-size:14px; color:#666666;">
                                                Si no solicitaste este cambio, ignora este correo y contacta a la sucursal.
                                            </p>

                                            <hr style="border:none; border-top:1px solid #dddddd; margin:30px 0;">

                                            <p><b>LE RECOMENDAMOS CAMBIAR SU CONTRASEÑA EN SU PRÓXIMO INICIO DE SESIÓN</b></p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td align="center"
                                            style="background:#f8f9fa; padding:20px; color:#666666; font-size:12px;">
                                            © {{DateTime.Now.Year}} COMAPA - Cd. Victoria, Tamaulipas. Todos los derechos reservados.
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

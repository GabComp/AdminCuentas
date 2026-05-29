using System.Text.Json.Serialization;
using static PrototipoComapa.Models.Restablecer.RestableceContra;

namespace PrototipoComapa.Models.UsuarioWeb
{
    public class UsuarioWebModel
    {
        #region Login
        public class UsuarioWebRegistroRequest
        {
            public string ACCION { get; set; }
            public string USUARIO { get; set; }
            public string CONTRASENIA { get; set; }
            public string CORREO_ELECTRONICO { get; set; }
            public string TELEFONO_CEL { get; set; }
            public string MOTIVO_BAJA { get; set; }
            public string ID_USUARIO { get; set; }
        }
        public class UsuarioWebRegistroResponse
        {
            public bool Exito { get; set; }
            public List<objReponse> Datos { get; set; }
            public string Mensaje { get; set; }
            public string Url { get; set; }
            public Usuario User { get; set; }
        }
        public class objReponse
        {
            [JsonPropertyName("mensaje")]
            public string mensaje { get; set; }
            [JsonPropertyName("mensaje2")]
            public string mensaje2 { get; set; }
            [JsonPropertyName("NOMBRE")]
            public string NOMBRE { get; set; }
            [JsonPropertyName("CORREO")]
            public string CORREO { get; set; }
        }
        #endregion

        public class EnivoCorreoRequest
        {
            public UsuarioWebRegistroRequest UserDatos { get; set; }
            public RestablecerContraRequest BotTest { get; set; }
        }

        public class Usuario
        {
            public string USUARIO { get; set; }
            public string CORREO_ELECTRONICO { get; set; }
        }
    }
}

using System.Text.Json.Serialization;

namespace PrototipoComapa.Models.UsuarioWeb
{
    public class UsuarioWebModel
    {
        #region Login
        public class UsuarioWebRegistroRequest
        {
            public string ACCION { get; set; } = string.Empty;
            public string USUARIO { get; set; } = string.Empty;
            public string CONTRASENIA { get; set; } = string.Empty;
            public string CORREO_ELECTRONICO { get; set; } = string.Empty;
            public string TELEFONO_CEL { get; set; } = string.Empty;
            public string MOTIVO_BAJA { get; set; } = string.Empty;
            public string ID_USUARIO { get; set; } = string.Empty;
            public string CODIGO { get; set; } = string.Empty;
        }

        public class UsuarioWebRegistroResponse
        {
            public bool Exito { get; set; }
            public List<objReponse> Datos { get; set; } = new();
            public string Mensaje { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public Usuario? User { get; set; }
        }

        public class objReponse
        {
            [JsonPropertyName("mensaje")]
            public string mensaje { get; set; } = string.Empty;

            [JsonPropertyName("mensaje2")]
            public string mensaje2 { get; set; } = string.Empty;

            [JsonPropertyName("mensaje3")]
            public string mensaje3 { get; set; } = string.Empty;

            [JsonPropertyName("NOMBRE")]
            public string NOMBRE { get; set; } = string.Empty;

            [JsonPropertyName("CORREO")]
            public string CORREO { get; set; } = string.Empty;
        }
        #endregion

        public class EnivoCorreoRequest
        {
            public UsuarioWebRegistroRequest UserDatos { get; set; } = new();
        }

        public class Usuario
        {
            public string USUARIO { get; set; } = string.Empty;
            public string CORREO_ELECTRONICO { get; set; } = string.Empty;
        }
    }
}

using System.Text.Json.Serialization;
using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;

namespace APIPipas.Models.CuentaUsuarios
{
    public class CuentaUsuarios
    {
        public class USUARIO_WEB_CUENTA
        {
            public int ID_USUARIO_WEB { get; set; }
        }

        public class CuentasUsuarioResponse
        {
            public bool Exito { get; set; }
            public List<USUARIO_WEB_CUENTA_Result> Datos { get; set; } = new();
            public string Mensaje { get; set; } = string.Empty;
            public Usuario? User { get; set; }
        }

        public class USUARIO_WEB_CUENTA_Result
        {
            [JsonPropertyName("CUENTA_COMAPA")]
            public int CUENTA_COMAPA { get; set; }

            [JsonPropertyName("ALIAS_CUENTA")]
            public string ALIAS_CUENTA { get; set; } = string.Empty;

            [JsonPropertyName("FECHA_ALTA")]
            public DateTime FECHA_ALTA { get; set; }

            [JsonPropertyName("RUTA_IMAGEN")]
            public string RUTA_IMAGEN { get; set; } = string.Empty;
        }

        public class ACTIONS_USUARIO_WEB_CUENTA
        {
            public string ACCION { get; set; } = string.Empty;
            public int ID_USUARIO_WEB { get; set; }
            public int CUENTA_COMAPA { get; set; }
            public string MOTIVO_BAJA { get; set; } = string.Empty;
            public int ID_IMAGEN_AUTORIZA_PAPERLESS { get; set; }
            public string ALIAS_CUENTA { get; set; } = string.Empty;
            public string Nombre_Propietario { get; set; } = string.Empty;
            public decimal PAGO_TOTAL { get; set; }
            public string CODIGO { get; set; } = string.Empty;
        }

        public class ActionsCuentasResponse
        {
            public bool Exito { get; set; }
            public List<ACTIONS_USUARIO_WEB_CUENTAResult> Datos { get; set; } = new();
            public string Mensaje { get; set; } = string.Empty;
            public Usuario? User { get; set; }
        }

        public class ACTIONS_USUARIO_WEB_CUENTAResult
        {
            public string MSG { get; set; } = string.Empty;
        }
    }
}

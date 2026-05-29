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
            public List<USUARIO_WEB_CUENTA_Result> Datos { get; set; }
            public string Mensaje { get; set; }
            public Usuario User { get; set; }
        }

        public class USUARIO_WEB_CUENTA_Result
        {
            public int CUENTA_COMAPA { get; set; }
            public string ALIAS_CUENTA { get; set; }
            public DateTime FECHA_ALTA { get; set; }
            public string RUTA_IMAGEN { get; set; }
        }

        public class ACTIONS_USUARIO_WEB_CUENTA
        {
            public string ACCION { get; set; }
            public int ID_USUARIO_WEB { get; set; }
            public int CUENTA_COMAPA { get; set; }
            public string MOTIVO_BAJA { get; set; }
            public int ID_IMAGEN_AUTORIZA_PAPERLESS { get; set; }
            public string ALIAS_CUENTA { get; set; }
            public string Nombre_Propietario { get; set; }
            public int PAGO_TOTAL { get; set; }
        }

        public class ActionsCuentasResponse
        {
            public bool Exito { get; set; }
            public List<ACTIONS_USUARIO_WEB_CUENTAResult> Datos { get; set; }
            public string Mensaje { get; set; }
            public Usuario User { get; set; }
        }

        public class ACTIONS_USUARIO_WEB_CUENTAResult
        {
            public string MSG { get; set; }
        }
    }
}

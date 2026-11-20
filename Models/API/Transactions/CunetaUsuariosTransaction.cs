using static AdministradorDeCuentas.Models.API.Transactions.CunetaUsuariosTransaction;
using static APIPipas.Models.CuentaUsuarios.CuentaUsuarios;
using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;

namespace AdministradorDeCuentas.Models.API.Transactions
{
    public class CunetaUsuariosTransaction
    {
        #region Login
        public class CuentaUsuarioApiRequest
        {
            public string Application { get; set; } = string.Empty;
            public List<CuentaUsuarioTransactionRequest> TransactionList { get; set; } = new();
        }

        public class CuentaUsuarioTransactionRequest
        {
            public string TransactionName { get; set; } = string.Empty;
            public string TransactionError { get; set; } = string.Empty;
            public bool TransactionLog { get; set; }
            public USUARIO_WEB_CUENTA TransactionAttributes { get; set; } = new();
        }

        public class ApiCunetaUsuariosResponse
        {
            public int ResponseCode { get; set; }
            public string ResponseMessage { get; set; } = string.Empty;
            public List<CunetaUsuariosTransactionResponse> TransactionResponses { get; set; } = new();
        }

        public class CunetaUsuariosTransactionResponse
        {
            public string TransactionName { get; set; } = string.Empty;
            public int TransactionStatus { get; set; }
            public string TransactionMessage { get; set; } = string.Empty;
            public List<USUARIO_WEB_CUENTA_Result> TransactionResults { get; set; } = new();
        }
        #endregion

        public class ActionsApiRequest
        {
            public string Application { get; set; } = string.Empty;
            public List<ActionsCuentaTransactionRequest> TransactionList { get; set; } = new();
        }

        public class ActionsCuentaTransactionRequest
        {
            public string TransactionName { get; set; } = string.Empty;
            public string TransactionError { get; set; } = string.Empty;
            public bool TransactionLog { get; set; }
            public ACTIONS_USUARIO_WEB_CUENTA TransactionAttributes { get; set; } = new();
        }

        public class ApiActionsCunetaResponse
        {
            public int ResponseCode { get; set; }
            public string ResponseMessage { get; set; } = string.Empty;
            public List<ActionsCunetaTransactionResponse> TransactionResponses { get; set; } = new();
        }

        public class ActionsCunetaTransactionResponse
        {
            public string TransactionName { get; set; } = string.Empty;
            public int TransactionStatus { get; set; }
            public string TransactionMessage { get; set; } = string.Empty;
            public List<ACTIONS_USUARIO_WEB_CUENTAResult> TransactionResults { get; set; } = new();
        }
    }
}

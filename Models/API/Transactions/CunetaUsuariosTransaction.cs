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
            public string Token { get; set; }
            public string User { get; set; }
            public string Application { get; set; }
            public List<CuentaUsuarioTransactionRequest> TransactionList { get; set; } = new List<CuentaUsuarioTransactionRequest>();
        }

        public class CuentaUsuarioTransactionRequest
        {
            public string TransactionName { get; set; }
            public string TransactionError { get; set; }
            public bool TransactionLog { get; set; }
            public USUARIO_WEB_CUENTA TransactionAttributes { get; set; }
        }

        public class ApiCunetaUsuariosResponse
        {
            public int ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
            public List<CunetaUsuariosTransactionResponse> TransactionResponses { get; set; }
        }
        public class CunetaUsuariosTransactionResponse
        {
            public string TransactionName { get; set; }
            public int TransactionStatus { get; set; }
            public string TransactionMessage { get; set; }
            public List<USUARIO_WEB_CUENTA_Result> TransactionResults { get; set; }
        }
        #endregion


        public class ActionsApiRequest
        {
            public string Token { get; set; }
            public string User { get; set; }
            public string Application { get; set; }
            public List<ActionsCuentaTransactionRequest> TransactionList { get; set; } = new List<ActionsCuentaTransactionRequest>();
        }

        public class ActionsCuentaTransactionRequest
        {
            public string TransactionName { get; set; }
            public string TransactionError { get; set; }
            public bool TransactionLog { get; set; }
            public ACTIONS_USUARIO_WEB_CUENTA TransactionAttributes { get; set; }
        }

        public class ApiActionsCunetaResponse
        {
            public int ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
            public List<ActionsCunetaTransactionResponse> TransactionResponses { get; set; }
        }

        public class ActionsCunetaTransactionResponse
        {
            public string TransactionName { get; set; }
            public int TransactionStatus { get; set; }
            public string TransactionMessage { get; set; }
            public List<ACTIONS_USUARIO_WEB_CUENTAResult> TransactionResults { get; set; }
        }
    }
}

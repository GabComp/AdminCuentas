using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;

namespace PrototipoComapa.Models.API.Transactions
{
    public class UsuarioWebTransactions
    {
        #region Login
        public class ApiRequest
        {
            public string Application { get; set; } = string.Empty;
            public List<TransactionRequest> TransactionList { get; set; } = new();
        }

        public class TransactionRequest
        {
            public string TransactionName { get; set; } = string.Empty;
            public string TransactionError { get; set; } = string.Empty;
            public bool TransactionLog { get; set; }
            public UsuarioWebRegistroRequest TransactionAttributes { get; set; } = new();
        }

        public class ApiResponse
        {
            public int ResponseCode { get; set; }
            public string ResponseMessage { get; set; } = string.Empty;
            public List<TransactionResponse> TransactionResponses { get; set; } = new();
        }

        public class TransactionResponse
        {
            public string TransactionName { get; set; } = string.Empty;
            public int TransactionStatus { get; set; }
            public string TransactionMessage { get; set; } = string.Empty;
            public List<objReponse> TransactionResults { get; set; } = new();
        }
        #endregion
    }
}

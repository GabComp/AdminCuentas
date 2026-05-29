using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;

namespace PrototipoComapa.Models.API.Transactions
{
    public class UsuarioWebTransactions
    {
        #region Login
        public class ApiRequest
        {
            public string Token { get; set; }
            public string User { get; set; }
            public string Application { get; set; }
            public List<TransactionRequest> TransactionList { get; set; } = new List<TransactionRequest>();
        }

        public class TransactionRequest
        {
            public string TransactionName { get; set; }
            public string TransactionError { get; set; }
            public bool TransactionLog { get; set; }
            public UsuarioWebRegistroRequest TransactionAttributes { get; set; }
        }


        public class ApiResponse
        {
            public int ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
            public List<TransactionResponse> TransactionResponses { get; set; }
        }
        public class TransactionResponse
        {
            public string TransactionName { get; set; }
            public int TransactionStatus { get; set; }
            public string TransactionMessage { get; set; }
            public List<objReponse> TransactionResults { get; set; }
        }
        #endregion
    }
}

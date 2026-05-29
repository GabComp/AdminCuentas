using AdministradorDeCuentas.Models.APIRecibos;
using static PrototipoComapa.Models.API.Transactions.UsuarioWebTransactions;
using static PrototipoComapa.Models.UsuarioWeb.UsuarioWebModel;

namespace PrototipoComapa.Models.API.Transactions
{
    public class UsuarioRecibos
    {
        public class  LogUserRecibos 
        {
            public string username { get; set; }
            public string password { get; set; }
        }

        public class LogApiResponse
        {
            public int ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
            public List<LogTransactionResponse> TransactionResponses { get; set; }
        }
        public class LogTransactionResponse
        {
            public string TransactionName { get; set; }
            public int TransactionStatus { get; set; }
            public string TransactionMessage { get; set; }
            public List<RecibosResponse> TransactionResults { get; set; }
        }
    }
}

namespace AdministradorDeCuentas.Models.APIRecibos
{
    public class RecibosModel
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class RecibosResponse
    {
        public string token { get; set; }
    }

    public class DatosValidacion
    {
        public string alias { get; set; }
        public List<filtros> filtros { get; set; }
    }

    public class filtros
    {
        public int? id_cuenta { get; set; }
        public string? id_recibo { get; set; }
        public string? nombre { get; set; }
        public int? total { get; set; }
    }

    public class DatosValidacionResponse
    {
        public bool usrValido { get; set; }
        public string mensaje { get; set; }
    }

    public class ConsultaRecibosResponse
    {
        public int id { get; set; }
        public string id_recibo { get; set; }
        public string ciclo_facturado { get; set; }
        public string periodo { get; set; }
        public int consumo { get; set; }
        public decimal total { get; set; }
    }

    public class ErrorMessageRecibos
    {
        public string message { get; set; }
        public string details { get; set; }
    }
}

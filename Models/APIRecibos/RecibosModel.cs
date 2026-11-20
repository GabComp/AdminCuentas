namespace AdministradorDeCuentas.Models.APIRecibos
{
    public class DatosValidacion
    {
        public string alias { get; set; } = string.Empty;
        public List<Filtro> filtros { get; set; } = new();
    }

    public class Filtro
    {
        public int? id_cuenta { get; set; }
        public string? id_recibo { get; set; }
        public string? nombre { get; set; }
        public decimal? total { get; set; }
    }

    public class DatosValidacionResponse
    {
        public bool usrValido { get; set; }
        public string mensaje { get; set; } = string.Empty;
    }

    public class ConsultaRecibosResponse
    {
        public int id { get; set; }
        public string id_recibo { get; set; } = string.Empty;
        public string ciclo_facturado { get; set; } = string.Empty;
        public string periodo { get; set; } = string.Empty;
        public int consumo { get; set; }
        public decimal total { get; set; }
    }
}

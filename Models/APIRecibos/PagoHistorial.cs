namespace AdministradorDeCuentas.Models.APIRecibos
{
    public class PagoHistorial
    {
        public long IdPago { get; set; }
        public DateTime? FechaPago { get; set; }
        public decimal? Importe { get; set; }
        public long? IdEstatus { get; set; }
        public string Llave { get; set; } = string.Empty;
    }
}

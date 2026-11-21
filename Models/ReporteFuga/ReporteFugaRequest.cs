namespace AdministradorDeCuentas.Models.ReporteFuga
{
    public class ReporteFugaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string TipoReporte { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Colonia { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int? IdCuenta { get; set; }
    }
}

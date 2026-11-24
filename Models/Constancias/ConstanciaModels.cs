using System.Text.Json.Serialization;

namespace AdministradorDeCuentas.Models.Constancias
{
    public sealed class ConstanciaCuentaRequest
    {
        [JsonPropertyName("idCuenta")]
        public int IdCuenta { get; set; }
    }

    public sealed class ConstanciaSolicitudApiRequest
    {
        [JsonPropertyName("idCuenta")]
        public int IdCuenta { get; set; }

        [JsonPropertyName("idTipoConstancia")]
        public int IdTipoConstancia { get; set; }

        [JsonPropertyName("solicitante")]
        public string Solicitante { get; set; } = string.Empty;

        [JsonPropertyName("telefono")]
        public string Telefono { get; set; } = string.Empty;

        [JsonPropertyName("notaria")]
        public string Notaria { get; set; } = string.Empty;

        [JsonPropertyName("datoNotaria")]
        public string DatoNotaria { get; set; } = string.Empty;

        [JsonPropertyName("observacion")]
        public string Observacion { get; set; } = string.Empty;

        [JsonPropertyName("usuarioPortal")]
        public string UsuarioPortal { get; set; } = string.Empty;
    }

    public sealed class ConstanciaValidacionApiResponse
    {
        [JsonPropertyName("exito")]
        public bool Exito { get; set; }

        [JsonPropertyName("idCuenta")]
        public int IdCuenta { get; set; }

        [JsonPropertyName("idPadron")]
        public int IdPadron { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("telefono")]
        public string Telefono { get; set; } = string.Empty;

        [JsonPropertyName("adeudo")]
        public decimal Adeudo { get; set; }

        [JsonPropertyName("puedeRegistrar")]
        public bool PuedeRegistrar { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;
    }

    public sealed class ConstanciaSolicitudApiResponse
    {
        [JsonPropertyName("exito")]
        public bool Exito { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [JsonPropertyName("idConstancia")]
        public long IdConstancia { get; set; }

        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("idEstatus")]
        public int IdEstatus { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("iva")]
        public decimal Iva { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }
    }

    public sealed class ConstanciaPagoPreviewApiRequest
    {
        [JsonPropertyName("idConstancia")]
        public long IdConstancia { get; set; }

        [JsonPropertyName("idCuenta")]
        public int IdCuenta { get; set; }
    }

    public sealed class ConstanciaPagoApiRequest
    {
        [JsonPropertyName("idConstancia")]
        public long IdConstancia { get; set; }

        [JsonPropertyName("idCuenta")]
        public int IdCuenta { get; set; }

        [JsonPropertyName("referencia")]
        public string Referencia { get; set; } = string.Empty;
    }

    public sealed class ConstanciaPagoPreviewApiResponse
    {
        [JsonPropertyName("exito")]
        public bool Exito { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [JsonPropertyName("idConstancia")]
        public long IdConstancia { get; set; }

        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("idCuenta")]
        public int IdCuenta { get; set; }

        [JsonPropertyName("idPadron")]
        public int IdPadron { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("idEstatus")]
        public int IdEstatus { get; set; }

        [JsonPropertyName("folioPago")]
        public string FolioPago { get; set; } = string.Empty;

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("iva")]
        public decimal Iva { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("puedeCobrar")]
        public bool PuedeCobrar { get; set; }
    }

    public sealed class ConstanciaPagoApiResponse
    {
        [JsonPropertyName("exito")]
        public bool Exito { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [JsonPropertyName("idConstancia")]
        public long IdConstancia { get; set; }

        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("folioPago")]
        public string FolioPago { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("idEstatus")]
        public int IdEstatus { get; set; }

        [JsonPropertyName("yaPagada")]
        public bool YaPagada { get; set; }
    }
}

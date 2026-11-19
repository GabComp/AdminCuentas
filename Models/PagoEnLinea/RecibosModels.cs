using System;
using System.Text.Json.Serialization;

namespace AdministradorDeCuentas.Models.PagoEnLinea
{
    public sealed class AdeudoApiItem
    {
        [JsonPropertyName("cuenta")]
        public decimal? Cuenta { get; set; }

        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        [JsonPropertyName("adeudo")]
        public decimal? Adeudo { get; set; }

        [JsonPropertyName("cuenta_valida")]
        public bool? CuentaValida { get; set; }

        [JsonPropertyName("tiene_negociacion")]
        public bool? TieneNegociacion { get; set; }

        [JsonPropertyName("key_pago")]
        public string? KeyPago { get; set; }

        [JsonPropertyName("id_ordencompra")]
        public string? IdOrdenCompra { get; set; }

        [JsonPropertyName("id_saldo")]
        public int? IdSaldo { get; set; }

        [JsonPropertyName("servidor")]
        public string? Servidor { get; set; }
    }

    public sealed class AdeudoResult
    {
        public bool Exito { get; init; }
        public bool CuentaValida { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public decimal Adeudo { get; init; }
        public bool TieneNegociacion { get; init; }
        public string Mensaje { get; init; } = string.Empty;
        public string KeyPago { get; init; } = string.Empty;
        public string IdOrdenCompra { get; init; } = string.Empty;
        public int IdSaldo { get; init; }
        public string Servidor { get; init; } = string.Empty;

        public static AdeudoResult Error(string mensaje) => new()
        {
            Exito = false,
            Mensaje = mensaje
        };
    }

    public sealed class AplicarPagoApiRequest
    {
        [JsonPropertyName("cIdUsuario")]
        public string CIdUsuario { get; set; } = string.Empty;

        [JsonPropertyName("nCuenta")]
        public int NCuenta { get; set; }

        [JsonPropertyName("dImporte")]
        public decimal DImporte { get; set; }

        [JsonPropertyName("nIdFormaPago")]
        public int NIdFormaPago { get; set; }

        [JsonPropertyName("cOrdenCompra")]
        public string COrdenCompra { get; set; } = string.Empty;

        [JsonPropertyName("success_indicator")]
        public string SuccessIndicator { get; set; } = string.Empty;

        [JsonPropertyName("cSessionId")]
        public string CSessionId { get; set; } = string.Empty;

        [JsonPropertyName("card_brand")]
        public string CardBrand { get; set; } = string.Empty;

        [JsonPropertyName("card_funding_method")]
        public string CardFundingMethod { get; set; } = string.Empty;

        [JsonPropertyName("card_name_on_card")]
        public string CardNameOnCard { get; set; } = string.Empty;

        [JsonPropertyName("card_number")]
        public string CardNumber { get; set; } = string.Empty;

        [JsonPropertyName("card_issuer")]
        public string CardIssuer { get; set; } = string.Empty;

        [JsonPropertyName("fecha")]
        public DateTime? Fecha { get; set; }

        [JsonPropertyName("idSaldo")]
        public int? IdSaldo { get; set; }

        [JsonPropertyName("servidor")]
        public string? Servidor { get; set; }
    }

    public sealed class AplicarPagoApiResponse
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("operacion")]
        public string? Operacion { get; set; }

        [JsonPropertyName("importe_aplicado")]
        public decimal? ImporteAplicado { get; set; }

        [JsonPropertyName("mensaje")]
        public string? Mensaje { get; set; }

        [JsonPropertyName("error_num")]
        public int? ErrorNum { get; set; }

        [JsonPropertyName("error_mensaje")]
        public string? ErrorMensaje { get; set; }

        [JsonPropertyName("folio")]
        public string? Folio { get; set; }

        [JsonPropertyName("fecha")]
        public string? Fecha { get; set; }

        [JsonPropertyName("servidor")]
        public string? Servidor { get; set; }
    }

    public sealed class AplicarPagoResult
    {
        public bool Exito { get; init; }
        public string Mensaje { get; init; } = string.Empty;
        public string Folio { get; init; } = string.Empty;
        public decimal ImporteAplicado { get; init; }
        public int? ErrorNum { get; init; }
        public string Servidor { get; init; } = string.Empty;
        public bool Contingencia => string.Equals(Servidor, "hosting", StringComparison.OrdinalIgnoreCase);

        public static AplicarPagoResult Error(string mensaje, int? errorNum = null) => new()
        {
            Exito = false,
            Mensaje = mensaje,
            ErrorNum = errorNum
        };
    }

    public sealed class IniciarPagoRequest
    {
        public int Cuenta { get; set; }
    }

    public sealed class EstadoPagoEnLinea
    {
        [JsonPropertyName("suspendido")]
        public bool Suspendido { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [JsonPropertyName("servidor")]
        public string Servidor { get; set; } = "comapa";

        public static EstadoPagoEnLinea NoDisponible => new()
        {
            Suspendido = true,
            Mensaje = "No fue posible consultar el estado del pago en línea. Intenta más tarde.",
            Servidor = "comapa"
        };
    }

    public sealed class RegistrarIntentoRequest
    {
        [JsonPropertyName("nCuenta")]
        public int NCuenta { get; set; }

        [JsonPropertyName("idSaldo")]
        public int? IdSaldo { get; set; }

        [JsonPropertyName("cOrdenCompra")]
        public string COrdenCompra { get; set; } = string.Empty;

        [JsonPropertyName("dImporte")]
        public decimal DImporte { get; set; }

        [JsonPropertyName("success_indicator")]
        public string SuccessIndicator { get; set; } = string.Empty;

        [JsonPropertyName("cSessionId")]
        public string CSessionId { get; set; } = string.Empty;

        [JsonPropertyName("cSessionVersion")]
        public string CSessionVersion { get; set; } = string.Empty;

        [JsonPropertyName("servidor")]
        public string Servidor { get; set; } = string.Empty;
    }

    public sealed class PagoPendiente
    {
        public int Cuenta { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Importe { get; set; }
        public string IdOrden { get; set; } = string.Empty;
        public int IdSaldo { get; set; }
        public string Servidor { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string SessionVersion { get; set; } = string.Empty;
        public string SuccessIndicator { get; set; } = string.Empty;
        public DateTime CreadoEn { get; set; }
    }

    public sealed class PagoCompletado
    {
        public int Cuenta { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Importe { get; set; }
        public string Folio { get; set; } = string.Empty;
        public string IdOrden { get; set; } = string.Empty;
        public string MarcaTarjeta { get; set; } = string.Empty;
        public string UltimosDigitos { get; set; } = string.Empty;
        public bool Contingencia { get; set; }
        public DateTime Fecha { get; set; }
    }
}

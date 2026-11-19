using System.Text.Json.Serialization;

namespace AdministradorDeCuentas.Models.PagoEnLinea
{
    public sealed class EvoSessionResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("checkoutMode")]
        public string? CheckoutMode { get; set; }

        [JsonPropertyName("merchant")]
        public string? Merchant { get; set; }

        [JsonPropertyName("session")]
        public EvoSession? Session { get; set; }

        [JsonPropertyName("successIndicator")]
        public string? SuccessIndicator { get; set; }
    }

    public sealed class EvoSession
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }

    public sealed class EvoOrderResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("merchantAmount")]
        public decimal? MerchantAmount { get; set; }

        [JsonPropertyName("lastUpdatedTime")]
        public string? LastUpdatedTime { get; set; }

        [JsonPropertyName("sourceOfFunds")]
        public EvoSourceOfFunds? SourceOfFunds { get; set; }
    }

    public sealed class EvoSourceOfFunds
    {
        [JsonPropertyName("provided")]
        public EvoProvided? Provided { get; set; }
    }

    public sealed class EvoProvided
    {
        [JsonPropertyName("card")]
        public EvoCard? Card { get; set; }
    }

    public sealed class EvoCard
    {
        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        [JsonPropertyName("fundingMethod")]
        public string? FundingMethod { get; set; }

        [JsonPropertyName("nameOnCard")]
        public string? NameOnCard { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("issuer")]
        public string? Issuer { get; set; }
    }
}

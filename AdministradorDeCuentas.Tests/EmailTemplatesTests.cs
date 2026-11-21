using PrototipoComapa.Utilerias;

namespace AdministradorDeCuentas.Tests;

public class EmailTemplatesTests
{
    [Fact]
    public void Restablecimiento_Incluye_Codigo_Y_Url()
    {
        var html = EmailTemplates.Restablecimiento("Juan Pérez", "ABC123", "https://ejemplo.com/cambio");

        Assert.Contains("ABC123", html);
        Assert.Contains("https://ejemplo.com/cambio", html);
        // WebUtility.HtmlEncode convierte "ó" y "ñ" a sus entidades numéricas.
        Assert.Contains("Recuperaci&#243;n de contrase&#241;a", html);
    }

    [Fact]
    public void Restablecimiento_Escapa_El_Usuario()
    {
        var html = EmailTemplates.Restablecimiento("<script>alert(1)</script>", "123456", "https://ejemplo.com");

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void CodigoVerificacion_Incluye_El_Codigo()
    {
        var html = EmailTemplates.CodigoVerificacion("998877");

        Assert.Contains("998877", html);
        Assert.Contains("C&#243;digo de verificaci&#243;n", html);
    }

    [Fact]
    public void PagoComprobante_Incluye_Cuenta_Y_Folio()
    {
        var html = EmailTemplates.PagoComprobante(
            "Juan Pérez",
            767658,
            1160m,
            "P11141604",
            new DateTime(2026, 11, 21, 12, 30, 0));

        Assert.Contains("767658", html);
        Assert.Contains("P11141604", html);
        Assert.Contains("Comprobante de pago", html);
    }

    [Fact]
    public void CuentaVinculada_Incluye_La_Cuenta()
    {
        var html = EmailTemplates.CuentaVinculada("Juan Pérez", 767658);

        Assert.Contains("767658", html);
        Assert.Contains("Cuenta vinculada", html);
    }
}

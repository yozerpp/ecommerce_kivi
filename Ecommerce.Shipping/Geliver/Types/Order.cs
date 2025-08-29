namespace Ecommerce.Shipping.Geliver.Types;

/// <summary>
/// Siparişe ait bilgiler.
/// </summary>
public class Order
{
    /// <summary>
    /// Sipariş numarası.
    /// </summary>
    public string OrderNumber { get; set; }

    /// <summary>
    /// Siparişin toplam tutarı.
    /// </summary>
    public string TotalAmount { get; set; }

    public static string TotalAmountCurrency => "TRY";
    /// <summary>
    /// Siparişin kaynağı (örn: API).
    /// </summary>
    public static string SourceCode => "API";

    /// <summary>
    /// Siparişin oluşturulduğu web site veya uygulama.
    /// </summary>
    public static string SourceIdentifier { get; set; }
}
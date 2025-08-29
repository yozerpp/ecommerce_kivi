namespace Ecommerce.Shipping.Geliver.Types;

/// <summary>
/// Ürün bilgilerini temsil eder.
/// </summary>
public class Item
{
    /// <summary>
    /// Ürün başlığı.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Ürün adedi.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Ürünün toplam fiyatı.
    /// </summary>
    public string TotalPrice { get; set; }

    /// <summary>
    /// Ürünün birim ağırlığı.
    /// </summary>
    public string UnitWeight { get; set; }

    /// <summary>
    /// Ürün stok kodu (SKU).
    /// </summary>
    public string Sku { get; set; }
}
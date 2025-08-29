namespace Ecommerce.Shipping.Geliver.Types;

public class Offer
{
    public string Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal AmountVat { get; set; }
    public decimal AmountTax { get; set; }
    public decimal TotalAmount { get; set; }
    public string ProviderCode { get; set; }
    public string ProviderServiceCode { get; set; }
    public string AverageEstimatedTimeHumanReadible { get; set; }
    public string DurationTerms { get; set; }
    public decimal Rating { get; set; }
    public bool IsMainOffer { get; set; }
}
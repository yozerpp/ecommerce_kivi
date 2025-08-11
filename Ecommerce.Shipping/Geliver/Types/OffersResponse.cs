
namespace Ecommerce.Shipping.Geliver.Types
{
public partial class OffersResponse
{
    public bool Result { get; set; }
    public string AdditionalMessage { get; set; }
    public PriceList[] PriceList { get; set; }
}

public partial class PriceList
{
    public long Desi { get; set; }
    public Offer[] Offers { get; set; }
}

public partial class Offer
{
    public string Amount { get; set; }
    public string Currency { get; set; }
    public string AmountLocal { get; set; }
    public string AmountVat { get; set; }
    public string AmountLocalVat { get; set; }
    public string AmountTax { get; set; }
    public string AmountLocalTax { get; set; }
    public string TotalAmount { get; set; }
    public string TotalAmountLocal { get; set; }
    public string ProviderCode { get; set; }
    public string ProviderServiceCode { get; set; }
}
}

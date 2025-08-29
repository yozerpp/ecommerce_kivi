namespace Ecommerce.Shipping.Geliver.Types;

public class LocationListResponse<TL>
{
    public bool Result { get; set; }
    public ICollection<TL> Data { get; set; }
}
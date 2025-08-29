namespace Ecommerce.Shipping.Geliver.Types;

public partial class Address : AResponse<Address>
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; } //hariç hepsi zorunlu
    public string CountryCode { get; set; }
    public string CityName { get; set; }
    public string CityCode { get; set; }
    public string DistrictName { get; set; }
    public long DistrictID { get; set; }
    public string Zip { get; set; }
    public bool IsRecipientAddress { get; set; }
    public string ShortName { get; set; }
}
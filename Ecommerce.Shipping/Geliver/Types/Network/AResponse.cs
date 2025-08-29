using Ecommerce.Shipping.Geliver.Types.Network;

namespace Ecommerce.Shipping.Geliver.Types;

public abstract class AResponse<TR> where TR : AResponse<TR>
{
    public bool Result { get; set; }
    public string Code { get; set; }
    public string? Message { get; set; }
    public TR Data { get; set; }
}
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using static Ecommerce.Bl.Concrete.SellerManager;

namespace Ecommerce.Bl.Interface;

public interface ISellerManager
{
    ProductOffer ListProduct(ProductOffer offer);
    ProductOffer updateOffer(ProductOffer offer, uint productId);
    void UnlistOffer(ProductOffer offer);
}

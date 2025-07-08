using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using static Ecommerce.Bl.Concrete.SellerManager;

namespace Ecommerce.Bl.Interface;

public interface ISellerManager
{
    void ListProduct(ProductOffer offer);
    void updateOffer(ProductOffer offer);
    void UnlistOffer(ProductOffer offer);
}

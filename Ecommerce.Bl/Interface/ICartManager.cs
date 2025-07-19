using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface ICartManager
{
    Cart? Get(bool includeAggregates = true, bool getItems = true, bool includeSeller = true);
    Session newCart(User? user=null, bool flush = false);
    CartItem Add(ProductOffer offer, uint amount = 1);
    CartItem Add(CartItem item, uint amount = 1);
    public void AddCoupon(ProductOffer offer, Coupon coupon);
    CartItem? Decrement(ProductOffer productOffer, uint amount = 1);
    public CartItem? Decrement(CartItem item, uint amount = 1);
    void Remove(ProductOffer offer);
     void Remove(CartItem item);
}

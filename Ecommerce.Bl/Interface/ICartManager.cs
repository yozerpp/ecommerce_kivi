using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface ICartManager
{
    Cart? Get(uint id,bool includeAggregates = true, bool getItems = true, bool includeSeller = true);
    Session newSession(User? user, bool flush = false);
    CartItem Add(Cart cart,ProductOffer offer, uint amount = 1);
    CartItem Add(CartItem item, uint amount = 1);
    public void AddCoupon(Cart cart,ProductOffer offer, Coupon coupon);
    CartItem? Decrement(Cart cart,ProductOffer productOffer, uint amount = 1);
    public CartItem? Decrement(CartItem item, uint amount = 1);
    void Remove(Cart cart,ProductOffer offer);
     void Remove(CartItem item);
}

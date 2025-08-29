using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Interface;

public interface ICartManager
{
    public Cart? Get(Session session, bool includeAggregates = false, bool getItems = false,
        bool includeItemAggregates = false, bool includeSeller = false,bool nonTracking = false);
    Session newSession(User? user, bool flush = false);
    CartItem Add(Cart cart, ProductOffer offer, int amount = 1);
    CartItem Add(CartItem item, int amount = 1);
    void Clear(uint cartId);
    public void AddCoupon(Cart cart,ProductOffer offer, string couponId);
    public void RemoveCoupon(Cart cart, ProductOffer offer); // Added this line
    public ICollection<Coupon> GetAvailableCoupons(Session session);
    public ICollection<Product> GetMoreProductsFromSellers(Session session, int page=1, int pageSize=20);


    CartItem? Decrement(Cart cart,ProductOffer productOffer, uint amount = 1);
    public CartItem? Decrement(CartItem item, uint amount = 1);
    void Remove(Cart cart,ProductOffer offer);
     void Remove(CartItem item);
}

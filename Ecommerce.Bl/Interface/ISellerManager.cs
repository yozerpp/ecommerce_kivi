using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using static Ecommerce.Bl.Concrete.SellerManager;

namespace Ecommerce.Bl.Interface;

public interface ISellerManager
{
    Seller? GetSeller(uint sellerId, bool includeOffers, bool includeReviews,bool includeAggregates,
        bool includeCoupons = false);

    public ICollection<Order> GetOrders(uint sellerId, uint?orderId = null, bool onlyOwnItems=true, int page = 1, int pageSize = 20);
    ICollection<ProductOffer> GetOffers(uint sellerId, int page = 1, int pageSize = 20);
    void UpdateSeller(Seller seller);
    ProductOffer ListOffer(Seller seller, ProductOffer offer);
    ProductOffer updateOffer(Seller seller, ProductOffer offer, uint productId);
    void UnlistOffer(Seller seller, ProductOffer offer);
    void CreateCoupon(Seller seller, Coupon coupon);
}

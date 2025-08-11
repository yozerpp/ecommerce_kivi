using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using static Ecommerce.Bl.Concrete.SellerManager;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface ISellerManager
{
    SellerWithAggregates? GetSellerWithAggregates(uint sellerId, bool includeOffers, bool includeReviews,
        bool includeCoupons = false);
    List<ProductOffer> GetOffers(uint sellerId, int page = 1, int pageSize = 20);
    Seller? GetSeller(uint sellerId, bool includeOffers, bool includeReviews, bool includeCoupons = false);
    void UpdateSeller(Seller seller);
    ProductOffer ListOffer(Seller seller, ProductOffer offer);
    ProductOffer updateOffer(Seller seller, ProductOffer offer, uint productId);
    void UnlistOffer(Seller seller, ProductOffer offer);
    void CreateCoupon(Seller seller, Coupon coupon);
}

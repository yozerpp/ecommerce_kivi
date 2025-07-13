using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using static Ecommerce.Bl.Concrete.SellerManager;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface ISellerManager
{
    SellerWithAggregates? GetSellerWithAggregates(uint sellerId, bool includeOffers, bool includeReviews);
    Seller? GetSeller(uint sellerId, bool includeOffers, bool includeReviews);
    void UpdateSeller(Seller seller);
    ProductOffer ListProduct(ProductOffer offer);
    ProductOffer updateOffer(ProductOffer offer, uint productId);
    void UnlistOffer(ProductOffer offer);
    void CreateCoupon(Coupon coupon);
}

using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using static Ecommerce.Bl.Concrete.SellerManager;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Interface;

public interface ISellerManager
{
    SellerWithAggregates? GetSellerWithAggregates(uint sellerId, bool includeOffers, bool includeReviews, bool includeCoupons=false, int offersPage = 1, int offersPageSize =20);
    List<ProductOffer> GetOffers(uint sellerId, int page = 1, int pageSize = 20);
    Seller? GetSeller(uint sellerId, bool includeOffers, bool includeReviews, bool includeCoupons = false);
    void UpdateSeller(Seller seller);
    ProductOffer ListProduct(ProductOffer offer);
    ProductOffer updateOffer(ProductOffer offer, uint productId);
    void UnlistOffer(ProductOffer offer);
    void CreateCoupon(Coupon coupon);
}

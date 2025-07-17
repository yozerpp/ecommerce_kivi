using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Concrete;

public class SellerManager : ISellerManager
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Seller> _sellerRepository;
    private readonly IRepository<ProductOffer> _productOfferRepository;
    private readonly IRepository<Coupon> _couponRepository;
    public SellerManager(IRepository<Coupon> couponRepository,IRepository<Product> productRepository, IRepository<Seller> sellerRepository, IRepository<ProductOffer> productOfferRepository) {
        _productRepository = productRepository;
        _couponRepository = couponRepository;
        _sellerRepository = sellerRepository;
        _productOfferRepository = productOfferRepository;
    }
    public SellerWithAggregates? GetSellerWithAggregates(uint sellerId, bool includeOffers, bool includeReviews, bool includeCoupons = false, int offersPage = 1, int offersPageSize = 20) {
        return _sellerRepository.First(GetAggregateProjection(offersPage, offersPageSize),s=>s.Id == sellerId,
            includes:GetIncludes(includeOffers,includeReviews, includeCoupons));
    }

    public List<ProductOffer> GetOffers(uint sellerId, int page = 1, int pageSize = 20) {
        return _productOfferRepository.Where(p => p.SellerId == sellerId, includes:[[nameof(ProductOffer.Product)]], offset: (page-1) * pageSize, limit: pageSize*page);
    }
    public Seller? GetSeller(uint sellerId, bool includeOffers, bool includeReviews, bool includeCoupons = false) {
        return _sellerRepository.First(s => s.Id == sellerId, includes: GetIncludes(includeOffers, includeReviews, includeCoupons));
    }
    //@param Seller should contain user information as well.
    public void UpdateSeller(Seller seller) {
        User user;
        if ((user = ContextHolder.Session!.User)==null || user is not Seller){
            throw new UnauthorizedAccessException("You have to be logged in as a Seller.");
        }
        var oldSeller = user as Seller;
        foreach (var property in typeof(Seller).GetProperties().Where(p=>p.DeclaringType == typeof(Seller))){
            property.SetValue(oldSeller, property.GetValue(seller));
        }
        _sellerRepository.Update(oldSeller);
        _sellerRepository.Flush();
    }
    public ProductOffer ListProduct(ProductOffer offer)
    {
        if (ContextHolder.Session!.User==null || !typeof(Seller).IsAssignableFrom(ContextHolder.Session.User.GetType()))
        {
            throw new UnauthorizedAccessException("You do not have permission to list product offerings.");
        }
        if (offer.ProductId==0 && offer.Product==null)
        {
            throw new ArgumentException("An offer should be associated with a new or existing product.");
        }
        if (offer.Product!=null)
        {
            offer.Product.Id = 0;
            offer.Product = _productRepository.Add(offer.Product);
            offer.ProductId = offer.Product.Id;
        }
   
        offer.SellerId = ContextHolder.Session.User.Id;
        var ret = _productOfferRepository.Add(offer);
        //this needs better transaction management.
        _productOfferRepository.Flush();
        return ret;
    }
    
    public ProductOffer updateOffer(ProductOffer offer, uint productId)
    {
        if (ContextHolder.Session?.User==null || ContextHolder.Session.User is not Seller)
        {
            throw new UnauthorizedAccessException("You need to be a logged in as a seller.");
        }
        var existingOffer = _productOfferRepository.First(o=>o.ProductId==productId && o.SellerId == ContextHolder.Session.User.Id);
        if (existingOffer==null){
            throw new ArgumentException("You don't have an offer for this product.");
        }
        if (offer.Product!=null && _productRepository.Exists(p=>p.Id==offer.ProductId))
        {
            offer.Product.Id = 0;
            offer.Product = _productRepository.Add(offer.Product);
            offer.ProductId = offer.Product.Id;
            return _productOfferRepository.Add(offer);
        }
        var ret = _productOfferRepository.Update(offer);
        _productOfferRepository.Flush();
        return ret;
    }

    public void UnlistOffer(ProductOffer offer)
    {
        var o = _productOfferRepository.Delete(offer);
        var p = _productRepository.First(p=>p.Id == o.ProductId);
        if (p?.Offers.Count==0)
        {
            _productRepository.Delete(p);
        }
        _productOfferRepository.Flush();
    }

    public void CreateCoupon(Coupon coupon) {
        
        if (ContextHolder.Session!.User == null || ContextHolder.Session.User is not Seller seller)
        {
            throw new UnauthorizedAccessException("You need to be a logged in as a seller.");
        }
        coupon.SellerId = seller.Id;
        var couponCount = _sellerRepository.First( s=>s.Id==seller.Id,includes:[[nameof(Seller.Coupons)]]).Coupons.Count;
        coupon.Id = seller.ShopName + (ushort)couponCount ;
        _couponRepository.Add(coupon);
        _couponRepository.Flush();
    }

    private static Expression<Func<Seller, SellerWithAggregates>> GetAggregateProjection(int offersPage, int offersPageSize) {
        return s =>
            new SellerWithAggregates{
                ReviewCount = (uint)s.Offers.SelectMany(o => o.Reviews).Count(),
                ReviewAverage = (float)(s.Offers.SelectMany(o => o.Reviews).Average(r => (decimal?)r.Rating) ?? 0m),
                SaleCount = (uint)(s.Offers.SelectMany(o => o.BoughtItems).Sum(oi => (int?)oi.Quantity) ?? 0),
                OfferCount = (uint)s.Offers.Count,
                Id = s.Id,
                ShopAddress = s.ShopAddress,
                ShopName = s.ShopName,
                ShopEmail = s.ShopEmail,
                ShopPhoneNumber = s.ShopPhoneNumber,
                Offers = s.Offers,
                Coupons = s.Coupons,
            };
    }

    private static readonly Expression<Func<Seller, SellerWithAggregates>> AggregateProjectionWithUser = s =>
        new SellerWithAggregates{
            ReviewCount = (uint)s.Offers.SelectMany(o => o.Reviews).Count(),
            ReviewAverage = (float)(s.Offers.SelectMany(o => o.Reviews).Average(r => (decimal?)r.Rating)??0),
            SaleCount = (uint)(s.Offers.SelectMany(o => o.BoughtItems).Sum(oi => (decimal?)oi.Quantity)??0),
            OfferCount = (uint)s.Offers.Count,
            Id = s.Id,
            ShopAddress = s.ShopAddress,
            ShopName = s.ShopName,
            ShopEmail = s.ShopEmail,
            ShopPhoneNumber = s.ShopPhoneNumber,
            Offers = s.Offers,
            Coupons = s.Coupons,
            
            FirstName = s.FirstName,
            LastName = s.LastName,
            Email = s.Email,
            ShippingAddress = s.ShippingAddress,
            PhoneNumber = s.PhoneNumber,
            Active = s.Active,
            Orders = s.Orders,
            SessionId = s.SessionId,
            Session = s.Session,
        };
    private static string[][] GetIncludes(bool offer, bool reviews, bool coupons) {
        ICollection<string[]> ret = new List<string[]>();
        if(offer) ret.Add([nameof(Seller.Offers),nameof(ProductOffer.Product), nameof(Product.Category)]);
        if (reviews) ret.Add([nameof(Seller.Offers), nameof(ProductOffer.Reviews)]);
        if(coupons) ret.Add([nameof(Seller.Coupons)]);
        return ret.ToArray();
    }
}

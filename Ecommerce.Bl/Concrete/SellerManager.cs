using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.Bl.Concrete;

public class SellerManager : ISellerManager
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Seller> _sellerRepository;
    private readonly IRepository<ProductOffer> _productOfferRepository;
    public SellerManager(IRepository<Product> productRepository, IRepository<Seller> sellerRepository, IRepository<ProductOffer> productOfferRepository) {
        _productRepository = productRepository;
        _sellerRepository = sellerRepository;
        _productOfferRepository = productOfferRepository;
    }
    public SellerWithAggregates? GetSellerWithAggregates(uint sellerId, bool includeOffers, bool includeReviews) {
        return _sellerRepository.First(AggregateProjection,s=>s.Id == sellerId,
            includes:GetIncludes(includeOffers,includeReviews));
    }

    public Seller? GetSeller(uint sellerId, bool includeOffers, bool includeReviews) {
        return _sellerRepository.First(s => s.Id == sellerId, includes: GetIncludes(includeOffers, includeReviews));
    }
    //@param Seller should contain all the information
    static string[][] GetIncludes(bool offer, bool reviews) {
        ICollection<string[]> ret = new List<string[]>();
        if(offer) ret.Add([nameof(Seller.Offers),nameof(ProductOffer.Product)]);
        if (reviews) ret.Add([nameof(Seller.Offers), nameof(ProductOffer.Reviews)]);
        return ret.ToArray();
    }
    public void UpdateSeller(Seller seller) {
        User user;
        if ((user = ContextHolder.Session?.User)==null || user is not Seller){
            throw new UnauthorizedAccessException("You have to be logged in as a Seller.");
        }
        var oldSeller = user as Seller;
        foreach (var property in typeof(Seller).GetProperties().Where(p=>p.DeclaringType == typeof(Seller))){
            property.SetValue(oldSeller, property.GetValue(seller));
        }

        _sellerRepository.Update(oldSeller);
    }
    public ProductOffer ListProduct(ProductOffer offer)
    {
        if (ContextHolder.Session.User==null || !typeof(Seller).IsAssignableFrom(ContextHolder.Session.User.GetType()))
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
        return _productOfferRepository.Add(offer);
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
        } else return _productOfferRepository.Update(offer);
    }

    public void UnlistOffer(ProductOffer offer)
    {
        var o = _productOfferRepository.Delete(offer);
        var p = _productRepository.First(p=>p.Id == o.ProductId);
        if (p?.Offers.Count==0)
        {
            _productRepository.Delete(p);
        }
    }

    private static readonly Expression<Func<Seller, SellerWithAggregates>> AggregateProjection = s =>
        new SellerWithAggregates{
            ReviewCount = (uint)s.Offers.SelectMany(o => o.Reviews).Count(),
            ReviewAverage = s.Offers.SelectMany(o => o.Reviews).Average(r => r.Rating),
// need to implement a OrderItem for this SaleCount =
            Id = s.Id,
            ShopAddress = s.ShopAddress,
            ShopName = s.ShopName,
            ShopEmail = s.ShopEmail,
            ShopPhoneNumber = s.ShopPhoneNumber,
            Offers = s.Offers,
            Coupons = s.Coupons,
        };
    private static readonly Expression<Func<Seller, SellerWithAggregates>> AggregateProjectionWithUser = s =>
        new SellerWithAggregates{
            ReviewCount = (uint)s.Offers.SelectMany(o => o.Reviews).Count(),
            ReviewAverage = s.Offers.SelectMany(o => o.Reviews).Average(r => r.Rating),
// need to implement a OrderItem for this SaleCount =
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
            BillingAddress = s.BillingAddress,
            Active = s.Active,
            Orders = s.Orders,
            SessionId = s.SessionId,
            Session = s.Session,
        };
}

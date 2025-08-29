using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Views;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class SellerManager : ISellerManager
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Seller> _sellerRepository;
    private readonly IRepository<ProductOffer> _productOfferRepository;
    private readonly IRepository<Coupon> _couponRepository;
    private readonly IRepository<Order> _orderRepository;

    public SellerManager(IRepository<Coupon> couponRepository,IRepository<Product> productRepository, IRepository<Seller> sellerRepository, IRepository<ProductOffer> productOfferRepository, IRepository<Order> orderRepository) {
        _productRepository = productRepository;
        _couponRepository = couponRepository;
        _sellerRepository = sellerRepository;
        _productOfferRepository = productOfferRepository;
        _orderRepository = orderRepository;
    }
    public Seller? GetSeller(uint sellerId, bool includeOffers, bool includeReviews, bool includeAggregates, bool includeCoupons = false) {
        var s =  _sellerRepository.FirstP(includeAggregates?WithAggregates:WithoutAggregates,s=>s.Id == sellerId,
            includes:GetIncludes(includeOffers,includeReviews, includeCoupons));
        if(s!=null && includeCoupons)
            s.Coupons = s.Coupons.Where(s => s.ExpirationDate > DateTime.UtcNow + TimeSpan.FromHours(3)).ToList();
        return s;
    }
    public ICollection<Order> GetOrders(uint sellerId, uint?orderId = null, bool onlyOwnItems =true, int page = 1, int pageSize = 20) {
        var orders =  _orderRepository.WhereP(OrderManager.OrderWithItemsAggregateProjection, o=>o.Items.Any(i=>i.SellerId == sellerId) && 
                (!orderId.HasValue || orderId == o.Id), offset:(page-1)*pageSize, limit:page*pageSize,nonTracking:true
            ,includes:[[nameof(Order.Items),nameof(OrderItem.SelectedOptions), nameof(ProductOption.Property), nameof(ProductCategoryProperty.CategoryProperty)]]);
        if(onlyOwnItems)
            orders.ForEach(o => {
                o.Items = o.Items.Where(i=>i.SellerId == sellerId).ToList();
            });
        return orders;
    }
    public ICollection<ProductOffer> GetOffers(uint sellerId, int page = 1, int pageSize = 20) {
        page = page == -1 ? _productOfferRepository.Count(p => p.SellerId == sellerId) / pageSize + 1 : page;
        var offersDict= _productOfferRepository.WhereP(OfferStaltessWithProduct,p => p.SellerId == sellerId, includes:[[nameof(ProductOffer.Product), nameof(Product.Images)],[nameof(ProductOffer.Product), nameof(Product.Category)]], offset: (page-1) * pageSize, limit: pageSize*page,nonTracking:true).ToDictionary(o=>ValueTuple.Create(o.SellerId, o.ProductId), o=>o);
        var stats = _productOfferRepository.WhereP(o => new OfferStats(){
            ProductId = (uint?)o.Stats.ProductId ?? 0,
            SellerId = (uint?)o.Stats.SellerId ?? 0,
            ReviewAverage = (decimal?)o.Stats.ReviewAverage?? 0m,
            ReviewCount = (uint?)o.Stats.ReviewCount?? 0,
            RatingTotal = (decimal?)o.Stats.RatingTotal ?? 0m,
            RefundCount = (uint?)o.Stats.RefundCount ?? 0,
        },p=>p.SellerId == sellerId && p.Stats !=null, nonTracking:true);
        stats.ForEach(o=>offersDict[(o.SellerId.Value, o.ProductId.Value)].Stats=o);
        return offersDict.Values;
    }
 
    //@param Seller should contain user information as well.
    public void UpdateSeller(Seller seller) {
        var ignores = new List<string>([nameof(Seller.PasswordHash), nameof(Seller.Session), nameof(Seller.SessionId)]);
        if (seller.ProfilePictureId == 0) seller.ProfilePictureId = null;
        else if(seller.ProfilePictureId == null) ignores.AddRange([nameof(Seller.ProfilePictureId), nameof(Seller.ProfilePicture)]);
        _sellerRepository.UpdateIgnore(seller,true,ignores.ToArray());
        _sellerRepository.Flush();
    }

    public Product ListProduct(Product product) {
        if(product.CategoryId==null && product.Category==null){
            throw new ArgumentException("A product should be associated with a new or existing category.");
        }
        var ret =  _productRepository.Add(product);
        _productRepository.Flush();
        return ret;
    }
    public ProductOffer ListOffer(Seller seller, ProductOffer offer)
    {
        if (offer.ProductId==0 && offer.Product==null)
        {
            throw new ArgumentException("An offer should be associated with a new or existing product.");
        }
        if (offer.Product!=null)
        {
            offer.Product.Id = 0;
            offer.ProductId = offer.Product.Id;
            offer.Product = _productRepository.Add(offer.Product);
        }
        
        offer.SellerId = seller.Id;
        offer.Seller = seller.Id != 0 ? null! : seller;
        try{
            var ret = _productOfferRepository.Add(offer);
            _productOfferRepository.Flush();
            return ret;
        }
        catch (Exception e){
            e = e.InnerException ?? e;
            if ((e is not DbUpdateException dbu|| !dbu.InnerException.Message.Contains("duplicate")) &&
                (e is not InvalidOperationException io || (io.InnerException?.Message.Contains("already") ?? false)|| !io.Message.Contains("already"))){
                throw;
            }

            throw new ArgumentException("You already have an offer for this product.");
        }
    }
    public ProductOffer updateOffer(Seller seller, ProductOffer offer, uint productId) {
        // throw new Exception();
        if (offer.Product!=null && _productRepository.Exists(p=>p.Id==offer.ProductId))
        {
            offer.Product.Id = 0;
            offer.Product = _productRepository.Add(offer.Product);
            offer.ProductId = offer.Product.Id;
            var ret1 =  _productOfferRepository.Add(offer);
            _productOfferRepository.Flush();
            return ret1;
        }
        var ret = _productOfferRepository.Update(offer);
        _productOfferRepository.Flush();
        return ret;
    }
    public void UnlistOffer(Seller seller, ProductOffer offer)
    {
        var o = _productOfferRepository.Delete(offer);
        var p = _productRepository.First(p=>p.Id == o.ProductId);
        if (p?.Offers.Count==0)
        {
            _productRepository.Delete(p);
        }
        _productOfferRepository.Flush();
    }
    public void CreateCoupon(Seller seller, Coupon coupon) {
        coupon.SellerId = seller.Id;
        var couponCount = _sellerRepository.First( s=>s.Id==seller.Id,includes:[[nameof(Seller.Coupons)]]).Coupons.Count;
        coupon.Id = seller.ShopName + (ushort)couponCount ;
        _couponRepository.Add(coupon);
        _couponRepository.Flush();
    }
    private static string[][] GetIncludes(bool offer, bool reviews, bool coupons) {
        ICollection<string[]> ret = new List<string[]>();
        if (offer){
            ret.Add([nameof(Seller.Offers),nameof(ProductOffer.Product), nameof(Product.Category)]);
            ret.Add([nameof(Seller.Offers), nameof(ProductOffer.Product), nameof(Product.Images)]);
        }
        if (reviews) ret.Add([nameof(Seller.Offers), nameof(ProductOffer.Reviews)]);
        if(coupons) ret.Add([nameof(Seller.Coupons)]);
        return ret.ToArray();
    }
    private static readonly Expression<Func<Seller, Seller>> WithoutAggregates = s => new Seller(){
        Id = s.Id,
        ShopName = s.ShopName,
        Active = s.Active,
        Address = s.Address,
        Coupons = s.Coupons,
        Email = s.Email,
        FirstName = s.FirstName,
        LastName = s.LastName,
        NormalizedEmail = s.NormalizedEmail,
        PasswordHash = s.PasswordHash,
        PhoneNumber = s.PhoneNumber,
        ProfilePicture = s.ProfilePicture,
        ProfilePictureId = s.ProfilePictureId,
        Stats = null,
    };
    private static readonly Expression<Func<Seller, Seller>> WithAggregates = s => new Seller{
        Id = s.Id,
        ShopName = s.ShopName,
        Active = s.Active,
        Address = s.Address,
        Coupons = s.Coupons,
        Email = s.Email,
        FirstName = s.FirstName,
        LastName = s.LastName,
        NormalizedEmail = s.NormalizedEmail,
        PasswordHash = s.PasswordHash,
        PhoneNumber = s.PhoneNumber,
        ProfilePicture = s.ProfilePicture,
        ProfilePictureId = s.ProfilePictureId,
        Stats = s.Stats
    };
    public static readonly Expression<Func<ProductOffer, ProductOffer>> OfferWithStatlessProduct = ((Expression<Func<ProductOffer,ProductOffer>>)(o => new ProductOffer(){
        ProductId = o.ProductId,
        SellerId = o.SellerId,
        Discount = o.Discount,
        Price = o.Price,
        Seller = o.Seller,
        Product = ProductManager.CardStatlessProjection.Invoke(o.Product),
        Stats = o.Stats!=null?ProductManager.OfferStatsProjection.Invoke(o.Stats):null,
        Stock = o.Stock,
    })).Expand();    public static readonly Expression<Func<ProductOffer, ProductOffer>> OfferStaltessWithProduct = ((Expression<Func<ProductOffer,ProductOffer>>)(o => new ProductOffer(){
        ProductId = o.ProductId,
        SellerId = o.SellerId,
        Discount = o.Discount,
        Price = o.Price,
        Seller = o.Seller,
        Product = ProductManager.CardStatlessProjection.Invoke(o.Product),
        Stats = null,
        Stock = o.Stock,
    })).Expand();
}

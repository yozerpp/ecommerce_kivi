using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class SellerManager : ISellerManager
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Seller> _sellerRepository;
    private readonly IRepository<ProductOffer> _productOfferRepository;
    private readonly IRepository<Coupon> _couponRepository;
    private readonly IRepository<Category> _categoryRepository;
    public SellerManager(IRepository<Category> categoryRepository,IRepository<Coupon> couponRepository,IRepository<Product> productRepository, IRepository<Seller> sellerRepository, IRepository<ProductOffer> productOfferRepository) {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _couponRepository = couponRepository;
        _sellerRepository = sellerRepository;
        _productOfferRepository = productOfferRepository;
    }
    public Seller? GetSeller(uint sellerId, bool includeOffers, bool includeReviews, bool includeCoupons = false) {
        var s =  _sellerRepository.First(s=>s.Id == sellerId,
            includes:GetIncludes(includeOffers,includeReviews, includeCoupons));
        if(s!=null && includeCoupons)
            s.Coupons = s.Coupons.Where(s => s.ExpirationDate > DateTime.UtcNow + TimeSpan.FromHours(3)).ToList();
        return s;
    }
    public List<ProductOffer> GetOffers(uint sellerId, int page = 1, int pageSize = 20) {
        page = page == -1 ? _productOfferRepository.Count(p => p.SellerId == sellerId) / pageSize + 1 : page;
        return _productOfferRepository.Where(p => p.SellerId == sellerId, includes:[[nameof(ProductOffer.Product), nameof(Product.Images)],[nameof(ProductOffer.Product), nameof(Product.Category)]], offset: (page-1) * pageSize, limit: pageSize*page);
    }
 
    //@param Seller should contain user information as well.
    public void UpdateSeller(Seller seller) {
        _sellerRepository.Update(seller);
        _sellerRepository.Flush();
    }

    public Product ListProduct(Seller seller, Product product) {
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
}

using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class SellerManager : ISellerManager
{
    private readonly IRepository<Product, DbContext> _productRepository;
    private readonly IRepository<Seller, DbContext> _sellerRepository;
    private readonly IRepository<ProductOffer, DbContext> _productOfferRepository;
    public SellerManager(IRepository<Product, DbContext> productRepository, IRepository<Seller, DbContext> sellerRepository, IRepository<ProductOffer, DbContext> productOfferRepository)
    {
        _productRepository = productRepository;
        _sellerRepository = sellerRepository;
        _productOfferRepository = productOfferRepository;
    }
    public void ListProduct(ProductOffer offer)
    {
        if (UserContextHolder.User==null || !typeof(Seller).IsAssignableFrom(UserContextHolder.User.GetType()))
        {
            throw new UnauthorizedAccessException("You do not have permission to list product offerings.");
        }
        if (offer.Product!=null)
        {
            offer.Product.Id = 0;
            offer.Product = _productRepository.Add(offer.Product);
            offer.ProductId = offer.Product.Id;
        }
        if (offer.ProductId==0)
        {
            throw new ArgumentException("An offer should be associated with a new or existing product.");
        }
        _productOfferRepository.Add(offer);
    }
    
    public void updateOffer(ProductOffer offer)
    {
        if (UserContextHolder.User==null || !typeof(Seller).IsAssignableFrom(UserContextHolder.User.GetType()) || UserContextHolder.User.Id != offer.SellerId)
        {
            throw new UnauthorizedAccessException("You do not have permission to alter this offer.");
        }
        if (offer.Product!=null && _productRepository.Exists(p=>p.Id==offer.ProductId))
        {
            offer.Product.Id = 0;
            offer.Product = _productRepository.Add(offer.Product);
            offer.ProductId = offer.Product.Id;
            _productOfferRepository.Add(offer);
        } else _productOfferRepository.Update(offer);
    }

    public void UnlistOffer(ProductOffer offer)
    {
        var o = _productOfferRepository.Delete(offer);
        var p = _productRepository.Find(p=>p.Id == o.ProductId);
        if (p?.Offers.Count==0)
        {
            _productRepository.Delete(p);
        }
    }

    public struct SearchPredicate
    {
        public enum OperatorType
        {
            Equals,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual,
            Like
        }
        public string PropName { get; set; }
        public string Value { get; set; }
        public OperatorType Operator { get; set; }
    }

    public struct SearchOrder
    {
        public string PropName { get; set; }
        public bool Ascending { get; set; }
    }
}

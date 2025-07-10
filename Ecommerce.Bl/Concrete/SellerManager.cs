using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Iface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class SellerManager : ISellerManager
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Seller> _sellerRepository;
    private readonly IRepository<ProductOffer> _productOfferRepository;
    public SellerManager(IRepository<Product> productRepository, IRepository<Seller> sellerRepository, IRepository<ProductOffer> productOfferRepository)
    {
        _productRepository = productRepository;
        _sellerRepository = sellerRepository;
        _productOfferRepository = productOfferRepository;
    }

    public Seller Login() {
        User user;
        if ((user = ContextHolder.Session!.User)==null){
            throw new UnauthorizedAccessException("You need to be logged in as a user to switch to seller account.");
        }
        Seller? seller;
        if ((seller = _sellerRepository.Find(s => s.Id == user.Id)) == null){
            throw new UnauthorizedAccessException("You need to be registered as a seller to switch to seller account.");
        }
        ContextHolder.Session.User = seller;
        return seller;
    }
    public Seller CreateSeller(Seller seller) {
        User user;
        if ((user =ContextHolder.Session.User)==null){
            throw new UnauthorizedAccessException("You need to be logged in as a user to create a seller account.");
        }
        foreach (var propertyInfo in user.GetType().GetProperties()){
            if (propertyInfo.CanWrite)
                propertyInfo.SetValue(seller, propertyInfo.GetValue(user));
        }

        return _sellerRepository.Add(seller);
    }
    //@param Seller should contain all the information
    public void UpdateSeller(Seller seller) {
        User user;
        if ((user = ContextHolder.Session?.User)==null || user is not Seller){
            throw new UnauthorizedAccessException("You have to be logged in as a Seller.");
        }
        var oldSeller = user as Seller;
        oldSeller.Address = seller.Address;
        oldSeller.ShopName = seller.ShopName;
        oldSeller.Address = seller.Address;
        oldSeller.SellerEmail = seller.SellerEmail;
        oldSeller.SellerPhoneNumber = seller.SellerPhoneNumber;
    }
    public ProductOffer ListProduct(ProductOffer offer)
    {
        if (ContextHolder.Session.User==null || !typeof(Seller).IsAssignableFrom(ContextHolder.Session.User.GetType()))
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
        offer.SellerId = ContextHolder.Session.User.Id;
        return _productOfferRepository.Add(offer);
    }
    
    public ProductOffer updateOffer(ProductOffer offer, uint productId)
    {
        if (ContextHolder.Session?.User==null || ContextHolder.Session.User is not Seller)
        {
            throw new UnauthorizedAccessException("You need to be a logged in as a seller.");
        }
        var existingOffer = _productOfferRepository.Find(o=>o.ProductId==productId && o.SellerId == ContextHolder.Session.User.Id);
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

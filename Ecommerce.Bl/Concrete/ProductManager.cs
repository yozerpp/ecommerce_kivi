using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using System.Linq.Expressions;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Views;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class ProductManager : IProductManager
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<ProductOffer> _productOfferRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<SessionVisitedCategory> _customerVisitedCategoryRepository;
    private readonly IRepository<ProductFavor> _productFavorRepository;
    public ProductManager(IRepository<Product> productRepository, IRepository<ProductFavor> productFavorRepository, IRepository<Category> categoryRepository, IRepository<ProductOffer> productOfferRepository, IRepository<SessionVisitedCategory> customerVisitedCategoryRepository) {
        _productRepository = productRepository;
        _productFavorRepository = productFavorRepository;
        _categoryRepository = categoryRepository;
        _productOfferRepository = productOfferRepository;
        _customerVisitedCategoryRepository = customerVisitedCategoryRepository;
    }

    //can be used to update the vote as well.

    public bool Favor(ProductFavor favor) {
        if(_productFavorRepository.Delete(f=>f.ProductId == favor.ProductId && f.CustomerId == f.CustomerId)!=0)return false;
        _productFavorRepository.Add(favor);
        _productFavorRepository.Flush();
        return true;
    }

    public List<Product> Search(string predicateQuery, ICollection<SearchOrder> orders, bool includeImage = false,
        bool fetchReviews = false, bool fetchOffers = false, int page = 1, int pageSize = 20) {
        SearchExpressionUtils.BuildPredicate<Product>(predicateQuery, out var predicateExpr);
        var orderByExpressions = SearchExpressionUtils.OrderByExpression<Product>(orders);
        return _productRepository.WhereP(CardProjection, predicateExpr, (page - 1) * pageSize, page * pageSize,
            orderByExpressions.ToArray()).DistinctBy(p=>p.Id).ToList();
    }
    public List<Product> Search(ICollection<SearchPredicate> predicates, ICollection<SearchOrder> ordering, bool includeImage = false, bool fetchReviews = false, bool fetchOffers=false, int page=1, int pageSize=20) {
        var offset = (page - 1) * pageSize;
        var limit = page * pageSize;
        // predicates.Add(new SearchPredicate(){PropName = "Active", Operator = SearchPredicate.OperatorType.Equals, Value = "true"});
        if(ordering.Count==0)ordering = new List<SearchOrder>([new SearchOrder(){PropName = nameof(Product.Id), Ascending = true}]);
        var statPropNames = typeof(ProductStats).GetProperties().Select(p => p.Name).ToArray();
        foreach (var predicate in predicates)
            if(statPropNames.Contains(predicate.PropName))
                predicate.PropName = $"{nameof(Product.Stats)}_{predicate.PropName}";
        foreach (var order in ordering)
            if(statPropNames.Contains(order.PropName))
                order.PropName = $"{nameof(Product.Stats)}_{order.PropName}";
        SearchExpressionUtils.Build<Product>(predicates, ordering, out var predicateExpr, out var orderByExpr);
        string[][] includes = GetIncludes(true,false,false);
        includes =[];
        return _productRepository.WhereP(CardProjection, predicateExpr, offset, limit,orderByExpr.ToArray(), includes)
            .DistinctBy(p=>p.Id).DistinctBy(p=>p.Id).ToList();
    }
    public void VisitCategory(SessionVisitedCategory category) {
        _customerVisitedCategoryRepository.TryAdd(category);
    }
    private static string[][] GetIncludes(bool images, bool offers, bool reviews) {
        var ret = new List<string[]>([[nameof(Product.Category)]]);
        if(images)ret.Add([nameof(Product.Images)]);
        if(offers) ret.Add([nameof(Product.Offers), nameof(ProductOffer.Seller)]);
        if(reviews) ret.Add([nameof(Product.Offers), nameof(ProductOffer.Reviews)]);
        return ret.ToArray();
    }

    public ICollection<ProductFavor> GetFavorites(Customer customer) {
        var cid = customer.Id;
        return _productFavorRepository.Where(p => p.CustomerId == cid).ToArray();
    }

    public ICollection<ProductFavor> GetFavorers(uint productId) {
        return _productFavorRepository.Where(p => p.ProductId == productId).ToArray();
    }

    public ICollection<Product> GetMoreProductsFromCategories(Session session, int page = 1,
        int pageSize = 20) {
        var cid=session.Id;
        var categories = _customerVisitedCategoryRepository.WhereP(c => c.CategoryId, c => c.SessionId == cid).ToArray();
        return _productRepository.WhereP(CardProjection,p => categories.Contains(p.CategoryId), includes: GetIncludes(true, false,false), offset: pageSize * (page - 1),
            limit: pageSize * page).DistinctBy(p=>p.Id).ToArray();
    }
    public ICollection<Category> GetCategories(bool includeChildren =true, bool includeProperties = false ) {
        return _categoryRepository.All(includes:GetCategoryIncludes(includeChildren, includeProperties));
    }
    public Category? GetCategoryById(uint id, bool includeChildren = false, bool includeProperties = true) {
        return _categoryRepository.First(c => c.Id == id, includes:GetCategoryIncludes(includeChildren, includeProperties));
    }

    public ICollection<Category> GetCategoriesByName(string name, bool includeChildren = false, bool includeProperties = true) {
        return _categoryRepository.Where(c => c.Name.Contains(name), includes:GetCategoryIncludes(includeChildren, includeProperties));
    }
    private static string[][] GetCategoryIncludes(bool includeChildren, bool includeProperties) {
        var includes = new List<string[]>();
        if(includeChildren) includes.Add([nameof(Category.Children)]);
        if (includeProperties) includes.Add([nameof(Category.CategoryProperties)]);
        return includes.ToArray();
    }
    public ICollection<ProductOffer> GetOffers(uint? productId = null, uint? sellerId = null, bool includeAggregates = true) {
        if (productId == null && sellerId == null)
            throw new ArgumentNullException("productId and sellerId cannot be null.");

        return _productOfferRepository.WhereP(includeAggregates?OfferWithStats:OfferWithoutStats,
            o => (productId == null || o.ProductId == productId) && (sellerId == null || o.SellerId == sellerId),
            includes: [[nameof(ProductOffer.Seller)]]).ToArray();
    }
    public Product? GetByIdWithAggregates(uint productId, bool fetchOffer = false, bool fetchReviews = true, bool fetchImage=true) {
       return _productRepository.FirstP(MainProjection,p => p.Id == productId, GetIncludes(fetchImage, fetchOffer, fetchReviews));
    }
    public Product? GetById(uint id, bool withOffers = true) {
        return _productRepository.First(p => p.Id == id, 
            includes:GetIncludes(true, withOffers, false));
    }
    public void UnlistOffer(ProductOffer offer) {
        _productOfferRepository.Delete(offer);
    }
    public void Delete(Product product) {
        _productRepository.Delete(product);
    }

    public static readonly Expression<Func<ProductOffer, ProductOffer>> OfferWithStats = o => new ProductOffer(){
        ProductId = o.ProductId,
        SellerId = o.SellerId,
        Discount = o.Discount,
        Price = o.Price,
        Seller = o.Seller,
        Stats = new OfferStats(){
            ReviewAverage = (o.Stats.RatingTotal / o.Stats.ReviewCount) ?? 0,
            RefundCount = o.Stats.RefundCount ?? 0,
            ReviewCount = o.Stats.ReviewCount ?? 0
        },
        Stock = o.Stock,
    };
    public static readonly Expression<Func<ProductOffer, ProductOffer>> OfferWithoutStats = o => new ProductOffer(){
        ProductId = o.ProductId,
        SellerId = o.SellerId,
        Discount = o.Discount,
        Price = o.Price,
        Seller = o.Seller,
        Stats = null,
        Stock = o.Stock,
    };

    private static readonly Expression<Func<ProductStats, ProductStats>> StatsProjection = s => new ProductStats(){
        FavorCount = s.FavorCount ?? 0,
        MinPrice = s.MinPrice ?? 0m,
        MaxPrice = s.MaxPrice ?? 0m,
        ReviewCount = s.ReviewCount ?? 0,
        RatingAverage = s.RatingAverage ?? 0m,
        OrderCount = s.OrderCount ?? 0,
        SaleCount = s.SaleCount ?? 0,
        RefundCount = s.RefundCount ?? 0,
        RatingTotal = s.RatingTotal??0m,
        ProductId = s.ProductId??0u,
    };
    public static readonly Expression<Func<Product, Product>> MainProjection = ((Expression<Func<Product,Product>>)(p => new Product(){
        Id = p.Id,
        CategoryId = p.CategoryId,
        Description = p.Description,
        Dimensions = p.Dimensions,
        Images = p.Images.Select(i=>new ImageProduct(){
            Image = i.Image,
            IsPrimary = i.IsPrimary,
            ProductId = i.ProductId,
            ImageId = i.ImageId,
        }).ToArray(),
        Name = p.Name,
        Active = p.Active,
        CategoryProperties = p.CategoryProperties,
        Stats = StatsProjection.Invoke(p.Stats),
    })).Expand();
    public static Expression<Func<Product, Product>> WithOfferAndAggregates = ((Expression<Func<Product,Product>>)(p => new Product{
        Id = p.Id,
        CategoryId = p.CategoryId,
        Description = p.Description,
        Dimensions = p.Dimensions,
        Images = p.Images,
        Name = p.Name,
        Active = p.Active,
        CategoryProperties = p.CategoryProperties,
        Stats = StatsProjection.Invoke(p.Stats),
        Offers = p.Offers.Select(o => OfferWithStats.Invoke(o)).ToArray()
    })).Expand();
    public static Expression<Func<ICollection<ImageProduct>, Image?>> MainImageProjection = images => images
        .Where(i => i.IsPrimary)
        .Select(i => i.Image)
        .FirstOrDefault() ?? images.Select(i => i.Image).FirstOrDefault();
    public static readonly Expression<Func<Product, Product>> CardProjection = ((Expression<Func<Product,Product>>)(p => new Product{
        Id = p.Id,
        CategoryId = p.CategoryId,
        Description = p.Description,
        Dimensions = p.Dimensions,
        MainImage = MainImageProjection.Invoke(p.Images),
        Name = p.Name,
        Active = p.Active,
        CategoryProperties = null,
        Stats = StatsProjection.Invoke(p.Stats),
    })).Expand();

}

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

    public bool SwitchFavor(ProductFavor favor) {
        if(_productFavorRepository.Delete(f=>f.ProductId == favor.ProductId && f.CustomerId == favor.CustomerId)!=0)return false;
        _productFavorRepository.Add(favor);
        _productFavorRepository.Flush();
        return true;
    }

    public List<Product> Search(string? predicateQuery, ICollection<SearchOrder> orders, bool includeImage = false,
        bool fetchReviews = false, bool fetchOffers = false, int page = 1, int pageSize = 20) {
        if (predicateQuery!=null && !(predicateQuery.StartsWith('&') || predicateQuery.StartsWith('|')))
            predicateQuery = $"&({predicateQuery})";
        SearchExpressionUtils.Build<Product>(predicateQuery, orders, out var predicateExpr, out var orderByExpressions);
        return _productRepository.WhereP(CardProjection, predicateExpr, (page - 1) * pageSize, page * pageSize,
            orderByExpressions.ToArray()).DistinctBy(p=>p.Id).ToList();
    }
    public List<Product> GetFromSeller(uint sellerId, int page = 1, int pageSize = 20, bool includeImage = false, bool fetchReviews = false, bool fetchOffers = false) {
        return _productRepository.WhereP(CardProjection, p => p.Offers.Any(o => o.SellerId == sellerId), (page - 1) * pageSize, page * pageSize,
            includes: GetIncludes(includeImage, fetchOffers, fetchReviews)).DistinctBy(p=>p.Id).ToList();
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
    private static string[][] GetIncludes(bool images, bool offers, bool reviews, bool categoryProperties = false) {
        var ret = new List<string[]>([[nameof(Product.Category)]]);
        if(images)ret.Add([nameof(Product.Images)]);
        if(offers) ret.Add([nameof(Product.Offers), nameof(ProductOffer.Seller)]);
        if(reviews) ret.Add([nameof(Product.Offers), nameof(ProductOffer.Reviews)]);
        if(categoryProperties) ret.Add([nameof(Product.CategoryProperties), nameof(ProductCategoryProperty.CategoryProperty)]);
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
    public Product? GetByIdWithAggregates(uint productId, bool fetchOffer = false, bool fetchReviews = false, bool fetchImage=false) {
       var pr  =_productRepository.FirstP(MainStatless,p => p.Id == productId, GetIncludes(fetchImage, fetchOffer, fetchReviews ,true));
       pr.Stats = _productRepository.FirstP(p=>new ProductStats(){
            FavorCount = p.Stats.FavorCount,
            MinPrice = p.Stats.MinPrice ,
            MaxPrice = p.Stats.MaxPrice ,
            ReviewCount = p.Stats.ReviewCount ,
            RatingAverage = p.Stats.RatingAverage,
            OrderCount = p.Stats.OrderCount ,
            SaleCount = p.Stats.SaleCount ,
            RefundCount = p.Stats.RefundCount ,
            RatingTotal = p.Stats.RatingTotal,
            ProductId = p.Stats.ProductId ,
       }, p => p.Id == pr.Id,nonTracking:true);
       pr.RatingStats = _productRepository.FirstP(p => new ProductRatingStats(){
           ReviewCount = p.RatingStats.ReviewCount??0,
           FiveStarCount = p.RatingStats.FiveStarCount ?? 0,
           FourStarCount = p.RatingStats.FourStarCount ?? 0,
           ThreeStarCount = p.RatingStats.ThreeStarCount ?? 0,
           TwoStarCount = p.RatingStats.TwoStarCount ?? 0,
           OneStarCount = p.RatingStats.OneStarCount ?? 0,
           ZeroStarCount = p.RatingStats.ZeroStarCount ?? 0,
       }, p => p.Id == pr.Id, nonTracking: true);
       return pr;
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

    public static readonly Expression<Func<OfferStats, OfferStats>> OfferStatsProjection = s => new OfferStats(){
        ProductId = s.ProductId ?? 0,
        SellerId = s.SellerId ?? 0,
        RatingTotal = s.RatingTotal ?? 0m,
        RefundCount = s.RefundCount ?? 0,
        ReviewAverage = s.ReviewAverage ?? 0m,
        ReviewCount = s.ReviewCount ?? 0,
    };
    public static readonly Expression<Func<ProductOffer, ProductOffer>> OfferWithStats = o => new ProductOffer(){
        ProductId = o.ProductId,
        SellerId = o.SellerId,
        Discount = o.Discount,
        Price = o.Price,
        Seller = o.Seller,
        Stats = new OfferStats(){
            ProductId = o.Stats.ProductId ??0,
            SellerId= o.Stats.SellerId ??0,
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
        FavorCount = s.FavorCount ,
        MinPrice = s.MinPrice ,
        MaxPrice = s.MaxPrice ,
        ReviewCount = s.ReviewCount ,
        RatingAverage = s.RatingAverage  ,
        OrderCount = s.OrderCount ,
        SaleCount = s.SaleCount ,
        RefundCount = s.RefundCount ,
        RatingTotal = s.RatingTotal,
        ProductId = s.ProductId ,
    };
    public static readonly Expression<Func<Product, Product>> MainProjection = ((Expression<Func<Product,Product>>)(p => new Product(){
        Id = p.Id,
        CategoryId = p.CategoryId,
        Description = p.Description,
        Dimensions = p.Dimensions,
        Images = p.Images.Select(i=> new ImageProduct{
            Image = i.Image,
            IsPrimary = i.IsPrimary,
            ProductId = i.ProductId,
            ImageId = i.ImageId
        }).ToArray(),        Name = p.Name,
        Active = p.Active,
        CategoryProperties = p.CategoryProperties,
        Stats = StatsProjection.Invoke(p.Stats),
    })).Expand();   public static readonly Expression<Func<Product, Product>> MainStatless = ((Expression<Func<Product,Product>>)(p => new Product(){
        Id = p.Id,
        CategoryId = p.CategoryId,
        Description = p.Description,
        Dimensions = p.Dimensions,
        Images = p.Images.Select(i=> new ImageProduct{
            Image = i.Image,
            IsPrimary = i.IsPrimary,
            ProductId = i.ProductId,
            ImageId = i.ImageId
        }).Take(3).ToArray(),
        Name = p.Name,
        Active = p.Active,
        CategoryProperties = p.CategoryProperties,
        Stats = null,
    })).Expand();
    public static Expression<Func<Product, Product>> WithOfferAndAggregates = ((Expression<Func<Product,Product>>)(p => new Product{
        Id = p.Id,
        CategoryId = p.CategoryId,
        Description = p.Description,
        Dimensions = p.Dimensions,
        Images = p.Images.Select(i=> new ImageProduct{
            Image = i.Image,
            IsPrimary = i.IsPrimary,
            ProductId = i.ProductId,
            ImageId = i.ImageId
        }).ToArray(),
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
    public static readonly Expression<Func<Product, Product>> CardStatlessProjection = ((Expression<Func<Product,Product>>)(p => new Product{
        Id = p.Id,
        CategoryId = p.CategoryId,
        Description = p.Description,
        Dimensions = p.Dimensions,
        MainImage = MainImageProjection.Invoke(p.Images),
        Name = p.Name,
        CategoryProperties = null,
        Stats = null,
    })).Expand();

}

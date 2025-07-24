using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using System.Linq.Expressions;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Concrete;

public class ProductManager : IProductManager
{
    private readonly IRepository<Product> _productRepository;

    public ProductManager(IRepository<Product> productRepository) {
        _productRepository = productRepository;
    }

    //can be used to update the vote as well.

    public List<ProductWithAggregates> SearchWithAggregates(ICollection<SearchPredicate> predicates, ICollection<SearchOrder> ordering, bool includeImage = false, bool fetchReviews = false, int page=1, int pageSize=20) {
        var offset = (page - 1) * pageSize;
        DbContext a;
        SearchExpressionUtils.Build<ProductWithAggregates>(predicates, ordering, out var predicateExpr, out var orderByExpr);
        string[][] includes = fetchReviews ?[[nameof(Product.Offers), nameof(ProductOffer.Reviews)], [nameof(Product.Category)]] :[[nameof(Product.Category)]];
        return _productRepository.Where(GetAggregateProjection(includeImage), predicateExpr, offset, pageSize,
            orderBy: orderByExpr.ToArray(), includes: includes);
    }
    public List<Product> Search(ICollection<SearchPredicate> predicates, ICollection<SearchOrder> ordering,
        int page = 1, int pageSize = 20) {
        var offset = (page - 1) * pageSize;
        var limit = page*pageSize;
        SearchExpressionUtils.Build<Product>(predicates, ordering, out var predicateExpr, out var orderByExpr);
        return _productRepository.Where(ImageLessProjection, predicateExpr, offset, limit,
            orderBy: orderByExpr.ToArray());
    }
    public ProductWithAggregates? GetByIdWithAggregates(uint productId, bool fetchReviews = true, bool fetchImage=true) {
        return _productRepository.First(GetAggregateProjection(fetchImage),p=>p.Id ==productId,includes:GetProductIncludes(fetchReviews) );
    }
    public Product? GetById(int id, bool fetchImage=true) {
        return _productRepository.First(fetchImage ? IdentityProjection : ImageLessProjection, p => p.Id == id);
    }
    
    private static Expression<Func<Product, ProductWithAggregates>> GetAggregateProjection(bool includeImage) {
        return p => new ProductWithAggregates{
            MaxPrice = p.Offers.Max(o => (decimal?)o.Price)??0,
            MinPrice = p.Offers.Min(o => (decimal?)o.Price)??0,
            SaleCount = p.Offers.SelectMany(o=>o.BoughtItems).Sum(i=>(uint?) i.Quantity) as uint? ??0,
            ReviewCount = p.Offers.SelectMany(o => o.Reviews).Count() as uint? ?? 0,
            ReviewAverage = p.Offers.SelectMany(o => o.Reviews).Average(r =>(decimal?) r.Rating) as float? ??0f,
            Id = p.Id,
            CategoryId = p.CategoryId,
            Category = p.Category,
            Description = p.Description,
            Image = includeImage?p.Image:null,
            Name = p.Name,
            Offers = p.Offers,
            
        };
    }
    private static readonly Expression<Func<Product, Product>> IdentityProjection = p => p;
    private static readonly Expression<Func<Product, Product>> ImageLessProjection = p => new Product{
        Id = p.Id,
        CategoryId = p.CategoryId,
        Category = p.Category,
        Description = p.Description,
        Image = null,
        Name = p.Name,
        Offers = p.Offers,
    };

    private static string[][] GetProductIncludes(bool fetchReviews) {
        return fetchReviews
            ?[[nameof(Product.Offers), nameof(ProductOffer.Reviews)], [nameof(Product.Category)], [nameof(Product.Offers), nameof(Seller)]]
            :[[nameof(Product.Category)], [nameof(Product.Offers), nameof(Seller)]];
    }
}

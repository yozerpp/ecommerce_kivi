using Ecommerce.Entity.Projections;

namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class ProductWithAggregatesCustomerView() : ProductWithAggregates
{
    public bool? CurrentFavored { get; set; } = null;
    public static ProductWithAggregatesCustomerView Promote(ProductWithAggregates product, bool? currrentFavored = null) {
        var ret =Activator.CreateInstance<ProductWithAggregatesCustomerView>();
        foreach (var propertyInfo in typeof(ProductWithAggregates).GetProperties().Where(p=>p.SetMethod!=null && !p.SetMethod.IsPrivate)){
            propertyInfo.SetValue(ret, propertyInfo.GetValue(product));
        }
        ret.CurrentFavored = currrentFavored;
        return ret;
    }

    public static ICollection<ProductWithAggregatesCustomerView> PromoteAll(IEnumerable<ProductWithAggregates> products,
        ICollection<uint>? favoriteIds=null) {
        return products.Select(p => Promote(p, favoriteIds?.Contains(p.Id))).ToArray();
    }
}
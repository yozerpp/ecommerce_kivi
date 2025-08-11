namespace Ecommerce.WebImpl.Pages.Shared.Product;

public class ProductWithAggregatesCustomerView()
{
    public required Entity.Product Product { get; init; }  
    public bool? CurrentFavored { get; init; }

    public static ProductWithAggregatesCustomerView Promote(Entity.Product product, ICollection<uint>? favorites) =>
        new(){
            Product = product,
            CurrentFavored = favorites?.Contains(product.Id)
        };
    public static ICollection<ProductWithAggregatesCustomerView> PromoteAll(ICollection<Entity.Product> products,
        ICollection<uint>? favorites) {
        return products.Select(p => new ProductWithAggregatesCustomerView(){
            Product = p,
            CurrentFavored = favorites?.Contains(p.Id)
        }).ToArray();
    } 
}
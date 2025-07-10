using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using static Ecommerce.Bl.Concrete.SellerManager;

namespace Ecommerce.Bl.Interface;

public interface IProductManager<P> where P: Product, new()
{
    List<P> Search(ICollection<SearchPredicate> predicates, ICollection<SearchOrder> ordering, int page = 0, int pageSize = 20);
}

using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Interface;

public interface ICartManager
{
    Cart newCart(User? user);
    CartItem Add(ProductOffer offer);
    CartItem Add(CartItem item);
    CartItem Decrement(ProductOffer productOffer);
}

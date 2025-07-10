using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Interface;

public interface ICartManager
{
    Session newCart();
    CartItem Add(ProductOffer offer, int amount = 1);
    CartItem Add(CartItem item);
    CartItem? Decrement(ProductOffer productOffer, int amount = 1);
    void Remove(ProductOffer offer);
}

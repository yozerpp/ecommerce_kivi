using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Bl.Interface;

public interface ICartManager
{
    Session newCart(User? user=null);
    CartItem Add(ProductOffer offer, uint amount = 1);
    CartItem Add(CartItem item);
    CartItem? Decrement(ProductOffer productOffer, uint amount = 1);
    void Remove(ProductOffer offer);
}

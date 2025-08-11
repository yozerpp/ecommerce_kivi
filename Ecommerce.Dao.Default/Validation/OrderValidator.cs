using System.ComponentModel.DataAnnotations;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;

namespace Ecommerce.Dao.Default.Validation;

public class OrderValidator : IValidator<Order>
{
    private readonly IRepository<Order> _orderRepository;
    
    public OrderValidator(IRepository<Order> orderRepository) {
        _orderRepository = orderRepository;
    }
    public ValidationResult Validate(Order entity) {
        if(entity.Email == null && entity.UserId==default && entity.User == default)
            return new ValidationResult("You must enter Email for anonymous orders.");
        var oldStatus = _orderRepository.FirstP(o => o.Status, o => o.Id == entity.Id);
        if((oldStatus != OrderStatus.WaitingConfirmation || oldStatus == OrderStatus.Shipped) && entity.Status!=OrderStatus.Returned && entity.Status!=OrderStatus.ReturnRequested )
            return new ValidationResult("You cannot change order status after it was cancelled or refunded.");
        return new ValidationResult(null);
    }
}
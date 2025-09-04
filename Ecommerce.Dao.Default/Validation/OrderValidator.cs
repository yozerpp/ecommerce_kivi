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
        var oldStatus = _orderRepository.FirstP(o => o.Status, o => o.Id == entity.Id, nonTracking:true);
        // if(entity.Status == OrderStatus.Cancelled && 
        //    oldStatus.HasFlag(OrderStatus.Returned| OrderStatus.Delivered | OrderStatus.Cancelled | OrderStatus.Complete | OrderStatus.Shipped)
        //    || entity.Status == OrderStatus.Complete && oldStatus.HasFlag(OrderStatus.Cancelled |OrderStatus.Returned))
        //     return new ValidationResult("Bu işlemi bu durumda gerçekleştiremezsiniz.");
        return new ValidationResult(null);
    }
}
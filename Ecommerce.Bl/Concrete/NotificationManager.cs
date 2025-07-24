using Ecommerce.Bl.Interface;
using Ecommerce.Entity;

namespace Ecommerce.Bl.Concrete;

public class NotificationManager : INotificationManager
{
    public void SendCouponNotification(Coupon coupon, Seller seller) {
        throw new NotImplementedException();
    }

    public void SendOrderNotification(Order order, Seller seller) {
        throw new NotImplementedException();
    }
}
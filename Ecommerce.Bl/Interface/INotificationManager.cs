using Ecommerce.Entity;

namespace Ecommerce.Bl.Interface;

public interface INotificationManager
{
    public void SendCouponNotification(Coupon coupon, Seller seller);
    public void SendOrderNotification(Order order, Seller seller);
}
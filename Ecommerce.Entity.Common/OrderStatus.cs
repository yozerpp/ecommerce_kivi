namespace Ecommerce.Entity.Common;

[Flags]
public enum OrderStatus
{
    WaitingPayment = 1,
    WaitingConfirmation=2,
    WaitingShipment = 4,
    Shipped=8,
    Delivered=16,
    Cancelled=32,
    Complete=64,
    ReturnRequested =128,
    Returned=256,
}

public static class OrderStatusExtensions
{
    public static bool IsDone(this OrderStatus orderStatus) {
        return ((int)orderStatus &
                ((int)OrderStatus.Delivered | (int)OrderStatus.Cancelled | (int)OrderStatus.Complete  | (int)OrderStatus.Returned)) ==
               0;
    }
    public static string ToLocalizedString(this OrderStatus status)
    {
        return status switch
        {
            OrderStatus.WaitingPayment => "Ödeme Bekleniyor",
            OrderStatus.WaitingConfirmation => "Satıcı Onayı Bekleniyor",
            OrderStatus.WaitingShipment => "Kargo Bekleniyor",
            OrderStatus.Shipped => "Kargolandı",
            OrderStatus.Delivered => "Teslim Edildi",
            OrderStatus.Cancelled => "İptal Edildi",
            OrderStatus.Complete => "Tamamlandı",
            OrderStatus.ReturnRequested => "İade Talebinde",
            OrderStatus.Returned => "İade Edildi",
            
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}

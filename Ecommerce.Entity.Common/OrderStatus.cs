namespace Ecommerce.Entity.Common;

[Flags]
public enum OrderStatus
{
    WaitingPayment = 0,
    WaitingConfirmation=1,
    WaitingShipment = 2,
    Shipped=4,
    Delivered=8,
    Cancelled=16,
    Complete=32,
    ReturnRequested =64,
    Returned=128,
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

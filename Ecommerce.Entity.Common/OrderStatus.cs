namespace Ecommerce.Entity.Common;

public enum OrderStatus
{
    WaitingConfirmation=0,
    Shipped=1,
    Delivered=2,
    Cancelled=3,
    Complete=4,
    ReturnRequested =5,
    Returned=6,
}

public static class OrderStatusExtensions
{
    public static string ToLocalizedString(this OrderStatus status)
    {
        return status switch
        {
            OrderStatus.WaitingConfirmation => "Onay Bekliyor",
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
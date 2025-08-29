using System.Linq.Expressions;

namespace Ecommerce.Entity.Common;

[Flags]
public enum OrderStatus
{
    WaitingPayment = 1,
    WaitingConfirmation=2,
    WaitingShipment = 4,
    Shipped=8,
    Delivered=16,
    ReturnRequested =31,
    ReturnApproved = 32,
    CancellationRequested = 64,
    //rest is Done
    Returned=128,
    Cancelled=256,
    CancelledBySeller=512,
    Complete=1024,
    ReturnDenied =2048,
}

public static class OrderStatusExtensions
{
    public static bool IsDone(this OrderStatus orderStatus) {
        return orderStatus >= OrderStatus.Returned;
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
            OrderStatus.CancellationRequested=>"İptal Talebi Değerlendiriliyor",
            OrderStatus.ReturnRequested => "İade Talebinde",
            OrderStatus.ReturnApproved => "İade Onaylandı",
            OrderStatus.Returned => "İade Edildi",
            _ => status.ToString()
        };
    }
}

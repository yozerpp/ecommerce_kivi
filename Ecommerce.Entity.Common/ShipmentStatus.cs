namespace Ecommerce.Entity.Common;

public enum ShipmentStatus
{
    Processing = 0,
    InTransit = 1,
    InPickup = 2,
    InDelivery=3,
    Delivered = 4,
    Cancelled = 5,
}

public static class ShipmentStatusExtensions
{
    public static string ToLocalizedString(this ShipmentStatus shipmentStatus) {
        return shipmentStatus switch{
            ShipmentStatus.Processing => "Kargoda",
            ShipmentStatus.InTransit => "Yolda",
            ShipmentStatus.Delivered => "Teslim Edildi",
            ShipmentStatus.Cancelled => "İptal Edildi",
            ShipmentStatus.InPickup => "Şubede",
            ShipmentStatus.InDelivery => "Dağıtımda",
            _ => throw new ArgumentOutOfRangeException(nameof(shipmentStatus), shipmentStatus, null)
        };
    }
}
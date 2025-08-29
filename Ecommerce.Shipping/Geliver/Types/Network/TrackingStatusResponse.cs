using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Ecommerce.Shipping.Geliver.Types.Network;

public class TrackingStatus 
{
    public string Id { get; set; }
    public string TrackingNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TrackingStatusCode TrackingStatusCode { get; set; }
    public TrackingSubStatusCode TrackingSubStatusCode { get; set; }
    public string StatusDetails => TrackingStatusCode.GetDescription();
    public DateTimeOffset StatusDate { get; set; }
    public string LocationName { get; set; }
    public string Hash { get; set; }
}
public class TrackingStatusResponse : AResponse<TrackingStatusResponse>
{
    public TrackingStatus? TrackingStatus { get; set; }
}
public enum TrackingStatusCode
{
    [Description("Kargo sistemde oluşturuldu")]
    PRE_TRANSIT,

    [Description("Kargo taşıma sürecinde")]
    TRANSIT,

    [Description("Kargo alıcıya teslim edildi")]
    DELIVERED,

    [Description("Kargo başarısız")]
    FAILURE,

    [Description("Kargo iade edildi")]
    RETURNED,

    [Description("Bilinmeyen durum")]
    UNKNOWN
}

public enum TrackingSubStatusCode
{
    [Description("Kargo sistemde oluşturuldu")]
    [JsonPropertyName("information_received")]
    information_received,

    [Description("Kargo, Gönderici şube tarafından teslim alındı")]
    [JsonPropertyName("package_accepted")]
    package_accepted,

    [Description("Kargo, aktarma merkezinden ayrıldı")]
    [JsonPropertyName("package_departed")]
    package_departed,

    [Description("Kargo aktarma merkezinde")]
    [JsonPropertyName("package_processing")]
    package_processing,

    [Description("Kargo alıcı şubede")]
    [JsonPropertyName("delivery_scheduled")]
    delivery_scheduled,

    [Description("Kargo dağıtımda")]
    [JsonPropertyName("out_for_delivery")]
    out_for_delivery,

    [Description("Paket hasarlı")]
    [JsonPropertyName("package_damaged")]
    package_damaged,

    [Description("Kargo aracı firmaya verildi")]
    [JsonPropertyName("package_forwarded_to_another_carrier")]
    package_forwarded_to_another_carrier,

    [Description("Kargo dağıtım zamanı değişti (yanlış şube vs)")]
    [JsonPropertyName("delivery_rescheduled")]
    delivery_rescheduled,

    [Description("Kargo alıcıya teslim edildi")]
    [JsonPropertyName("delivered")]
    delivered,

    [Description("Paket kayboldu")]
    [JsonPropertyName("package_lost")]
    package_lost,

    [Description("Kargo dağıtılamıyor")]
    [JsonPropertyName("package_undeliverable")]
    package_undeliverable,

    [Description("Kargo iade edildi")]
    [JsonPropertyName("return_to_sender")]
    return_to_sender,

    [Description("Bilinmeyen durum")]
    [JsonPropertyName("other")]
    other
}
public static class TrackingStatusCodeExtensions
{
    public static string GetDescription(this TrackingStatusCode value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
        return attribute == null ? value.ToString() : attribute.Description;
    }
    public static string GetDescription(this TrackingSubStatusCode value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
        return attribute == null ? value.ToString() : attribute.Description;
    }
}
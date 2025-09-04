using System.Text.Json;
using Ecommerce.Entity.Events;

namespace Ecommerce.Notifications;

public class NotificationJsonConverter : System.Text.Json.Serialization.JsonConverter<Entity.Events.Notification>
{
    public override Notification? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var doc = JsonDocument.ParseValue(ref reader);
        var type = doc.RootElement.GetProperty(nameof(Notification.Type).ToLower()).GetInt32();
        return Enum.GetValues<Notification.NotificationType>()[type].ToString("G") switch{
            nameof(Notification.NotificationType.Review) => doc.Deserialize<ReviewNotification>(options),
            nameof(Notification.NotificationType.Order) => doc.Deserialize<OrderNotification>(options),
            nameof(Notification.NotificationType.Coupon) => doc.Deserialize<CouponNotification>(options),
            nameof(Notification.NotificationType.Vote) => doc.Deserialize<VoteNotification>(options),
            nameof(Notification.NotificationType.Discount) => doc.Deserialize<DiscountNotification>(options),
            nameof(Notification.NotificationType.ReviewComment) => doc.Deserialize<ReviewCommentNotification>(options),
            nameof(Notification.NotificationType.OrderCompletion) => doc.Deserialize<OrderCompletionNotification>(options),
            nameof(Notification.NotificationType.RefundRequest) => doc.Deserialize<RefundRequest>(options),
            nameof(Notification.NotificationType.PermissionRequest) => doc.Deserialize<PermissionRequest>(options),
            nameof(Notification.NotificationType.CancellationRequest) => doc.Deserialize<CancellationRequest>(options),
            _ => throw new JsonException($"Unknown notification type: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Notification value, JsonSerializerOptions options) {
        JsonSerializer.Serialize(value, options);
    }
}

using System.Text.Json;
using Ecommerce.Entity.Events;

namespace Ecommerce.Notifications;

public class NotificationJsonConverter : System.Text.Json.Serialization.JsonConverter<Entity.Events.Notification>
{
    public override Notification? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var doc = JsonDocument.ParseValue(ref reader);
        var type = doc.RootElement.GetProperty(nameof(Notification.Type)).GetString();
        return type switch{
            nameof(Notification.NotificationType.Review) => doc.Deserialize<ReviewNotification>(options),
            nameof(Notification.NotificationType.Order) =>doc.Deserialize<OrderNotification>(options),
            nameof(Notification.NotificationType.Coupon) => doc.Deserialize<FavorNotification>(options),
            
        }
    }

    public override void Write(Utf8JsonWriter writer, Notification value, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }
}
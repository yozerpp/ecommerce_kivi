using Ecommerce.Entity.Common;

namespace Ecommerce.Entity;

public class Shipment
{
 public ulong Id { get; set; }
 public Address SenderAddress { get; set; }
 public Address RecepientAddress { get; set; }
 public string? ApiId { get; set; }
 public string? TrackingNumber { get; set; }
 public string? Provider { get; set; }
 public ShipmentStatus Status { get; set; }
 public ICollection<OrderItem> OrderItems { get; set; }
 protected bool Equals(Shipment other) {
  return Id == other.Id;
 }
 public override bool Equals(object? obj) {
  return ReferenceEquals(this, obj) ||Id!=default&& obj is Shipment other && Equals(other);
 }

 public override int GetHashCode() {
  if (Id == default) return base.GetHashCode();
  return Id.GetHashCode();
 }
}
using Ecommerce.Shipping.Geliver.Types.Network;

namespace Ecommerce.Shipping.Geliver.Types;

  public class GeliverShipment
    {
        public string Id { get; set; }
        public string StatusCode { get; set; }
        public string? TrackingNumber { get; set; }
        public Uri? TrackingUrl { get; set; }
        public string Barcode { get; set; } //provider barcode, not app barcode
        public bool InvoiceGenerated { get; set; } // below is null if false
        public string? LabelFileType { get; set; }
        public Uri? LabelUrl { get; set; }
        public Uri? ResponsiveLabelUrl { get; set; }
        public TrackingStatus TrackingStatus { get; set; }
    }

  public class ShipmentResponse : AResponse<ShipmentResponse>
  {
      public GeliverShipment Shipment { get; set; }
      public ShipmentResponse(GeliverShipment Shipment) {
          this.Shipment = Shipment;
      }
  }
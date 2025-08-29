using System.Globalization;
using Ecommerce.Shipping.Dto;
using Ecommerce.Shipping.Entity;
using Ecommerce.Shipping.Geliver.Types;

namespace Ecommerce.Shipping.Geliver;

public static class CommonEntityExtensions
{
    public static Dimensions ToGeliver(this Ecommerce.Entity.Common.Dimensions dimensions) => new(){
            DistanceUnit = "cm",
            MassUnit = "kg",
            Height = decimal.Round(dimensions.Height, 2).ToString("F", CultureInfo.InvariantCulture),
            Width = decimal.Round(dimensions.Width, 2).ToString("F", CultureInfo.InvariantCulture),
            Length = decimal.Round(dimensions.Depth, 2).ToString("F", CultureInfo.InvariantCulture),
            Weight = decimal.Round(dimensions.Weight, 2).ToString("F", CultureInfo.InvariantCulture),
        };

    public static Item ToGeliver(this ShipmentItem item) => new(){
        Quantity = (int)item.Quantity,
        Sku = item.ItemSku,
        Title = item.ItemName,
        TotalPrice = item.ItemPrice.HasValue?decimal.Round(item.ItemPrice.Value * item.Quantity,2).ToString("F", CultureInfo.InvariantCulture):null,
        UnitWeight = decimal.Round(item.Dimensions.Weight,2).ToString("F", CultureInfo.InvariantCulture),
    };
    public static Order ToGeliver(this OrderInfo orderInfo) => new(){
        OrderNumber = orderInfo.OrderId,
        TotalAmount = decimal.Round(orderInfo.Total,2).ToString("F", CultureInfo.InvariantCulture),
    };
}
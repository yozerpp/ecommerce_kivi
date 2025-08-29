// See https://aka.ms/new-console-template for more information

using Ecommerce.Dao.Default.Initializer;
using Ecommerce.Dao.Initializer;
using Ecommerce.Shipping;
using Ecommerce.Shipping.Dummy;
using Ecommerce.Shipping.Entity;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<ShippingContext>()
    .EnableSensitiveDataLogging(false).UseSqlServer(ShippingContext.DefaultConntectionString).EnableServiceProviderCaching().Options;
var initializer = new DatabaseInitializer(typeof(ShippingContext), options, new Dictionary<Type, int?>{
    {typeof(Provider), 5},
    {typeof(Shipment), 1000},
    {typeof(ShipmentItem), 10000},
},defaultCount:0);
initializer.initialize();
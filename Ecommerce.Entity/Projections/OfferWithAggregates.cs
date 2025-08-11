using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Ecommerce.Entity.Projections;

public class OfferWithAggregates : ProductOffer
{
    public OfferWithAggregates(){}
    
    public static void AssignFrom(ProductOffer offer, OfferWithAggregates offerWithAggregates) {
        foreach (var property in typeof(ProductOffer).GetProperties().Where(p=>p.CanWrite && p.GetCustomAttribute<NotMappedAttribute>()==null)){
            property.SetValue(offerWithAggregates, property.GetValue(offer));
        }
    }
    public int RefundCount { get; init; }
    public int ReviewCount { get; init; }
    public decimal ReviewAverage { get; init; }
}
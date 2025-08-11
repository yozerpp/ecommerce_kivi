namespace Ecommerce.Dao.Default.Migrations;
using Ecommerce.Entity;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;


[Migration("20250811214530_View")]
[DbContext(typeof(DefaultDbContext))]
public class View : Initialize
{
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductReview)}] WITH SCHEMABINDING AS
            SELECT 
                p.Id as {nameof(ProductStats.ProductId)},
                COUNT_BIG(*) as {nameof(ProductStats.ReviewCount)},
                SUM(pr.{nameof(ProductReview.Rating)}) as {nameof(ProductStats.RatingTotal)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Product)}] p
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] pr ON p.{nameof(Product.Id)} = pr.{nameof(ProductReview.ProductId)}
        GROUP BY p.Id");
        migrationBuilder.CreateIndex(
                $"IX_{nameof(ProductStats)}_{nameof(ProductReview)}",
                $"{nameof(ProductStats)}_{nameof(ProductReview)}",
                nameof(ProductStats.ProductId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(OrderItem)}] WITH SCHEMABINDING AS
            SELECT
                p.Id as {nameof(ProductStats.ProductId)},
                COUNT_BIG(*) as {nameof(ProductStats.OrderCount)},
                SUM(oi.{nameof(OrderItem.Quantity)}) as {nameof(ProductStats.SaleCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Product)}] p
                INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi ON p.{nameof(Product.Id)} = oi.{nameof(OrderItem.ProductId)}
            GROUP BY p.Id");
        migrationBuilder.CreateIndex(
                $"IX_{nameof(ProductStats)}_{nameof(OrderItem)}",
                $"{nameof(ProductStats)}_{nameof(OrderItem)}",
                nameof(ProductStats.ProductId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductFavor)}] WITH SCHEMABINDING AS
            SELECT 
                p.Id as {nameof(ProductStats.ProductId)},
                COUNT_BIG(*) as {nameof(ProductStats.FavorCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Product)}] p
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductFavor)}] pf ON p.{nameof(Product.Id)} = pf.{nameof(ProductFavor.ProductId)}
            GROUP BY p.Id
        ");
        migrationBuilder.CreateIndex(
                $"{nameof(ProductStats)}_{nameof(ProductFavor)}",
                $"{nameof(ProductStats)}_{nameof(ProductFavor)}",
                nameof(ProductStats.ProductId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(RefundRequest)}] WITH SCHEMABINDING AS
            SELECT 
                p.Id as {nameof(ProductStats.ProductId)},
                COUNT_BIG(*) as {nameof(ProductStats.RefundCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Product)}] p
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(RefundRequest)}] rr ON p.{nameof(Product.Id)} = rr.{nameof(RefundRequest.ProductId)}
            GROUP BY p.Id");
        migrationBuilder.CreateIndex(
                $"IX_{nameof(ProductStats)}_{nameof(RefundRequest)}",
                $"{nameof(ProductStats)}_{nameof(RefundRequest)}",
                nameof(ProductStats.ProductId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductOffer)}] WITH SCHEMABINDING AS
            SELECT p.Id as {nameof(ProductStats.ProductId)},
            MAX(po.{nameof(ProductOffer.Price)}) as {nameof(ProductStats.MaxPrice)},
            MIN(po.{nameof(ProductOffer.Price)}) as {nameof(ProductStats.MinPrice)}             
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Product)}] p
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po 
            ON p.Id = po.{nameof(ProductOffer.ProductId)}
            GROUP BY p.Id
        ");
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}] WITH SCHEMABINDING AS
            SELECT o.Id as {nameof(OrderAggregates.OrderId)},
            SUM(oi.{nameof(OrderItem.Quantity)}) as {nameof(OrderAggregates.ItemCount)},
            SUM(oa.{nameof(OrderItemAggregates.BasePrice)}) as {nameof(OrderAggregates.BasePrice)},
            SUM(oa.{nameof(OrderItemAggregates.DiscountedPrice)}) as {nameof(OrderAggregates.DiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi ON o.{nameof(Order.Id)} = oi.{nameof(OrderItem.OrderId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] oa ON oa.{nameof(OrderItemAggregates.OrderId)} = o.{nameof(Order.Id)} AND oa.{nameof(OrderItemAggregates.ProductId)} = oi.{nameof(OrderItem.ProductId)} AND oa.{nameof(OrderItemAggregates.SellerId)} = oi.{nameof(OrderItem.SellerId)}
            GROUP BY o.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderAggregates)}", nameof(OrderAggregates), nameof(OrderAggregates.OrderId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}]_{nameof(Coupon)} WITH SCHEMABINDING AS
            SELECT os.Id as {nameof(OrderAggregates.OrderId)},
            SUM(oac.{nameof(OrderItemAggregates.CouponDiscountedPrice)}) as {nameof(OrderAggregates.CouponDiscountedPrice)},
            os.{nameof(OrderAggregates.BasePrice)} - os.{nameof(OrderAggregates.DiscountedPrice)} as {nameof(OrderAggregates.DiscountAmount)},
            os.{nameof(OrderAggregates.DiscountedPrice)} - SUM(oac.{nameof(OrderItemAggregates.CouponDiscountedPrice)}) as {nameof(OrderAggregates.CouponDiscountAmount)},
            os.{nameof(OrderAggregates.BasePrice)} - SUM(oac.{nameof(OrderItemAggregates.CouponDiscountedPrice)}) as {nameof(OrderAggregates.TotalDiscountAmount)},
            (os.{nameof(OrderAggregates.BasePrice)} - SUM(oac.{nameof(OrderItemAggregates.CouponDiscountedPrice)})) / os.{nameof(OrderAggregates.BasePrice)} as {nameof(OrderAggregates.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}] os
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi ON os.{nameof(OrderAggregates.OrderId)} = oi.{nameof(OrderItem.OrderId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}] oac ON oi.{nameof(OrderItem.OrderId)} = oac.{nameof(OrderItemAggregates.OrderId)} AND oi.{nameof(OrderItem.ProductId)} = oac.{nameof(OrderItemAggregates.ProductId)} AND oi.{nameof(OrderItem.SellerId)} = oac.{nameof(OrderItemAggregates.SellerId)}
            GROUP BY os.Id, os.{nameof(OrderAggregates.BasePrice)}, os.{nameof(OrderAggregates.DiscountedPrice)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderAggregates)}_{nameof(Coupon)}",
            $"{nameof(OrderAggregates)}_{nameof(Coupon)}", nameof(OrderAggregates.OrderId), DefaultDbContext.DefaultSchema, true);
        // migrationBuilder.CreateIndex($"IX_{nameof(ProductStats)}_{nameof(ProductOffer)}",
        //     $"{nameof(ProductStats)}_{nameof(ProductOffer)}", $"{nameof(ProductStats.ProductId)}",
        //     DefaultDbContext.DefaultSchema, true);
        
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] WITH SCHEMABINDING AS
            SELECT 
                oi.{nameof(OrderItem.OrderId)},
                oi.{nameof(OrderItem.ProductId)},
                oi.{nameof(OrderItem.SellerId)},
                (po.{nameof(ProductOffer.Price)} * oi.{nameof(OrderItem.Quantity)}) AS {nameof(OrderItemAggregates.BasePrice)},
                (po.{nameof(ProductOffer.Price)} * oi.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Discount)}) AS {nameof(OrderItemAggregates.DiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON oi.{nameof(OrderItem.ProductId)} = po.{nameof(ProductOffer.ProductId)} AND oi.{nameof(OrderItem.SellerId)} = po.{nameof(ProductOffer.SellerId)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderItemAggregates)}", nameof(OrderItemAggregates), new[] { nameof(OrderItemAggregates.OrderId), nameof(OrderItemAggregates.SellerId), nameof(OrderItemAggregates.ProductId) },
            DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                oi.{nameof(OrderItem.OrderId)},
                oi.{nameof(OrderItem.ProductId)},
                oi.{nameof(OrderItem.SellerId)},
                (oa.{nameof(OrderItemAggregates.DiscountedPrice)} * COALESCE(c.{nameof(Coupon.DiscountRate)}, 1)) AS {nameof(OrderItemAggregates.CouponDiscountedPrice)},
                (oa.{nameof(OrderItemAggregates.BasePrice)} - (oa.{nameof(OrderItemAggregates.DiscountedPrice)} * COALESCE(c.{nameof(Coupon.DiscountRate)}, 1)))*100/oa.{nameof(OrderItemAggregates.BasePrice)} AS {nameof(OrderItemAggregates.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] oa on oa.{nameof(OrderItemAggregates.OrderId)} = oi.{nameof(OrderItem.OrderId)} AND oa.{nameof(OrderItemAggregates.ProductId)} = oi.{nameof(OrderItem.ProductId)} AND oa.{nameof(OrderItemAggregates.SellerId)} = oi.{nameof(OrderItem.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Coupon)}] c ON oi.{nameof(OrderItem.CouponId)} = c.{nameof(Coupon.Id)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            $"{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            new[]{
                nameof(OrderItemAggregates.OrderId),nameof(OrderItemAggregates.SellerId), nameof(OrderItemAggregates.ProductId)
            },
            DefaultDbContext.DefaultSchema, true);

        // SellerStats Views
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductOffer)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                COUNT_BIG(po.{nameof(ProductOffer.ProductId)}) as {nameof(SellerStats.OfferCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Seller)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON s.Id = po.{nameof(ProductOffer.SellerId)}
            GROUP BY s.Id
        ");
        migrationBuilder.CreateIndex(
            $"IX_{nameof(SellerStats)}_{nameof(ProductOffer)}",
            $"{nameof(SellerStats)}_{nameof(ProductOffer)}",
            nameof(SellerStats.SellerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                COUNT_BIG(pr.{nameof(ProductReview.Id)}) as {nameof(SellerStats.ReviewCount)},
                AVG(CAST(pr.{nameof(ProductReview.Rating)} AS FLOAT)) as {nameof(SellerStats.ReviewAverage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Seller)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON s.Id = po.{nameof(ProductOffer.SellerId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] pr ON po.{nameof(ProductOffer.ProductId)} = pr.{nameof(ProductReview.ProductId)} AND po.{nameof(ProductOffer.SellerId)} = pr.{nameof(ProductReview.SellerId)}
            GROUP BY s.Id
        ");
        migrationBuilder.CreateIndex(
            $"IX_{nameof(SellerStats)}_{nameof(ProductReview)}",
            $"{nameof(SellerStats)}_{nameof(ProductReview)}",
            nameof(SellerStats.SellerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(OrderItem)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                SUM(oi.{nameof(OrderItem.Quantity)}) as {nameof(SellerStats.SaleCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Seller)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi ON s.Id = oi.{nameof(OrderItem.SellerId)}
            GROUP BY s.Id
        ");
        migrationBuilder.CreateIndex(
            $"IX_{nameof(SellerStats)}_{nameof(OrderItem)}",
            $"{nameof(SellerStats)}_{nameof(OrderItem)}",
            nameof(SellerStats.SellerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop indexes first, then views (order matters!)
    
        migrationBuilder.DropIndex(
            $"IX_{nameof(ProductStats)}_{nameof(ProductReview)}",
            $"{nameof(ProductStats)}_{nameof(ProductReview)}",
            DefaultDbContext.DefaultSchema);
        
        migrationBuilder.DropIndex(
            $"IX_{nameof(ProductStats)}_{nameof(OrderItem)}",
            $"{nameof(ProductStats)}_{nameof(OrderItem)}",
            DefaultDbContext.DefaultSchema);
        
        migrationBuilder.DropIndex(
            $"{nameof(ProductStats)}_{nameof(ProductFavor)}",
            $"{nameof(ProductStats)}_{nameof(ProductFavor)}",
            DefaultDbContext.DefaultSchema);
        
        migrationBuilder.DropIndex(
            $"IX_{nameof(ProductStats)}_{nameof(RefundRequest)}",
            $"{nameof(ProductStats)}_{nameof(RefundRequest)}",
            DefaultDbContext.DefaultSchema);
        // migrationBuilder.DropIndex($"IX_{nameof(ProductStats)}_{nameof(ProductOffer)}",
        //     $"{nameof(ProductStats)}_{nameof(ProductOffer)}",
        //     DefaultDbContext.DefaultSchema);
        // Drop views
    migrationBuilder.Sql($@"
        DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductReview)}]
    ");
    
    migrationBuilder.Sql($@"
        DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(OrderItem)}]
    ");
    
    migrationBuilder.Sql($@"
        DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductFavor)}]
    ");
    
    migrationBuilder.Sql($@"
        DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(RefundRequest)}]
    ");
    
        migrationBuilder.Sql($@"
        DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductOffer)}]
    ");
        migrationBuilder.DropIndex(
            $"IX_{nameof(OrderAggregates)}",
            nameof(OrderAggregates),
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(OrderAggregates)}_{nameof(Coupon)}",
            $"{nameof(OrderAggregates)}_{nameof(Coupon)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}]_{nameof(Coupon)}");
        
        migrationBuilder.DropIndex(
            $"IX_{nameof(OrderItemAggregates)}",
            nameof(OrderItemAggregates),
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}]");

        migrationBuilder.DropIndex(
            $"IX_{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            $"{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}]");

        // Drop SellerStats Views
        migrationBuilder.DropIndex(
            $"IX_{nameof(SellerStats)}_{nameof(ProductOffer)}",
            $"{nameof(SellerStats)}_{nameof(ProductOffer)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(SellerStats)}_{nameof(ProductReview)}",
            $"{nameof(SellerStats)}_{nameof(ProductReview)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(SellerStats)}_{nameof(OrderItem)}",
            $"{nameof(SellerStats)}_{nameof(OrderItem)}",
            DefaultDbContext.DefaultSchema);

        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductOffer)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(OrderItem)}]");
    }
}

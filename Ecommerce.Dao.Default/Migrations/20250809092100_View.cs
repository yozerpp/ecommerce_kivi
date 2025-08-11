namespace Ecommerce.Dao.Default.Migrations;
using Ecommerce.Entity;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;


[Migration("20250809092100_View")]
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
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderStats)}] WITH SCHEMABINDING AS
            SELECT o.Id as {nameof(OrderStats.OrderId)},
            SUM(oi.{nameof(OrderItem.Quantity)}) as {nameof(OrderStats.ItemCount)},
            SUM(po.{nameof(ProductOffer.Price)} * oi.{nameof(OrderItem.Quantity)}) as {nameof(OrderStats.BasePrice)},
            SUM(po.{nameof(ProductOffer.Price)} * oi.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Discount)}) as {nameof(OrderStats.DiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi ON o.{nameof(Order.Id)} = oi.{nameof(OrderItem.OrderId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON oi.{nameof(OrderItem.ProductId)} = po.{nameof(ProductOffer.ProductId)} AND oi.{nameof(OrderItem.SellerId)} = po.{nameof(ProductOffer.SellerId)} 
            GROUP BY o.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderStats)}", nameof(OrderStats), nameof(OrderStats.OrderId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderStats)}]_{nameof(Coupon)} WITH SCHEMABINDING AS
            SELECT os.Id as {nameof(OrderStats.OrderId)},
            os.{nameof(OrderStats.DiscountedPrice)} - SUM(COALESCE((1-c.{nameof(Coupon.DiscountRate)})*oi.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Price)}, 0)) as {nameof(OrderStats.CouponDiscountedPrice)},
            os.{nameof(OrderStats.BasePrice)} - os.{nameof(OrderStats.DiscountedPrice)} as {nameof(OrderStats.DiscountAmount)},
            os.{nameof(OrderStats.DiscountedPrice)} - (os.{nameof(OrderStats.DiscountedPrice)} - SUM(COALESCE((1-c.{nameof(Coupon.DiscountRate)})*oi.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Price)}, 0))) as {nameof(OrderStats.CouponDiscountAmount)},
            os.{nameof(OrderStats.BasePrice)} - (os.{nameof(OrderStats.DiscountedPrice)} - SUM(COALESCE((1-c.{nameof(Coupon.DiscountRate)})*oi.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Price)}, 0))) as {nameof(OrderStats.TotalDiscountAmount)},
            (os.{nameof(OrderStats.BasePrice)} - (os.{nameof(OrderStats.DiscountedPrice)} - SUM(COALESCE((1-c.{nameof(Coupon.DiscountRate)})*oi.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Price)}, 0)))) / os.{nameof(OrderStats.BasePrice)} as {nameof(OrderStats.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(OrderStats)}] os
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi ON os.{nameof(OrderStats.OrderId)} = oi.{nameof(OrderItem.OrderId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON oi.{nameof(OrderItem.ProductId)} = po.{nameof(ProductOffer.ProductId)} AND oi.{nameof(OrderItem.SellerId)} = po.{nameof(ProductOffer.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Coupon)}] c ON oi.{nameof(OrderItem.CouponId)} = c.{nameof(Coupon.Id)}
            GROUP BY os.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderStats)}_{nameof(Coupon)}",
            $"{nameof(OrderStats)}_{nameof(Coupon)}", nameof(OrderStats.OrderId), DefaultDbContext.DefaultSchema, true);
        // migrationBuilder.CreateIndex($"IX_{nameof(ProductStats)}_{nameof(ProductOffer)}",
        //     $"{nameof(ProductStats)}_{nameof(ProductOffer)}", $"{nameof(ProductStats.ProductId)}",
        //     DefaultDbContext.DefaultSchema, true);
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
        DROP VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductReview)}]
    ");
    
        migrationBuilder.Sql($@"
        DROP VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(OrderItem)}]
    ");
    
        migrationBuilder.Sql($@"
        DROP VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductFavor)}]
    ");
    
        migrationBuilder.Sql($@"
        DROP VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(RefundRequest)}]
    ");
    
        migrationBuilder.Sql($@"
        DROP VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductOffer)}]
    ");
        migrationBuilder.DropIndex(
            $"IX_{nameof(OrderStats)}",
            nameof(OrderStats),
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(OrderStats)}_{nameof(Coupon)}",
            $"{nameof(OrderStats)}_{nameof(Coupon)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderStats)}]");
        migrationBuilder.Sql($"DROP VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderStats)}]_{nameof(Coupon)}");
    }
}

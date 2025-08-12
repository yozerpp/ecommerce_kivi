namespace Ecommerce.Dao.Default.Migrations;
using Ecommerce.Entity;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;


[Migration("20250812084000_View")]
[DbContext(typeof(DefaultDbContext))]
public class View : Initialize
{
    protected override void Up(MigrationBuilder migrationBuilder) {
        _Product(migrationBuilder);
        _OrderItem(migrationBuilder);
        _Order(migrationBuilder);
        _SellerStats(migrationBuilder);
        _OfferStats(migrationBuilder);
        _Review(migrationBuilder);
        _ReviewComment(migrationBuilder);
        _CustomerStats(migrationBuilder);
        _CartItem(migrationBuilder);
        _Cart(migrationBuilder);
    }

    private static void _CartItem(MigrationBuilder migrationBuilder) {
                migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}] WITH SCHEMABINDING AS
            SELECT 
                oi.{nameof(CartItem.CartId)},
                oi.{nameof(CartItem.ProductId)},
                oi.{nameof(CartItem.SellerId)},
                (po.{nameof(ProductOffer.Price)} * oi.{nameof(CartItem.Quantity)}) AS {nameof(CartItemAggregates.BasePrice)},
                (po.{nameof(ProductOffer.Price)} * oi.{nameof(CartItem.Quantity)} * po.{nameof(ProductOffer.Discount)}) AS {nameof(CartItemAggregates.DiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(CartItem)}] oi
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON oi.{nameof(CartItem.ProductId)} = po.{nameof(ProductOffer.ProductId)} AND oi.{nameof(CartItem.SellerId)} = po.{nameof(ProductOffer.SellerId)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CartItemAggregates)}", nameof(CartItemAggregates), new[] { nameof(CartItemAggregates.CartId), nameof(CartItemAggregates.SellerId), nameof(CartItemAggregates.ProductId) },
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                oi.{nameof(CartItem.CartId)},
                oi.{nameof(CartItem.ProductId)},
                oi.{nameof(CartItem.SellerId)},
                (oa.{nameof(CartItemAggregates.DiscountedPrice)} * COALESCE(c.{nameof(Coupon.DiscountRate)}, 1)) AS {nameof(CartItemAggregates.CouponDiscountedPrice)},
                (oa.{nameof(CartItemAggregates.BasePrice)} - (oa.{nameof(CartItemAggregates.DiscountedPrice)} * COALESCE(c.{nameof(Coupon.DiscountRate)}, 1)))*100/oa.{nameof(CartItemAggregates.BasePrice)} AS {nameof(CartItemAggregates.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(CartItem)}] oi
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}] oa on oa.{nameof(CartItemAggregates.CartId)} = oi.{nameof(CartItem.CartId)} AND oa.{nameof(CartItemAggregates.ProductId)} = oi.{nameof(CartItem.ProductId)} AND oa.{nameof(CartItemAggregates.SellerId)} = oi.{nameof(CartItem.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Coupon)}] c ON oi.{nameof(CartItem.CouponId)} = c.{nameof(Coupon.Id)}
        ");
        // migrationBuilder.CreateIndex($"IX_{nameof(CartItemAggregates)}_{nameof(Coupon)}",
        //     $"{nameof(CartItemAggregates)}_{nameof(Coupon)}",
        //     new[]{
        //         nameof(CartItemAggregates.CartId),nameof(CartItemAggregates.SellerId), nameof(CartItemAggregates.ProductId)
        //     },
        //     DefaultDbContext.DefaultSchema, true);
    }
    private static void _Cart(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}] WITH SCHEMABINDING AS
            SELECT o.Id as {nameof(CartAggregates.CartId)},
            SUM(oa.{nameof(CartItem.Quantity)} * po.{nameof(ProductOffer.Price)}) as {nameof(CartAggregates.BasePrice)},
            SUM(oa.{nameof(CartItem.Quantity)} * po.{nameof(ProductOffer.Price)} * po.{nameof(ProductOffer.Discount)}) as {nameof(CartAggregates.DiscountedPrice)},
            COUNT_BIG(*) as {nameof(CartAggregates.ItemCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Cart)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItem)}] oa ON oa.{nameof(CartItem.CartId)} = o.{nameof(Cart.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON po.{nameof(ProductOffer.ProductId)} = oa.{nameof(CartItem.ProductId)} AND po.{nameof(ProductOffer.SellerId)} = oa.{nameof(CartItem.SellerId)}
            GROUP BY o.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CartAggregates)}", nameof(CartAggregates), nameof(CartAggregates.CartId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                o.{nameof(Cart.Id)} as {nameof(CartAggregates.CartId)},
                SUM(oi.{nameof(CartItemAggregates.DiscountedPrice)} * COALESCE(1 - c.{nameof(Coupon.DiscountRate)}, 1)) as {nameof(CartAggregates.CouponDiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Cart)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}] oi ON oi.{nameof(CartItemAggregates.CartId)} = o.{nameof(Cart.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItem)}] oii ON oii.{nameof(CartItem.CartId)} = o.{nameof(Cart.Id)} AND oii.{nameof(CartItem.ProductId)} = oi.{nameof(CartItemAggregates.ProductId)} AND oii.{nameof(CartItem.SellerId)} = oi.{nameof(CartItemAggregates.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Coupon)}] c ON oii.{nameof(CartItem.CouponId)} = c.{nameof(Coupon.Id)}
            GROUP BY o.{nameof(Cart.Id)}
        ");
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}_{nameof(CartAggregates)}] WITH SCHEMABINDING AS
            SELECT
                o.{nameof(Cart.Id)} as {nameof(CartAggregates.CartId)},
                oa.{nameof(CartAggregates.BasePrice)} - oa.{nameof(CartAggregates.DiscountedPrice)} as {nameof(CartAggregates.DiscountAmount)},
                oa.{nameof(CartAggregates.DiscountedPrice)} - oac.{nameof(CartAggregates.CouponDiscountedPrice)} as {nameof(CartAggregates.CouponDiscountAmount)},
                COALESCE((oa.{nameof(CartAggregates.BasePrice)} - oac.{nameof(CartAggregates.CouponDiscountedPrice)})/NULLIF(oa.{nameof(CartAggregates.BasePrice)}, 0),0)*100 as  {nameof(CartAggregates.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Cart)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}] oa ON oa.{nameof(CartAggregates.CartId)}=o.{nameof(Cart.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}_{nameof(Coupon)}] oac ON oac.{nameof(CartAggregates.CartId)}=o.{nameof(Cart.Id)}
        ");
    }
    private static void _OfferStats(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(ProductReview)}] WITH SCHEMABINDING AS
            SELECT
                o.{nameof(OfferStats.ProductId)} as {nameof(OfferStats.ProductId)},
                o.{nameof(OfferStats.SellerId)} as {nameof(OfferStats.SellerId)},
                COUNT_BIG(*) as {nameof(OfferStats.ReviewCount)},
                SUM(r.{nameof(ProductReview.Rating)}) as {nameof(OfferStats.RatingTotal)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].{nameof(ProductReview)} r ON r.{nameof(ProductReview.SellerId)} = o.{nameof(ProductOffer.SellerId)} AND r.{nameof(ProductReview.ProductId)} = o.{nameof(ProductOffer.ProductId)}
            GROUP BY o.{nameof(ProductOffer.SellerId)}, o.{nameof(ProductOffer.ProductId)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OfferStats)}_{nameof(ProductReview)}", $"{nameof(OfferStats)}_{nameof(ProductReview)}",[nameof(OfferStats.SellerId), nameof(OfferStats.ProductId)],DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(RefundRequest)}] WITH SCHEMABINDING AS
            SELECT
                o.{nameof(OfferStats.ProductId)} as {nameof(OfferStats.ProductId)},
                o.{nameof(OfferStats.SellerId)} as {nameof(OfferStats.SellerId)},
                COUNT_BIG(*) as {nameof(OfferStats.RefundCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(RefundRequest)}] rr ON o.{nameof(ProductOffer.ProductId)} = rr.{nameof(RefundRequest.ProductId)} AND o.{nameof(ProductOffer.SellerId)} = rr.{nameof(RefundRequest.UserId)}
            WHERE [rr].[{nameof(RefundRequest.IsApproved)}] = 1
            GROUP BY o.{nameof(ProductOffer.SellerId)}, o.{nameof(ProductOffer.ProductId)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OfferStats)}_{nameof(RefundRequest)}", $"{nameof(OfferStats)}_{nameof(RefundRequest)}", [nameof(OfferStats.ProductId), nameof(OfferStats.SellerId)], DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
    }
    private static void _Product(MigrationBuilder migrationBuilder) {
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
            GROUP BY p.Id
        ");
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
        // migrationBuilder.CreateIndex($"IX_{nameof(ProductStats)}_{nameof(ProductOffer)}",
        //     $"{nameof(ProductStats)}_{nameof(ProductOffer)}", $"{nameof(ProductStats.ProductId)}",
        //     DefaultDbContext.DefaultSchema, true);
    }

    private static void _Order(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}] WITH SCHEMABINDING AS
            SELECT o.Id as {nameof(OrderAggregates.OrderId)},
            SUM(oa.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Price)}) as {nameof(OrderAggregates.BasePrice)},
            SUM(oa.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Price)} * po.{nameof(ProductOffer.Discount)}) as {nameof(OrderAggregates.DiscountedPrice)},
            COUNT_BIG(*) as {nameof(OrderAggregates.ItemCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oa ON oa.{nameof(OrderItem.OrderId)} = o.{nameof(Order.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON po.{nameof(ProductOffer.ProductId)} = oa.{nameof(OrderItem.ProductId)} AND po.{nameof(ProductOffer.SellerId)} = oa.{nameof(OrderItem.SellerId)}
            GROUP BY o.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderAggregates)}", nameof(OrderAggregates), nameof(OrderAggregates.OrderId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                o.{nameof(Order.Id)} as {nameof(OrderAggregates.OrderId)},
                SUM(oi.{nameof(OrderItemAggregates.DiscountedPrice)} * COALESCE(1 - c.{nameof(Coupon.DiscountRate)}, 1)) as {nameof(OrderAggregates.CouponDiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] oi ON oi.{nameof(OrderItemAggregates.OrderId)} = o.{nameof(Order.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oii ON oii.{nameof(OrderItem.OrderId)} = o.{nameof(Order.Id)} AND oii.{nameof(OrderItem.ProductId)} = oi.{nameof(OrderItemAggregates.ProductId)} AND oii.{nameof(OrderItem.SellerId)} = oi.{nameof(OrderItemAggregates.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Coupon)}] c ON oii.{nameof(OrderItem.CouponId)} = c.{nameof(Coupon.Id)}
            GROUP BY o.{nameof(Order.Id)}
        ");
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}_{nameof(OrderAggregates)}] WITH SCHEMABINDING AS
            SELECT
                o.{nameof(Order.Id)} as {nameof(OrderAggregates.OrderId)},
                oa.{nameof(OrderAggregates.BasePrice)} - oa.{nameof(OrderAggregates.DiscountedPrice)} as {nameof(OrderAggregates.DiscountAmount)},
                oa.{nameof(OrderAggregates.DiscountedPrice)} - oac.{nameof(OrderAggregates.CouponDiscountedPrice)} as {nameof(OrderAggregates.CouponDiscountAmount)},
                oa.{nameof(OrderAggregates.BasePrice)} - oac.{nameof(OrderAggregates.CouponDiscountedPrice)} as {nameof(OrderAggregates.TotalDiscountAmount)},
                COALESCE((oa.{nameof(OrderAggregates.BasePrice)} - oac.{nameof(OrderAggregates.CouponDiscountedPrice)})/NULLIF(oa.{nameof(OrderAggregates.BasePrice)}, 0),0)*100 as  {nameof(OrderAggregates.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}] oa ON oa.{nameof(OrderAggregates.OrderId)}=o.{nameof(Order.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}_{nameof(Coupon)}] oac ON oac.{nameof(OrderAggregates.OrderId)}=o.{nameof(Order.Id)}
        ");
    }

    private static void _OrderItem(MigrationBuilder migrationBuilder) {
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
        // migrationBuilder.CreateIndex($"IX_{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
        //     $"{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
        //     new[]{
        //         nameof(OrderItemAggregates.OrderId),nameof(OrderItemAggregates.SellerId), nameof(OrderItemAggregates.ProductId)
        //     },
        //     DefaultDbContext.DefaultSchema, true);
    }

    private static void _CustomerStats(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Order)}] WITH SCHEMABINDING AS
            SELECT 
                c.Id as {nameof(CustomerStats.CustomerId)},
                COUNT_BIG(*) as {nameof(CustomerStats.TotalOrders)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o ON c.Id = o.{nameof(Order.UserId)}
            WHERE c.Role = {(int)User.UserRole.Customer}
            GROUP BY c.Id
        ");
        migrationBuilder.CreateIndex(
                $"IX_{nameof(CustomerStats)}_{nameof(Order)}",
                $"{nameof(CustomerStats)}_{nameof(Order)}",
                nameof(CustomerStats.CustomerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ProductReview)}] WITH SCHEMABINDING AS
            SELECT 
                c.Id as {nameof(CustomerStats.CustomerId)},
                COUNT_BIG(*) as {nameof(CustomerStats.TotalReviews)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] pr ON c.Id = pr.{nameof(ProductReview.ReviewerId)}
            WHERE c.Role = {(int)User.UserRole.Customer}
            GROUP BY c.Id
        ");
        migrationBuilder.CreateIndex(
                $"IX_{nameof(CustomerStats)}_{nameof(ProductReview)}",
                $"{nameof(CustomerStats)}_{nameof(ProductReview)}",
                nameof(CustomerStats.CustomerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewComment)}] WITH SCHEMABINDING AS
            SELECT
                c.Id as {nameof(CustomerStats.CustomerId)},
                COUNT_BIG(*) AS {nameof(CustomerStats.TotalComments)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewComment)}] rc ON c.Id = rc.{nameof(ReviewComment.UserId)}
            WHERE c.Role = {(int)User.UserRole.Customer}
            GROUP BY c.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CustomerStats)}_{nameof(ReviewComment)}",
                $"{nameof(CustomerStats)}_{nameof(ReviewComment)}", nameof(CustomerStats.CustomerId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@" 
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}] WITH SCHEMABINDING AS
            SELECT
                c.Id as {nameof(CustomerStats.CustomerId)},
                SUM(CASE rv.{nameof(ReviewVote.Up)} WHEN 1 THEN 1 ELSE -1 END) as {nameof(CustomerStats.CommentVotes)},
                COUNT_BIG(*) as Rc
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewComment)}] rc ON c.Id = rc.{nameof(ReviewComment.UserId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewVote)}] rv ON rc.{nameof(ReviewComment.Id)} = rv.{nameof(ReviewVote.CommentId)}
            WHERE c.Role = {(int)User.UserRole.Customer}
            GROUP BY c.{nameof(Customer.Id)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}",
            $"{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}",
            nameof(CustomerStats.CustomerId),DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}] WITH SCHEMABINDING AS
            SELECT
                c.Id as {nameof(CustomerStats.CustomerId)},
                SUM(CASE rv.{nameof(ReviewVote.Up)} WHEN 1 THEN 1 ELSE -1 END) as {nameof(CustomerStats.ReviewVotes)},
                COUNT_BIG(*) as Rc
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] r ON c.Id = r.{nameof(ProductReview.ReviewerId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewVote)}] rv ON r.{nameof(ProductReview.Id)} = rv.{nameof(ReviewVote.ReviewId)}
            WHERE c.Role = {(int)User.UserRole.Customer} AND rv.{nameof(ReviewVote.CommentId)} IS NULL
            GROUP BY c.{nameof(Customer.Id)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}", $"{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}",
            nameof(CustomerStats.CustomerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                c.Id as {nameof(CustomerStats.CustomerId)},
                SUM(oa.{nameof(OrderAggregates.BasePrice)}) as {nameof(CustomerStats.TotalSpent)},
                SUM(oag.{nameof(OrderAggregates.TotalDiscountAmount)}) as {nameof(CustomerStats.TotalDiscountUsed)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o ON c.Id = o.{nameof(Order.UserId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}] oa ON oa.{nameof(OrderAggregates.OrderId)} = o.{nameof(Order.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}_{nameof(OrderAggregates)}] oag ON o.{nameof(Order.Id)} = oag.{nameof(OrderAggregates.OrderId)}
            WHERE c.Role = {(int)User.UserRole.Customer}
            GROUP BY c.Id
        ");
        // migrationBuilder.CreateIndex(
        //     $"IX_{nameof(CustomerStats)}_{nameof(Coupon)}",
        //     $"{nameof(CustomerStats)}_{nameof(Coupon)}",
        //     nameof(CustomerStats.CustomerId), DefaultDbContext.DefaultSchema, true);
    }

    private void _ReviewComment(MigrationBuilder migrationBuilder) {
        //ReviewCommentStats Views
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}] WITH SCHEMABINDING AS
            SELECT
                r.{nameof(Entity.ReviewComment.Id)} as {nameof(ReviewCommentStats.CommentId)},
                COUNT_BIG(*) AS {nameof(ReviewCommentStats.ReplyCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Entity.ReviewComment)}] r
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Entity.ReviewComment)}] rc ON r.{nameof(Entity.ReviewComment.Id)} = rc.{nameof(Entity.ReviewComment.ParentId)}
            GROUP BY r.Id
        ");
        // migrationBuilder.CreateIndex($"IX_{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}",
        //         $"{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}", nameof(ReviewCommentStats.CommentId),
        //         DefaultDbContext.DefaultSchema, true)
        //     .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}] WITH SCHEMABINDING AS
            SELECT
                r.{nameof(Entity.ReviewComment.Id)} as {nameof(ReviewCommentStats.CommentId)},
                SUM(CASE rv.{nameof(ReviewVote.Up)} WHEN 1 THEN 1 ELSE -1 END) AS {nameof(ReviewCommentStats.Votes)},
                COUNT_BIG(*) AS {nameof(ReviewCommentStats.VoteCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Entity.ReviewComment)}] r
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewVote)}] rv ON r.{nameof(Entity.ReviewComment.Id)} = rv.{nameof(ReviewVote.CommentId)}
            GROUP BY r.{nameof(Entity.ReviewComment.Id)} 
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}",
                $"{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}", nameof(ReviewCommentStats.CommentId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
    }

    private static void _Review(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewComment)}] WITH SCHEMABINDING AS
            SELECT
                r.Id AS {nameof(ReviewStats.ReviewId)},
                COUNT_BIG(*) as {nameof(ReviewStats.CommentCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] r
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewComment)}] rc ON r.{nameof(ProductReview.Id)} = rc.{nameof(ReviewComment.ReviewId)}
            GROUP BY r.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(ReviewStats)}_{nameof(ReviewComment)}",
                $"{nameof(ReviewStats)}_{nameof(ReviewComment)}", nameof(ReviewStats.ReviewId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewVote)}] WITH SCHEMABINDING AS
            SELECT
                r.{nameof(ProductReview.Id)} AS {nameof(ReviewStats.ReviewId)},
                SUM(CASE rv.{nameof(ReviewVote.Up)} WHEN 1 THEN 1 ELSE -1 END) AS {nameof(ReviewStats.Votes)},
                COUNT_BIG(*) AS VOTE_COUNT
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] r
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewVote)}] rv ON r.{nameof(ProductReview.Id)} = rv.{nameof(ReviewVote.ReviewId)}
            GROUP BY r.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(ReviewStats)}_{nameof(ReviewVote)}",
                $"{nameof(ReviewStats)}_{nameof(ReviewVote)}", nameof(ReviewStats.ReviewId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
    }

    private static void _SellerStats(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductOffer)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                COUNT_BIG(*) as {nameof(SellerStats.OfferCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON s.Id = po.{nameof(ProductOffer.SellerId)}
            WHERE  s.Role = {(int)User.UserRole.Seller}
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
                COUNT_BIG(*) as {nameof(SellerStats.ReviewCount)},
                SUM(pr.{nameof(ProductReview.Rating)}) as {nameof(SellerStats.RatingTotal)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON s.Id = po.{nameof(ProductOffer.SellerId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] pr ON po.{nameof(ProductOffer.ProductId)} = pr.{nameof(ProductReview.ProductId)} AND po.{nameof(ProductOffer.SellerId)} = pr.{nameof(ProductReview.SellerId)}
            WHERE  s.Role = {(int)User.UserRole.Seller}
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
                COUNT_BIG(*) as {nameof(SellerStats.SaleCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi ON s.Id = oi.{nameof(OrderItem.SellerId)}
            WHERE  s.Role = {(int)User.UserRole.Seller}
            GROUP BY s.Id
        ");
        migrationBuilder.CreateIndex(
                $"IX_{nameof(SellerStats)}_{nameof(OrderItem)}",
                $"{nameof(SellerStats)}_{nameof(OrderItem)}",
                nameof(SellerStats.SellerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(RefundRequest)}] WITH SCHEMABINDING AS
            SELECT
                s.Id as {nameof(SellerStats.SellerId)},
                COUNT_BIG(*) as {nameof(SellerStats.RefundCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(RefundRequest)}] rr ON s.Id = rr.{nameof(RefundRequest.UserId)}
            WHERE [rr].[{nameof(RefundRequest.IsApproved)}] = 1 AND s.{nameof(User.Role)} = {(int)User.UserRole.Seller}
            GROUP BY s.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(SellerStats)}_{nameof(RefundRequest)}", $"{nameof(SellerStats)}_{nameof(RefundRequest)}",nameof(SellerStats.SellerId),
            DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                SUM(oia.{nameof(OrderItemAggregates.CouponDiscountedPrice)}) as {nameof(SellerStats.TotalSold)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}] oia ON oia.{nameof(OrderItemAggregates.SellerId)} = s.{nameof(Seller.Id)}
            WHERE  s.Role = {(int)User.UserRole.Seller}
            GROUP BY s.Id
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Product Stats
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

        // Order Aggregates
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
        
        // OrderItem Aggregates
        migrationBuilder.DropIndex(
            $"IX_{nameof(OrderItemAggregates)}",
            nameof(OrderItemAggregates),
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}]");
        // migrationBuilder.DropIndex(
        //     $"IX_{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
        //     $"{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
        //     DefaultDbContext.DefaultSchema);
        // migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}]");
        
        // Seller Stats
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
        migrationBuilder.DropIndex($"IX_{nameof(SellerStats)}_{nameof(RefundRequest)}",
            $"{nameof(SellerStats)}_{nameof(RefundRequest)}", DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex($"IX_{nameof(SellerStats)}_{nameof(Coupon)}",
            $"{nameof(SellerStats)}_{nameof(Coupon)}", DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductOffer)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(OrderItem)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(Coupon)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(RefundRequest)}]");
        // Customer Stats
        migrationBuilder.DropIndex(
            $"IX_{nameof(CustomerStats)}_{nameof(Order)}",
            $"{nameof(CustomerStats)}_{nameof(Order)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(CustomerStats)}_{nameof(ProductReview)}",
            $"{nameof(CustomerStats)}_{nameof(ProductReview)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex($"IX_{nameof(CustomerStats)}_{nameof(ReviewComment)}",
            $"{nameof(CustomerStats)}_{nameof(ReviewComment)}", DefaultDbContext.DefaultSchema);
                    // migrationBuilder.DropIndex(
        //     $"IX_{nameof(CustomerStats)}_{nameof(Coupon)}",
        //     $"{nameof(CustomerStats)}_{nameof(Coupon)}",
        //     DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}",
            $"{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}",
            $"{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Order)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Coupon)}]");

        // Review Stats
        migrationBuilder.DropIndex(
            $"IX_{nameof(ReviewStats)}_{nameof(ReviewComment)}",
            $"{nameof(ReviewStats)}_{nameof(ReviewComment)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(ReviewStats)}_{nameof(ReviewVote)}",
            $"{nameof(ReviewStats)}_{nameof(ReviewVote)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewVote)}]");

        // ReviewComment Stats
        // migrationBuilder.DropIndex(
        //     $"IX_{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}",
        //     $"{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}",
        //     DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}",
            $"{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}]");

        // Offer Stats
        migrationBuilder.DropIndex(
            $"IX_{nameof(OfferStats)}_{nameof(ProductReview)}",
            $"{nameof(OfferStats)}_{nameof(ProductReview)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(OfferStats)}_{nameof(RefundRequest)}",
            $"{nameof(OfferStats)}_{nameof(RefundRequest)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(RefundRequest)}]");

        // Cart Aggregates
        migrationBuilder.DropIndex(
            $"IX_{nameof(CartAggregates)}",
            nameof(CartAggregates),
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}]_{nameof(Coupon)}");

        // CartItem Aggregates
        migrationBuilder.DropIndex(
            $"IX_{nameof(CartItemAggregates)}",
            nameof(CartItemAggregates),
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(Coupon)}]");
    }
}

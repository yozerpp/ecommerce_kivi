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
        _Product(migrationBuilder);
        _Order(migrationBuilder);
        _OrderItem(migrationBuilder);
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
        migrationBuilder.CreateIndex($"IX_{nameof(CartItemAggregates)}_{nameof(Coupon)}",
            $"{nameof(CartItemAggregates)}_{nameof(Coupon)}",
            new[]{
                nameof(CartItemAggregates.CartId),nameof(CartItemAggregates.SellerId), nameof(CartItemAggregates.ProductId)
            },
            DefaultDbContext.DefaultSchema, true);
    }
    private static void _Cart(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}] WITH SCHEMABINDING AS
            SELECT o.Id as {nameof(CartAggregates.CartId)},
            SUM(oi.{nameof(CartItem.Quantity)}) as {nameof(CartAggregates.ItemCount)},
            SUM(oa.{nameof(CartItemAggregates.BasePrice)}) as {nameof(CartAggregates.BasePrice)},
            SUM(oa.{nameof(CartItemAggregates.DiscountedPrice)}) as {nameof(CartAggregates.DiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Cart)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}] oa ON oa.{nameof(CartItemAggregates.CartId)} = o.{nameof(Cart.Id)}
            GROUP BY o.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CartAggregates)}", nameof(CartAggregates), nameof(CartAggregates.CartId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}]_{nameof(Coupon)} WITH SCHEMABINDING AS
            SELECT os.Id as {nameof(CartAggregates.CartId)},
            SUM(oac.{nameof(CartItemAggregates.CouponDiscountedPrice)}) as {nameof(CartAggregates.CouponDiscountedPrice)},
            os.{nameof(CartAggregates.BasePrice)} - os.{nameof(CartAggregates.DiscountedPrice)} as {nameof(CartAggregates.DiscountAmount)},
            os.{nameof(CartAggregates.DiscountedPrice)} - SUM(oac.{nameof(CartItemAggregates.CouponDiscountedPrice)}) as {nameof(CartAggregates.CouponDiscountAmount)},
            (os.{nameof(CartAggregates.BasePrice)} - SUM(oac.{nameof(CartItemAggregates.CouponDiscountedPrice)})) / os.{nameof(CartAggregates.BasePrice)} as {nameof(CartAggregates.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}] os
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(Coupon)}] oac ON os.{nameof(CartAggregates.CartId)} = oac.{nameof(CartItemAggregates.CartId)}
            GROUP BY os.Id, os.{nameof(CartAggregates.BasePrice)}, os.{nameof(CartAggregates.DiscountedPrice)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CartAggregates)}_{nameof(Coupon)}",
            $"{nameof(CartAggregates)}_{nameof(Coupon)}", nameof(CartAggregates.CartId), DefaultDbContext.DefaultSchema, true);
    }
    private static void _OfferStats(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(ProductReview)}] WITH SCHEMABINDING AS
            SELECT 
                o.{nameof(ProductOffer.ProductId)} as {nameof(OfferStats.ProductId)},
                o.{nameof(ProductOffer.SellerId)} as {nameof(OfferStats.SellerId)},
                COUNT_BIG(*) as {nameof(OfferStats.ReviewCount)},
                SUM(r.{nameof(ProductReview.Rating)}) as {nameof(OfferStats.RatingTotal)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] r ON r.{nameof(ProductReview.SellerId)} = o.{nameof(ProductOffer.SellerId)} AND r.{nameof(ProductReview.ProductId)} = o.{nameof(ProductOffer.ProductId)}
            GROUP BY o.{nameof(ProductOffer.SellerId)}, o.{nameof(ProductOffer.ProductId)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OfferStats)}_{nameof(ProductReview)}", $"{nameof(OfferStats)}_{nameof(ProductReview)}",new[]{nameof(OfferStats.ProductId), nameof(OfferStats.SellerId)},DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(RefundRequest)}] WITH SCHEMABINDING AS
            SELECT
                o.{nameof(ProductOffer.ProductId)} as {nameof(OfferStats.ProductId)},
                o.{nameof(ProductOffer.SellerId)} as {nameof(OfferStats.SellerId)},
                COUNT_BIG(*) as {nameof(OfferStats.RefundCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(RefundRequest)}] rr ON o.{nameof(ProductOffer.ProductId)} = rr.{nameof(RefundRequest.ProductId)} AND o.{nameof(ProductOffer.SellerId)} = rr.{nameof(RefundRequest.SellerId)}
            WHERE [rr].[{nameof(RefundRequest.IsApproved)}] = 1
            GROUP BY o.{nameof(ProductOffer.SellerId)}, o.{nameof(ProductOffer.ProductId)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OfferStats)}_{nameof(RefundRequest)}", $"{nameof(OfferStats)}_{nameof(RefundRequest)}", new[]{nameof(OfferStats.ProductId), nameof(OfferStats.SellerId)}, DefaultDbContext.DefaultSchema, true)
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
        // migrationBuilder.CreateIndex($"IX_{nameof(ProductStats)}_{nameof(ProductOffer)}",
        //     $"{nameof(ProductStats)}_{nameof(ProductOffer)}", $"{nameof(ProductStats.ProductId)}",
        //     DefaultDbContext.DefaultSchema, true);
    }

    private static void _Order(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}] WITH SCHEMABINDING AS
            SELECT o.Id as {nameof(OrderAggregates.OrderId)},
            SUM(oi.{nameof(OrderItem.Quantity)}) as {nameof(OrderAggregates.ItemCount)},
            SUM(oa.{nameof(OrderItemAggregates.BasePrice)}) as {nameof(OrderAggregates.BasePrice)},
            SUM(oa.{nameof(OrderItemAggregates.DiscountedPrice)}) as {nameof(OrderAggregates.DiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] oa ON oa.{nameof(OrderItemAggregates.OrderId)} = o.{nameof(Order.Id)}
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
            (os.{nameof(OrderAggregates.BasePrice)} - SUM(oac.{nameof(OrderItemAggregates.CouponDiscountedPrice)})) / os.{nameof(OrderAggregates.BasePrice)} as {nameof(OrderAggregates.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}] os
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}] oac ON os.{nameof(OrderAggregates.OrderId)} = oac.{nameof(OrderItemAggregates.OrderId)}
            GROUP BY os.Id, os.{nameof(OrderAggregates.BasePrice)}, os.{nameof(OrderAggregates.DiscountedPrice)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderAggregates)}_{nameof(Coupon)}",
            $"{nameof(OrderAggregates)}_{nameof(Coupon)}", nameof(OrderAggregates.OrderId), DefaultDbContext.DefaultSchema, true);
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
        migrationBuilder.CreateIndex($"IX_{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            $"{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            new[]{
                nameof(OrderItemAggregates.OrderId),nameof(OrderItemAggregates.SellerId), nameof(OrderItemAggregates.ProductId)
            },
            DefaultDbContext.DefaultSchema, true);
    }

    private static void _CustomerStats(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Order)}] WITH SCHEMABINDING AS
            SELECT 
                c.Id as {nameof(CustomerStats.CustomerId)},
                COUNT_BIG(o.{nameof(Order.Id)}) as {nameof(CustomerStats.TotalOrders)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Customer)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o ON c.Id = o.{nameof(Order.UserId)}
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
                COUNT_BIG(pr.{nameof(ProductReview.Id)}) as {nameof(CustomerStats.TotalReviews)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Customer)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] pr ON c.Id = pr.{nameof(ProductReview.ReviewerId)}
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
                COUNT_BIG(rc.{nameof(ReviewComment.Id)}) AS {nameof(CustomerStats.TotalComments)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Customer)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewComment)}] rc ON c.Id = rc.{nameof(ReviewComment.UserId)}
            GROUP BY c.Id
        ");
        migrationBuilder.CreateIndex(
            $"IX_{nameof(CustomerStats)}_{nameof(ReviewComment)}",
            $"{nameof(CustomerStats)}_{nameof(ReviewComment)}",
            nameof(CustomerStats.CustomerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@" 
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}] WITH SCHEMABINDING AS
            SELECT
                c.Id as {nameof(CustomerStats.CustomerId)},
                SUM(rcs.{nameof(ReviewCommentStats.Votes)}) as {nameof(CustomerStats.CommentVotes)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Customer)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewComment)}] rc ON c.Id = rc.{nameof(ReviewComment.UserId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}] rcs ON rc.{nameof(ReviewComment.Id)} = rcs.{nameof(ReviewCommentStats.CommentId)}
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
                SUM(rs.{nameof(ReviewStats.Votes)}) as {nameof(CustomerStats.ReviewVotes)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Customer)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] r ON c.Id = r.{nameof(ProductReview.ReviewerId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewVote)}] rs ON r.{nameof(ProductReview.Id)} = rs.{nameof(ReviewStats.ReviewId)}
            GROUP BY c.{nameof(Customer.Id)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}", $"{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}",
            nameof(CustomerStats.CustomerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                c.Id as {nameof(CustomerStats.CustomerId)},
                SUM(oag.{nameof(OrderAggregates.BasePrice)}) as {nameof(CustomerStats.TotalSpent)},
                SUM(oag.{nameof(OrderAggregates.TotalDiscountAmount)}) as {nameof(CustomerStats.TotalDiscountUsed)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Customer)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o ON c.Id = o.{nameof(Order.UserId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}] oag ON o.Id = oag.{nameof(OrderAggregates.OrderId)}
            GROUP BY c.Id
        ");
        migrationBuilder.CreateIndex(
            $"IX_{nameof(CustomerStats)}_{nameof(Coupon)}",
            $"{nameof(CustomerStats)}_{nameof(Coupon)}",
            nameof(CustomerStats.CustomerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
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
        migrationBuilder.CreateIndex($"IX_{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}",
                $"{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}", nameof(ReviewCommentStats.CommentId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}] WITH SCHEMABINDING AS
            SELECT
                r.{nameof(Entity.ReviewComment.Id)} as {nameof(ReviewCommentStats.CommentId)},
                SUM(CASE rv.{nameof(ReviewVote.Up)} WHEN 1 THEN 1 ELSE -1 END) AS {nameof(ReviewCommentStats.Votes)}
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
                SUM(CASE rv.{nameof(ReviewVote.Up)} WHEN 1 THEN 1 ELSE -1 END) AS {nameof(ReviewStats.Votes)}
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
                COUNT_BIG(*) as {nameof(SellerStats.ReviewCount)},
                SUM(pr.{nameof(ProductReview.Rating)}) as {nameof(SellerStats.RatingTotal)}
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

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(RefundRequest)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                COUNT_BIG(rr.{nameof(RefundRequest.Id)}) as {nameof(SellerStats.RefundCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Seller)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(RefundRequest)}] rr ON s.Id = rr.{nameof(RefundRequest.SellerId)}
            WHERE rr.{nameof(RefundRequest.IsApproved)} = 1
            GROUP BY s.Id
        ");
        migrationBuilder.CreateIndex(
                $"IX_{nameof(SellerStats)}_{nameof(RefundRequest)}",
                $"{nameof(SellerStats)}_{nameof(RefundRequest)}",
                nameof(SellerStats.SellerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                SUM(oi.{nameof(OrderItem.Quantity)} * oi.{nameof(OrderItem.Price)}) as {nameof(SellerStats.TotalSold)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Seller)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi ON s.Id = oi.{nameof(OrderItem.SellerId)}
            GROUP BY s.Id
        ");
        migrationBuilder.CreateIndex(
                $"IX_{nameof(SellerStats)}_{nameof(Coupon)}",
                $"{nameof(SellerStats)}_{nameof(Coupon)}",
                nameof(SellerStats.SellerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Product Stats
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(ProductStats)}_{nameof(ProductReview)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(ProductStats)}_{nameof(ProductReview)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(ProductStats)}_{nameof(OrderItem)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(ProductStats)}_{nameof(OrderItem)}");
        migrationBuilder.DropIndex(
            name: $"{nameof(ProductStats)}_{nameof(ProductFavor)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(ProductStats)}_{nameof(ProductFavor)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(ProductStats)}_{nameof(RefundRequest)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(ProductStats)}_{nameof(RefundRequest)}");
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
            name: $"IX_{nameof(OrderAggregates)}",
            schema: DefaultDbContext.DefaultSchema,
            table: nameof(OrderAggregates));
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(OrderAggregates)}_{nameof(Coupon)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(OrderAggregates)}_{nameof(Coupon)}");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}]_{nameof(Coupon)}");
        
        // OrderItem Aggregates
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(OrderItemAggregates)}",
            schema: DefaultDbContext.DefaultSchema,
            table: nameof(OrderItemAggregates));
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}]");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(OrderItemAggregates)}_{nameof(Coupon)}");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}]");
        
        // Seller Stats
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(SellerStats)}_{nameof(ProductOffer)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(SellerStats)}_{nameof(ProductOffer)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(SellerStats)}_{nameof(ProductReview)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(SellerStats)}_{nameof(ProductReview)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(SellerStats)}_{nameof(OrderItem)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(SellerStats)}_{nameof(OrderItem)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(SellerStats)}_{nameof(RefundRequest)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(SellerStats)}_{nameof(RefundRequest)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(SellerStats)}_{nameof(Coupon)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(SellerStats)}_{nameof(Coupon)}");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductOffer)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(OrderItem)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(RefundRequest)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(Coupon)}]");

        // Customer Stats
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CustomerStats)}_{nameof(Order)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(CustomerStats)}_{nameof(Order)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CustomerStats)}_{nameof(ProductReview)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(CustomerStats)}_{nameof(ProductReview)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CustomerStats)}_{nameof(Coupon)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(CustomerStats)}_{nameof(Coupon)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CustomerStats)}_{nameof(ReviewComment)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(CustomerStats)}_{nameof(ReviewComment)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}");

        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Order)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Coupon)}]");

        // Review Stats
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(ReviewStats)}_{nameof(ReviewComment)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(ReviewStats)}_{nameof(ReviewComment)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(ReviewStats)}_{nameof(ReviewVote)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(ReviewStats)}_{nameof(ReviewVote)}");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewVote)}]");

        // ReviewComment Stats
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(Entity.ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}]");

        // Offer Stats
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(OfferStats)}_{nameof(ProductReview)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(OfferStats)}_{nameof(ProductReview)}");
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(OfferStats)}_{nameof(RefundRequest)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(OfferStats)}_{nameof(RefundRequest)}");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(RefundRequest)}]");

        // Cart Aggregates
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CartAggregates)}",
            schema: DefaultDbContext.DefaultSchema,
            table: nameof(CartAggregates));
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CartAggregates)}_{nameof(Coupon)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(CartAggregates)}_{nameof(Coupon)}");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}]_{nameof(Coupon)}");

        // CartItem Aggregates
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CartItemAggregates)}",
            schema: DefaultDbContext.DefaultSchema,
            table: nameof(CartItemAggregates));
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CartItemAggregates)}_{nameof(Coupon)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(CartItemAggregates)}_{nameof(Coupon)}");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(Coupon)}]");
    }
}

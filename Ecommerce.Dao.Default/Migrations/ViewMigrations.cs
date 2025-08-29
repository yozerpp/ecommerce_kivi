namespace Ecommerce.Dao.Default.Migrations;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;

public static class ViewMigrations
{
    public  static void Up(MigrationBuilder migrationBuilder) {
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
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(ProductOffer)}] WITH SCHEMABINDING AS
            SELECT 
                ci.{nameof(CartItem.CartId)},
                ci.{nameof(CartItem.ProductId)},
                ci.{nameof(CartItem.SellerId)},
                (po.{nameof(ProductOffer.Price)} * ci.{nameof(CartItem.Quantity)}) AS {nameof(CartItemAggregates.BasePrice)},
                (po.{nameof(ProductOffer.Price)} * ci.{nameof(CartItem.Quantity)} * po.{nameof(ProductOffer.Discount)}) AS {nameof(CartItemAggregates.DiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(CartItem)}] ci
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON ci.{nameof(CartItem.ProductId)} = po.{nameof(ProductOffer.ProductId)} AND ci.{nameof(CartItem.SellerId)} = po.{nameof(ProductOffer.SellerId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Cart)}] c ON ci.{nameof(CartItem.CartId)} = c.{nameof(Cart.Id)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CartItemAggregates)}_{nameof(ProductOffer)}", nameof(CartItemAggregates)+'_' + nameof(ProductOffer),
                [nameof(CartItemAggregates.CartId), nameof(CartItemAggregates.SellerId), nameof(CartItemAggregates.ProductId)],
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                ci.{nameof(CartItem.CartId)},
                ci.{nameof(CartItem.ProductId)},
                ci.{nameof(CartItem.SellerId)},
                po.{nameof(ProductOffer.Price)} * ci.{nameof(CartItem.Quantity)} * po.{nameof(ProductOffer.Discount)} * c.{nameof(Coupon.DiscountRate)} AS {nameof(CartItemAggregates.CouponDiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(CartItem)}] ci
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON po.{nameof(ProductOffer.ProductId)} = ci.{nameof(CartItem.ProductId)} AND po.{nameof(ProductOffer.SellerId)} = ci.{nameof(CartItem.SellerId)} 
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Coupon)}] c ON ci.{nameof(CartItem.CouponId)} = c.{nameof(Coupon.Id)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CartItemAggregates)}_{nameof(Coupon)}",
            $"{nameof(CartItemAggregates)}_{nameof(Coupon)}",
            new[]{
                nameof(CartItemAggregates.CartId),nameof(CartItemAggregates.SellerId), nameof(CartItemAggregates.ProductId)
            },
            DefaultDbContext.DefaultSchema, true).Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}] WITH SCHEMABINDING AS
            SELECT 
                ci.{nameof(CartItem.CartId)},
                ci.{nameof(CartItem.ProductId)},
                ci.{nameof(CartItem.SellerId)},
                SUM(cif.{nameof(CartItemAggregates.BasePrice)}) as {nameof(CartItemAggregates.BasePrice)},
                SUM(cif.{nameof(CartItemAggregates.DiscountedPrice)}) as {nameof(CartItemAggregates.DiscountedPrice)},
                COALESCE(SUM(cic.{nameof(CartItemAggregates.CouponDiscountedPrice)}),SUM(cif.{nameof(CartItemAggregates.DiscountedPrice)})) as {nameof(CartItemAggregates.CouponDiscountedPrice)},
                COALESCE((SUM(cif.{nameof(CartItemAggregates.BasePrice)}) - COALESCE(SUM(cic.{nameof(CartItemAggregates.CouponDiscountedPrice)}),SUM(cif.{nameof(CartItemAggregates.BasePrice)})))/NULLIF(SUM(cif.{nameof(CartItemAggregates.BasePrice)}), 0.0) *100,0.0) AS {nameof(CartItemAggregates.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(CartItem)}] ci
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(ProductOffer)}] cif ON cif.{nameof(CartItemAggregates.SellerId)} = ci.{nameof(CartItem.SellerId)} AND cif.{nameof(CartItemAggregates.CartId)} = ci.{nameof(CartItem.CartId)} AND cif.{nameof(CartItemAggregates.ProductId)} = ci.{nameof(CartItem.ProductId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(Coupon)}] cic ON cic.{nameof(CartItemAggregates.SellerId)} = cif.{nameof(CartItemAggregates.SellerId)} AND cic.{nameof(CartItemAggregates.CartId)} = cif.{nameof(CartItemAggregates.CartId)} AND cic.{nameof(CartItemAggregates.ProductId)} = cif.{nameof(CartItemAggregates.ProductId)}
            GROUP BY ci.{nameof(CartItem.CartId)}, ci.{nameof(CartItem.ProductId)}, ci.{nameof(CartItem.SellerId)}
        ");
    }
    private static void _Cart(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}] WITH SCHEMABINDING AS
            SELECT c.Id as {nameof(CartAggregates.CartId)},
            SUM(ci.{nameof(CartItem.Quantity)} * po.{nameof(ProductOffer.Price)}) as {nameof(CartAggregates.BasePrice)},
            SUM(ci.{nameof(CartItem.Quantity)} * po.{nameof(ProductOffer.Price)} * po.{nameof(ProductOffer.Discount)}) as {nameof(CartAggregates.DiscountedPrice)},
            COUNT_BIG(*) as {nameof(CartAggregates.ItemCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Cart)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItem)}] ci ON ci.{nameof(CartItem.CartId)} = c.{nameof(Cart.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON po.{nameof(ProductOffer.ProductId)} = ci.{nameof(CartItem.ProductId)} AND po.{nameof(ProductOffer.SellerId)} = ci.{nameof(CartItem.SellerId)}
            GROUP BY c.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(CartAggregates)}", nameof(CartAggregates), nameof(CartAggregates.CartId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                c.{nameof(Cart.Id)} as {nameof(CartAggregates.CartId)},
                SUM(ci.{nameof(CartItemAggregates.CouponDiscountedPrice)}) as {nameof(CartAggregates.CouponDiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Cart)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}] ci ON ci.{nameof(CartItemAggregates.CartId)} = c.{nameof(Cart.Id)} 
            GROUP BY c.{nameof(Cart.Id)}
        ");
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}_{nameof(CartAggregates)}] WITH SCHEMABINDING AS
            SELECT
                c.{nameof(Cart.Id)} as {nameof(CartAggregates.CartId)},
                COALESCE(SUM(ca.{nameof(CartAggregates.BasePrice)}),0.0) as {nameof(CartAggregates.BasePrice)},
                COALESCE(SUM(ca.{nameof(CartAggregates.DiscountedPrice)}),0.0) as {nameof(CartAggregates.DiscountedPrice)},
                COALESCE(SUM(ci.{nameof(CartItemAggregates.CouponDiscountedPrice)}),SUM(ca.{nameof(CartAggregates.DiscountedPrice)}),0.0) as {nameof(CartAggregates.CouponDiscountedPrice)},
                COALESCE(SUM(ca.{nameof(CartAggregates.BasePrice)}),0.0) - COALESCE(SUM(ca.{nameof(CartAggregates.DiscountedPrice)}),0.0) as {nameof(CartAggregates.DiscountAmount)},
                COALESCE(SUM(ca.{nameof(CartAggregates.DiscountedPrice)}),0.0) - COALESCE(SUM(cac.{nameof(CartAggregates.CouponDiscountedPrice)}),0.0) as {nameof(CartAggregates.CouponDiscountAmount)},
                COALESCE((COALESCE(SUM(ca.{nameof(CartAggregates.BasePrice)}),0.0) - COALESCE(SUM(cac.{nameof(CartAggregates.CouponDiscountedPrice)}), SUM(ca.{nameof(CartAggregates.DiscountedPrice)}),0.0))/NULLIF(SUM(ca.{nameof(CartAggregates.BasePrice)}), 0),0)*100 as  {nameof(CartAggregates.TotalDiscountPercentage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Cart)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}] ca ON ca.{nameof(CartAggregates.CartId)}=c.{nameof(Cart.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}] ci ON ci.{nameof(CartItemAggregates.CartId)} = c.{nameof(Cart.Id)} 
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}_{nameof(Coupon)}] cac ON cac.{nameof(CartAggregates.CartId)}=c.{nameof(Cart.Id)}
            GROUP BY c.{nameof(Cart.Id)}
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
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}] WITH SCHEMABINDING AS
            SELECT 
                o.{nameof(ProductOffer.ProductId)},
                o.{nameof(ProductOffer.SellerId)},
                COALESCE(orev.{nameof(OfferStats.RatingTotal)} / NULLIF(orev.{nameof(OfferStats.ReviewCount)}, 0),0.0) AS {nameof(OfferStats.ReviewAverage)},
                COALESCE(SUM(orev.{nameof(OfferStats.ReviewCount)}), 0) AS {nameof(OfferStats.ReviewCount)},
                COALESCE(SUM(orev.{nameof(OfferStats.RatingTotal)}), 0.0) AS {nameof(OfferStats.RatingTotal)},
                COALESCE(SUM(oref.{nameof(OfferStats.RefundCount)}), 0) AS {nameof(OfferStats.RefundCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] o
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(ProductReview)}] orev ON orev.{nameof(OfferStats.ProductId)} = o.{nameof(ProductOffer.ProductId)} AND orev.{nameof(OfferStats.SellerId)} = o.{nameof(ProductOffer.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(RefundRequest)}] oref ON oref.{nameof(OfferStats.ProductId)} = o.{nameof(ProductOffer.ProductId)} AND oref.{nameof(OfferStats.SellerId)} = o.{nameof(ProductOffer.SellerId)}
            GROUP BY o.{nameof(ProductOffer.ProductId)}, o.{nameof(ProductOffer.SellerId)}
        ");
    }
    private static void _Product(MigrationBuilder migrationBuilder) {
        var cancelledStatus = (int)OrderStatus.Cancelled;
        var returnedStatus = (int)OrderStatus.Returned;

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
                INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o ON oi.{nameof(OrderItem.OrderId)} = o.{nameof(Order.Id)}
            WHERE o.{nameof(Order.Status)} != {cancelledStatus} AND o.{nameof(Order.Status)} != {returnedStatus}
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

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductReview)}Average] WITH SCHEMABINDING AS
            SELECT 
                pspr.{nameof(ProductStats.ProductId)},
                CAST(pspr.{nameof(ProductStats.RatingTotal)} AS DECIMAL(10, 2)) / pspr.{nameof(ProductStats.ReviewCount)} AS {nameof(ProductStats.RatingAverage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductReview)}] pspr
            WHERE pspr.{nameof(ProductStats.ReviewCount)} > 0
        ");

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}] WITH SCHEMABINDING AS
            SELECT 
                p.Id as {nameof(ProductStats.ProductId)},
                COALESCE(SUM(pspo.{nameof(ProductStats.MinPrice)}), 0.0) AS {nameof(ProductStats.MinPrice)},
                COALESCE(SUM(pspo.{nameof(ProductStats.MaxPrice)}), 0.0) AS {nameof(ProductStats.MaxPrice)},
                COALESCE(SUM(pspr.{nameof(ProductStats.ReviewCount)}), 0) AS {nameof(ProductStats.ReviewCount)},
                COALESCE(SUM(pspr.{nameof(ProductStats.RatingTotal)}), 0.0) AS {nameof(ProductStats.RatingTotal)},
                COALESCE(SUM(pspra.{nameof(ProductStats.RatingAverage)}), 0.0) AS {nameof(ProductStats.RatingAverage)},
                COALESCE(SUM(psoi.{nameof(ProductStats.OrderCount)}), 0) AS {nameof(ProductStats.OrderCount)},
                COALESCE(SUM(psoi.{nameof(ProductStats.SaleCount)}), 0) AS {nameof(ProductStats.SaleCount)},
                COALESCE(SUM(pspf.{nameof(ProductStats.FavorCount)}), 0) AS {nameof(ProductStats.FavorCount)},
                COALESCE(SUM(psrr.{nameof(ProductStats.RefundCount)}), 0) AS {nameof(ProductStats.RefundCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Product)}] p
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductOffer)}] pspo ON p.Id = pspo.{nameof(ProductStats.ProductId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductReview)}] pspr ON p.Id = pspr.{nameof(ProductStats.ProductId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductReview)}Average] pspra ON p.Id = pspra.{nameof(ProductStats.ProductId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(OrderItem)}] psoi ON p.Id = psoi.{nameof(ProductStats.ProductId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductFavor)}] pspf ON p.Id = pspf.{nameof(ProductStats.ProductId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(RefundRequest)}] psrr ON p.Id = psrr.{nameof(ProductStats.ProductId)},
        GROUP BY p.Id
        ");
        DoProductRatingStats(migrationBuilder,5,nameof(ProductRatingStats.FiveStarCount));
        DoProductRatingStats(migrationBuilder,4,nameof(ProductRatingStats.FourStarCount));
        DoProductRatingStats(migrationBuilder,3,nameof(ProductRatingStats.ThreeStarCount));
        DoProductRatingStats(migrationBuilder,2,nameof(ProductRatingStats.TwoStarCount));
        DoProductRatingStats(migrationBuilder,1,nameof(ProductRatingStats.OneStarCount));
        DoProductRatingStats(migrationBuilder,0,nameof(ProductRatingStats.ZeroStarCount));
    }

    private static void DoProductRatingStats(MigrationBuilder migrationBuilder,int cond, string name) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ProductRatingStats)}_{name}] WITH SCHEMABINDING AS
            SELECT
                p.{nameof(Product.Id)} as {nameof(ProductRatingStats.ProductId)},
                COUNT_BIG(*) as {name}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Product)}] p
            JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] pr ON p.{nameof(Product.Id)} = pr.{nameof(ProductReview.ProductId)}
            WHERE pr.{nameof(ProductReview.Rating)} >= {cond} AND pr.{nameof(ProductReview.Rating)} < {cond + 1}
            GROUP BY p.{nameof(Product.Id)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(ProductRatingStats)}_{name}", $"{nameof(ProductRatingStats)}_{name}",
                $"{nameof(ProductRatingStats.ProductId)}", DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
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
                SUM(oi.{nameof(OrderItemAggregates.CouponDiscountedPrice)}) as {nameof(OrderAggregates.CouponDiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] oi ON oi.{nameof(OrderItemAggregates.OrderId)} = o.{nameof(Order.Id)} 
            GROUP BY o.{nameof(Order.Id)}
        ");
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}_{nameof(OrderAggregates)}] WITH SCHEMABINDING AS
            SELECT
                o.{nameof(Order.Id)} as {nameof(OrderAggregates.OrderId)},
                COALESCE(SUM(oa.{nameof(OrderAggregates.BasePrice)}),0.0) as {nameof(OrderAggregates.BasePrice)},
                COALESCE(SUM(oa.{nameof(OrderAggregates.DiscountedPrice)}),0.0) as {nameof(OrderAggregates.DiscountedPrice)},
                COALESCE(SUM(oi.{nameof(OrderItemAggregates.CouponDiscountedPrice)}),SUM(oa.{nameof(OrderAggregates.DiscountedPrice)}),0.0) as {nameof(OrderAggregates.CouponDiscountedPrice)},
                COALESCE(SUM(oa.{nameof(OrderAggregates.BasePrice)}),0.0) - COALESCE(SUM(oa.{nameof(OrderAggregates.DiscountedPrice)}),0.0) as {nameof(OrderAggregates.DiscountAmount)},
                COALESCE(SUM(oa.{nameof(OrderAggregates.DiscountedPrice)}),0.0) - COALESCE(SUM(oac.{nameof(OrderAggregates.CouponDiscountedPrice)}),0.0) as {nameof(OrderAggregates.CouponDiscountAmount)},
                COALESCE((COALESCE(SUM(oa.{nameof(OrderAggregates.BasePrice)}),0.0) - COALESCE(SUM(oac.{nameof(OrderAggregates.CouponDiscountedPrice)}), SUM(oa.{nameof(OrderAggregates.DiscountedPrice)}),0.0))/NULLIF(SUM(oa.{nameof(OrderAggregates.BasePrice)}), 0),0)*100 as  {nameof(OrderAggregates.TotalDiscountPercentage)},
                COALESCE(SUM(oa.{nameof(OrderAggregates.BasePrice)}),0.0) - COALESCE(SUM(oac.{nameof(OrderAggregates.CouponDiscountedPrice)}), SUM(oa.{nameof(OrderAggregates.DiscountedPrice)}),0.0) as {nameof(OrderAggregates.TotalDiscountAmount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}] oa ON oa.{nameof(OrderAggregates.OrderId)}=o.{nameof(Order.Id)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] oi ON oi.{nameof(OrderItemAggregates.OrderId)} = o.{nameof(Order.Id)} 
            GROUP BY o.{nameof(Order.Id)}
        ");
    }

    private static void _OrderItem(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(ProductOffer)}] WITH SCHEMABINDING AS
            SELECT 
                oi.{nameof(OrderItem.OrderId)},
                oi.{nameof(OrderItem.ProductId)},
                oi.{nameof(OrderItem.SellerId)},
                (po.{nameof(ProductOffer.Price)} * oi.{nameof(OrderItem.Quantity)}) AS {nameof(OrderItemAggregates.BasePrice)},
                (po.{nameof(ProductOffer.Price)} * oi.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Discount)}) AS {nameof(OrderItemAggregates.DiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON oi.{nameof(OrderItem.ProductId)} = po.{nameof(ProductOffer.ProductId)} AND oi.{nameof(OrderItem.SellerId)} = po.{nameof(ProductOffer.SellerId)}
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o ON oi.{nameof(OrderItem.OrderId)} = o.{nameof(Order.Id)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderItemAggregates)}_{nameof(ProductOffer)}", nameof(OrderItemAggregates)+'_' + nameof(ProductOffer),
                [nameof(OrderItemAggregates.OrderId), nameof(OrderItemAggregates.SellerId), nameof(OrderItemAggregates.ProductId)],
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}] WITH SCHEMABINDING AS
            SELECT 
                oi.{nameof(OrderItem.OrderId)},
                oi.{nameof(OrderItem.ProductId)},
                oi.{nameof(OrderItem.SellerId)},
                po.{nameof(ProductOffer.Price)} * oi.{nameof(OrderItem.Quantity)} * po.{nameof(ProductOffer.Discount)} * {nameof(Coupon.DiscountRate)} AS {nameof(OrderItemAggregates.CouponDiscountedPrice)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ProductOffer)}] po ON po.{nameof(ProductOffer.ProductId)} = oi.{nameof(OrderItem.ProductId)} AND po.{nameof(ProductOffer.SellerId)} = oi.{nameof(OrderItem.SellerId)} 
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Coupon)}] c ON oi.{nameof(OrderItem.CouponId)} = c.{nameof(Coupon.Id)}
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            $"{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            new[]{
                nameof(OrderItemAggregates.OrderId),nameof(OrderItemAggregates.SellerId), nameof(OrderItemAggregates.ProductId)
            },
            DefaultDbContext.DefaultSchema, true).Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] WITH SCHEMABINDING AS
            SELECT 
                oi.{nameof(OrderItem.OrderId)},
                oi.{nameof(OrderItem.ProductId)},
                oi.{nameof(OrderItem.SellerId)},
                SUM(oif.{nameof(OrderItemAggregates.BasePrice)}) as {nameof(OrderItemAggregates.BasePrice)},
                SUM(oif.{nameof(OrderItemAggregates.DiscountedPrice)}) as {nameof(OrderItemAggregates.DiscountedPrice)},
                COALESCE(SUM(oic.{nameof(OrderItemAggregates.CouponDiscountedPrice)}),SUM(oif.{nameof(OrderItemAggregates.DiscountedPrice)})) as {nameof(OrderItemAggregates.CouponDiscountedPrice)},
                COALESCE((SUM(oif.{nameof(OrderItemAggregates.BasePrice)}) - COALESCE(SUM(oic.{nameof(OrderItemAggregates.CouponDiscountedPrice)}),SUM(oif.{nameof(OrderItemAggregates.BasePrice)})))/NULLIF(SUM(oif.{nameof(OrderItemAggregates.BasePrice)}), 0.0) *100,0.0) AS {nameof(OrderItemAggregates.TotalDiscountPercentage)},
                SUM(oi.{nameof(OrderItem.Status)}) as {nameof(OrderItem.Status)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(ProductOffer)}] oif ON oif.{nameof(OrderItemAggregates.SellerId)} = oi.{nameof(OrderItem.SellerId)} AND oif.{nameof(OrderItemAggregates.OrderId)} = oi.{nameof(OrderItem.OrderId)} AND oif.{nameof(OrderItemAggregates.ProductId)} = oi.{nameof(OrderItem.ProductId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}] oic ON oic.{nameof(OrderItemAggregates.SellerId)} = oif.{nameof(OrderItemAggregates.SellerId)} AND oic.{nameof(OrderItemAggregates.OrderId)} = oif.{nameof(OrderItemAggregates.OrderId)} AND oic.{nameof(OrderItemAggregates.ProductId)} = oif.{nameof(OrderItemAggregates.ProductId)}
            GROUP BY oi.{nameof(OrderItem.OrderId)}, oi.{nameof(OrderItem.ProductId)}, oi.{nameof(OrderItem.SellerId)}
        ");
    }

    private static void _CustomerStats(MigrationBuilder migrationBuilder) {
        var cancelledStatus = (int)OrderStatus.Cancelled;
        var returnedStatus = (int)OrderStatus.Returned;

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Order)}] WITH SCHEMABINDING AS
            SELECT 
                c.Id as {nameof(CustomerStats.CustomerId)},
                COUNT_BIG(*) as {nameof(CustomerStats.TotalOrders)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] c
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(Order)}] o ON c.Id = o.{nameof(Order.UserId)}
            WHERE c.Role = {(int)User.UserRole.Customer} AND o.{nameof(Order.Status)} != {cancelledStatus} AND o.{nameof(Order.Status)} != {returnedStatus}
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
            WHERE c.Role = {(int)User.UserRole.Customer} AND o.{nameof(Order.Status)} != {cancelledStatus} AND o.{nameof(Order.Status)} != {returnedStatus}
            GROUP BY c.Id
        ");
        // migrationBuilder.CreateIndex(
        //     $"IX_{nameof(CustomerStats)}_{nameof(Coupon)}",
        //     $"{nameof(CustomerStats)}_{nameof(Coupon)}",
        //     nameof(CustomerStats.CustomerId), DefaultDbContext.DefaultSchema, true);

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}] WITH SCHEMABINDING AS
            SELECT 
                c.Id as {nameof(CustomerStats.CustomerId)},
                COALESCE(SUM(cso.{nameof(CustomerStats.TotalOrders)}), 0) AS {nameof(CustomerStats.TotalOrders)},
                COALESCE(SUM(cspr.{nameof(CustomerStats.TotalReviews)}), 0) AS {nameof(CustomerStats.TotalReviews)},
                COALESCE(SUM(csrc.{nameof(CustomerStats.TotalComments)}), 0) AS {nameof(CustomerStats.TotalComments)},
                COALESCE(SUM(csrvpr.{nameof(CustomerStats.ReviewVotes)}), 0) AS {nameof(CustomerStats.ReviewVotes)},
                COALESCE(SUM(csrvrc.{nameof(CustomerStats.CommentVotes)}), 0) AS {nameof(CustomerStats.CommentVotes)},
                COALESCE(SUM(csc.{nameof(CustomerStats.TotalSpent)}), 0.0) AS {nameof(CustomerStats.TotalSpent)},
                COALESCE(SUM(csc.{nameof(CustomerStats.TotalDiscountUsed)}), 0.0) AS {nameof(CustomerStats.TotalDiscountUsed)}
                (COALESCE(csrvpr.{nameof(CustomerStats.ReviewVotes)}, 0) + COALESCE(csrvrc.{nameof(CustomerStats.CommentVotes)}, 0)) as {nameof(CustomerStats.TotalKarma)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] c
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Order)}] cso ON c.Id = cso.{nameof(CustomerStats.CustomerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ProductReview)}] cspr ON c.Id = cspr.{nameof(CustomerStats.CustomerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewComment)}] csrc ON c.Id = csrc.{nameof(CustomerStats.CustomerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}] csrvpr ON c.Id = csrvpr.{nameof(CustomerStats.CustomerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}] csrvrc ON c.Id = csrvrc.{nameof(CustomerStats.CustomerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Coupon)}] csc ON c.Id = csc.{nameof(CustomerStats.CustomerId)}
            WHERE c.Role = {(int)User.UserRole.Customer}                
            GROUP BY c.{nameof(User.Id)}
        ");
    }

    private static void _ReviewComment(MigrationBuilder migrationBuilder) {
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
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}] WITH SCHEMABINDING AS
            SELECT
                r.{nameof(ReviewComment.Id)} AS {nameof(ReviewCommentStats.CommentId)},
                rc.{nameof(ReviewCommentStats.ReplyCount)} ,
                rv.{nameof(ReviewCommentStats.VoteCount)} ,
                rv.{nameof(ReviewCommentStats.Votes)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewComment)}] r
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(ReviewComment)}] rc ON rc.{nameof(ReviewCommentStats.CommentId)} = r.{nameof(ReviewComment.Id)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}_{nameof(ReviewVote)}] rv ON rv.{nameof(ReviewCommentStats.CommentId)} = r.{nameof(ReviewComment.Id)}
            GROUP BY r.Id, rc.{nameof(ReviewCommentStats.ReplyCount)},
                rv.{nameof(ReviewCommentStats.VoteCount)} ,
                rv.{nameof(ReviewCommentStats.Votes)} 
        ");
    }

    private static void _Review(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewComment)}] WITH SCHEMABINDING AS
            SELECT
                r.Id AS {nameof(ReviewStats.ReviewId)},
                COUNT_BIG(*) as {nameof(ReviewStats.CommentCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] r
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewComment)}] rc ON r.{nameof(ProductReview.Id)} = rc.{nameof(ReviewComment.ReviewId)}
            WHERE rc.{nameof(ReviewComment.ParentId)} IS NULL
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
                COUNT_BIG(*) AS {nameof(ReviewStats.VoteCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] r
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewVote)}] rv ON r.{nameof(ProductReview.Id)} = rv.{nameof(ReviewVote.ReviewId)}
            GROUP BY r.Id
        ");
        migrationBuilder.CreateIndex($"IX_{nameof(ReviewStats)}_{nameof(ReviewVote)}",
                $"{nameof(ReviewStats)}_{nameof(ReviewVote)}", nameof(ReviewStats.ReviewId),
                DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}] WITH SCHEMABINDING AS
            SELECT
                r.{nameof(ProductReview.Id)} AS {nameof(ReviewStats.ReviewId)},
                rc.{nameof(ReviewStats.CommentCount)} AS  {nameof(ReviewStats.CommentCount)},
                rv.{nameof(ReviewStats.VoteCount)} AS  {nameof(ReviewStats.VoteCount)},
                rv.{nameof(ReviewStats.Votes)} AS {nameof(ReviewStats.Votes)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(ProductReview)}] r
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewComment)}] rc ON rc.{nameof(ReviewStats.ReviewId)} = r.{nameof(ProductReview.Id)} 
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}_{nameof(ReviewVote)}] rv ON rv.{nameof(ReviewStats.ReviewId)} = r.{nameof(ProductReview.Id)}
            GROUP BY r.Id, rc.{nameof(ReviewStats.CommentCount)}, rv.{nameof(ReviewStats.VoteCount)}, rv.{nameof(ReviewStats.Votes)}
        ");
    }

    private static void _SellerStats(MigrationBuilder migrationBuilder) {
        var badStatuses = OrderStatus.Cancelled | OrderStatus.CancelledBySeller | OrderStatus.ReturnApproved |
                          OrderStatus.Returned;
        var intermediateStatuses = OrderStatus.CancellationRequested | OrderStatus.Shipped |
                                   OrderStatus.WaitingShipment | OrderStatus.ReturnRequested;
        var goodStatuses = OrderStatus.Delivered | OrderStatus.Complete;
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
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItem)}] oi ON s.Id = oi.{nameof(OrderItem.SellerId)} AND oi.{nameof(OrderItem.Status)} & {(int) badStatuses}=0
            WHERE  s.Role = {(int)User.UserRole.Seller}
            GROUP BY s.Id
        ");
        migrationBuilder.CreateIndex(
                $"IX_{nameof(SellerStats)}_{nameof(OrderItem)}",
                $"{nameof(SellerStats)}_{nameof(OrderItem)}",
                nameof(SellerStats.SellerId), DefaultDbContext.DefaultSchema, true)
            .Annotation(SqlServerAnnotationNames.Clustered, true);

        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerOrderStats)}_{nameof(SellerOrderStats.TotalComplete)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                COUNT_BIG(*) as {nameof(SellerOrderStats.CountComplete)},
                SUM(oia.{nameof(OrderItemAggregates.CouponDiscountedPrice)}) as {nameof(SellerOrderStats.TotalComplete)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] oia ON oia.{nameof(OrderItemAggregates.SellerId)} = s.{nameof(Seller.Id)}
            WHERE s.Role={(int)User.UserRole.Seller} AND oia.{nameof(OrderItem.Status)} & {(int) goodStatuses} != 0
            GROUP BY s.Id
        ");
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerOrderStats)}_{nameof(SellerOrderStats.TotalCanceled)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                COUNT_BIG(*) as {nameof(SellerOrderStats.CountCanceled)},
                SUM(oia.{nameof(OrderItemAggregates.CouponDiscountedPrice)}) as {nameof(SellerOrderStats.TotalCanceled)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] oia ON oia.{nameof(OrderItemAggregates.SellerId)} = s.{nameof(Seller.Id)}
            WHERE s.Role={(int)User.UserRole.Seller} AND oia.{nameof(OrderItem.Status)} & {(int) badStatuses} != 0
            GROUP BY s.Id
        ");
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerOrderStats)}_{nameof(SellerOrderStats.TotalInProgress)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                COUNT_BIG(*) as {nameof(SellerOrderStats.CountInProgress)},
                SUM(oia.{nameof(OrderItemAggregates.CouponDiscountedPrice)}) as {nameof(SellerOrderStats.TotalInProgress)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            INNER JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}] oia ON oia.{nameof(OrderItemAggregates.SellerId)} = s.{nameof(Seller.Id)}
            WHERE s.Role={(int)User.UserRole.Seller} AND oia.{nameof(OrderItem.Status)} & {(int) intermediateStatuses} != 0
            GROUP BY s.Id
        ");
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
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}Average] WITH SCHEMABINDING AS
            SELECT 
                spspr.{nameof(SellerStats.SellerId)},
                CAST(spspr.{nameof(SellerStats.RatingTotal)} AS DECIMAL(10, 2)) / spspr.{nameof(SellerStats.ReviewCount)} AS {nameof(SellerStats.ReviewAverage)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}] spspr
            WHERE spspr.{nameof(SellerStats.ReviewCount)} > 0
        ");
      
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerStats.SellerId)},
                COALESCE(SUM(spo.{nameof(SellerStats.OfferCount)}),0) as {nameof(SellerStats.OfferCount)},
                COALESCE(SUM(spr.{nameof(SellerStats.ReviewCount)}),0) as {nameof(SellerStats.ReviewCount)},
                COALESCE(SUM(spra.{nameof(SellerStats.ReviewAverage)}),0.0) as {nameof(SellerStats.ReviewAverage)},
                COALESCE(SUM(spr.{nameof(SellerStats.RatingTotal)}),0.0) as {nameof(SellerStats.RatingTotal)},
                COALESCE(SUM(sosc.{nameof(SellerOrderStats.CountComplete)}),0) + COALESCE(SUM(sosi.{nameof(SellerOrderStats.CountInProgress)}),0.0) as {nameof(SellerStats.SaleCount)},
                COALESCE(SUM(sosc.{nameof(SellerOrderStats.TotalComplete)}),0.0) + COALESCE(SUM(sosi.{nameof(SellerOrderStats.TotalInProgress)}),0.0) as {nameof(SellerStats.TotalSold)},
                COALESCE(SUM(srr.{nameof(SellerStats.RefundCount)}),0) as {nameof(SellerStats.RefundCount)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductOffer)}] spo ON s.Id = spo.{nameof(SellerStats.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}] spr ON s.Id = spr.{nameof(SellerStats.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(SellerOrderStats)}_{nameof(SellerOrderStats.TotalComplete)}] sosc ON s.Id = sosc.{nameof(SellerOrderStats.SellerId)} 
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(SellerOrderStats)}_{nameof(SellerOrderStats.TotalInProgress)}] sosi ON s.Id = sosi.{nameof(SellerOrderStats.SellerId)} 
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}Average] spra ON s.Id = spra.{nameof(SellerStats.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(RefundRequest)}] srr ON s.Id = srr.{nameof(SellerStats.SellerId)}
            WHERE s.Role = {(int)User.UserRole.Seller}
            GROUP BY s.{nameof(User.Id)}
        ");
        migrationBuilder.Sql($@"
            CREATE VIEW [{DefaultDbContext.DefaultSchema}].[{nameof(SellerOrderStats)}] WITH SCHEMABINDING AS
            SELECT 
                s.Id as {nameof(SellerOrderStats.SellerId)},
                COALESCE(SUM(soc.{nameof(SellerOrderStats.TotalComplete)}), 0.0) as {nameof(SellerOrderStats.TotalComplete)},
                COALESCE(SUM(soc.{nameof(SellerOrderStats.CountComplete)}), 0) as {nameof(SellerOrderStats.CountComplete)},
                COALESCE(SUM(soca.{nameof(SellerOrderStats.TotalCanceled)}),0.0) as {nameof(SellerOrderStats.TotalCanceled)},
                COALESCE(SUM(soca.{nameof(SellerOrderStats.CountCanceled)}),0) as {nameof(SellerOrderStats.CountCanceled)},
                COALESCE(SUM(soci.{nameof(SellerOrderStats.TotalInProgress)}),0.0) as {nameof(SellerOrderStats.TotalInProgress)},
                COALESCE(SUM(soci.{nameof(SellerOrderStats.CountInProgress)}),0) as {nameof(SellerOrderStats.CountInProgress)}
            FROM [{DefaultDbContext.DefaultSchema}].[{nameof(User)}] s
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(SellerOrderStats)}_{nameof(SellerOrderStats.TotalComplete)}] soc ON s.Id = soc.{nameof(SellerOrderStats.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(SellerOrderStats)}_{nameof(SellerOrderStats.TotalCanceled)}] soca ON s.Id = soca.{nameof(SellerOrderStats.SellerId)}
            LEFT JOIN [{DefaultDbContext.DefaultSchema}].[{nameof(SellerOrderStats)}_{nameof(SellerOrderStats.TotalInProgress)}] soci ON s.Id = soci.{nameof(SellerOrderStats.SellerId)}
            WHERE s.Role ={(int)User.UserRole.Seller}
            GROUP BY s.{nameof(User.Id)}
        ");
        
    }

    public static void Down(MigrationBuilder migrationBuilder)
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
            DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}]
        ");
        migrationBuilder.Sql($@"
            DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ProductStats)}_{nameof(ProductReview)}Average]
        ");
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
        dropProductRatingStatsView(nameof(ProductRatingStats.FiveStarCount));
        dropProductRatingStatsView(nameof(ProductRatingStats.FourStarCount));
        dropProductRatingStatsView(nameof(ProductRatingStats.ThreeStarCount));
        dropProductRatingStatsView(nameof(ProductRatingStats.TwoStarCount));
        dropProductRatingStatsView(nameof(ProductRatingStats.OneStarCount));
        dropProductRatingStatsView(nameof(ProductRatingStats.ZeroStarCount));
        void dropProductRatingStatsView(string name) {
            migrationBuilder.DropIndex($"IX_{nameof(ProductRatingStats)}_{name}",
                $"{nameof(ProductRatingStats)}_{name}", DefaultDbContext.DefaultSchema);
            migrationBuilder.Sql($@"
                DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ProductRatingStats)}_{name}]
            ");
        }
        // Order Aggregates
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(OrderAggregates)}",
            schema: DefaultDbContext.DefaultSchema,
            table: nameof(OrderAggregates));

        
        // OrderItem Aggregates
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(OrderItemAggregates)}",
            schema: DefaultDbContext.DefaultSchema,
            table: nameof(OrderItemAggregates));
        
        migrationBuilder.DropIndex(
            $"IX_{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            $"{nameof(OrderItemAggregates)}_{nameof(Coupon)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}]");
        
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
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(SellerStats)}_{nameof(RefundRequest)}",
            schema: DefaultDbContext.DefaultSchema,
            table: $"{nameof(SellerStats)}_{nameof(RefundRequest)}");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}Average]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductOffer)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(OrderItem)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(RefundRequest)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(SellerStats)}_{nameof(Coupon)}]");
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
            $"{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}",
            $"{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Order)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ReviewComment)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(ReviewVote)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CustomerStats)}_{nameof(Coupon)}]");
        // Review Stats
        // migrationBuilder.DropIndex(
        //     $"IX_{nameof(ReviewStats)}_{nameof(ReviewComment)}",
        //     $"{nameof(ReviewStats)}_{nameof(ReviewComment)}",
        //     DefaultDbContext.DefaultSchema);
        migrationBuilder.DropIndex(
            $"IX_{nameof(ReviewStats)}_{nameof(ReviewVote)}",
            $"{nameof(ReviewStats)}_{nameof(ReviewVote)}",
            DefaultDbContext.DefaultSchema);
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewStats)}]");
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
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(ReviewCommentStats)}]");
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
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(ProductReview)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OfferStats)}_{nameof(RefundRequest)}]");
        // Cart Aggregates
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CartAggregates)}",
            schema: DefaultDbContext.DefaultSchema,
            table: nameof(CartAggregates));

        // CartItem Aggregates
        migrationBuilder.DropIndex(
            name: $"IX_{nameof(CartItemAggregates)}",
            schema: DefaultDbContext.DefaultSchema,
            table: nameof(CartItemAggregates));

        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}_{nameof(CartAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}_{nameof(Coupon)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(Coupon)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(CartItemAggregates)}_{nameof(ProductOffer)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}_{nameof(OrderAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}_{nameof(Coupon)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderAggregates)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}_{nameof(Coupon)}]");
        migrationBuilder.Sql($"DROP VIEW IF EXISTS [{DefaultDbContext.DefaultSchema}].[{nameof(OrderItemAggregates)}]");
    }
}

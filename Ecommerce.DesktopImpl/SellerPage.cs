using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.DesktopImpl
{
    public partial class SellerPage : UserControl, IPage
    {
        public static SellerPage Instance { get; private set; }
        private readonly Navigation _navigation;
        private readonly ProductPage _productPage;
        private readonly ReviewPage _reviewPage;
        private readonly ISellerManager _sellerManager;
        private static readonly string[] offersInclude = [
            string.Join('_', nameof(ProductOffer.Product), nameof(Product.Name)),
            string.Join('_', nameof(ProductOffer.Product), nameof(Product.Category), nameof(Category.Name))
        ];
        private static readonly string[] reviewsExclude = [
            string.Join('_', nameof(ProductReview.CensorName)),
            string.Join('_', nameof(ProductReview.HasBought)),
            string.Join('_', nameof(ReviewWithAggregates.OwnVote))
        ];

        private static readonly string[] coupınsExclude = [
            string.Join('_', nameof(Coupon.SellerId)),
            string.Join('_', nameof(Coupon.Seller))
        ];

        private static readonly string[] aggreagateBoxInclude =
        [
            string.Join('_', nameof(SellerWithAggregates.OfferCount)),
            string.Join('_', nameof(SellerWithAggregates.ReviewAverage)),
            string.Join('_', nameof(SellerWithAggregates.ReviewCount)),
            string.Join('_', nameof(SellerWithAggregates.SaleCount))
        ];
        private static readonly string[] offerExclude = [];
        private static readonly string[] reviewsInclude = [string.Join('_', nameof(ProductReview.Reviewer), nameof(User.FirstName), string.Join('_', nameof(ProductReview.Reviewer), nameof(User.LastName)))];
        private readonly IReviewManager _reviewManager;
        private uint _loaded;
        public SellerPage(Navigation navigation, ReviewPage reviewPage, ProductPage productPage, IReviewManager reviewManager, ISellerManager sellerManager)
        {
            _sellerManager = sellerManager;
            _reviewPage = reviewPage;
            _reviewManager = reviewManager;
            _productPage = productPage;
            _navigation = navigation;
            InitializeComponent();
            foreach (var columnName in Utils.ColumnNames(typeof(ProductOffer), offerExclude, offersInclude))
            {
                OffersView.Columns.Add(columnName, columnName);
            }

            foreach (var columnName in Utils.ColumnNames(typeof(ReviewWithAggregates), reviewsExclude, reviewsInclude))
            {
                reviewsView.Columns.Add(columnName, columnName);
            }

            foreach (var columnName in Utils.ColumnNames(typeof(Coupon), coupınsExclude, [nameof(Coupon.Id)])){
                couponsView.Columns.Add(columnName, columnName);
            }
            Instance = this;
        }

        public new void Load(uint id)
        {
            _loaded = id;
        }

        private int _reviewsPage = 1;
        private int _offersPage = 1;

        private void LoadCoupons(Seller seller)
        {
            foreach (var sellerCoupon in seller.Coupons)
            {
                var i = couponsView.Rows.Add();
                foreach (var  valueTuple in Utils.ToPairs(sellerCoupon, coupınsExclude,[nameof(Coupon.Id)]))
                {
                    couponsView.Rows[i].Cells[valueTuple.Item1].Value = valueTuple.Item2;
                }

                couponsView.Rows[i].Tag = sellerCoupon;
            }
        }
        private void LoadOffers()
        {
            var offers = _sellerManager.GetOffers(_loaded, page: _offersPage, pageSize: 20);
            foreach (var productOffer in offers)
            {
                var i = OffersView.Rows.Add();
                foreach (var valueTuple in Utils.ToPairs(productOffer, offerExclude, offersInclude))
                {
                    OffersView.Rows[i].Cells[valueTuple.Item1].Value = valueTuple.Item2;
                }
                OffersView.Rows[i].Tag = productOffer;
            }

        }

        private void GoToProduct(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (OffersView.Rows[e.RowIndex].Tag is not ProductOffer offer) return;
            _productPage.LoadProduct(offer.ProductId);
            _navigation.Go(this, _productPage);
        }
        private void Clear()
        {
            reviewsView.Rows.Clear();
            OffersView.Rows.Clear();
            couponsView.Rows.Clear();
            shopNameBox.Clear();
            addressBox1.Clear();
        }
        public void Go()
        {
            Clear();
            var seller = _sellerManager.GetSellerWithAggregates(_loaded, false, false, true, offersPage: _offersPage, offersPageSize: 20)!;
            LoadSeller(seller);
            LoadOffers();
            LoadReviews();
            LoadCoupons(seller);
        }

        private void LoadSeller(Seller seller)
        {
            shopNameBox.Text = seller.ShopName;
            addressBox1.Lines = [seller.ShopAddress.ToString(), " Telefon: " + seller.ShopPhoneNumber];
            foreach (var aggregates in Utils.ToPairs(seller, [], aggreagateBoxInclude))   
            {
                if (aggreagateBoxInclude.Contains(aggregates.Item1)) {
                    aggregateBox.Items.Add(aggregates.Item1 + ": " + aggregates.Item2);
                }
            }
        }
        private void LoadReviews()
        {
            var revs = _reviewManager.GetReviewsWithAggregates(false, sellerId: _loaded, page: _reviewsPage);
            foreach (var rev in revs)
            {
                var i = reviewsView.Rows.Add();
                foreach (var valueTuple in Utils.ToPairs(rev, reviewsExclude, reviewsInclude))
                {
                    reviewsView.Rows[i].Cells[valueTuple.Item1].Value = valueTuple.Item2;
                }
                reviewsView.Rows[i].Tag = rev;
            }
        }

        private void offersPrevBtn_Click(object sender, EventArgs e)
        {
            if (_offersPage > 1) _offersPage--;
            ClearOffers();
            LoadOffers();
        }

        private void reviewsNextBtn_Click(object sender, EventArgs e)
        {
            _reviewsPage++;
            ClearReviews();
            LoadReviews();
        }

        private void ClearReviews()
        {
            reviewsView.Rows.Clear();
        }
        private void reviewsPrevBtn_Click(object sender, EventArgs e)
        {
            if (_reviewsPage > 1)
                --_reviewsPage;
            ClearReviews();
            LoadReviews();
        }

        private void ClearOffers()
        {
            OffersView.Rows.Clear();
        }
        private void offersNextBtn_Click(object sender, EventArgs e)
        {
            _offersPage++;
            ClearOffers();
            LoadOffers();
        }

        private void couponsView_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (couponsView.Columns[e.ColumnIndex].Name.Equals(nameof(Coupon.Id))){
                Clipboard.SetText(couponsView.Rows[e.RowIndex].Cells[nameof(Coupon.Id)].Value.ToString());
            }
        }
    }
}

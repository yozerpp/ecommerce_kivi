using Ecommerce.Bl;
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

        private void LoadCoupons(ICollection<Coupon >coupons)
        {
            foreach (var sellerCoupon in coupons)
            {
                var i = couponsView.Rows.Add();
                foreach (var  valueTuple in Utils.ToPairs(sellerCoupon, coupınsExclude,[nameof(Coupon.Id)]))
                {
                    couponsView.Rows[i].Cells[valueTuple.Item1].Value = valueTuple.Item2;
                }

                couponsView.Rows[i].Tag = sellerCoupon;
            }
        }

        private List<ProductOffer> GetOffers() {
            return _sellerManager.GetOffers(_loaded, page: _offersPage, pageSize: 20);
        }

        private void LoadOffers(List<ProductOffer> offers)
        {
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
            aggregateBox.Items.Clear();
            addressBox1.Clear();
        }
        public void Go()
        {
            Clear();
            addCpnBtn.Visible =addCpnBtn.Enabled = listProductBtn.Visible = listProductBtn.Enabled= ContextHolder.Session?.User is Seller;
            var offersTask = Task.Run(GetOffers);
            var reviewsTask = offersTask.ContinueWith(_=>GetReviews());
            var couponsTask = reviewsTask.ContinueWith(_=>GetCoupons());
            var loadTask = couponsTask.ContinueWith(_=>GetSeller());
            loadTask.ContinueWith(r => Invoke(()=>LoadSeller(r.Result)) );
            offersTask.ContinueWith(o => Invoke(() => LoadOffers(o.Result)));
            couponsTask.ContinueWith(c => Invoke(() => LoadCoupons(c.Result)));
            reviewsTask.ContinueWith(r=> Invoke(() => LoadReviews(r.Result)));
            Task.WaitAll(offersTask, reviewsTask, loadTask);
        }

        private ICollection<Coupon> GetCoupons() {
            return _sellerManager.GetSeller(_loaded, false, false, true).Coupons;
        }

        private SellerWithAggregates GetSeller() {
            return _sellerManager.GetSellerWithAggregates(_loaded, false, false, true,
                offersPage: _offersPage, offersPageSize: 20)!;
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

        private List<ReviewWithAggregates> GetReviews() {
            return _reviewManager.GetReviewsWithAggregates(false, sellerId: _loaded, page: _reviewsPage);
        }
        private void LoadReviews(List<ReviewWithAggregates> reviews)
        {
            foreach (var rev in reviews)
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
            Task.Run(GetOffers).ContinueWith((r) => Invoke(() => LoadOffers(r.Result)));
        }

        private void reviewsNextBtn_Click(object sender, EventArgs e)
        {
            _reviewsPage++;
            ClearReviews();
            Task.Run(GetReviews).ContinueWith(r => Invoke(() => LoadReviews(r.Result)));
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
            Task.Run(GetReviews).ContinueWith(r=> Invoke(() => LoadReviews(r.Result)));
        }

        private void ClearOffers()
        {
            OffersView.Rows.Clear();
        }
        private void offersNextBtn_Click(object sender, EventArgs e)
        {
            _offersPage++;
            ClearOffers();
            Task.Run(GetOffers).ContinueWith(r => Invoke(() => LoadOffers(r.Result)));
        }

        private void couponsView_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (couponsView.Columns[e.ColumnIndex].Name.Equals(nameof(Coupon.Id))){
                Clipboard.SetText(couponsView.Rows[e.RowIndex].Cells[nameof(Coupon.Id)].Value.ToString());
            }
        }

        private void listProductBtn_Click(object sender, EventArgs e) {
            var p =(Product)Utils.GetInput(typeof(Product), "Ürün tanımla");
            if(p==null )return;
            var offer = (ProductOffer)Utils.GetInput(typeof(ProductOffer), "Stok ve Fiyat bilgileri");
            if (offer == null) return;
            offer.Product = p;
            _sellerManager.ListProduct(offer);
            Utils.Info("Ürün başarıyla oluşturuldu.");
            Go();
        }

        private void addCpnBtn_Click(object sender, EventArgs e) {
            var cpn = (Coupon)Utils.GetInput(typeof(Coupon), "Kupon Gir.", ["Id"]);
            if (cpn == null) return;
            _sellerManager.CreateCoupon(cpn);
            Utils.Info("Kupon başarıyla oluşturuldu.");
            Go();
        }

        private void OffersView_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (OffersView.Columns[e.ColumnIndex].Name.Contains("Product")){
                var product = (ProductOffer)OffersView.Rows[e.RowIndex].Tag;
                _productPage.LoadProduct(product.Product?.Id??product.ProductId);
                _navigation.Go(this, _productPage);
            }
        }

        private void couponsView_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (couponsView.Columns[e.ColumnIndex].Name.Equals(nameof(Coupon.Id))){
                Clipboard.SetText(couponsView.Rows[e.RowIndex].Cells[nameof(Coupon.Id)].Value.ToString());
            }
        }
    }
}

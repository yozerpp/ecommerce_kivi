using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.DesktopImpl
{

    public partial class ProductPage : UserControl, IPage
    {
        private readonly IProductManager _manager;
        private readonly ICartManager _cartManager;
        private readonly IReviewManager _reviewManager;
        private readonly Navigation _navigation;
        private readonly ISellerManager _sellerManager;
        private uint _reviewPage = 1;
        private static readonly string[] offerExcldues =[];
        private static readonly string[] offerIncludes = [string.Join('_', nameof(ProductOffer.Seller), nameof(Seller.ShopName))];
        private static readonly string[] reviewExcludes = [nameof(ProductReview.CensorName), nameof(ReviewWithAggregates.OwnVote)];
        private static readonly string[] reviewIncludes =[string.Join('_', nameof(ProductReview.Reviewer), nameof(Customer.FirstName)),
            string.Join('_', nameof(ProductReview.Reviewer), nameof(Customer.LastName)),
            string.Join('_', nameof(ProductReview.Offer) , nameof(ProductOffer.Seller), nameof(Seller.ShopName))
        ];
        private static readonly string[] productExcludes =
            [nameof(Product.Name), nameof(Product.Description), nameof(Product.Image)];
        private static readonly string[] productIncludes =[];
        public ProductPage(ICartManager cartManager, ISellerManager sellerManager, IProductManager productManager, IReviewManager reviewManager, Navigation navigation) {
            _reviewManager = reviewManager;
            _sellerManager = sellerManager;
            _manager = productManager;
            _cartManager = cartManager;
            _navigation = navigation;
            InitializeComponent();
            title1.TextAlign = HorizontalAlignment.Center;
            textBox1.TextAlign = HorizontalAlignment.Left;
            foreach (var columnName in Utils.ColumnNames(typeof(ProductOffer), offerExcldues,offerIncludes))
            {
                offersView.Columns.Add(columnName, columnName);
            }
            foreach (var columnName in Utils.ColumnNames(typeof(ReviewWithAggregates), reviewExcludes, reviewIncludes))
            {
                reviewView.Columns.Add(columnName, columnName);
            }
        }
        private uint? _loaded;
        private void clear()
        {
            _reviewPage = 1;
            offersView.Rows.Clear();
            reviewView.Rows.Clear();
            listBox1.Items.Clear();
        }
        
        public void LoadProduct(uint id) {
            _loaded = id;

        }

        private ProductWithAggregates? _product;
        private void doLoad() {
            if(_loaded==null) return;
            var product =Task.Run(GetProduct);
            var revTask = product.ContinueWith(_=>GetReviewsWithAggregates());
            revTask.ContinueWith(r=>Invoke(()=>LoadReviews(r.Result)));
            product.ContinueWith(p=>Invoke(() => LoadProductInfo(_product = p.Result!)));
            product.ContinueWith(p=>Invoke(()=> LoadOffers(p.Result!)));
            Task.WaitAll(product, revTask);
        }

        private ProductWithAggregates? GetProduct() {
            return _manager.GetByIdWithAggregates((uint)_loaded!);
        }

        private void LoadProductInfo(ProductWithAggregates product) {
            if (product.Image != null)
            {
                pictureBox1.Image = Image.FromStream(new MemoryStream(Convert.FromBase64String(product.Image)));
            }
            foreach (var col in Utils.ToPairs(product, productExcludes, productIncludes))
            {
                listBox1.Items.Add(col.Item1 + ": " + col.Item2);
            }
            title1.Text = product.Name;
            textBox1.Text = product.Description;
        }

        private void LoadOffers(ProductWithAggregates product) {
            foreach (var productOffer in product.Offers){
                var i = offersView.Rows.Add();
                
                foreach (var valueTuple in Utils.ToPairs(productOffer, offerExcldues ,offerIncludes)){
                    offersView.Rows[i].Cells[valueTuple.Item1].Value = valueTuple.Item2;
                }
                offersView.Rows[i].Tag = (productOffer, productOffer.Seller);
            }
        }
        private void reviewView_RowHeaderMouseDoubleClick(object s, DataGridViewCellMouseEventArgs e)
        {
            ReviewPage.Instance.LoadReviews(_loaded.Value);
            _navigation.Go(this,ReviewPage.Instance);
        }
        private void addToCartBtn_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in offersView.SelectedRows)
            {
                var (offer, _) = ((ProductOffer, Seller))row.Tag;
                var quantity = Convert.ToUInt32(quantityBox.Text);
                if (quantity > 0){
                    _cartManager.Add(offer, quantity);
                    Utils.Info($"Sepetinize {offer.Seller.ShopName} satıcısından {quantity} öğe eklendi.");                    
                }else Utils.Error("Quantity must be greater than 0.");
            }
            
        }

        private void Rate(decimal rating, string comment, ProductOffer offer,bool censorName)
        {
            try
            {
                _reviewManager.LeaveReview(new ProductReview
                {
                    Comment = comment,
                    Rating = rating,
                    ProductId = offer.ProductId,
                    SellerId = offer.SellerId,
                    CensorName = censorName
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utils.Error(e.Message);
            }
        }
        private void commentBtn_Click(object sender, EventArgs e)
        {
            if (offersView.SelectedRows.Count == 0)
            { //update
                if (reviewView.SelectedRows.Count == 0)
                {
                    Utils.Error("Yorum bırakmak istediğiniz teklifi, güncellemek istediğniz yorum veya cevaplamak istediğniz yorumu seçmelisiniz.");
                    return;
                }

                var u = ContextHolder.GetUserOrThrow();
                var rev = (ProductReview)reviewView.SelectedRows[0].Tag;
                if(rev!.Reviewer.Equals(u)|| rev.ReviewerId.Equals(u.Id)){
                rev.Comment = commentBox.Text;
                rev.Rating = Convert.ToDecimal(ratingBox.Text);
                    _reviewManager.UpdateReview(rev);
                }else{
                    _reviewManager.CommentReview(new ReviewComment(){
                        SessionId = rev.ReviewerId, CommenterId = ContextHolder.Session.Id, ProductId = rev.ProductId,
                        SellerId = rev.SellerId,
                        Comment = commentBox.Text,
                    });
                }
            }
            else{
                var comment = commentBox.Text;
                var rating = Convert.ToDecimal(ratingBox.Text);
                var (offer,_) = ((ProductOffer, Seller))offersView.SelectedRows[0].Tag;
                //var review = (ProductReview)reviewView.SelectedRows[0].Tag;
                Rate(rating, comment, offer, censorNameBtn.Checked);
            }
            Task.Run(GetReviewsWithAggregates).ContinueWith(r=>Invoke(()=>LoadReviews(r.Result)));
        }
        public void Go()
        {
            clear();
            addOfferBtn.Visible = addOfferBtn.Enabled = ContextHolder.Session?.User is Seller;
            doLoad();
        }
        private void LoadReviews(List<ReviewWithAggregates> reviews)
        {
            reviewView.Rows.Clear();
            foreach (var productReview in reviews){
                var i = reviewView.Rows.Add();
                foreach (var review in Utils.ToPairs(productReview, reviewExcludes , reviewIncludes)){
                    object v;
                    if ((review.Item1.Equals(string.Join('_', nameof(ProductReview.Reviewer), nameof(Customer.FirstName))) ||
                         review.Item1.Equals(string.Join('_', nameof(ProductReview.Reviewer), nameof(Customer.LastName)))) &&
                        productReview.CensorName)
                        v = review.Item2.ToString()![0] + "***";
                    else v = review.Item2;
                    reviewView.Rows[i].Cells[review.Item1].Value = v;
                }
                reviewView.Rows[i].Tag = productReview;
            }
        }

        private List<ReviewWithAggregates> GetReviewsWithAggregates() {
            return _reviewManager.GetReviewsWithAggregates( false,true,_loaded);
        }

        private void pageBack_Click(object sender, EventArgs e)
        {
            if (_reviewPage <= 1) return;
            --_reviewPage;
            Task.Run(GetReviewsWithAggregates).ContinueWith(r=>Invoke(() => LoadReviews(r.Result)));
        }
        private void pageNext_Click(object sender, EventArgs e)
        {
            ++_reviewPage;
            Task.Run(GetReviewsWithAggregates).ContinueWith(r=> Invoke(()=>LoadReviews(r.Result)));
        }

        private void reviewsView_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (reviewView.Columns[e.ColumnIndex].Name.Contains("Seller")){
                var sellerId = ((ProductReview)reviewView.Rows[e.RowIndex].Tag).Offer.Seller.Id;
                SellerPage.Instance.Load(sellerId);
                _navigation.Go(this, SellerPage.Instance);
            } 
            // else if (reviewView.Columns[e.ColumnIndex].Name.Contains("Reviewer")){
                // var uid = ((ProductReview)reviewView.Rows[e.RowIndex].Tag).ReviewerId;
                // UserPage.Instance.Load(uid);
                // _navigation.Go(this, UserPage.Instance);
            // }
        }
        private void offersView_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (offersView.Columns[e.ColumnIndex].Name.Equals(string.Join('_', nameof(ProductOffer.Seller), nameof(Seller.ShopName)))){
                var (_, seller) = ((ProductOffer, Seller))offersView.Rows[e.RowIndex].Tag;
                SellerPage.Instance.Load(seller.Id);
                _navigation.Go(this, SellerPage.Instance);
            }
        }

        private void addOfferBtn_Click(object sender, EventArgs e) {
            var offer = (ProductOffer)Utils.GetInput(typeof(ProductOffer), "Ürün İlanı Ver");
            offer.ProductId = _loaded.Value;
            _sellerManager.ListOffer(offer);
            Utils.Info("Ürün ilanınız başarıyla verildi.");
            Go();
        }
    }
}

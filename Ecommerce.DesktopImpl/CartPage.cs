using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.DesktopImpl
{
    public partial class CartPage : UserControl
    {
        private readonly ICartManager _cartManager;
        private readonly IOrderManager _orderManager;
        private CartWithAggregates _cart;
        private Navigation _navigation;
        private readonly SellerPage _sellerPage;
        private readonly ProductPage _productPage;
        private static readonly string CouponCodeColName = nameof(Coupon)  + "Code";
        private static readonly string[] itemsExcluded =[
            nameof(CartItem.CartId), nameof(CartItem.ProductId), nameof(CartItem.ProductId), nameof(CartItem.SellerId)
        ];
        private static readonly string[] itemsIncluded = [ string.Join('_', nameof(CartItem.ProductOffer), nameof(ProductOffer.Price)),
            string.Join('_', nameof(CartItem.ProductOffer), nameof(ProductOffer.Discount)),
            string.Join('_', nameof(CartItem.ProductOffer), nameof(ProductOffer.Product), nameof(Product.Name)),
            string.Join('_', nameof(CartItem.ProductOffer), nameof(ProductOffer.Seller), nameof(Seller.ShopName))
        ];
        public CartPage(ICartManager cartManager, ProductPage productPage, Navigation navigation, IOrderManager orderManager, SellerPage sellerPage)
        {
            this._cartManager = cartManager;
            _productPage = productPage;
            _sellerPage = sellerPage;
            _navigation = navigation;
            _orderManager = orderManager;
            InitializeComponent();
            cartView.Columns.Add(CouponCodeColName, CouponCodeColName);
            foreach (var columnName in Utils.ColumnNames(typeof(CartItemWithAggregates),itemsExcluded, itemsIncluded ))
            {
                cartView.Columns.Add(columnName, columnName);
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            Clear();
            Load();
        }

        public void Clear()
        {
            aggregateBpx.Clear();
            cartView.Rows.Clear();
        }
        public new void Load()
        {
            _cart = (CartWithAggregates)_cartManager.Get(true, true);
            foreach (var valueTuple in Utils.ToPairs(_cart, [nameof(Cart.SessionId), nameof(Cart.Id)], [])){
                var lineS = new List<string>(aggregateBpx.Lines){ valueTuple.Item1 + ": " + valueTuple.Item2 };
                aggregateBpx.Lines = lineS.ToArray();
            }
            foreach (var cartItem in _cart.Items)
            {
                var i = cartView.Rows.Add();
                foreach (var valueTuple in Utils.ToPairs(cartItem, itemsExcluded,itemsIncluded)){
                    cartView.Rows[i].Cells[valueTuple.Item1].Value = valueTuple.Item2;
                }
                cartView.Rows[i].Cells[CouponCodeColName].Value = cartItem.CouponId;
                cartView.Rows[i].Tag = (cartItem, cartItem.ProductOffer.Seller);
            }
        }
        private void deleteBtn_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow cartViewSelectedRow in cartView.SelectedRows)
            {
                var (item, _) = ((CartItemWithAggregates, Seller))cartViewSelectedRow.Tag;
                _cartManager.Remove(item);
            }
            Refresh();

        }

        private void addBtn_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow cartViewSelectedRow in cartView.SelectedRows)
            {
                var (item, _) = ((CartItemWithAggregates, Seller))cartViewSelectedRow.Tag;
                _cartManager.Add(item, 1);
            }
            Refresh();
        }

        private void decrementButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow cartViewSelectedRow in cartView.SelectedRows)
            {
                var (item, _) = ((CartItemWithAggregates, Seller))cartViewSelectedRow.Tag;

                _cartManager.Decrement(item);
            }           
            Refresh();

        }

        private void couponBtn_Click(object sender, EventArgs e)
        {
            var code = couponBox.Text;
            foreach (DataGridViewRow cartViewSelectedRow in cartView.SelectedRows)
            {

                var (item, _) = ((CartItemWithAggregates, Seller))cartViewSelectedRow.Tag;
                _cartManager.AddCoupon( item.ProductOffer,new Coupon { Id = code });
            }
            Refresh();

        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            _cartManager.newCart(flush:true);
            Refresh();
        }

        private void orderBtn_Click(object sender, EventArgs e)
        {
            var order = _orderManager.CreateOrder();
            Utils.Info("Sipariş Oluşturuldu.");
            Refresh();
        }

        private void cartView_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if ( cartView.Columns[e.ColumnIndex].Name.Equals(string.Join('_', nameof(CartItem.ProductOffer), nameof(ProductOffer.Seller), nameof(Seller.ShopName)))){
                var (_, seller) = ((CartItemWithAggregates, Seller))cartView.Rows[e.RowIndex].Tag;
                _sellerPage.Load(seller.Id);
                _navigation.Go(this,_sellerPage);
            } else if (cartView.Columns[e.ColumnIndex].Name.Equals(string.Join('_', nameof(CartItem.ProductOffer),
                           nameof(ProductOffer.Product), nameof(Product.Name)))){
                var (item,_) = ((CartItemWithAggregates, Seller))cartView.Rows[e.RowIndex].Tag;
                _productPage.LoadProduct(item.ProductId);
                _navigation.Go(this,_productPage);
            }
        }
    }
}

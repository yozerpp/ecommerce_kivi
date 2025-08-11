using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Projections;

namespace Ecommerce.DesktopImpl
{
public partial class UserPage : UserControl, IPage
{
    private readonly IOrderManager _orderManager;
    private readonly IUserManager _userManager;
    public static UserPage Instance { get; private set; }
    public uint? _loadedId = null;
    private int _ordersPage = 1;
    private bool editing = false;

    private static readonly string[] orderItemsIncludes = new string[]{
        string.Join('_', nameof(OrderItemWithAggregates.ProductOffer), nameof(ProductOffer.Product),
            nameof(Product.Name)),
        string.Join('_', nameof(OrderItemWithAggregates.ProductOffer), nameof(ProductOffer.Seller),
            nameof(Seller.ShopName)),
        string.Join('_', nameof(OrderItemWithAggregates.ProductOffer), nameof(ProductOffer.Product),
            nameof(Product.Category), nameof(Category.Name)),
        string.Join('_', nameof(OrderItemWithAggregates.ProductOffer), nameof(ProductOffer.Seller),
            nameof(Seller.Address)),
    };

    private readonly FlowLayoutPanel _infoContainer, _aggregatesContainer;
    private readonly Dictionary<string, TextBox> _infoBoxes = new(), _aggregatesBoxes = new();
    private readonly LoginPage _loginPage;
    private static readonly string[] userIncludes =[nameof(Customer.NormalizedEmail)];
    private static readonly string[] userExclude =[nameof(Customer.Active)];
    private static readonly string[] orderItemsExclude =[];

    public UserPage(IOrderManager orderManager, IUserManager userManager, LoginPage loginPage) {
        InitializeComponent();
        _loginPage = loginPage;
        orderItemsView.AutoSize = true;
        _changePasswordBtn.Click += _changePasswordBtn_click;
        _orderManager = orderManager;
        _userManager = userManager;
        _loginPage.OnLogin += (_, args) => {
            _loadedId = args.Customer.Id;
        };
        foreach (var orderItem in Utils.ColumnNames(typeof(OrderItemWithAggregates), orderItemsIncludes,
                     orderItemsExclude)){
            orderItemsView.Columns.Add(orderItem).AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
        _infoContainer = Utils.InitDetailsScheme(_infoBoxes, typeof(Customer), userExclude, userIncludes);
        _aggregatesContainer = Utils.InitDetailsScheme(_aggregatesBoxes, typeof(CustomerWithAggregates), [], [], true);
        infoBox.Controls.Add(_infoContainer);
        statisticsBox.Controls.Add(_aggregatesContainer);
        editBtn.Click += (_, _) => {
            if(editing) SaveUser();
        };
        editBtn.Click += (_, _) => editing = !editing;
        editBtn.Click+=(_,_)=> {
            if (editing) editBtn.Text = "Kaydet";
            else editBtn.Text = "Düzenle";
        };
        Instance = this;
    }

    public new void Load(uint id) {
        _loadedId = id;
    }

    private CustomerWithAggregates GetUser() {
        return _userManager.GetWithAggregates(_loadedId);
    }
    public new void Load() {
        if (_loadedId == null) return;
        var userTask = Task.Run(GetUser);
        var ordersTask = userTask.ContinueWith(_=>GetAllOrders());
        var ownPage = (ContextHolder.Session.User?.Id ?? ContextHolder.Session.UserId) == _loadedId;
        if(ownPage) ordersTask.ContinueWith(o =>Invoke( ()=>LoadOrders(o.Result)));
        if (!ownPage){
            confirmBtn.Enabled = confirmBtn.Visible = editBtn.Enabled = editBtn.Visible = false;
        }
        else confirmBtn.Enabled = confirmBtn.Visible = editBtn.Enabled = editBtn.Visible = true;
        userTask.ContinueWith(u=>Invoke(()=>LoadUser(u.Result, ownPage)));
        Task.WaitAll(userTask, ordersTask);
    }
    private struct EmailAndPassword {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string RepeatNewPassword { get; set; }
    }
    private void _changePasswordBtn_click(object? s, EventArgs e) {
        var ep = (EmailAndPassword) Utils.GetInput(typeof(EmailAndPassword) , "Şifre Gir");
        // if (ep.OldPassword == ep.NewPassword){
        //     
        // }
        if(ep.NewPassword .Equals( ep.RepeatNewPassword)) throw new ArgumentException("Şifreler eşleşmiyor.");
        _userManager.ChangePassword(ep.OldPassword, ep.NewPassword);
    }
    private static string[] sensitiveExclude = [
        nameof(Customer.PasswordHash), nameof(Customer.Address), nameof(Customer.LastName), nameof(Customer.PhoneNumber)
    ];
    private void LoadUser(Customer customer, bool ownPage = true) {
        foreach (var  us in Utils.ToPairs(customer,!ownPage?userExclude.Concat(sensitiveExclude).ToArray():userExclude, userIncludes)){
            if(us.Item1.Equals(nameof(Customer.PasswordHash))) continue;
            if(!_infoBoxes.TryGetValue(us.Item1, out var box)) 
                if(!_aggregatesBoxes.TryGetValue(us.Item1, out box)) continue;
            box.Text = us.Item2.ToString();
        }
        infoBox.Refresh();
    }
    private void LoadOrders(List<OrderWithAggregates> orders, bool ownPage = true) {
        orderItemsView.Groups.Clear();
        orderItemsView.Items.Clear();
        foreach (var order in orders){
            var group = new ListViewGroup(
                $"Order #{order.Id} - {order.Status} : {order.Date.ToShortDateString()} \nİndirimsiz Fiyat: {order.BasePrice} \nSon Fiyat: {order.CouponDiscountedPrice}",
                HorizontalAlignment.Left){Subtitle = "Teslimat Adresi: " + order.ShippingAddress.ToString()};
            group.Tag = order;
            foreach (var item in order.Items){
                var i = new ListViewItem(Utils.ToPairs(item, orderItemsExclude, orderItemsIncludes)
                    .Select(p => p.Item2.ToString()).ToArray()!){
                    Group = group
                };
                group.Items.Add(i);
                orderItemsView.Items.Add(i);
            }
            orderItemsView.Groups.Add(group);
        }
        orderItemsView.Refresh();
    }

    private List<OrderWithAggregates> GetAllOrders() {
        return _orderManager.GetAllOrders(true,page: _ordersPage);
    }

    public void Go() {
        Clear();
        Load();
    }

    private void Clear() {
        orderItemsView.Groups.Clear();
        orderItemsView.Items.Clear();
        foreach (var keyValuePair in _infoBoxes){
            keyValuePair.Value.Clear();
        }
    }
    // private void orderItemsView_SelectedIndexChanged(object? sender, EventArgs e) {
    //     if (orderItemsView.SelectedItems.Count == 0) return;
    //     foreach (int selectedİndex in orderItemsView.SelectedIndices){
    //         var order = (Order)orderItemsView.Groups[selectedİndex].Tag;
    //         order = _orderManager.GetOrderWithItems(order.Id)!;
    //         foreach (var orderItem in order.Items){
    //             var item = new ListViewItem();
    //             foreach (var valueTuple in Utils.ToPairs(orderItem, orderItemsExclude, orderItemsIncludes)){
    //                 item.SubItems[valueTuple.Item1].Text = valueTuple.Item2.ToString();
    //             }
    //
    //             item.Tag = orderItem;
    //             orderItemsView.Groups[selectedİndex].Items.Add(item);
    //         }
    //     }
    // }

    private void cancelBtn_Click(object sender, EventArgs e) {
        foreach (int index in orderItemsView.SelectedIndices){
            var order  =(Order)orderItemsView.Groups[index].Tag;
            _orderManager.CancelOrder(order);
        }
        Task.Run(GetAllOrders).ContinueWith(o=>Invoke(()=>LoadOrders(o.Result)) );
        Utils.Info("Siparişler İptal Edildi.");
    }

    private void updateAdres_Click(object sender, EventArgs e) {
        var newaddress =(Address?) Utils.GetInput(typeof(Address), "Addres Güncelle");
        if(newaddress==null) return;
        foreach (int idx in orderItemsView.SelectedIndices){
            var order = (Order)orderItemsView.Groups[idx].Tag ;
            order.ShippingAddress = newaddress;
            _orderManager.UpdateOrder(order);
        }
        var o=Task.Run(GetAllOrders);
        o.ContinueWith(o=>Invoke(()=>LoadOrders(o.Result)));
        Utils.Info("Adres Bilgileri Güncellendi.");
        Task.WaitAll(o);
    }
    
    private void confirmBtn_Click(object sender, EventArgs e) {
        foreach (int i in orderItemsView.SelectedIndices){
            var order = (Order)orderItemsView.Groups[i].Tag;
            order.Status = OrderStatus.Delivered;
            _orderManager.Complete(order);
        }
    }
    private void SaveUser() {
        var user=(Customer)Utils.DictToObject(typeof(Customer), _infoBoxes.Where(p=>typeof(Customer).GetProperty(p.Key.Split('_').First())!=null).ToDictionary(kv => kv.Key, kv => kv.Value.Text));
        user.Id = (uint)_loadedId;
        user.SessionId = ContextHolder.Session.Id;
        var u = ContextHolder.GetUserOrThrow();
        user.PasswordHash = u.PasswordHash;
        var updateTask = Task.Run(()=> _userManager.Update(user));
        updateTask.ContinueWith(t => Invoke(() => LoadUser(t.Result)));
        Utils.Info("Kullanıcı Bilgileri Güncellendi.");
    }
    private void editBtn_Click(object sender, EventArgs e) {
        foreach (var keyValuePair in _infoBoxes){
            keyValuePair.Value.ReadOnly = false;
        }
    }
}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ecommerce.Bl;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Ecommerce.DesktopImpl
{
public partial class UserPage : UserControl
{
    private readonly IOrderManager _orderManager;
    private readonly IUserManager _userManager;
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
            nameof(Seller.ShopAddress)),
    };

    private readonly FlowLayoutPanel _infoContainer;
    private readonly LoginPage _loginPage;
    private static readonly string[] userIncludes =["Email"];
    private static readonly string[] userExclude =[];
    private static readonly string[] orderItemsExclude =[];

    public UserPage(IOrderManager orderManager, IUserManager userManager, LoginPage loginPage) {
        InitializeComponent();
        _loginPage = loginPage;
        orderItemsView.AutoSize = true;
        _changePasswordBtn.Click += _changePasswordBtn_click;
        _orderManager = orderManager;
        _userManager = userManager;
        _loginPage.OnLogin += (_, args) => {
            _loadedId = args.User.Id;
        };
        foreach (var orderItem in Utils.ColumnNames(typeof(OrderItemWithAggregates), orderItemsIncludes,
                     orderItemsExclude)){
            orderItemsView.Columns.Add(orderItem).AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
        _infoContainer = InitDetailsScheme();
        editBtn.Click += (_, _) => {
            if(editing) SaveUser();
        };
        editBtn.Click += (_, _) => editing = !editing;
        editBtn.Click+=(_,_)=> {
            if (editing) editBtn.Text = "Kaydet";
            else editBtn.Text = "Düzenle";
        };
    }
    public new void Load() {
        if (_loadedId == null) return;
        var u = _userManager.GetWithAggregates();
        LoadOrders();
        LoadUser(u);
    }
    private struct EmailAndPassword {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string RepeatNewPassword { get; set; }
    }
    private void _changePasswordBtn_click(object? s, EventArgs e) {
        var ep = (EmailAndPassword) Utils.GetInput(typeof(EmailAndPassword));
        if(ep.NewPassword .Equals( ep.RepeatNewPassword)) throw new ArgumentException("Şifreler eşleşmiyor.");
        _userManager.ChangePassword(ep.OldPassword, ep.NewPassword);
    }
    private readonly Dictionary<string, TextBox> _textBoxes = new();
    private FlowLayoutPanel InitDetailsScheme() {
        var infoContainer = new FlowLayoutPanel(){
            Dock = DockStyle.Fill, WrapContents = true, FlowDirection = FlowDirection.TopDown,AutoScroll = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left, Visible = true, Enabled = true
        };
        infoBox.Controls.Add(infoContainer);
        foreach (var kv in Utils.ColumnNames(typeof(UserWithAggregates), userExclude, userIncludes)){
            if(kv.Equals(nameof(User.PasswordHash))) continue;
            var label = new Label{
                Text = kv, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular)
            };
            var text = new TextBox{
                Text = kv, ReadOnly = true, AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular), Enabled = true, Visible = true
            };
            var container = new FlowLayoutPanel{
                FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left, Visible = true, Enabled = true
            };
            _textBoxes.Add( kv,text);
            container.Controls.Add(label);
            container.Controls.Add(text);
            infoContainer.Controls.Add(container);
        }
        return infoContainer;
    }

    private void LoadUser(User user) {
        foreach (var  us in Utils.ToPairs(user,userExclude, userIncludes)){
            if(us.Item1.Equals(nameof(User.PasswordHash))) continue;
            if(!_textBoxes.TryGetValue(us.Item1, out var box))continue;
            box.Text = us.Item2.ToString();
        }
        infoBox.Refresh();
    }
    private void LoadOrders() {
        orderItemsView.Groups.Clear();
        orderItemsView.Items.Clear();
        foreach (var order in _orderManager.getAllOrders(page: _ordersPage)){
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
    }

    public override void Refresh() {
        base.Refresh();
        Clear();
        Load();
    }

    private void Clear() {
        orderItemsView.Groups.Clear();
        orderItemsView.Items.Clear();
        foreach (var keyValuePair in _textBoxes){
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
        LoadOrders();
        Utils.Info("Siparişler İptal Edildi.");
    }

    private void updateAdres_Click(object sender, EventArgs e) {
        var newaddress =(Address?) Utils.GetInput(typeof(Address));
        if(newaddress==null) return;
        foreach (int idx in orderItemsView.SelectedIndices){
            var order = (Order)orderItemsView.Groups[idx].Tag ;
            order.ShippingAddress = newaddress;
            _orderManager.UpdateOrder(order);
        }
        LoadOrders();
        Utils.Info("Adres Bilgileri Güncellendi.");
    }
    
    private void confirmBtn_Click(object sender, EventArgs e) {
        foreach (int i in orderItemsView.SelectedIndices){
            var order = (Order)orderItemsView.Groups[i].Tag;
            order.Status = OrderStatus.DELIVERED;
            _orderManager.complete(order);
        }
    }
    private void SaveUser() {
        var user=(User)Utils.DictToObject(typeof(User), _textBoxes.Where(p=>typeof(User).GetProperty(p.Key)!=null).ToDictionary(kv => kv.Key, kv => kv.Value.Text));
        user.Id = (uint)_loadedId;
        user.SessionId = ContextHolder.Session.Id;
        user.PasswordHash = ContextHolder.GetUserOrThrow().PasswordHash;
        _userManager.Update(user);
        LoadUser(user);
        Utils.Info("Kullanıcı Bilgileri Güncellendi.");
    }
    private void editBtn_Click(object sender, EventArgs e) {
        foreach (var keyValuePair in _textBoxes){
            keyValuePair.Value.ReadOnly = false;
        }
    }
}
}

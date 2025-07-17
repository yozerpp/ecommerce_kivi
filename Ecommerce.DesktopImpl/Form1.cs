using System.Drawing.Drawing2D;
using Ecommerce.Bl;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Microsoft.Identity.Client;

namespace Ecommerce.DesktopImpl;

public partial class Form1 : Form
{
    private readonly Navigation _navigation;
    private readonly Homepage homepage1; 
    private readonly CartPage cart1;
    private readonly UserPage userPage1;
    private readonly LoginPage login1;
    private readonly SellerPage sellerPage1;
    private readonly RegisteryPage registerUser1; 
    private readonly ProductPage product1;
    private readonly LocalStorage localStorage;
    private readonly ICartManager _cartManager;
    private readonly IUserManager _userManager;

    public Form1(Homepage homepage, CartPage cartPage, UserPage useraPage,ICartManager cartManager, 
        LoginPage loginPage, SellerPage sellerPage, Navigation navigation, RegisteryPage registeryPage,IUserManager userManager, ProductPage productPage, LocalStorage localStorage) {
        this.localStorage = localStorage;
        _cartManager = cartManager;
        _userManager = userManager;
        cart1 = cartPage;
        _navigation = navigation;
        userPage1 = useraPage;
        homepage1 = homepage;
        product1 = productPage;
        sellerPage1 = sellerPage;
        registerUser1 = registeryPage;
        login1 = loginPage;
        _navigation.MainPage = homepage1;
        InitializeComponent();
        InitPages();
        var i = localStorage.GetSavedSessionInfo();
        if (i == null || i.Value.Item1==null && i.Value.Item2==null){
            var s = _cartManager.newCart(flush:true);
            localStorage.PersistAnonymousSession(s);
        }
        else{
            if (i.Value.Item1 != null || i.Value.Item2.User != null){
                loginBtn.Text = "Kullanıcı Bilgileri";
                registerBtn.Visible = false;
                registerBtn.Enabled = false;
                logoutBtn.Enabled = true;
                logoutBtn.Visible = true;
                userPage1._loadedId = i.Value.Item2?.User?.Id?? (i.Value.Item1?.Id);
            }
            ContextHolder.Session = i.Value.Item1.Session;
        }
        login1.OnLogin += (_, _) => loginBtn.Text = "Kullanıcı Bilgileri";
        login1.OnLogin += (_, _) => {
            registerBtn.Visible = false;
            registerBtn.Enabled = false;
        };
        login1.OnLogin += (_, _) => {
            logoutBtn.Enabled = true;
            logoutBtn.Visible = true;
        };
        logoutBtn.Click += (_, _) => loginBtn.Text = "Giriş Yap";
        logoutBtn.Click += (_, _) => {
            registerBtn.Enabled = true;
            registerBtn.Visible = true;
            logoutBtn.Visible = false;
            logoutBtn.Enabled = false;
        };
        logoutBtn.Click += CleanSession;
        _navigation.Go(homepage1, homepage1);
    }
    
    private void InitPages()
    {
        homepage1.Visible = true;
        homepage1.Dock = DockStyle.Fill;
        pageContainer.Controls.Add(homepage1);
        product1.Visible = false;
        product1.Dock = DockStyle.Fill;
        pageContainer.Controls.Add(product1);
        cart1.Visible = false;
        cart1.Dock = DockStyle.Fill;
        pageContainer.Controls.Add(cart1);
        sellerPage1.Visible = false;
        sellerPage1.Dock = DockStyle.Fill;
        pageContainer.Controls.Add(sellerPage1);
        userPage1.Visible = true;
        userPage1.Dock = DockStyle.Fill;
        pageContainer.Controls.Add(userPage1);
        login1.Visible = false;
        login1.Dock = DockStyle.Fill;
        pageContainer.Controls.Add(login1);
        registerUser1.Visible = false;
        registerUser1.Dock = DockStyle.Fill;
        pageContainer.Controls.Add(registerUser1);
    }
    private void backBtn_Click_1(object sender, EventArgs e)
    {
        _navigation.Back();
    }

    private void refreshBtn_Click_1(object sender, EventArgs e)
    {
        _navigation.Refresh();
    }

    private void forwardBtn_Click(object sender, EventArgs e)
    {
        _navigation.Forward();
    }

    private void LoginBtnClick(object sender, EventArgs e) {
        if(ContextHolder.Session.User==null)
            _navigation.Go(null, login1);
        else if(ContextHolder.Session.User is Seller)
            _navigation.Go(null, sellerPage1);
        else _navigation.Go(null, userPage1);
    }
    private void cartBtn_Click(object sender, EventArgs e) {
        _navigation.Go(null, cart1);
    }
    private void homepageBtn_Click(object sender, EventArgs e) {
        _navigation.Go(null, homepage1);
    }
    private void CleanSession(object? sender, EventArgs e) {
        if(ContextHolder.Session.User!=null){
            localStorage.DeleteLoginInfo();
            var s = localStorage.GetAnonymousSessionInfo();
            if (s==null){
                s= _cartManager.newCart(flush:true);
                localStorage.PersistAnonymousSession(s);
            }
            ContextHolder.Session = s;
            _navigation.Go(null, homepage1);
        }
    }

    private void registerBtn_Click(object sender, EventArgs e) {
        _navigation.Go(null, registerUser1);
    }
}
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;

namespace Ecommerce.DesktopImpl
{
    public class LoginEventArgs: EventArgs
    {
        public User User { get; set; }
        public Session Session { get; set; }
    }
    public partial class LoginPage : UserControl, IPage
    {
        public event EventHandler<LoginEventArgs> OnLogin = delegate{};
        public static LoginPage Instance { get; set; }
        private readonly IUserManager _userManager;
        private readonly LocalStorage _localStorage;
        private readonly SellerPage _sellerPage;
        private readonly Navigation _navigation;
        private readonly RegisteryPage _registeryPage;
        public LoginPage(IUserManager userManager, RegisteryPage registeryPage, SellerPage sellerPage,Navigation navigation, LocalStorage localStorage) {
            _localStorage = localStorage;
            _sellerPage = sellerPage;
            _registeryPage = registeryPage;
            _navigation = navigation;
            _userManager = userManager;
            InitializeComponent();
            Instance = this;
        }

        public void Go() { }

        private void userLgnBtn_Click(object sender, EventArgs e) {
            var email = emailBox.Text;
            var password = passwordBox.Text;
            var user = _userManager.LoginUser(email,password, out var token);
            if (token == null){
                Utils.Error("Eşleşen Kullanıcı Bulunamadı.");
                return;
            }
            if(rememberMeBtn.Checked)
                _localStorage.PersistLoginInfo(token);
            OnLogin(this, new LoginEventArgs{User =user, Session = user.Session});
            _navigation.Go(this, null);
        }
        private void sellerLoginBtn_Click(object sender, EventArgs e) {
            var email = emailBox.Text;
            var password = passwordBox.Text;
            var seller =  _userManager.LoginSeller(email, password, out var token);
            if (token == null){
                Utils.Error("Eşleşen Kullanıcı Bulunamadı.");
                return;
            }
            if(rememberMeBtn.Checked)
                _localStorage.PersistLoginInfo(token);
            _sellerPage.Load(seller.Id);
            OnLogin(this, new LoginEventArgs(){User = seller, Session = seller.Session});
            _navigation.Go(this, _sellerPage);
        }
        private void registerBtn_Click(object sender, EventArgs e) {
            _navigation.Go(this, _registeryPage);
        }

    }
}

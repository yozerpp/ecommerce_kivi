using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ecommerce.DesktopImpl
{
    public partial class RegisteryPage : UserControl, IPage
    {
        private Dictionary<string, TextBox>[] inputs = [new(),new()];
        private readonly IUserManager _userManager;
        private void CreateForm(Type type, int i) {
            var form = new FlowLayoutPanel(){
                FlowDirection = FlowDirection.TopDown,Padding = new Padding(5,0,5,0),
                AutoScroll = true,WrapContents = false, Dock = DockStyle.Fill
            };
            foreach (var col in Utils.ColumnNames(type, ["Active"], ["Email"])){
                var l = new Label{
                    AutoSize = true,Text = col,TextAlign = ContentAlignment.MiddleCenter,Dock = DockStyle.Left,
                    Anchor = AnchorStyles.Left
                };
                var inputbox = new TextBox{
                    AcceptsReturn = false,PlaceholderText = col.Equals("PasswordHash")?"Password":col,Dock = DockStyle.Right,Anchor = AnchorStyles.Right
                };
                var container = new FlowLayoutPanel{
                    FlowDirection = FlowDirection.LeftToRight,AutoSize = true,Dock = DockStyle.Top, Anchor = AnchorStyles.Top,
                };
                container.Controls.Add(l);
                container.Controls.Add(inputbox);
                form.Controls.Add(container);
                inputs[i].Add(col, inputbox);
            }
            formTabs.TabPages[i].Controls.Add(form);
        }
        public RegisteryPage(IUserManager userManager) {
            _userManager = userManager;
            InitializeComponent();
            CreateForm(typeof(User), 0);
            CreateForm(typeof(Seller), 1);
        }

        public void Go() {
            Clear();
        }

        public void Clear() {
            foreach (var dictionary in inputs){
                foreach (var keyValuePair in dictionary){
                    keyValuePair.Value.Clear();
                }  
                
            }
        }
        //register
        private void button1_Click(object sender, EventArgs e) {
            var user = (User)Utils.DictToObject(formTabs.SelectedIndex==0?typeof(User):typeof(Seller),inputs[formTabs.SelectedIndex].ToDictionary(kv => kv.Key, kv => kv.Value.Text));
            _userManager.Register(user);
            Clear();
            Utils.Info("Başarıyla Kayıt Yapıldı.");
        }
        
    }
}

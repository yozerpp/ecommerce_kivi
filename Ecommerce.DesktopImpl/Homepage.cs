using System.Text.RegularExpressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.DesktopImpl
{
    public partial class Homepage : UserControl, IPage
    {
        private readonly IProductManager _productManager;
        private readonly ProductPage _productPage;
        private readonly Navigation _navigation;
        private int _page = 1;
        private string _query = string.Empty;
        private static readonly string[] excluded =["Image"];
        private static readonly string[] included = ["Category_Name"];
        public Homepage(ProductPage productPage, IProductManager productManager, Navigation navigation)
        {
            _productManager = productManager;
            _productPage = productPage;
            _navigation = navigation;
            InitializeComponent();
            foreach (var columnName in Utils.ColumnNames(typeof(ProductWithAggregates), excluded, included)){
                searchResults.Columns.Add(columnName, columnName);
            }
        }
        private void search_Click(object sender, EventArgs e)
        {
            if (_query!=searchBar1.Text){
                _query = searchBar1.Text;
                _page = 1;
            }
            var (preds, orders) = IProductManager.ParseQuery(_query);
            doSearch(preds, orders);
        }

        private void doSearch(ICollection<SearchPredicate> preds, ICollection<SearchOrder> orders)
        {
            var results = Task.Run(()=> _productManager.SearchWithAggregates(preds, orders,false, false, page:_page));
            results.ContinueWith((r) => {
                Invoke(() => {
                    searchResults.Rows.Clear();
                    foreach (var product in r.Result){
                        var i = searchResults.Rows.Add();
                        foreach (var pair in Utils.ToPairs(product, excluded, included)){
                            searchResults.Rows[i].Cells[pair.Item1].Value = pair.Item2;
                        }

                        searchResults.Rows[i].Tag = product;
                    }
                });
            });
            results.Wait();
        }

        public void Go() {
            _page = 1;
            search_Click(null,null);
        }

        private void nextBtn_Click(object sender, EventArgs e) {
            _page++;
            search_Click(null,null);
        }
        private void prevBtn_Click(object sender, EventArgs e) {
            if (_page > 1) {
                _page--;
                search_Click(null,null);
            }
        }

        private void searchResults_CellContentClick_1(object sender, DataGridViewCellMouseEventArgs e) {
            _productPage.LoadProduct(((Product)searchResults.Rows[e.RowIndex].Tag).Id);
            _navigation.Go(this,_productPage);
        }
    }
}

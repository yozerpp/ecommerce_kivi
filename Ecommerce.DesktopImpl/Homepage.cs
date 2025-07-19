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
            ICollection<SearchPredicate> preds=[];
            ICollection<SearchOrder> orders= [];
            if (_query.Length > 0){
                var filter = _query.Split('&');
                var order = filter[filter.Length - 1].Split("&&");
                filter[filter.Length - 1] = order.Length > 1 ? order[0] : filter[filter.Length - 1];
                order = order.Skip(1).ToArray();
                preds = GetPredicates(filter);
                orders = GetOrdering(order);
            }
            doSearch(preds, orders);
        }

        private void doSearch(ICollection<SearchPredicate> preds, ICollection<SearchOrder> orders)
        {
            var results = _productManager.SearchWithAggregates(preds, orders,false, false, page:_page);
            searchResults.Rows.Clear();
            foreach (var product in results){
                var i = searchResults.Rows.Add();
                foreach (var pair in Utils.ToPairs(product, excluded, included)){
                    searchResults.Rows[i].Cells[pair.Item1].Value = pair.Item2;
                }
                searchResults.Rows[i].Tag = product;
            }
        }

        private ICollection<SearchOrder> GetOrdering(string[] orderings)
        {
            var result = new List<SearchOrder>();
            foreach (var ordering in orderings)
            {
                var s = ordering.Split(',');
                bool desc = true;
                string propName;
                if (s.Length == 1) propName = s[0];
                else
                {
                    propName = s[0];
                    if (s[1].Equals("DESC")) desc = true;
                    else if (s[1].Equals("ASC")) desc = false;
                    else continue;
                }
                result.Add(new SearchOrder() { Ascending = !desc, PropName = propName });
            }

            return result;
        }
        private ICollection<SearchPredicate> GetPredicates(string[] filter)
        {
            var predicates = new List<SearchPredicate>();
            foreach (var pred in filter)
            {
                var regex = new Regex("\\w+((<)|(>)|(=)|(%)|(>=)|(<=))\\w+");
                var m = regex.Match(pred);
                string op;
                SearchPredicate.OperatorType operatorType;
                if (m.Groups[2].Success)
                {
                    operatorType = SearchPredicate.OperatorType.LessThan;
                    op = m.Groups[2].Value;
                }
                else if (m.Groups[3].Success)
                {
                    operatorType = SearchPredicate.OperatorType.GreaterThan;
                    op = m.Groups[3].Value;
                }
                else if (m.Groups[4].Success)
                {
                    operatorType = SearchPredicate.OperatorType.Equals;
                    op = m.Groups[4].Value;
                }
                else if (m.Groups[5].Success)
                {
                    operatorType = SearchPredicate.OperatorType.Like;
                    op = m.Groups[5].Value;
                }
                else if (m.Groups[6].Success)
                {
                    operatorType = SearchPredicate.OperatorType.GreaterThanOrEqual;
                    op = m.Groups[6].Value;
                }
                else if (m.Groups[7].Success)
                {
                    operatorType = SearchPredicate.OperatorType.LessThanOrEqual;
                    op = m.Groups[7].Value;
                }
                else continue;
                var s = pred.Split(op);
                predicates.Add(new SearchPredicate() { Operator = operatorType, PropName = s[0], Value = s[1] });
            }

            return predicates;
        }

        public void Go()
        {
            search_Click(null,null);
        }

        private void nextBtn_Click(object sender, EventArgs e) {
            _page++;
            doSearch([],[]);
        }
        private void prevBtn_Click(object sender, EventArgs e) {
            if (_page > 1) {
                _page--;
                doSearch([],[]);
            }
        }

        private void searchResults_CellContentClick_1(object sender, DataGridViewCellMouseEventArgs e) {
            _productPage.LoadProduct(((Product)searchResults.Rows[e.RowIndex].Tag).Id);
            _navigation.Go(this,_productPage);
        }
    }
}

using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Views;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebImpl.Pages;

public class HomepageModel : BaseModel
{
    private readonly ILogger<HomepageModel> _logger;
    private readonly IProductManager _productManager;
    private readonly ICartManager _cartManager;
    public readonly Dictionary<uint,Category> Categories;
    public HomepageModel(ILogger<HomepageModel> logger, IProductManager productManager, ICartManager cartManager, Dictionary<uint,Category> categories) {
        _logger = logger;
        _cartManager = cartManager;
        _productManager = productManager;
        Categories = categories;
    }

    public class RangeInput
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public bool IsEntered { get; set; }
    }
    public class OrderInput
    {
        public bool Selected { get; set; }
        public bool? Ascending { get; set; }
    }
    [BindProperty(SupportsGet = true)] 
    public Dictionary<string, RangeInput> AggregateFilters { get; set; } = new();
    [BindProperty(SupportsGet = true)] 
    public Dictionary<string,string?> PropertyFilters{ get; set; } = new();
    [BindProperty(SupportsGet = true)] public Dictionary<string, OrderInput> Orders { get; set; } = new();
    [BindProperty(SupportsGet = true)]
    public string? SearchName { get; set; } 
    [BindProperty]
    public uint? SearchCategory { get; set; }
    [BindProperty(SupportsGet = true)] 
    public int PageIndex { get; set; } = 1;

    [BindProperty(SupportsGet = true)] 
    private int PageSize { get; set; } = 20;
    [BindProperty]
    public List<ProductWithAggregatesCustomerView> Products { get; set; } =[];

    [BindProperty]
    public ICollection<ProductWithAggregatesCustomerView>? CategoryRecommendations { get; set; }
    [BindProperty] public ICollection<ProductWithAggregatesCustomerView>? SellerRecommendations { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? QueryString { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? OrderString { get; set; }
    [BindProperty(SupportsGet = true)]
    public ViewType_? ViewType { get; set; }
    public enum ViewType_
    {
        Main,Featured,Seller
    }
    public IActionResult OnGet() {
        var favorites = GetFavorites();
        // if(QueryString!=null)
            // ParseQuery(QueryString=QueryString.Replace('.','_'), out  preds,out orders  );
        // else GetParams(out preds, out orders);
        // Products = Search(preds, orders, PageIndex).Select(p=>new ProductWithAggregatesCustomerView(){Product = p, CurrentFavored = favorites?.Contains(p.Id)}).ToList();
        Products = _productManager.Search(QueryString, [], true, false, false, page: PageIndex).Select(p=>new ProductWithAggregatesCustomerView(){
            Product = p,
            CurrentFavored = favorites?.Contains(p.Id)
        }).ToList();
        CategoryRecommendations = [];
        SellerRecommendations = [];
        if(ViewType == null || ViewType == ViewType_.Main)
            return Page();
        if (ViewType == ViewType_.Featured)
            return Partial("Shared/Product/_FeaturedPartial", new _FeaturedPartial(){
                PageIndex = PageIndex,
                Type = null, Categories = Categories, Products = Products,
            });
        if (ViewType == ViewType_.Seller){
            
        }
    }

    private List<Entity.Product> Search(ICollection<SearchPredicate> preds, ICollection<SearchOrder> orders, int page) {
         return _productManager.Search(preds, orders, includeImage:true,fetchOffers:false, fetchReviews:false,page: page, pageSize: PageSize);
    }
    private ICollection<uint>? GetFavorites() {
        return (CurrentCustomer != null ? _productManager.GetFavorites(CurrentCustomer) :null)?.Select(f=>f.ProductId).ToArray();
    }

    [BindProperty(SupportsGet = true)]
    public RecommendationType Type { get; set; }

    public enum RecommendationType
    {
        Seller,Category
    }

    public const string HasPropertiesKey = "HasProperties";
    public IActionResult OnGetRecommended() {
        var products = Type==RecommendationType.Category?_productManager.GetMoreProductsFromCategories(CurrentSession, PageIndex): _cartManager.GetMoreProductsFromSellers(CurrentSession, page: PageIndex);
        if (products.Count == 0) return new NoContentResult();
        return Partial("Shared/Product/" + nameof(_FeaturedPartial), new _FeaturedPartial{
            Products = ProductWithAggregatesCustomerView.PromoteAll(products,
                GetFavorites()),
            PageIndex = PageIndex,
            Categories = Categories,
            Type = Type
        });
    }

    private static ICollection<SearchOrder> ParseOrders(string q) {
        if (q == null) return[];
            var orders = q.Split("&&",StringSplitOptions.RemoveEmptyEntries);
        return orders.Select(o => {
            var splt = o.Split(';', StringSplitOptions.RemoveEmptyEntries);
            return new SearchOrder(){
                Ascending = splt.Length==2 && splt[1].Equals("asc", StringComparison.OrdinalIgnoreCase),
                PropName = splt[0]
            };
        }).ToArray();
    }
    public IActionResult OnGetMore() {
        var ps = _productManager.Search(QueryString, ParseOrders(OrderString), true, false, false, page:PageIndex);
        if(ps.Count == 0) return new NoContentResult();
        var favs = GetFavorites();
        var p = new _VerticalListPartial(){
            Products = ProductWithAggregatesCustomerView.PromoteAll(ps,favs),
            Categories = Categories,
            CategoryRecommendations = [],
            SellerRecommendations = [],
            PageIndex = PageIndex
        };
        return Partial("Shared/Product/" + nameof(_VerticalListPartial), p);
    }
}

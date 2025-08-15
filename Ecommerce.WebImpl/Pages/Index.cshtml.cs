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

    [BindProperty(SupportsGet = true)] public bool JsonResult { get; set; }

    [BindProperty]
    public List<ProductWithAggregatesCustomerView> Products { get; set; } =[];

    [BindProperty]
    public ICollection<ProductWithAggregatesCustomerView>? CategoryRecommendations { get; set; }
    [BindProperty] public ICollection<ProductWithAggregatesCustomerView>? SellerRecommendations { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? QueryString { get; set; }
    public IActionResult OnGet() {
        ICollection<SearchPredicate> preds;
        ICollection<SearchOrder> orders;
        if(QueryString!=null)
            ParseQuery(QueryString, out  preds,out orders  );
        else GetParams(out preds, out orders);
        var favorites = GetFavorites();
        Products = Search(preds, orders, PageIndex).Select(p=>new ProductWithAggregatesCustomerView(){Product = p, CurrentFavored = favorites?.Contains(p.Id)}).ToList();
        if (JsonResult) return new JsonResult(Products);
        CategoryRecommendations = [];
        SellerRecommendations = [];
        return Page();
    }

    private List<Entity.Product> Search(ICollection<SearchPredicate> preds, ICollection<SearchOrder> orders, int page) {
         return _productManager.Search(preds, orders, includeImage:true,fetchOffers:false, fetchReviews:false,page: page, pageSize: PageSize);
    }
    private ICollection<uint>? GetFavorites() {
        return (CurrentCustomer != null ? _productManager.GetFavorites(CurrentCustomer) :null)?.Select(f=>f.ProductId).ToArray();
    }
    [BindProperty(SupportsGet = true)] public int? PageNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public RecommendationType Type { get; set; }

    public enum RecommendationType
    {
        Seller,Category
    }

    public const string HasPropertiesKey = "HasProperties";
    public IActionResult OnGetRecommended() {
        var products = Type==RecommendationType.Category?_productManager.GetMoreProductsFromCategories(CurrentSession, PageNumber?? 1): _cartManager.GetMoreProductsFromSellers(CurrentSession, page: PageNumber ?? 1);
        if (products.Count == 0) return new NoContentResult();
        return Partial("Shared/Product/" + nameof(_FeaturedPartial), new _FeaturedPartial{
            Products = ProductWithAggregatesCustomerView.PromoteAll(products,
                GetFavorites()),
            PageIndex = PageNumber ?? 1,
            Categories = Categories,
            Type = Type
        });
    }

    private void ParseQuery(string query, out ICollection<SearchPredicate> predicates, out ICollection<SearchOrder> orders) {
        predicates = new List<SearchPredicate>();
        orders = new List<SearchOrder>();

        var parts = query.Split(["&&"], StringSplitOptions.RemoveEmptyEntries);

        // Regex to capture property name, operator, and value
        // It handles operators like >=, <=, >, <, =, % (for LIKE)
        // Property names and values can contain alphanumeric, underscores, and dots.
        var predicateRegex = new Regex(@"^([\w\d\._]+?)(>=|<=|>|<|=|%)(.*)$", RegexOptions.Compiled);

        foreach (var part in parts)
        {
            if (part.Contains(',')) // Likely an order clause
            {
                var orderParts = part.Split(',');
                if (orderParts.Length == 2)
                {
                    orders.Add(new SearchOrder()
                    {
                        PropName = orderParts[0].Trim(),
                        Ascending = orderParts[1].Trim().Equals("ASC", StringComparison.OrdinalIgnoreCase),
                    });
                }
            }
            else // Likely a predicate
            {
                var match = predicateRegex.Match(part);
                if (match.Success)
                {
                    var propName = match.Groups[1].Value;
                    var op = match.Groups[2].Value;
                    var val = match.Groups[3].Value;

                    SearchPredicate.OperatorType operatorType;
                    switch (op)
                    {
                        case ">=": operatorType = SearchPredicate.OperatorType.GreaterThanOrEqual; break;
                        case "<=": operatorType = SearchPredicate.OperatorType.LessThanOrEqual; break;
                        case ">": operatorType = SearchPredicate.OperatorType.GreaterThan; break;
                        case "<": operatorType = SearchPredicate.OperatorType.LessThan; break;
                        case "=": operatorType = SearchPredicate.OperatorType.Equals; break;
                        case "%": operatorType = SearchPredicate.OperatorType.Like; break;
                        default: continue; // Should not happen with the regex, but good for safety
                    }

                    predicates.Add(new SearchPredicate()
                    {
                        Operator = operatorType,
                        PropName = propName,
                        Value = val
                    });
                }
            }
        }
    }
    [BindProperty(SupportsGet = true)]public string AggregatesJson { get; set; } = "{}";
    [BindProperty(SupportsGet = true)] public string OrdersJson { get; set; } = "{}";
    public IActionResult OnGetMore() {
        AggregateFilters = JsonSerializer.Deserialize<Dictionary<string, RangeInput>>(AggregatesJson) ?? new();
        Orders = JsonSerializer.Deserialize<Dictionary<string, OrderInput>>(OrdersJson) ?? new();
        GetParams(out var preds, out var orders);
        var favs = GetFavorites();
        var p = new _VerticalListPartial(){
            Products = ProductWithAggregatesCustomerView.PromoteAll(Search(preds, orders, PageIndex),favs),
            Categories = Categories,
            CategoryRecommendations = [],
            SellerRecommendations = [],
            PageIndex = PageIndex
        };
        return Partial("Shared/Product/" + nameof(_VerticalListPartial), p);
    }
    public void GetParams(out ICollection<SearchPredicate> preds,out ICollection<SearchOrder> orders) {
        orders = Orders.Where(o=>o.Value?.Selected??false).Select(kv=>new SearchOrder(){
            PropName = kv.Key,
            Ascending = kv.Value.Ascending?? false,
        }).ToArray();
        var predicates = AggregateFilters.Where(f=>f.Value.IsEntered).SelectMany(kv => new[]{
            new SearchPredicate(){
                PropName = kv.Key.Equals(nameof(ProductOffer.Price))?nameof(ProductStats.MinPrice):kv.Key, //TODO make generic
                Operator = SearchPredicate.OperatorType.GreaterThan,
                Value = kv.Value.Min.ToString(CultureInfo.InvariantCulture),
            },
            new SearchPredicate(){
                PropName = kv.Key.Equals(nameof(ProductOffer.Price))?nameof(ProductStats.MinPrice):kv.Key,
                Operator = SearchPredicate.OperatorType.LessThan,
                Value = kv.Value.Max.ToString(),
            }
        }).ToList();
        if(PropertyFilters.ContainsKey(HasPropertiesKey))
            predicates.AddRange(PropertyFilters.Select(p=>new SearchPredicate(){
                Operator = SearchPredicate.OperatorType.Equals,
                PropName = p.Key,
                Value = p.Value,
            }));
        if (SearchName?.Length > 0)
            predicates.Add(new SearchPredicate()
                { Operator = SearchPredicate.OperatorType.Like, PropName = nameof(Entity.Product.Name), Value = SearchName });
        if (SearchCategory !=null){
            predicates.Add(new SearchPredicate(){
                Operator = SearchPredicate.OperatorType.Equals, PropName = string.Join('_',nameof(Entity.Product.Category), nameof(Category.Id)),
                Value = SearchCategory.Value.ToString()
            });
        }
        preds = predicates;
    }
}

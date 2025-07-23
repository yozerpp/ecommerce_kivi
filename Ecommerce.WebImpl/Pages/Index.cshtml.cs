using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages;

public class HomepageModel : PageModel
{
    private readonly ILogger<HomepageModel> _logger;
    private readonly IProductManager _productManager;
    private readonly EntityMapper.Factory _mapperFactory;
    public EntityMapper ProductMapper { get; init; }
    public HomepageModel(ILogger<HomepageModel> logger, IProductManager productManager, EntityMapper.Factory mapperFactory) {
        _logger = logger;
        _mapperFactory = mapperFactory;
        _productManager = productManager;
        ProductMapper = mapperFactory.Create(typeof(ProductWithAggregates),
            [string.Join('_', nameof(Product.Category), nameof(Category.Name))], ["Image"]);
    }

    [BindProperty] 
    public Dictionary<string, string?> Operators { get; set; } = new();
    [BindProperty] 
    public Dictionary<string,string?> Filters{ get; set; } = new();
    [BindProperty] public Dictionary<string, bool?> Orders { get; set; } = new();
    [BindProperty]
    public string SearchName { get; set; } = string.Empty;
    [BindProperty(Name = "Page")]
    public int PageIndex { get; set; } = 1;
    [BindProperty(Name = "PageSize")] 
    private int PageSize { get; set; } = 20;
    [BindProperty(Name = "Products")]
    public List<ProductWithAggregates> Products { get; set; } =[];

    public IActionResult OnPostSearch() {
        Operators[nameof(Product.Name)] = nameof(SearchPredicate.OperatorType.Like);
        OnGet();
        return Page();
        //return new RedirectToPageResult("/Index", new QueryString(
        //  '?'+string.Join("&&",
        // string.Join('&', Filters.Select(kv => $"{kv.Key}={kv.Value}")),
        // Orders.Select(kv => kv.Key + ',' + (kv.Value ? "DESC" : "ASC")))
        // ));
    }
    public void OnGet() {
        var (preds, orders) = GetParams();
        Products = _productManager.SearchWithAggregates(preds, orders, includeImage:true, fetchReviews:false,page: PageIndex, pageSize: PageSize);
    }
    public (ICollection<SearchPredicate>, ICollection<SearchOrder>) GetParams() {
        var predicates =  Filters.Where(kV=>kV.Value!=null).Select(kv => {
            if (!SearchPredicate.OperatorType.TryParse(Operators[kv.Key], out SearchPredicate.OperatorType op))
                throw new ArgumentException("Wrong operator type" + Operators[kv.Key]);
            return new SearchPredicate(){
                Operator = op,
                PropName = kv.Key,
                Value = kv.Value
            };
        });
        if (SearchName.Length > 0)
            predicates = predicates.Append(new SearchPredicate()
                { Operator = SearchPredicate.OperatorType.Like, PropName = nameof(Product.Name), Value = SearchName });
        var orders = Orders.Where(kv=>kv.Value.HasValue).Select(kv => new SearchOrder(){ PropName = kv.Key, Ascending = kv.Value!.Value }).ToArray();
        return (predicates.ToArray(), orders);
    }
}
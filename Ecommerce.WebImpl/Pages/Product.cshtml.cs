using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Projections;
using Ecommerce.Notifications;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Ecommerce.WebImpl.Pages;

public class Product : BaseModel
{
    private readonly IProductManager _productManager;
    private readonly ISellerManager _sellerManager;
    private readonly IRepository<ProductOffer> _offerRepository;
    private readonly INotificationService _notificationService;
    public readonly Dictionary<uint, Category> Categories;
    public Product(IProductManager productManager, INotificationService notificationService, ISellerManager sellerManager, ICartManager cartManager, Dictionary<uint, Category> categories, IRepository<ProductOffer> offerRepository) {
        _productManager = productManager;
        _notificationService = notificationService;
        _sellerManager = sellerManager;
        Categories = categories;
        _offerRepository = offerRepository;
    }



    [BindProperty(SupportsGet = true)]
    public uint ProductId { get; set; }
    [BindProperty]
    public ProductWithAggregates ViewedProduct { get; set; } 
    [BindProperty]
    public ICollection<OfferWithAggregates> Offers { get; set; }
    [BindProperty]
    public string? SentImages { get; set; }
    public IActionResult OnGet() {  
        var session = (Session)HttpContext.Items[nameof(Session)];
        var pr =_productManager.GetByIdWithAggregates(ProductId, false, false);
        if (pr == null) return new NotFoundObjectResult(new{ Message = "Product not found" });
        _productManager.VisitCategory(new SessionVisitedCategory(){SessionId = session.Id, CategoryId = pr.CategoryId});
        ViewedProduct = pr;
        Offers = _productManager.GetOffersWithAggregates(productId: ProductId);
        return new PageResult();
    }
    public IActionResult OnGetCategoryProperties([FromQuery] uint categoryId,[FromQuery] bool jsonResponse=false,[FromQuery] string? idPrefix = null,[FromQuery] bool isEditable=false) {
        var category = _productManager.GetCategoryById((uint)categoryId);
        if(category == null) throw new ArgumentException("Kategori bulunamadı.", nameof(categoryId));
        if (jsonResponse) return new JsonResult(category.CategoryProperties);
        if(idPrefix==null) throw new ArgumentNullException(nameof(idPrefix));
        return Partial("Shared/Product/"+nameof(_CategoryPropertiesPartial), new _CategoryPropertiesPartial(){
            InputNamePrefix = idPrefix,
            Properties = category.CategoryProperties,
        });
    }
    public JsonResult OnPostFavor() {
        if(CurrentCustomer==null) throw new UnauthorizedAccessException("Ürün favorilemek için giriş yapmış olmanız lazım.");
        return new JsonResult(new{
            Added = _productManager.Favor(new ProductFavor(){ CustomerId = CurrentCustomer.Id, ProductId = ProductId })
        });
    }


    [HasRole(nameof(Staff))]
    public IActionResult OnPostDelete() {
        _productManager.Delete(ViewedProduct);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Ürün Silindi.", Title = "İşlem Başarılı",
            Redirect = "/Index"
        });
    }
    [BindProperty]
    public ProductOffer EditedOffer { get; set; }
    [BindProperty]
    public bool IsProductEdited { get; set; }
    [BindProperty]
    public bool IsOfferEdited { get; set; }

    private void NotifyFavorers() {
        var pid = EditedOffer.ProductId;
        var favoredCustomers = _productManager.GetFavorers(EditedOffer.ProductId);
        var old= _offerRepository.FirstP(p=>new {p.Price, p.Discount}, p=>p.ProductId ==pid );
        var oldNet = old.Price * old.Discount;
        var newNet = EditedOffer.Price * EditedOffer.Discount;
        if(oldNet >= newNet && old.Discount <= EditedOffer.Discount) return;
        var ns =favoredCustomers.Select(f => new DiscountNotification(){
            UserId = f.CustomerId,
            ProductId = EditedOffer.ProductId,
            SellerId = CurrentSeller.Id,
            DiscountAmount = oldNet - newNet,
            DiscountRate = (oldNet - newNet) / oldNet
        });
        _notificationService.SendBatchAsync(ns);
    }
    [HasRole(nameof(Staff), nameof(Entity.Seller))]
    public IActionResult OnPostEdit() {
        var EditedProduct = EditedOffer.Product;
        // throw new Exception();
        EditedOffer.Discount = (100m - EditedOffer.Discount) / 100m;
        // throw new Exception();
        if (!IsProductEdited){
            EditedOffer.Product = null;
            // throw new Exception();
            _sellerManager.updateOffer(CurrentSeller, EditedOffer, EditedOffer.ProductId);
            if(IsOfferEdited && !IsProductEdited)NotifyFavorers();
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = true,
                Message = "Teklif Güncellendi.",
                Title = "İşlem Başarılı",
                Redirect = $"/Product?ProductId={EditedOffer.ProductId}"
            });
        }
        EditedOffer.ProductId = default;
        EditedProduct.Active = true;
        for (int i = 0; i < EditedProduct.Images.Count; i++){
            var image = EditedProduct.Images[i];
            if (image.Data == null!){
                EditedProduct.Images.Remove(image);
                continue;                
            }
        }
        if(SentImages!=null)
            foreach (var sentImage in SentImages.Split(";;")){
                // throw new Exception();
                EditedProduct.Images.Add(new Image(){
                    Data = sentImage,
                    IsMain = false,
                });
            }

        if (!ValidateProperties(EditedProduct, _productManager.GetCategoryById(EditedProduct.CategoryId), out var errorMessage))
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Title = "Hatalı Özellik Girişi", Message = errorMessage,TimeOut = 10000,
            });
        var offer = _sellerManager.updateOffer(CurrentSeller, EditedOffer, EditedOffer.ProductId);
        if(IsOfferEdited && !IsProductEdited)NotifyFavorers();

        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Yeni bir ürün oluşturuldu. Ürün sayfasına yönlendiriliyorsunuz.",
            Title = "İşlem Başarılı",
            Redirect = $"/Product?ProductId={offer.Product.Id}"
        });
    }

    private bool ValidateProperties(Entity.Product product, Category category, out string errorMessage) {
        bool ret = true;
        List<string> errors =[];
        foreach (var productCategoryProperty in product.CategoryProperties){
            Category.CategoryProperty? prop;
            if((prop = category.CategoryProperties.FirstOrDefault(p=>p.PropertyName.Equals(productCategoryProperty.Key)))==null)
                continue;
            if (prop.IsRequired && productCategoryProperty.Value == null || productCategoryProperty.Value == ""){
                errors.Add("" + prop.PropertyName + " alanı zorunludur.");
                ret = false;
            }
            else if (prop.IsNumber && !decimal.TryParse(productCategoryProperty.Value, out _)){
                errors.Add( ""+ prop.PropertyName + " alanı sayı olmalıdır.");
                ret = false;
            }
            else if (prop.EnumValues != null && prop.EnumValues.Length > 0 &&
                     !prop.EnumValues.Contains(productCategoryProperty.Value)){
                errors.Add("" + prop.PropertyName +
                                                            " alanı geçersiz değer içermektedir. Geçerli değerler: " +
                                                            string.Join(',', prop.EnumValues));
                ret = false;
            }
        }
        errorMessage = string.Join('\n', errors);
        return ret;
    }
}
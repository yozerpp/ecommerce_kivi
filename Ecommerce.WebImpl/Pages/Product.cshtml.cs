using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;
using Ecommerce.Notifications;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Ecommerce.WebImpl.Pages;

public class Product : BaseModel
{
    private readonly IProductManager _productManager;
    private readonly ISellerManager _sellerManager;
    private readonly IRepository<ProductOffer> _offerRepository;
    private readonly IRepository<Image> _imageRepository;
    private readonly IRepository<Entity.Product> _productRepository;
    private readonly INotificationService _notificationService;
    public readonly Dictionary<uint, Category> Categories;
    private readonly IRepository<ImageProduct> _imageProductRepository;
    public Product(IProductManager productManager, INotificationService notificationService, ISellerManager sellerManager, ICartManager cartManager, Dictionary<uint, Category> categories, IRepository<ProductOffer> offerRepository, IRepository<Image> productRepository, IRepository<Entity.Product> productRepository1, IRepository<ImageProduct> imageProductRepository) {
        _productManager = productManager;
        _notificationService = notificationService;
        _sellerManager = sellerManager;
        Categories = categories;
        _offerRepository = offerRepository;
        _imageRepository = productRepository;
        _productRepository = productRepository1;
        _imageProductRepository = imageProductRepository;
    }



    [BindProperty(SupportsGet = true)]
    public uint ProductId { get; set; }
    [BindProperty]
    public Entity.Product ViewedProduct { get; set; } 
    [BindProperty]
    public ICollection<ProductOffer> Offers { get; set; }
    [BindProperty]
    public string? SentImages { get; set; }
    public ICollection<uint>? Favorites { get; set; }
    public IActionResult OnGet() {
        var session = (Session)HttpContext.Items[nameof(Session)];
        Favorites = CurrentCustomer==null?[]:_productManager.GetFavorites(CurrentCustomer).Select(f=>f.ProductId).ToArray();
        var pr =_productManager.GetByIdWithAggregates(ProductId, false, false);
        if (pr == null) return new NotFoundObjectResult(new{ Message = "Product not found" });
        _productManager.VisitCategory(new SessionVisitedCategory(){SessionId = session.Id, CategoryId = pr.CategoryId});
        ViewedProduct = pr;
        Offers = _productManager.GetOffers(productId: ProductId);
        
        return new PageResult();
    }
    public PartialViewResult OnGetOffers([FromQuery] string sortColumn = nameof(ProductOffer.Stats.ReviewAverage), [FromQuery]bool sortDesc = true) {
        var offers = _productManager.GetOffers(ProductId, null, includeAggregates: true);
        Func<ProductOffer, decimal?> orderByExpression = sortColumn switch {
            nameof(ProductOffer.Price) => o => o.Price * o.Discount,
            nameof(ProductOffer.Stats.ReviewAverage) => o => o.Stats.ReviewAverage,
            _ => throw new ArgumentException("Geçersiz sıralama sütunu.", nameof(sortColumn))
        };
        return Partial("Shared/Product/"+nameof(_OfferListPartial), new _OfferListPartial(){
            Offers = sortDesc?offers.OrderByDescending(orderByExpression).ToList():offers.OrderBy(orderByExpression).ToList(),
            ViewingSellerId = CurrentSeller?.Id,
        });
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
    public void OnPostFavor() {
        if(CurrentCustomer==null) throw new UnauthorizedAccessException("Ürün favorilemek için giriş yapmış olmanız lazım.");
        _productManager.Favor(new ProductFavor(){
            CustomerId = CurrentCustomer.Id, ProductId = ProductId
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
    public string? DimensionString { get; set; }
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
        }).ToArray();
        _notificationService.SendBatchAsync(ns).Wait();
    }
    [HasRole(nameof(Staff), nameof(Entity.Seller))]
    public IActionResult OnPostEdit() {
        var EditedProduct = EditedOffer.Product;
        EditedOffer.Discount = (100m - EditedOffer.Discount) / 100m;
        if (!IsProductEdited){
            EditedOffer.Product = null;
            _sellerManager.updateOffer(CurrentSeller, EditedOffer, EditedOffer.ProductId);
            if(IsOfferEdited && !IsProductEdited)NotifyFavorers();
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = true,
                Message = "Teklif Güncellendi.",
                Title = "İşlem Başarılı",
                Redirect = $"/Product?ProductId={EditedOffer.ProductId}"
            });
        }
        if(DimensionString == null) return BadRequest(new {Message = "Boyut bilgisi eksik."});
        EditedProduct.Id = 0;   
        EditedProduct.Dimensions = Dimensions.Parse(DimensionString);
        EditedOffer.ProductId = default;
        EditedProduct.Active = true;
        var images = EditedProduct.Images;
        EditedProduct.Images = null!;
        for (int i = 0; i < images.Count; i++){
            var image = images[i];
             if (image.Image.Data == null!){
                 _imageProductRepository.Add(new ImageProduct(){
                     ImageId = image.ImageId,
                     Product = EditedProduct,
                     IsPrimary = image.IsPrimary
                 });
             } else if (image.Image.Data.Equals("-")){
                 images.Remove(image);
            }
            else{
                 image.Image.Id = image.ImageId = 0;
                 _imageProductRepository.Add(new ImageProduct(){
                     Product = EditedProduct,
                     Image = image.Image,
                     IsPrimary = image.IsPrimary
                 });
            }
        }
        _offerRepository.Add(EditedOffer);
        if(SentImages!=null)
            foreach (var sentImage in SentImages.Split(";;")){
                // throw new Exception();
                var image = new Image(){Data = sentImage};
                _imageProductRepository.Add(new ImageProduct(){
                    Product = EditedProduct, Image = image, IsPrimary = false
                });
            }
        if (!ValidateProperties(EditedProduct, _productManager.GetCategoryById(EditedProduct.CategoryId), out var errorMessage))
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Title = "Hatalı Özellik Girişi", Message = errorMessage,TimeOut = 10000,
            });
        _productRepository.Flush();
        if(IsOfferEdited && !IsProductEdited)NotifyFavorers();
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Yeni bir ürün oluşturuldu. Ürün sayfasına yönlendiriliyorsunuz.",
            Title = "İşlem Başarılı",
            Redirect = $"/Product?ProductId={EditedOffer.Product.Id}"
        });
    }

    private bool ValidateProperties(Entity.Product product, Category category, out string errorMessage) {
        bool ret = true;
        List<string> errors =[];
        foreach (var productCategoryProperty in product.CategoryProperties.Where(c=>c.CategoryPropertyId!=default)){
            Category.CategoryProperty? prop;
            if((prop = category.CategoryProperties.FirstOrDefault(p=>p.Id.Equals(productCategoryProperty.CategoryPropertyId)))==null)
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
                     !prop.EnumValues.Split(Category.CategoryProperty.EnumValuesSeparator).Contains(productCategoryProperty.Value)){
                errors.Add("" + prop.PropertyName +
                                                            " alanı geçersiz değer içermektedir. Geçerli değerler: " +prop.EnumValues);
                ret = false;
            }
        }
        errorMessage = string.Join('\n', errors);
        foreach (var prop in product.CategoryProperties.Where(c=>c.CategoryPropertyId==default)){
            
        }
        return ret;
    }
}
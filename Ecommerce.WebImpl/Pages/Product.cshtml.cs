using System.Linq.Expressions;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Default;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Ecommerce.WebImpl.Pages;

public class Product : BaseModel
{
    private readonly IProductManager _productManager;
    private readonly ISellerManager _sellerManager;
    private readonly IRepository<ProductOffer> _offerRepository;
    private readonly INotificationService _notificationService;
    public readonly IDictionary<uint,Category> Categories;
    private readonly IRepository<ImageProduct> _imageProductRepository;
    private readonly DbContext _dbContext;
    private readonly IRepository<ProductOption> _optionRepository;
    public Product(IProductManager productManager, INotificationService notificationService, ISellerManager sellerManager, ICartManager cartManager, IDictionary<uint,Category> categories, IRepository<ProductOffer> offerRepository, IRepository<Image> productRepository, IRepository<Entity.Product> productRepository1, IRepository<ImageProduct> imageProductRepository,[FromKeyedServices(nameof(DefaultDbContext))] DbContext dbContext, IRepository<ProductOption> optionRepository) {
        _productManager = productManager;
        _notificationService = notificationService;
        _sellerManager = sellerManager;
        Categories = categories;
        _offerRepository = offerRepository;
        _imageProductRepository = imageProductRepository;
        _dbContext = dbContext;
        _optionRepository = optionRepository;
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
    public CartItem? AddedItem { get; set; }
    public IActionResult OnGet() {
        Favorites = CurrentCustomer==null?null:_productManager.GetFavorites(CurrentCustomer).Select(f=>f.ProductId).ToArray();
        var pr =_productManager.GetByIdWithAggregates(ProductId, false, false);
        if (pr == null) return new NotFoundObjectResult(new{ Message = "Product not found" });
        var cid = CurrentSession.CartId;
        var q = _dbContext.Set<CartItem>().AsNoTracking().Where(i => i.ProductId == ProductId && i.CartId == cid).Select(i=>new CartItem(){Quantity = i.Quantity, SellerId = i.ProductId}).FirstOrDefault();
        AddedItem = q;
        _productManager.VisitCategory(new SessionVisitedCategory(){SessionId = CurrentSession.Id, CategoryId = pr.CategoryId});
        ViewedProduct = pr;
        Offers = _productManager.GetOffers(productId: ProductId);
        return new PageResult();
    }
    public PartialViewResult OnGetOffers([FromQuery] string sortColumn = nameof(ProductOffer.Stats.ReviewAverage), [FromQuery]bool sortDesc = true) {
        var offers = _productManager.GetOffers(ProductId, null, includeAggregates: true);
        var existingitems = _dbContext.Set<CartItem>().AsNoTracking().Where(i=>i.CartId == CurrentSession.CartId && i.ProductId == ProductId).Select(i=>new {i.SellerId, i.Quantity}).ToDictionary(i=>i.SellerId, i=>(uint?)i.Quantity);
        Func<ProductOffer, decimal?> orderByExpression = sortColumn switch {
            nameof(ProductOffer.Price) => o => o.Price * o.Discount,
            nameof(ProductOffer.Stats.ReviewAverage) => o => o.Stats.ReviewAverage,
            _ => throw new ArgumentException("Geçersiz sıralama sütunu.", nameof(sortColumn))
        };
        return Partial("Shared/Product/"+nameof(_OfferListPartial), new _OfferListPartial(){
            Offers = (sortDesc ? offers.OrderByDescending(orderByExpression) : offers.OrderBy(orderByExpression))
                .Select(o=>ValueTuple.Create(existingitems.GetValueOrDefault(o.SellerId), o)).ToArray(),
            ViewingSellerId = CurrentSeller?.Id,
        });
    }

    public IActionResult OnGetImages([FromQuery] bool json, [FromQuery] int page, [FromQuery] int pageSize) {
        var images = _imageProductRepository.Where(i => i.ProductId == ProductId, offset: (page - 1) * pageSize,
            limit: page * pageSize, includes:[[nameof(ImageProduct.Image)]]);
        if (json){
            if (images.Count == 0) return new NoContentResult();
            return new JsonResult(images);
        }
        return Partial("Shared/Product/"+nameof(_CarouselPartial), new _CarouselPartial(){
            Editable = false /*??*/, Images = images, FetchUrl = Url.Page($"/{nameof(Product)}?handler=images&{nameof(ProductId)}={ProductId}")
        });
    }

    public IActionResult OnGetOptions([FromQuery] uint SellerId, [FromQuery] uint? categoryId = null) {
         var opts = _offerRepository.FirstP(o=>o.Options, o=>o.ProductId==ProductId && o.SellerId==SellerId, includes:[[nameof(ProductOffer.Options), nameof(ProductOption.Property), nameof(ProductCategoryProperty.CategoryProperty)]]);
         if (opts.Count == 0) return new NoContentResult();
         var cid = CurrentSession.CartId;
         var selectedOptions = _dbContext.Set<CartItem>().AsNoTracking().Where(i=>i.CartId==cid && i.ProductId == ProductId && i.SellerId == SellerId).Select(i=>i.SelectedOptions).FirstOrDefault();
         var editable = CurrentSeller?.Id == SellerId;
         return Partial("Shared/Product/"+nameof(_ProductOptionsPartial), new _ProductOptionsPartial{
             Options = opts.Select(o=>ValueTuple.Create(selectedOptions?.Any(o1=>o1.Equals(o)) ??false, o)).ToArray(), Editable = editable,PropertyCandidates = editable&&categoryId.HasValue?Categories[categoryId.Value].CategoryProperties:[]
         });
    }

    public IActionResult OnPostDeleteOption([FromQuery] uint OptionId) {
        uint? id;
        if((id=CurrentSeller?.Id)==null) throw new UnauthorizedAccessException("Ürün seçeneğini silmek için satıcı olarak giriş yapmanız lazım.");
        var c = _optionRepository.Delete(o => o.Id == OptionId && o.ProductOffer.SellerId == id.Value);
        if (c==0){
            throw new UnauthorizedAccessException("Seçenek size ait değil ya da bulunmuyor.");
        }

        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Seçenek silindi.", Title = "İşlem Başarılı",
        });
    }    
    [BindProperty]
    public ProductOption ProductOption { get; set; }
    public IActionResult OnPostAddOption() {
        if(ProductOption.CategoryPropertyId==null && string.IsNullOrEmpty(ProductOption.Key))
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Message = "Seçenek anahtarı boş olamaz.", Title = "Geçersiz Giriş.", ErrorCause = _InfoPartial.ErrorCause_.Input
            });
        uint? id;
        if((id=CurrentSeller?.Id)==null) throw new UnauthorizedAccessException("Ürün seçeneğini silmek için satıcı olarak giriş yapmanız lazım.");
        ProductOption.SellerId = id.Value;
        try{
            _optionRepository.Add(ProductOption);
        }
        catch (Exception e){
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = true, Message = "Seçenek eklenemedi: " + e.Message, Title = "İşlem Başarısız",
            });
        }
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Seçenek silindi.", Title = "İşlem Başarılı",Redirect = "refresh"
        });
    }
    public IActionResult OnGetCategoryProperties([FromQuery] uint categoryId,[FromQuery] bool jsonResponse=false,[FromQuery] string? idPrefix = null,[FromQuery] bool isEditable=false) {
        var category = Categories[categoryId];
        if(category == null) throw new ArgumentException("Kategori bulunamadı.", nameof(categoryId));
        if (jsonResponse) return new JsonResult(category.CategoryProperties);
        if(idPrefix==null) throw new ArgumentNullException(nameof(idPrefix));
        return Partial("Shared/Product/"+nameof(_CategoryPropertiesPartial), new _CategoryPropertiesPartial(){
            InputNamePrefix = idPrefix,
            Properties = category.CategoryProperties.Select(p=>new ProductCategoryProperty(){
                CategoryProperty = p, CategoryPropertyId = p.Id
            }).ToArray(),Mode = _CategoryPropertiesPartial.DisplayMode.Filter
        });
    }
    public IActionResult OnPostFavor() {
        if(CurrentCustomer==null) throw new UnauthorizedAccessException("Ürün favorilemek için giriş yapmış olmanız lazım.");
        _productManager.SwitchFavor(new ProductFavor(){
            CustomerId = CurrentCustomer.Id, ProductId = ProductId
        });
        return new OkResult();
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
        }).ToArray();
        _notificationService.SendBatchAsync(ns).Wait();
    }
    [HasRole(nameof(Staff), nameof(Entity.Seller))]
    public IActionResult OnPostEdit() {
         var EditedProduct = EditedOffer.Product;
        EditedOffer.Discount = (100m - Math.Round(EditedOffer.Discount,2,MidpointRounding.AwayFromZero)) / 100m;
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
        EditedProduct.Id = 0;
        EditedOffer.ProductId = default;
        EditedProduct.Active = true;
        if (!ValidateProperties(EditedProduct, _productManager.GetCategoryById(EditedProduct.CategoryId), out var errorMessage))
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Title = "Hatalı Özellik Girişi", Message = errorMessage,TimeOut = 10000,
            });
        _dbContext.ChangeTracker.Clear();
        var images = EditedProduct.Images;
        EditedProduct.Images = null!;
        for (int i = 0; i < images.Count; i++){
            var imageProduct = images[i];
            imageProduct.ProductId = EditedOffer.ProductId;
            if (imageProduct.Image.Data == null!){
                _dbContext.Set<ImageProduct>().Add(new ImageProduct(){
                    ImageId = imageProduct.ImageId,
                    Product = EditedProduct,
                    IsPrimary = imageProduct.IsPrimary
                });
            } else if (imageProduct.Image.Data.Equals("-")){
                images.Remove(imageProduct);
            }
            else{
                imageProduct.Image.Id = imageProduct.ImageId = 0;
                _dbContext.Set<ImageProduct>().Add(new ImageProduct(){
                    Product = EditedProduct,
                    Image = imageProduct.Image,
                    IsPrimary = imageProduct.IsPrimary
                });
            }
        }
        if (SentImages != null){
            foreach (var sentImage in SentImages.Split(";;")){
                // throw new Exception();
                var image = new Image(){Data = sentImage};
                _dbContext.Set<ImageProduct>().Add(new ImageProduct(){
                    Product = EditedProduct, Image = image, IsPrimary = false
                });
            }
        }
        _dbContext.Set<ProductOffer>().Add(EditedOffer);
        _dbContext.SaveChanges();
        if(IsOfferEdited && !IsProductEdited)NotifyFavorers();
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Yeni bir ürün oluşturuldu. Ürün sayfasına yönlendiriliyorsunuz.",
            Title = "İşlem Başarılı",
            Redirect = $"/Product?ProductId={EditedOffer.Product.Id}"
        });
    }

    public void OnPostCreate() {
        
    }
    private bool ValidateProperties(Entity.Product product, Category category, out string errorMessage) {
        bool ret = true;
        List<string> errors =[];
        foreach (var productCategoryProperty in product.CategoryProperties.Where(c=>c.CategoryPropertyId!=default)){
            CategoryProperty? prop;
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
                     !prop.EnumValues.Split(CategoryProperty.EnumValuesSeparator).Contains(productCategoryProperty.Value)){
                errors.Add("" + prop.PropertyName +
                                                            " alanı geçersiz değer içermektedir. Geçerli değerler: " +prop.EnumValues);
                ret = false;
            }
        }
        errorMessage = string.Join('\n', errors);
        return ret;
    }
}
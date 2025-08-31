using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Views;
using Ecommerce.Notifications;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebImpl.Pages.Seller;

[Authorize(Policy = nameof(Entity.Seller))]
public class AddProduct : BaseModel
{
    private readonly ISellerManager _sellerManager;
    public readonly IDictionary<uint,Category> Categories;
    public readonly IRepository<Category> _categoryRepository;
    public AddProduct(INotificationService notificationService, IProductManager productManager, ISellerManager sellerManager, IDictionary<uint,Category> categories, IRepository<Category> categoryRepository): base(notificationService) {
        _sellerManager = sellerManager;
        Categories = categories;
        _categoryRepository = categoryRepository;
    }
    [BindProperty]
    public ProductOffer NewOffer { get; set; }
    public PartialViewResult OnPostAddExisting() {
        try{
            _sellerManager.ListOffer(CurrentSeller, NewOffer);
        }
        catch (ArgumentException e){
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Message = e.Message,
                Title = "Hata",
            });
        }
        // throw new Exception();
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true,
            Message = "Ürün ilanınız başarıyla oluşturuldu. Bu linkten ürün sayfasına ulaşabilirsiniz: " + Url.Page(nameof(Product), null, new {ProductId=NewOffer.ProductId}, Request.Scheme),
            Title = "İşlem Başarılı",
            TimeOut = 10000
        });
    }
    [BindProperty]
    public ICollection<IFormFile> productImages { get; set; }
    public PartialViewResult OnPostAddNew() {
        NewOffer.ProductId = 0;
        NewOffer.Discount = (100m - NewOffer.Discount) / 100m;
        NewOffer.Product.Images = productImages.Select(i => {
            var ms = new MemoryStream();
            i.CopyTo(ms);
            return new ImageProduct(){
                Product = NewOffer.Product,
                Image = new Image(){
                    Data = $"data:image/{i.FileName.Split('.').Last()};base64,{Convert.ToBase64String(ms.ToArray())}"
                },
            };
        }).ToArray();
        var craeted = _sellerManager.ListOffer(CurrentSeller, NewOffer);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true,
            Message = "Ürün ilanınız başarıyla oluşturuldu. Ürün sayfasına yönlendiriliyorsunuz.",
            Title = "İşlem Başarılı",
            Redirect = "/Product?ProductId=" + craeted.ProductId
        });
    }
    [BindProperty]
    public Category CreatedCategory { get; set; }
    
    public IActionResult OnPostCategory([FromQuery] bool getTemplate) {
        _categoryRepository.Clear();
        _categoryRepository.Add(CreatedCategory);
        _categoryRepository.Flush();
        Categories.Add(CreatedCategory.Id, CreatedCategory);
        if(!getTemplate)
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Kategori Başarıyla Oluşturuldu. Ürün detaylarını belirlemeye geçebilirsiniz.",
        });
        return OnGetTemplate(CreatedCategory.Id);
    }
    public PartialViewResult OnGetTemplate([FromQuery] uint categoryId) {
        var cat = Categories[categoryId];
        var props = cat.CategoryProperties.Select(s => new ProductCategoryProperty(){
            CategoryPropertyId = s.Id,
            CategoryProperty = s,
            ProductId = 0,
        }).ToArray();
        return Partial("Shared/Product/_ProductMainPartial", new _ProductMainPartial(){
            Categories =Categories,
            Creating = true,
            ViewingSellerId = CurrentSeller.Id,
            ViewedProduct = new Entity.Product(){
                Name = "Başlık",
                Description = "Açıklama",
                CategoryId = cat.Id,CategoryProperties = props, 
                Dimensions = new Dimensions(),
                Stats = new ProductStats(),
            }
        });
    }
}
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.WebImpl.Pages.Shared;
using Ecommerce.WebImpl.Pages.Shared.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Seller;

[Authorize(Policy = nameof(Entity.Seller))]
public class AddProduct : BaseModel
{
    private readonly IProductManager _productManager;
    private readonly ISellerManager _sellerManager;
    public AddProduct(IProductManager productManager, ISellerManager sellerManager) {
        _productManager = productManager;
        _sellerManager = sellerManager;
    }
    [BindProperty]
    public ICollection<Category> Categories { get; private set; }
    public void OnGet() {
        Categories = _productManager.GetCategories();
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
            Message = "Ürün ilanınız başarıyla oluşturuldu.",
            Title = "İşlem Başarılı",
            Redirect = "/Seller/Seller?SellerId=" + CurrentSeller.Id
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
    [BindProperty(SupportsGet = true)]
    public int CategoryId { get; set; }
}
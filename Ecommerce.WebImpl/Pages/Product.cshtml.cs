using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
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
    private readonly ICartManager _cartManager;
    public Product(IProductManager productManager, ISellerManager sellerManager, ICartManager cartManager) {
        _productManager = productManager;
        _sellerManager = sellerManager;
        _cartManager = cartManager;
    }



    [BindProperty(SupportsGet = true)]
    public uint ProductId { get; set; }
    public ProductWithAggregates ViewedProduct { get; set; } 
    [BindProperty]
    public string? SentImages { get; set; }
    public IActionResult OnGet() {
        var pr =_productManager.GetByIdWithAggregates(ProductId);
        if (pr == null) return new NotFoundObjectResult(new{ Message = "Product not found" });
        ViewedProduct = pr;
        return new PageResult();
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

            image.Product = EditedProduct;
        }
        if(SentImages!=null)
            foreach (var sentImage in SentImages.Split(";;")){
                // throw new Exception();
                EditedProduct.Images.Add(new Image(){
                    Data = sentImage,
                    Product = EditedProduct,
                    IsMain = false,
                });
            }

        if (!ValidateProperties(EditedProduct, _productManager.GetCategoryById(EditedProduct.CategoryId!.Value), out var errorMessage))
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Title = "Hatalı Özellik Girişi", Message = errorMessage,TimeOut = 10000,
            });
        var offer = _sellerManager.updateOffer(CurrentSeller, EditedOffer, EditedOffer.ProductId);
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
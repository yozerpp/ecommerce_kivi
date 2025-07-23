using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Account;

public class Register : PageModel   
{
    private readonly IUserManager _userManager;

    public Register(IUserManager userManager) {
        _userManager = userManager;
    }
    [BindProperty]
    public Customer FormCustomer { get; set; }
    public IActionResult OnPostUser() {
        _userManager.Register(FormCustomer);
        TempData["SuccessMessage"] = "Kayıt Başarılı! Giriş yapabilirsiniz.";
        return RedirectToPage("/Index");
    }
    [BindProperty]
    public Seller FormSeller { get; set; }
    public IActionResult OnPostSeller() {
        _userManager.Register(FormSeller);
        TempData["SuccessMessage"] = "Kayıt Başarılı! Giriş yapabilirsiniz.";
        return RedirectToPage("/Index");
    }
    public void OnGet() {
        return;
    }
}
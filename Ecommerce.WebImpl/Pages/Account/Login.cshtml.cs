using Ecommerce.Bl.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Account;

public class Login : PageModel
{
    private readonly IUserManager _userManager;
    public Login(IUserManager userManager) {
        _userManager = userManager;
    }
    public class LoginCredential
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    [BindProperty]
    public LoginCredential LoginCredentials { get; set; }
    public IActionResult OnPostUser() {
        _userManager.LoginCustomer(LoginCredentials.Email, LoginCredentials.Password, out var jwt);
        return new JsonResult(new{ Token = jwt });
    }
    public IActionResult OnPostSeller() {
        _userManager.LoginSeller(LoginCredentials.Email, LoginCredentials.Password, out var jwt);
        return new JsonResult(new{ Token = jwt });
    }
}
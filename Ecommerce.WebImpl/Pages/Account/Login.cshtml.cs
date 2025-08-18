using System.Text;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
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
    [BindProperty]
    public bool RememberMe { get; set; }
    public IActionResult OnPostCustomer() {
        _userManager.LoginCustomer(LoginCredentials.Email, LoginCredentials.Password, RememberMe, out var jwt);
        return HandleToken(jwt ,RememberMe);
    }

    public IActionResult OnPostLogout() {
        Response.Cookies.Delete(JwtBearerDefaults.AuthenticationScheme);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true,
            Title = "Çıkış Başarılı",
            Message = "Anasayfaya yönlendiriliyorsunuz.",
            Redirect = "/Index"
        });
    }
    private IActionResult HandleToken(string? jwt, bool rememberMe ) {
        if (jwt == null)
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Title = "Giriş Başarısız",
                Message = "Kullanıcı adı veya şifre hatalı",
            });
        Response.Cookies.Append(JwtBearerDefaults.AuthenticationScheme,jwt,new CookieOptions(){
            MaxAge = rememberMe?TimeSpan.FromDays(1):TimeSpan.FromHours(1) 
        }); 
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true,
            Title = "Giriş Başarılı",
            Message = "Anasayfaya yönlendiriliyorsunuz.",
            Redirect = "/Index"
        });
    }
    public IActionResult OnPostSeller() {
        _userManager.LoginSeller(LoginCredentials.Email, LoginCredentials.Password, RememberMe, out var jwt);
        return HandleToken(jwt, RememberMe);
    }

    public IActionResult OnPostStaff() {
        _userManager.LoginStaff(LoginCredentials.Email, LoginCredentials.Password, RememberMe, out var jwt);
        return HandleToken(jwt, RememberMe);
    }
}
using System.Security.Claims;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.WebImpl.Pages.Account.Oauth;
using Ecommerce.WebImpl.Pages.Shared;
using Google.Apis.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebImpl.Pages.Account;

[AllowAnonymous]
public class Register : PageModel
{
    private readonly IUserManager _userManager;
    private readonly IJwtManager _jwtManager;
    private readonly DbContext _dbContext;
    public Register(IUserManager userManager, IJwtManager jwtManager, DbContext dbContext) {
        _userManager = userManager;
        _jwtManager = jwtManager;
        _dbContext = dbContext;
    }

    [BindProperty]
    public Entity.Customer RegisteredCustomer { get; set; }
    [BindProperty]
    public Entity.Seller RegisteredSeller { get; set; }
    public Entity.User RegisteringUser { get; set; }
    [BindProperty(SupportsGet = true)] public string? Email { get; set; }
    [BindProperty(SupportsGet = true)] public string? Name { get; set; }
    [BindProperty(SupportsGet = true)] public string? FirstName { get; set; }
    [BindProperty(SupportsGet = true)] public string? LastName { get; set; }
    [BindProperty(SupportsGet = true)] public string? GoogleId { get; set; }
    [BindProperty(SupportsGet = true)] public string? ProfilePicture { get; set; }
    [BindProperty(SupportsGet = true)] public Entity.User.UserRole Role { get; set; }
    public void OnGet() {
        var user = RegisteringUser = Role switch{
            Entity.User.UserRole.Customer => new Entity.Customer(),
            Entity.User.UserRole.Seller => new Entity.Seller(),
            _ => throw new NotImplementedException()
        };
        user.Role = Role;
        user.Email = Email;
        user.FirstName = FirstName;
        user.LastName = LastName;
        user.GoogleId = GoogleId;
        user.ProfilePicture = ProfilePicture!=null?new Image(){ Data = ProfilePicture }:null;
        user.PhoneNumber = new PhoneNumber(){ Number = "" };
    }

    public async Task<IActionResult> OnGetOauth([FromQuery] string? continueUrl) {
        var authResult = await HttpContext.AuthenticateAsync(nameof(Google));
        if (!authResult.Succeeded){
            return RedirectToPage("/Account/Unauthorized",new {authResult.Failure?.GetBaseException().Message, Type=nameof(Oauth)});
        }
        if (authResult.Properties == null) return new BadRequestObjectResult("Geçersiz durum. Lütfen tekrar deneyin.");
        var claims = authResult.Principal.Claims.ToArray();
        var googleId = claims.First(c => c.Type == ClaimTypes.NameIdentifier);
        Entity.User? user;
        var email = claims.First(c => c.Type == ClaimTypes.Email);
        if ((user = await _dbContext.Set<Entity.User>().FirstOrDefaultAsync(u=>u.GoogleId == googleId.Value  )) != null){
            Response.Cookies.Append(JwtBearerDefaults.AuthenticationScheme, _jwtManager.Serialize(_jwtManager.CreateToken(user.Session)), new CookieOptions(){
                MaxAge = TimeSpan.FromHours(1),
            });
            return continueUrl!=null? Redirect(continueUrl) : RedirectToPage("/Index");
        }
        var type = authResult.Properties.Items[nameof(AuthProperties.AuthType)];
        if (type == nameof(AuthProperties.Type.Identity)){
            var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? firstName + " " + lastName;
            var profilePicture = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value;
            var role = Enum.Parse<Entity.User.UserRole>(authResult.Properties.Items[nameof(AuthProperties.Role)]);
            user = role switch{
                Entity.User.UserRole.Customer
                    => new Entity.Customer(),
                Entity.User.UserRole.Seller => new Entity.Seller(){Address = Address.Empty}, _ => throw new NotImplementedException()
            };
            user.Email = email.Value;
            user.GoogleId = googleId.Value;
            user.NormalizedEmail = email.Value.ToUpperInvariant();
            user.FirstName = firstName;
            user.LastName = lastName;
            user.GoogleId = googleId.Value;
            user.PhoneNumber = new PhoneNumber(){ Number = "" };
            user.ProfilePicture = profilePicture != null ? new Image(){ Data = profilePicture } : null;
            user.Role = role;
            RegisteringUser = user;
            return Page();
        }
        throw new NotImplementedException();
    }
    public IActionResult OnPost() {
        Entity.User u = Role switch{
            Entity.User.UserRole.Seller => RegisteredSeller,
            Entity.User.UserRole.Customer => RegisteredCustomer,
            _ => throw new NotImplementedException()
        };
        if (u.NormalizedEmail == null) u.NormalizedEmail = u.Email.ToUpperInvariant();
        _userManager.Register(Role, u);
        // if (GoogleId != null){
        var t = _jwtManager.CreateToken(u.Session);
        Console.WriteLine("OAuth Registered.-------------------------------");
        Response.Cookies.Delete(JwtBearerDefaults.AuthenticationScheme);
        Response.Cookies.Append(JwtBearerDefaults.AuthenticationScheme, _jwtManager.Serialize(t));
        // }
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Kaydınız yapıldı. Kullanıcı sayfanıza yönlendiriliyorsunuz.",
            Redirect = "/User?" + nameof(Pages.User.UserId) + '=' + u.Id,
        });
    }
}
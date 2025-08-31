using System.Security.Claims;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.WebImpl.Pages.Account.Oauth;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Authentication;
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
    }

    public async Task<IActionResult> OnGetOauth() {
        var authResult = await HttpContext.AuthenticateAsync(nameof(Google));
        if (!authResult.Succeeded){
            return RedirectToPage("/Account/Unauthorized",new {authResult.Failure?.GetBaseException().Message, Type=nameof(Oauth)});
        }
        if (authResult.Properties == null) return RedirectToPage("/Index");
        var type = authResult.Properties.Items[nameof(AuthProperties.AuthType)];
        if (type == nameof(AuthProperties.Type.Register)){
            var claims = authResult.Principal.Claims.ToArray();
            var email = claims.First(c => c.Type == ClaimTypes.Email);
            var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? firstName + " " + lastName;
            var googleId = claims.First(c => c.Type == ClaimTypes.NameIdentifier);
            var profilePicture = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value;
            var role = Enum.Parse<Entity.User.UserRole>(authResult.Properties.Items[nameof(AuthProperties.Role)]);
            Entity.User user = role switch{
                Entity.User.UserRole.Customer
                    => new Entity.Customer(),
                Entity.User.UserRole.Seller => new Entity.Seller(), _ => throw new NotImplementedException()
            };
            user.Email = email.Value;
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
        _userManager.Register(Role, u);
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Kaydınız yapıldı. Kullanıcı sayfanıza yönlendiriliyorsunuz.",
            Redirect = "/User?" + nameof(Pages.User.UserId) + '=' + u.Id,
        });
    }
    public void OnGetAuth() {
        
    }
}
using System.Security.Claims;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic;

namespace Ecommerce.WebImpl.Pages.Account.Oauth;
[AllowAnonymous, IgnoreAntiforgeryToken, ResponseCache( NoStore = true)]
public class Google : PageModel
{
    private readonly IRepository<User> _userRepository;
    private readonly IUserManager _userManager;
    public Google( IRepository<User> userRepository, IUserManager userManager) {
        _userRepository = userRepository;
        _userManager = userManager;
    }
    public async Task<IActionResult> OnGetAsync() {
        var authResult = await HttpContext.AuthenticateAsync(nameof(Google));
        if (!authResult.Succeeded){
            return RedirectToPage("/Account/Unauthorized",new {authResult.Failure?.GetBaseException().Message, Type=nameof(Oauth)});
        }
        if (authResult.Properties == null) return RedirectToPage("/Index");
        var type = authResult.Properties.Items[nameof(AuthProperties.AuthType)];
        if (type == nameof(AuthProperties.Type.Identity)){
            var claims = authResult.Principal.Claims.ToArray();
            var email = claims.First(c => c.Type == ClaimTypes.Email);
            var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? firstName + " " + lastName;
            var googleId = claims.First(c => c.Type == ClaimTypes.NameIdentifier);
            var profilePicture = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value;
            return RedirectToPage("/Account/Register?from=oauth", new{
                email, name,  firstName,   lastName,  googleId, profilePicture,
            });
        }
        else throw new NotImplementedException();
    }
}
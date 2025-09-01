using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace Ecommerce.WebImpl.Pages.Account;
[AllowAnonymous]
public class UnauthorizedModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Message { get; set; }
    public void OnGet()
    {
        
    }
}

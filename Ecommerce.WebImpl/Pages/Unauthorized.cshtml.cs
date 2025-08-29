using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages
{
    public class UnauthorizedModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? Message { get; set; }
        public void OnGet()
        {
            
        }
    }
}

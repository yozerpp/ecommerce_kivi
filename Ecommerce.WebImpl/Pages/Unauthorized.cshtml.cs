using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages
{
    public class UnauthorizedModel : PageModel
    {
        public string? Message { get; set; }
        
        public void OnGet(string? message = null)
        {
            Message = message;
        }
    }
}

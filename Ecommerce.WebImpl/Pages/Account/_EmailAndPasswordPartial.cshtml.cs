using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.Account
{
    public class _EmailAndPasswordPartial : PageModel
    {
        public bool ShowEmail { get; set; } = true;
        public bool ShowPassword { get; set; } = true;
        public bool ShowOldPassword { get; set; } = false;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string OldPassword { get; set; } = string.Empty;
    }
}

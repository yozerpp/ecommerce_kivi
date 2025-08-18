using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages;

[Authorize(Roles = nameof(User))]
public class User : BaseModel
{
    private readonly IJwtManager _jwtManager;

    public User(IJwtManager jwtManager) {
        _jwtManager = jwtManager;
    }

    public IActionResult OnPostChangeEmail() {
        var email = CurrentUser.Email;
    }
}
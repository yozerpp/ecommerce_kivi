using Ecommerce.Entity.Common;

namespace Ecommerce.WebImpl.Pages.Account.Oauth;

public class GoogleService
{
    private readonly HttpClient _client = new(){
        BaseAddress = new Uri("https://www.googleapis.com/auth"),
    };
    private readonly IHttpContextAccessor _context;
    public GoogleService(IHttpContextAccessor context) {
        _context = context;
    }

    // public async Task<Address> GetAddress() {
    //     var accessToken = _context.HttpContext.GetItem<string>("access_token");
    //     if (accessToken == null){
    //         
    //     }
    // }
}
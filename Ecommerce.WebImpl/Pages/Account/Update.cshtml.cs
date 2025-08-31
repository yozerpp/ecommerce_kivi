using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Notifications;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using MailKit;
using Microsoft.AspNetCore.Mvc;
using IMailService = Ecommerce.Mail.IMailService;

namespace Ecommerce.WebImpl.Pages.Account;

public class UpdateModel : BaseModel
{
    private readonly IMailService _mailService;
    private readonly IUserManager _userManager;
    private readonly IRepository<Entity.User> _userRepository;
    private readonly IJwtManager _jwtManager;
    public UpdateModel(INotificationService notificationService,IMailService mailService, IUserManager userManager, IRepository<Entity.User> userRepository, IJwtManager jwtManager) : base(notificationService){
        _mailService = mailService;
        _userManager = userManager;
        _userRepository = userRepository;
        _jwtManager = jwtManager;
    }

    public enum Type
    {
        Email, Password
    }
    [HasRole(nameof(Entity.User))]
    public IActionResult OnPost() {
        var id = CurrentUser.Id;
        var email = _userRepository.FirstP(u=>u.Email, u => u.Id == id);
        if(email == null) 
        _mailService.SendAsync(email, "E-posta Değiştirme Talebi",
            $"{(UpdateType == Type.Email?"E-Postanızı":"Şifrenizi")} Değiştirmek için Lütfen Aşağıdaki Linke Tıklayın:\n" +
            Url.Page("/Account/Update", new{
                Referer = _jwtManager.CreateAuthToken(CurrentSession.Id.ToString()+','+CurrentUser.Id.ToString(), TimeSpan.FromMinutes(10)),
                UpdateType = Enum.GetName(UpdateType)
            }));
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = (UpdateType==Type.Email?"E-Posta":"Şifre")+" Değiştirme Talebiniz Başarıyla Gönderildi",
            TimeOut = 5, Title = "Onay Epostası Gönderildi"
        });
    }
    [BindProperty(SupportsGet = true)]
    public Type UpdateType { get; set; }
    [BindProperty(SupportsGet = true)]
    public string Referer { get; set; }
    public IActionResult OnGet() {
        Authenticate();
        return Page();
    }
    [BindProperty]
    public string OldPassword { get; set; }
    [BindProperty]
    public string NewPassword { get; set; }
    [BindProperty]
    public string NewEmail { get; set; }

    private (string,string) Authenticate() {
        var s= _jwtManager.ReadAuthToken(Referer).Split(',');
        var sessionId = s[0];
        if(sessionId == null || CurrentSession.Id.ToString( )!= sessionId)
            throw new UnauthorizedAccessException("Geçersiz Oturum, lütfen tekrar deneyin.");
        return (sessionId, s[1]);
    }
    public IActionResult OnPostComplete() {
        var (sessionId, userId) = Authenticate();
        if (UpdateType == Type.Email && NewEmail?.Count(s => s is '@') != 1 && NewEmail?.Count(s => s is '.') < 1){
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false, Title = "Geçersiz Giriş",
                Message = "Girdiğiniz Yeni bilgiler geçersiz, lütfen yeniden deneyin.",
            });
        }
        switch (UpdateType){
            case Type.Email:
                _userManager.ChangeEmail(new Entity.Customer(){Id = uint.Parse(userId)}, OldPassword, NewEmail);
                break;
            case Type.Password:
                _userManager.ChangePassword(new Entity.Customer(){Id = uint.Parse(userId)} ,OldPassword, NewPassword);
                break;
        }
        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, Message = "Profilinize Yönlendiriliyorsunuz...",
            Title = UpdateType switch{
                Type.Email => "E-Posta",
                Type.Password => "Şifre", _ => throw new InvalidOperationException("Unknown Update Type: " + UpdateType)
            } + " Değiştirildi",
            Redirect = Url.Page(nameof(Pages.User), new {UserId = userId}),
        });
    }
}
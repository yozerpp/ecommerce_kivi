using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Mail;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages;

[Authorize(Roles = nameof(User))]
public class User : BaseModel
{
    private readonly IJwtManager _jwtManager;
    private readonly IMailService _mailService;
    private readonly IRepository<Entity.User> _userRepository;

    public User(IJwtManager jwtManager, IMailService mailService, IRepository<Entity.User> userRepository) {
        _jwtManager = jwtManager;
        _mailService = mailService;
        _userRepository = userRepository;
    }

    [BindProperty(SupportsGet = true, Name = "token")]
    public string Token { get; set; }

    [BindProperty]
    public string NewEmail { get; set; }

    [BindProperty]
    public string NewPassword { get; set; }

    [BindProperty]
    public string ConfirmPassword { get; set; }

    public enum ChangeType
    {
        Email,
        Password
    }

    public IActionResult OnPostRequestEmailChange([FromForm] string newEmail) {
        if (string.IsNullOrEmpty(newEmail) || newEmail == CurrentUser.Email) {
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Title = "Geçersiz Email",
                Message = "Lütfen geçerli ve farklı bir email adresi girin."
            });
        }

        var token = _jwtManager.CreateAuthToken($"email:{CurrentUser.Id}:{newEmail}", TimeSpan.FromMinutes(10));
        _mailService.SendAsync(newEmail, "Email Değişikliği Onayı",
            "Email adresinizi değiştirmek için lütfen aşağıdaki bağlantıya tıklayın:\n" + 
            Url.Page(nameof(User), "ConfirmEmailChange", new {token}, Request.Scheme));

        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, 
            Title = "Doğrulama Bekleniyor",
            Message = "Lütfen yeni e-posta adresinizi kontrol edin ve değişikliği onaylamak için bağlantıya tıklayın."
        });
    }

    public IActionResult OnPostRequestPasswordChange() {
        var token = _jwtManager.CreateAuthToken($"password:{CurrentUser.Id}", TimeSpan.FromMinutes(10));
        _mailService.SendAsync(CurrentUser.Email, "Şifre Değişikliği Onayı",
            "Şifrenizi değiştirmek için lütfen aşağıdaki bağlantıya tıklayın:\n" + 
            Url.Page(nameof(User), "ConfirmPasswordChange", new {token}, Request.Scheme));

        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, 
            Title = "Doğrulama Bekleniyor",
            Message = "Lütfen e-posta adresinizi kontrol edin ve şifre değişikliği için bağlantıya tıklayın."
        });
    }

    public IActionResult OnGetConfirmEmailChange() {
        var tokenValue = _jwtManager.ReadAuthToken(Token);
        if (tokenValue == null || !tokenValue.StartsWith("email:")) {
            return Partial(nameof(_InfoPartial), new _InfoPartial() {
                Success = false,
                Title = "Geçersiz Bağlantı",
                Message = "Lütfen e-posta adresinizi kontrol edin ve tekrar deneyin.",
                TimeOut = 10000,
            });
        }

        var parts = tokenValue.Split(':');
        if (parts.Length != 3 || !uint.TryParse(parts[1], out var userId) || userId != CurrentUser.Id) {
            return Partial(nameof(_InfoPartial), new _InfoPartial() {
                Success = false,
                Title = "Geçersiz Bağlantı",
                Message = "Bu bağlantı geçersiz veya süresi dolmuş.",
                TimeOut = 10000,
            });
        }

        var newEmail = parts[2];
        _userRepository.UpdateExpr([
            (u => u.Email, newEmail)
        ], u => u.Id == CurrentUser.Id);

        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, 
            Title = "Email Başarıyla Değiştirildi",
            Message = $"Email adresiniz {newEmail} olarak güncellendi.",
            TimeOut = 3000,
            Redirect = "refresh"
        });
    }

    public IActionResult OnGetConfirmPasswordChange() {
        var tokenValue = _jwtManager.ReadAuthToken(Token);
        if (tokenValue == null || !tokenValue.StartsWith("password:")) {
            return Partial(nameof(_InfoPartial), new _InfoPartial() {
                Success = false,
                Title = "Geçersiz Bağlantı",
                Message = "Lütfen e-posta adresinizi kontrol edin ve tekrar deneyin.",
                TimeOut = 10000,
            });
        }

        var parts = tokenValue.Split(':');
        if (parts.Length != 2 || !uint.TryParse(parts[1], out var userId) || userId != CurrentUser.Id) {
            return Partial(nameof(_InfoPartial), new _InfoPartial() {
                Success = false,
                Title = "Geçersiz Bağlantı",
                Message = "Bu bağlantı geçersiz veya süresi dolmuş.",
                TimeOut = 10000,
            });
        }

        // Return a form for password change
        ViewData["ShowPasswordForm"] = true;
        return Page();
    }

    public IActionResult OnPostChangePassword() {
        var tokenValue = _jwtManager.ReadAuthToken(Token);
        if (tokenValue == null || !tokenValue.StartsWith("password:")) {
            return Partial(nameof(_InfoPartial), new _InfoPartial() {
                Success = false,
                Title = "Geçersiz Bağlantı",
                Message = "Bu bağlantı geçersiz veya süresi dolmuş."
            });
        }

        if (string.IsNullOrEmpty(NewPassword) || NewPassword != ConfirmPassword) {
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Title = "Şifre Hatası",
                Message = "Şifreler eşleşmiyor veya boş."
            });
        }

        if (NewPassword.Length < 6) {
            return Partial(nameof(_InfoPartial), new _InfoPartial(){
                Success = false,
                Title = "Şifre Hatası",
                Message = "Şifre en az 6 karakter olmalıdır."
            });
        }

        var parts = tokenValue.Split(':');
        if (parts.Length != 2 || !uint.TryParse(parts[1], out var userId) || userId != CurrentUser.Id) {
            return Partial(nameof(_InfoPartial), new _InfoPartial() {
                Success = false,
                Title = "Geçersiz Bağlantı",
                Message = "Bu bağlantı geçersiz."
            });
        }

        // Hash the password (you should use proper password hashing)
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        
        _userRepository.UpdateExpr([
            (u => u.Password, hashedPassword)
        ], u => u.Id == CurrentUser.Id);

        return Partial(nameof(_InfoPartial), new _InfoPartial(){
            Success = true, 
            Title = "Şifre Başarıyla Değiştirildi",
            Message = "Şifreniz başarıyla güncellendi.",
            TimeOut = 3000,
            Redirect = "refresh"
        });
    }
}

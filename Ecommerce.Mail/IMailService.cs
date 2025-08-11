namespace Ecommerce.Mail;

public interface IMailService
{
    public Task SendAsync(string to, string subject, string body, BodyType bodyType = BodyType.Text, string? from = null, string replyTo = null);
    
}

public enum BodyType
{
    Html,
    Text
}

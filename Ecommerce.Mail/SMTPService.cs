using MailKit.Security;
using MimeKit;

namespace Ecommerce.Mail;
using MailKit.Net.Smtp;
public class SMTPService : IMailService
{
    private readonly string _server;
    private readonly int _port;
    private readonly string _mailAddress;
    private readonly string _password;
    public SMTPService(string server, int port, string mailAddress, string password) {
        _server = server;
        _port = port;
        _mailAddress = mailAddress;
        _password = password;
    }
    public async Task SendAsync(string to, string subject, string body,BodyType bodyType = BodyType.Text, string from = null, string replyTo = null) {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(from, _mailAddress));
        message.To.Add(new MailboxAddress(to, to));
        message.Subject = subject;
        var builder = new BodyBuilder
        {
            HtmlBody = bodyType == BodyType.Html? body : null,
            TextBody = bodyType == BodyType.Text? body : null
        };
        message.Body = builder.ToMessageBody();
        using var client = new SmtpClient();
        await client.ConnectAsync(_server, _port);
        await client.AuthenticateAsync(_mailAddress, _password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
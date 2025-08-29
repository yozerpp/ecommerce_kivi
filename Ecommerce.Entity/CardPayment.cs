namespace Ecommerce.Entity;

public class CardPayment : Payment
{
    public string ApiId { get; set; }
    public string Last4 { get; set; }
}
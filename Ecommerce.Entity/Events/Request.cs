namespace Ecommerce.Entity.Events;

// Notification => User is the Requestee.
public abstract class Request : Notification
{
    public bool IsApproved { get; set; }
    public DateTime? TimeAnswered { get; set; }
    public bool IsAnswerRead { get; set; }
    public uint? RequesterId { get; set; }
    public User? Requester { get; set; }

}
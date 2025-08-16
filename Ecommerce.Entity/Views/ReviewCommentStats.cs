namespace Ecommerce.Entity.Views;

public class ReviewCommentStats
{
    public ulong? CommentId { get; set; }
    public uint? ReplyCount { get; set; }
    public int? Votes { get; set; }
    public uint? VoteCount { get; set; }
}
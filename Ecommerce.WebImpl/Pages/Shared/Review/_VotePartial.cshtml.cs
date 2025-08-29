namespace Ecommerce.WebImpl.Pages.Shared.Review;

public class _VotePartial
{
    public required int CurrentUserVote{ get; init; }
    public required int Karma { get; init; }
    public required ulong? ReviewId { get; init; }
    public required ulong? CommentId { get; init; }
}
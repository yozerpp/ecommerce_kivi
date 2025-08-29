using Ecommerce.Entity;

namespace Ecommerce.WebImpl.Pages.Shared.Review;

public class CommentUserView(ReviewComment c)
{
    public ReviewComment ReviewComment { get; init; } = c;

    public required int CurrentUserVote { get; init; }
}
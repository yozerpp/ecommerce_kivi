using Ecommerce.Entity;

namespace Ecommerce.WebImpl.Pages.Shared.Review;

public class _CommentPartial(CommentUserView comment)
{
    public CommentUserView CommentUserView{get; init; } = comment;
    public required int NestLevel { get; init; } = 0;
}
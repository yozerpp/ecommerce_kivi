using Ecommerce.Entity;

namespace Ecommerce.WebImpl.Pages.Shared.Review;

public class ReviewUserView(ProductReview review)
{
    public ProductReview Review { get; init; } = review;
    public required int CurrentUserVote { get; init; } = 0;
}
using Ecommerce.Entity;

namespace Ecommerce.WebImpl.Pages.Shared.Review;

public class _CommentPartial : ReviewComment
{
    public int NestLevel { get; set; } = 0;
    private _CommentPartial(ReviewComment comment) {
        foreach (var property in typeof(ReviewComment).GetProperties()){
            property.SetValue(this, property.GetValue(comment));
        }
    }
    public _CommentPartial(ReviewComment comment, int nestLevel): this(comment) {
        NestLevel = nestLevel;
    }
}
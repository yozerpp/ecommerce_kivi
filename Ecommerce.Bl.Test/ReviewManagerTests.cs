using Ecommerce.Entity;
using Ecommerce.Entity.Common;
using Ecommerce.Entity.Projections;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework.Legacy;

namespace Ecommerce.Bl.Test;

public class ReviewManagerTests
{
    private ProductOffer _testOffer;
    private ProductReview _testReview;
    private User _reviewerUser;
    private User _commenterUser;
    private User _voterUser;

    [OneTimeSetUp]
    public void SetupUsersAndProducts()
    {
        // Register a main user (reviewer)
        _reviewerUser = new User
        {
            Email = "reviewer@example.com",
            PasswordHash = "password",
            FirstName = "Reviewer",
            LastName = "User",
            BillingAddress = new Address(),
            ShippingAddress = new Address(),
            PhoneNumber = new PhoneNumber()
        };
        _reviewerUser = TestContext._userManager.Register(_reviewerUser);
        TestContext._userRepository.Flush();

        // Register a commenter user
        _commenterUser = new User
        {
            Email = "commenter@example.com",
            PasswordHash = "password",
            FirstName = "Commenter",
            LastName = "User",
            BillingAddress = new Address(),
            ShippingAddress = new Address(),
            PhoneNumber = new PhoneNumber()
        };
        _commenterUser = TestContext._userManager.Register(_commenterUser);
        TestContext._userRepository.Flush();

        // Register a voter user
        _voterUser = new User
        {
            Email = "voter@example.com",
            PasswordHash = "password",
            FirstName = "Voter",
            LastName = "User",
            BillingAddress = new Address(),
            ShippingAddress = new Address(),
            PhoneNumber = new PhoneNumber()
        };
        _voterUser = TestContext._userManager.Register(_voterUser);
        TestContext._userRepository.Flush();


        // Login as the main user to create a product offer
        TestContext._userManager.LoginUser(_reviewerUser.Email, _reviewerUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        // Create a product and offer for testing reviews
        var category = TestContext._categoryRepository.First(_ => true);
        var product = new Product { Name = "Review Test Product", Description = "Description", CategoryId = category.Id };
        _testOffer = new ProductOffer
        {
            Product = product,
            Price = 100,
            Stock = 10,
            SellerId = TestContext.Seller.Id // Assuming a seller is already set up in TestContext
        };
        _testOffer = TestContext._sellerManager.ListProduct(_testOffer);
        TestContext._offerRepository.Flush();

        // Simulate a purchase by _reviewerUser for _testOffer
        TestContext._cartManager.newCart(_reviewerUser); // Set cart for reviewer
        TestContext._cartManager.Add(_testOffer, 1);
        TestContext._cartRepository.Flush();
        var payment = new Payment { TransactionId = "REVIEW_PURCHASE_" + Guid.NewGuid().ToString(), Amount = _testOffer.Price, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        TestContext._orderManager.CreateOrder(payment);
        TestContext._orderRepository.Flush();
    }

    [SetUp]
    public void LoginAsReviewer()
    {
        TestContext._userManager.LoginUser(_reviewerUser.Email, _reviewerUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);
    }

    [Test, Order(1)]
    public void LeaveReview_Success()
    {
        _testReview = new ProductReview
        {
            ProductId = _testOffer.ProductId,
            SellerId = _testOffer.SellerId,
            ReviewerId = _reviewerUser.Id, // ReviewerId is User.Id
            Rating = 5,
            Comment = "This is a great product!",
            CensorName = false
        };

        var leftReview = TestContext._reviewManager.LeaveReview(_testReview);
        TestContext._reviewRepository.Flush();

        Assert.That(leftReview, Is.Not.Null);
        Assert.That(leftReview.ProductId, Is.EqualTo(_testReview.ProductId));
        Assert.That(leftReview.SellerId, Is.EqualTo(_testReview.SellerId));
        Assert.That(leftReview.ReviewerId, Is.EqualTo(_testReview.ReviewerId));
        Assert.That(leftReview.Rating, Is.EqualTo(_testReview.Rating));
        Assert.That(leftReview.Comment, Is.EqualTo(_testReview.Comment));
        Assert.That(leftReview.HasBought, Is.True); // Should be true because we simulated a purchase
    }

    [Test, Order(2)]
    public void UpdateReview_Success()
    {
        _testReview.Rating = 4;
        _testReview.Comment = "It's good, but not perfect.";
        TestContext._reviewManager.UpdateReview(_testReview);
        TestContext._reviewRepository.Flush();

        var updatedReview = TestContext._reviewRepository.First(r =>
            r.ProductId == _testReview.ProductId &&
            r.SellerId == _testReview.SellerId &&
            r.ReviewerId == _testReview.ReviewerId);

        Assert.That(updatedReview, Is.Not.Null);
        Assert.That(updatedReview.Rating, Is.EqualTo(4));
        Assert.That(updatedReview.Comment, Is.EqualTo("It's good, but not perfect."));
    }

    [Test, Order(3)]
    public void CommentReview_Success()
    {
        TestContext._userManager.LoginUser(_commenterUser.Email, _commenterUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        var comment = new ReviewComment
        {
            ProductId = (uint)_testReview.ProductId,
            SellerId = (uint)_testReview.SellerId,
            ReviewerId = (uint)_testReview.ReviewerId,
            Comment = "I agree with this review!"
        };

        var addedComment = TestContext._reviewManager.CommentReview(comment);
        TestContext._reviewCommentRepository.Flush();

        Assert.That(addedComment, Is.Not.Null);
        Assert.That(addedComment.ProductId, Is.EqualTo(comment.ProductId));
        Assert.That(addedComment.CommenterId, Is.EqualTo(ContextHolder.Session.Id)); // CommenterId is Session.Id
        Assert.That(addedComment.Comment, Is.EqualTo(comment.Comment));
    }

    [Test, Order(4)]
    public void UpdateComment_Success()
    {
        TestContext._userManager.LoginUser(_commenterUser.Email, _commenterUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        var commentToUpdate = TestContext._reviewCommentRepository.First(c =>
            c.ProductId == _testReview.ProductId &&
            c.SellerId == _testReview.SellerId &&
            c.ReviewerId == _testReview.ReviewerId &&
            c.CommenterId == ContextHolder.Session.Id); // Use Session.Id for lookup

        Assert.That(commentToUpdate, Is.Not.Null);
        commentToUpdate.Comment = "Actually, I strongly agree!";
        TestContext._reviewManager.UpdateComment(commentToUpdate);
        TestContext._reviewCommentRepository.Flush();

        var updatedComment = TestContext._reviewCommentRepository.First(c =>
            c.ProductId == commentToUpdate.ProductId &&
            c.SellerId == commentToUpdate.SellerId &&
            c.ReviewerId == commentToUpdate.ReviewerId &&
            c.CommenterId == commentToUpdate.CommenterId);

        Assert.That(updatedComment, Is.Not.Null);
        Assert.That(updatedComment.Comment, Is.EqualTo("Actually, I strongly agree!"));
    }

    [Test, Order(5)]
    public void VoteReview_Upvote_Success()
    {
        TestContext._userManager.LoginUser(_voterUser.Email, _voterUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        var vote = new ReviewVote
        {
            ProductId = (uint)_testReview.ProductId,
            SellerId = (uint)_testReview.SellerId,
            ReviewerId = (uint)_testReview.ReviewerId,
            Up = true
        };

        var addedVote = TestContext._reviewManager.Vote(vote);
        TestContext._reviewVoteRepository.Flush();

        Assert.That(addedVote, Is.Not.Null);
        Assert.That(addedVote.VoterId, Is.EqualTo(ContextHolder.Session.Id)); // VoterId is Session.Id
        Assert.That(addedVote.Up, Is.True);
    }

    [Test, Order(6)]
    public void VoteReview_Downvote_Success()
    {
        // Login as a different voter to downvote
        var anotherVoterUser = new User
        {
            Email = "another_voter@example.com",
            PasswordHash = "password",
            FirstName = "Another",
            LastName = "Voter",
            BillingAddress = new Address(),
            ShippingAddress = new Address(),
            PhoneNumber = new PhoneNumber()
        };
        anotherVoterUser = TestContext._userManager.Register(anotherVoterUser);
        TestContext._userRepository.Flush();

        TestContext._userManager.LoginUser(anotherVoterUser.Email, anotherVoterUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        var vote = new ReviewVote
        {
            ProductId = (uint)_testReview.ProductId,
            SellerId = (uint)_testReview.SellerId,
            ReviewerId = (uint)_testReview.ReviewerId,
            Up = false
        };

        var addedVote = TestContext._reviewManager.Vote(vote);
        TestContext._reviewVoteRepository.Flush();

        Assert.That(addedVote, Is.Not.Null);
        Assert.That(addedVote.VoterId, Is.EqualTo(ContextHolder.Session.Id)); // VoterId is Session.Id
        Assert.That(addedVote.Up, Is.False);
    }

    [Test, Order(7)]
    public void GetReviewsWithAggregates_OwnVoteAndComments_Success()
    {
        // Login as the voter user to check OwnVote
        TestContext._userManager.LoginUser(_voterUser.Email, _voterUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        var reviews = TestContext._reviewManager.GetReviewsWithAggregates(
            (int)_testReview.ProductId, (int)_testReview.SellerId, true);

        Assert.That(reviews, Is.Not.Null);
        Assert.That(reviews.Count, Is.EqualTo(1));

        var reviewWithAggregates = reviews.First();

        // Assert Review Aggregates
        Assert.That(reviewWithAggregates.ProductId, Is.EqualTo(_testReview.ProductId));
        Assert.That(reviewWithAggregates.SellerId, Is.EqualTo(_testReview.SellerId));
        Assert.That(reviewWithAggregates.ReviewerId, Is.EqualTo(_testReview.ReviewerId));
        Assert.That(reviewWithAggregates.Rating, Is.EqualTo(_testReview.Rating));
        Assert.That(reviewWithAggregates.Comment, Is.EqualTo(_testReview.Comment));
        Assert.That(reviewWithAggregates.HasBought, Is.True);
        Assert.That(reviewWithAggregates.OwnVote, Is.EqualTo(1)); // _voterUser's session upvoted
        Assert.That(reviewWithAggregates.Votes, Is.EqualTo(0)); // 1 upvote - 1 downvote = 0
        Assert.That(reviewWithAggregates.CommentCount, Is.EqualTo(1));

        // Assert Reviewer Name Censor (if CensorName was true, but it's false in this test)
        Assert.That(reviewWithAggregates.Reviewer.FirstName, Is.EqualTo(_reviewerUser.FirstName));
        Assert.That(reviewWithAggregates.Reviewer.LastName, Is.EqualTo(_reviewerUser.LastName));

        // Assert Comments
        Assert.That(reviewWithAggregates.Comments.Count(), Is.EqualTo(1));
        var commentWithAggregates = reviewWithAggregates.Comments.First();

        Assert.That(commentWithAggregates.CommenterId, Is.EqualTo(_commenterUser.SessionId)); // CommenterId is Session.Id
        Assert.That(commentWithAggregates.Comment, Is.EqualTo("Actually, I strongly agree!"));
        Assert.That(commentWithAggregates.OwnVote, Is.EqualTo(0)); // _voterUser's session did not vote on this comment
        Assert.That(commentWithAggregates.Votes, Is.EqualTo(0)); // No votes on comment yet
    }

    [Test, Order(8)]
    public void UnVoteReview_Success()
    {
        TestContext._userManager.LoginUser(_voterUser.Email, _voterUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        var vote = new ReviewVote
        {
            ProductId = (uint)_testReview.ProductId,
            SellerId = (uint)_testReview.SellerId,
            ReviewerId = (uint)_testReview.ReviewerId,
            Up = true // This is the vote we want to remove
        };

        TestContext._reviewManager.UnVote(vote);
        TestContext._reviewVoteRepository.Flush();

        var reviews = TestContext._reviewManager.GetReviewsWithAggregates(
            (int)_testReview.ProductId, (int)_testReview.SellerId, false);
        var reviewWithAggregates = reviews.First();

        Assert.That(reviewWithAggregates.OwnVote, Is.EqualTo(0)); // _voterUser's vote removed
        Assert.That(reviewWithAggregates.Votes, Is.EqualTo(-1)); // Only the downvote remains
    }

    [Test, Order(9)]
    public void DeleteComment_Success()
    {
        TestContext._userManager.LoginUser(_commenterUser.Email, _commenterUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        var commentToDelete = TestContext._reviewCommentRepository.First(c =>
            c.ProductId == _testReview.ProductId &&
            c.SellerId == _testReview.SellerId &&
            c.ReviewerId == _testReview.ReviewerId &&
            c.CommenterId == ContextHolder.Session.Id); // Use Session.Id for lookup

        Assert.That(commentToDelete, Is.Not.Null);
        TestContext._reviewManager.DeleteComment(commentToDelete);
        TestContext._reviewCommentRepository.Flush();

        var reviews = TestContext._reviewManager.GetReviewsWithAggregates(
            (int)_testReview.ProductId, (int)_testReview.SellerId, true);
        var reviewWithAggregates = reviews.First();

        Assert.That(reviewWithAggregates.Comments.Count(), Is.EqualTo(0));
        Assert.That(reviewWithAggregates.CommentCount, Is.EqualTo(0));
    }

    [Test, Order(10)]
    public void DeleteReview_Success()
    {
        LoginAsReviewer(); // Login as the reviewer to delete their review

        TestContext._reviewManager.DeleteReview(_testReview);
        TestContext._reviewRepository.Flush();

        var reviews = TestContext._reviewManager.GetReviewsWithAggregates(
            (int)_testReview.ProductId, (int)_testReview.SellerId, false);

        Assert.That(reviews.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetReviewsWithAggregates_CensorName_Success()
    {
        // Login as the main user to create a product offer
        TestContext._userManager.LoginUser(_reviewerUser.Email, _reviewerUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        // Create a review with CensorName = true
        var censoredReview = new ProductReview
        {
            ProductId = _testOffer.ProductId,
            SellerId = _testOffer.SellerId,
            ReviewerId = _reviewerUser.Id,
            Rating = 3,
            Comment = "Censored review.",
            CensorName = true
        };
        TestContext._reviewManager.LeaveReview(censoredReview);
        TestContext._reviewRepository.Flush();

        // Get reviews and check censored name
        var reviews = TestContext._reviewManager.GetReviewsWithAggregates(
            (int)censoredReview.ProductId, (int)censoredReview.SellerId, false);

        Assert.That(reviews, Is.Not.Null);
        Assert.That(reviews.Count, Is.EqualTo(1));
        var reviewWithAggregates = reviews.First();

        Assert.That(reviewWithAggregates.Reviewer.FirstName, Is.EqualTo(_reviewerUser.FirstName[0] + "***"));
        Assert.That(reviewWithAggregates.Reviewer.LastName, Is.EqualTo(_reviewerUser.LastName[0] + "***"));

        // Clean up
        TestContext._reviewManager.DeleteReview(censoredReview);
        TestContext._reviewRepository.Flush();
    }
}

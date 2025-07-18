using Bogus;
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
    private Seller _testSeller; // Declare a test seller

    [OneTimeSetUp]
    public void SetupUsersAndProducts()
    {
        // Register a main user (reviewer)
        _reviewerUser = new User
        {
            Email = new Faker().Internet.Email(),
            PasswordHash = "password",
            FirstName = "Reviewer",
            LastName = "User",
            ShippingAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };
        ContextHolder.Session = null;
        _reviewerUser = TestContext._userManager.Register(_reviewerUser);
        // Register a commenter user
        _commenterUser = new User
        {
            Email = new Faker().Internet.Email(),
            PasswordHash = "password",
            FirstName = "Commenter",
            LastName = "User",
            ShippingAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };
        ContextHolder.Session = null;
        _commenterUser = TestContext._userManager.Register(_commenterUser);
        // Register a voter user
        _voterUser = new User
        {
            Email = new Faker().Internet.Email(),
            PasswordHash = "password",
            FirstName = "Voter",
            LastName = "User",
            ShippingAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };
        ContextHolder.Session = null;
        _voterUser = TestContext._userManager.Register(_voterUser);
        // Register and Login as a Seller for product listing
        _testSeller = new Seller
        {
            Email = new Faker().Internet.Email(),
            PasswordHash = "sellerpass",
            FirstName = "Test",
            LastName = "Seller",
            ShopName = "TestShop",
            ShopEmail = new Faker().Internet.Email(),
            ShopPhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"},
            ShopAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            ShippingAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}

        };
        ContextHolder.Session = null;
        _testSeller = (Seller)TestContext._userManager.Register(_testSeller);
        // Login as the seller to list the product offer
        TestContext._userManager.LoginSeller(_testSeller.Email, _testSeller.PasswordHash, out SecurityToken sellerToken);
        TestContext._jwtmanager.UnwrapToken(sellerToken, out var sellerUser, out var sellerSession);


        // Create a product and offer for testing reviews
        var category = TestContext._categoryRepository.First(_ => true);
        var product = new Product { Name = "Review Test Product", Description = "Description", CategoryId = category.Id };
        _testOffer = new ProductOffer
        {
            Product = product,
            Price = 100,
            Stock = 10,
            SellerId = _testSeller.Id // Use the registered seller's ID
        };
        _testOffer = TestContext._sellerManager.ListProduct(_testOffer);

        // Simulate a purchase by _reviewerUser for _testOffer
        // First, login as the reviewer user
        ContextHolder.Session = null;
        TestContext._userManager.LoginUser(_reviewerUser.Email, _reviewerUser.PasswordHash, out SecurityToken reviewerToken);
        TestContext._jwtmanager.UnwrapToken(reviewerToken, out var reviewerUser, out var reviewerSession);

        TestContext._cartManager.Add(_testOffer, 1);
        var payment = new Payment { TransactionId = "REVIEW_PURCHASE_" + Guid.NewGuid().ToString(), Amount = _testOffer.Price, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        TestContext._orderManager.CreateOrder();
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
            SessionId = _reviewerUser.Id, // ReviewSessionId is User.Id
            Rating = 5,
            Comment = "This is a great product!",
            CensorName = true
        };

        var leftReview = TestContext._reviewManager.LeaveReview(_testReview);

        Assert.That(leftReview, Is.Not.Null);
        Assert.That(leftReview.ProductId, Is.EqualTo(_testReview.ProductId));
        Assert.That(leftReview.SellerId, Is.EqualTo(_testReview.SellerId));
        Assert.That(leftReview.SessionId, Is.EqualTo(_testReview.SessionId));
        Assert.That(leftReview.Rating, Is.EqualTo(_testReview.Rating));
        Assert.That(leftReview.Comment, Is.EqualTo(_testReview.Comment));
        Assert.That(leftReview.HasBought, Is.True); // Should be true because we simulated a purchase
        TestContext._reviewRepository.Detach(leftReview);
        
        var reviewWithAggregates = TestContext._reviewManager.GetReviewWithAggregates(leftReview.ProductId, leftReview.SellerId, leftReview.SessionId,false);
        // assert that name is blurred
        Assert.That(reviewWithAggregates.Reviewer.FirstName, Is.EqualTo(_reviewerUser.FirstName[0] + "***"));
        Assert.That(reviewWithAggregates.Reviewer.LastName, Is.EqualTo(_reviewerUser.LastName[0] + "***"));
        _testReview = reviewWithAggregates;
    }

    [Test, Order(2)]
    public void UpdateReview_Success()
    {
        _testReview.Rating = 4;
        _testReview.Comment = "It's good, but not perfect.";
        TestContext._reviewManager.UpdateReview(_testReview);

        var updatedReview = TestContext._reviewRepository.First(r =>
            r.ProductId == _testReview.ProductId &&
            r.SellerId == _testReview.SellerId &&
            r.SessionId == _testReview.SessionId);

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
            ReviewSessionId = (uint)_testReview.SessionId,
            Comment = "I agree with this review!"
        };

        var addedComment = TestContext._reviewManager.CommentReview(comment);
        TestContext._reviewCommentRepository.Flush();

        Assert.That(addedComment, Is.Not.Null);
        Assert.That(addedComment.ProductId, Is.EqualTo(comment.ProductId));
        Assert.That(addedComment.SessionId, Is.EqualTo(ContextHolder.Session.Id)); // SessionId is Session.Id
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
            c.ReviewSessionId == _testReview.SessionId &&
            c.SessionId == ContextHolder.Session.Id); // Use Session.Id for lookup

        Assert.That(commentToUpdate, Is.Not.Null);
        commentToUpdate.Comment = "Actually, I strongly agree!";
        TestContext._reviewManager.UpdateComment(commentToUpdate);
        TestContext._reviewCommentRepository.Flush();

        var updatedComment = TestContext._reviewCommentRepository.First(c =>
            c.ProductId == commentToUpdate.ProductId &&
            c.SellerId == commentToUpdate.SellerId &&
            c.ReviewSessionId == commentToUpdate.ReviewSessionId &&
            c.SessionId == commentToUpdate.SessionId);

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
            ReviewSessionId = (uint)_testReview.SessionId,
            Up = true
        };

        var addedVote = TestContext._reviewManager.Vote(vote);
        TestContext._reviewVoteRepository.Flush();

        Assert.That(addedVote, Is.Not.Null);
        Assert.That(addedVote.VoterId, Is.EqualTo(ContextHolder.Session.Id)); // VoterId is Session.Id
        Assert.That(addedVote.Up, Is.True);
    }

    [Test, Order(6)]
    public void VoteReview_Downvote_Success() {
        ContextHolder.Session = null;
        // Login as a different voter to downvote
        var anotherVoterUser = new User
        {
            Email = new Faker().Internet.Email(),
            PasswordHash = "password",
            FirstName = "Another",
            LastName = "Voter",
            ShippingAddress = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };
        anotherVoterUser = TestContext._userManager.Register(anotherVoterUser);
        TestContext._userRepository.Flush();

        TestContext._userManager.LoginUser(anotherVoterUser.Email, anotherVoterUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        var vote = new ReviewVote
        {
            ProductId = (uint)_testReview.ProductId,
            SellerId = (uint)_testReview.SellerId,
            ReviewSessionId = (uint)_testReview.SessionId,
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

        var review = TestContext._reviewManager.GetReviewWithAggregates(
            _testReview.ProductId, _testReview.SellerId, _testReview.SessionId, true);

        Assert.That(review, Is.Not.Null);

        // Assert Review Aggregates
        Assert.That(review.ProductId, Is.EqualTo(_testReview.ProductId));
        Assert.That(review.SellerId, Is.EqualTo(_testReview.SellerId));
        Assert.That(review.SessionId, Is.EqualTo(_testReview.SessionId));
        Assert.That(review.Rating, Is.EqualTo(_testReview.Rating));
        Assert.That(review.Comment, Is.EqualTo(_testReview.Comment));
        Assert.That(review.HasBought, Is.True);
        Assert.That(review.OwnVote, Is.EqualTo(1)); // _voterUser's session upvoted
        Assert.That(review.Votes, Is.EqualTo(0)); // 1 upvote - 1 downvote = 0
        Assert.That(review.CommentCount, Is.EqualTo(1));
        
        // Assert Comments
        Assert.That(review.Comments.Count(), Is.EqualTo(1));
        var commentWithAggregates = review.Comments.First();

        Assert.That(commentWithAggregates.SessionId, Is.EqualTo(_commenterUser.SessionId)); // SessionId is Session.Id
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
            ReviewSessionId = (uint)_testReview.SessionId,
            Up = true // This is the vote we want to remove
        };

        TestContext._reviewManager.UnVote(vote);
        TestContext._reviewVoteRepository.Flush();

        var review = TestContext._reviewManager.GetReviewWithAggregates(
            _testReview.ProductId, _testReview.SellerId, _testReview.SessionId, false);

        Assert.That(review.OwnVote, Is.EqualTo(0)); // _voterUser's vote removed
        Assert.That(review.Votes, Is.EqualTo(-1)); // Only the downvote remains
    }

    [Test, Order(9)]
    public void DeleteComment_Success()
    {
        TestContext._userManager.LoginUser(_commenterUser.Email, _commenterUser.PasswordHash, out SecurityToken token);
        TestContext._jwtmanager.UnwrapToken(token, out var user, out var session);

        var commentToDelete = TestContext._reviewCommentRepository.First(c =>
            c.ProductId == _testReview.ProductId &&
            c.SellerId == _testReview.SellerId &&
            c.ReviewSessionId == _testReview.SessionId &&
            c.SessionId == ContextHolder.Session.Id); // Use Session.Id for lookup

        Assert.That(commentToDelete, Is.Not.Null);
        TestContext._reviewManager.DeleteComment(commentToDelete);
        TestContext._reviewCommentRepository.Flush();

        var reviewWithAggregates = TestContext._reviewManager.GetReviewWithAggregates(
            _testReview.ProductId, _testReview.SellerId, _testReview.SessionId, true);
        Assert.That(reviewWithAggregates.CommentCount, Is.EqualTo(0));
    }

    [Test, Order(10)]
    public void DeleteReview_Success()
    {
        LoginAsReviewer(); // Login as the reviewer to delete their review

        TestContext._reviewManager.DeleteReview(_testReview);
        TestContext._reviewRepository.Flush();

        var reviews = TestContext._reviewManager.GetReviewWithAggregates(
            _testReview.ProductId, _testReview.SellerId, _testReview.SessionId, false);

        Assert.That(reviews, Is.Null);
    }


}

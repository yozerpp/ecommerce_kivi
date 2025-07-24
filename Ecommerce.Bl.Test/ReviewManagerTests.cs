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
    private Customer _reviewerCustomer;
    private Customer _commenterCustomer;
    private Customer _voterCustomer;
    private Seller _testSeller; // Declare a test seller
    private Session _reviewerSession;
    private Session _commenterSession;
    private Session _voterSession;
    private Session _sellerSession;


    [OneTimeSetUp]
    public void SetupUsersAndProducts()
    {
        // Register a main user (reviewer)
        _reviewerCustomer = new Customer
        {
            NormalizedEmail = new Faker().Internet.Email(),
            PasswordHash = "password",
            FirstName = "Reviewer",
            LastName = "User",
            Address = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };
        _reviewerCustomer = (Customer)TestContext._userManager.Register(_reviewerCustomer);
        _reviewerSession = TestContext._userManager.LoginCustomer(_reviewerCustomer.NormalizedEmail, _reviewerCustomer.PasswordHash, out _);
        
        // Register a commenter user
        _commenterCustomer = new Customer
        {
            NormalizedEmail = new Faker().Internet.Email(),
            PasswordHash = "password",
            FirstName = "Commenter",
            LastName = "User",
            Address = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };
        _commenterCustomer = (Customer)TestContext._userManager.Register(_commenterCustomer);
        _commenterSession = TestContext._userManager.LoginCustomer(_commenterCustomer.NormalizedEmail, _commenterCustomer.PasswordHash, out _);
        
        // Register a voter user
        _voterCustomer = new Customer
        {
            NormalizedEmail = new Faker().Internet.Email(),
            PasswordHash = "password",
            FirstName = "Voter",
            LastName = "User",
            Address = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };
        _voterCustomer = (Customer)TestContext._userManager.Register(_voterCustomer);
        _voterSession = TestContext._userManager.LoginCustomer(_voterCustomer.NormalizedEmail, _voterCustomer.PasswordHash, out _);
        
        // Register and Login as a Seller for product listing
        _testSeller = new Seller
        {
            NormalizedEmail = new Faker().Internet.Email(),
            PasswordHash = "sellerpass",
            FirstName = "Test",
            LastName = "Seller",
            ShopName = "TestShop",
            Address = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}

        };
        _testSeller = (Seller)TestContext._userManager.Register(_testSeller);
        _sellerSession = TestContext._userManager.LoginSeller(_testSeller.NormalizedEmail, _testSeller.PasswordHash, out _);


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
        _testOffer = TestContext._sellerManager.ListProduct(_testSeller, _testOffer);

        // Simulate a purchase by _reviewerUser for _testOffer
        // First, ensure reviewer is logged in to create a cart item
        TestContext._cartManager.Add(_reviewerSession.Cart, _testOffer, 1);
        var payment = new Payment { TransactionId = "REVIEW_PURCHASE_" + Guid.NewGuid().ToString(), Amount = _testOffer.Price, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        TestContext._orderManager.CreateOrder(_reviewerSession);
    }

    [SetUp]
    public void LoginAsReviewer()
    {
        // Ensure _reviewerSession is the active session for tests that require it
        _reviewerSession = TestContext._userManager.LoginCustomer(_reviewerCustomer.NormalizedEmail, _reviewerCustomer.PasswordHash, out _);
    }

    [Test, Order(1)]
    public void LeaveReview_Success()
    {
        _testReview = new ProductReview
        {
            ProductId = _testOffer.ProductId,
            SellerId = _testOffer.SellerId,
            ReviewerId = _reviewerCustomer.Id, // ReviewerId is User.Id
            Rating = 5,
            Comment = "This is a great product!",
            CensorName = true
        };

        var leftReview = TestContext._reviewManager.LeaveReview(_reviewerSession, _testReview);

        Assert.That(leftReview, Is.Not.Null);
        Assert.That(leftReview.ProductId, Is.EqualTo(_testReview.ProductId));
        Assert.That(leftReview.SellerId, Is.EqualTo(_testReview.SellerId));
        Assert.That(leftReview.ReviewerId, Is.EqualTo(_testReview.ReviewerId));
        Assert.That(leftReview.Rating, Is.EqualTo(_testReview.Rating));
        Assert.That(leftReview.Comment, Is.EqualTo(_testReview.Comment));
        Assert.That(leftReview.HasBought, Is.True); // Should be true because we simulated a purchase
        TestContext._reviewRepository.Detach(leftReview);
        
        var reviewWithAggregates = TestContext._reviewManager.GetReviewWithAggregates(_reviewerCustomer, leftReview.ProductId, leftReview.SellerId, leftReview.ReviewerId,false);
        // assert that name is blurred
        Assert.That(reviewWithAggregates.Reviewer.FirstName, Is.EqualTo(_reviewerCustomer.FirstName[0] + "***"));
        Assert.That(reviewWithAggregates.Reviewer.LastName, Is.EqualTo(_reviewerCustomer.LastName[0] + "***"));
        _testReview = reviewWithAggregates;
    }

    [Test, Order(2)]
    public void UpdateReview_Success()
    {
        _testReview.Rating = 4;
        _testReview.Comment = "It's good, but not perfect.";
        TestContext._reviewManager.UpdateReview(_reviewerSession, _testReview);

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
        // Ensure commenter is logged in
        _commenterSession = TestContext._userManager.LoginCustomer(_commenterCustomer.NormalizedEmail, _commenterCustomer.PasswordHash, out _);

        var comment = new ReviewComment
        {
            ProductId = (uint)_testReview.ProductId,
            SellerId = (uint)_testReview.SellerId,
            ReviewId = _testReview.Id, // Use the actual review ID
            Comment = "I agree with this review!"
        };

        var addedComment = TestContext._reviewManager.CommentReview(_commenterSession, comment);
        TestContext._reviewCommentRepository.Flush();

        Assert.That(addedComment, Is.Not.Null);
        Assert.That(addedComment.ProductId, Is.EqualTo(comment.ProductId));
        Assert.That(addedComment.CommenterId, Is.EqualTo(_commenterSession.Id)); // CommenterId is Session.Id
        Assert.That(addedComment.Comment, Is.EqualTo(comment.Comment));
    }

    [Test, Order(4)]
    public void UpdateComment_Success()
    {
        // Ensure commenter is logged in
        _commenterSession = TestContext._userManager.LoginCustomer(_commenterCustomer.NormalizedEmail, _commenterCustomer.PasswordHash, out _);

        var commentToUpdate = TestContext._reviewCommentRepository.First(c =>
            c.ProductId == _testReview.ProductId &&
            c.SellerId == _testReview.SellerId &&
            c.ReviewId == _testReview.Id && // Use the actual review ID
            c.CommenterId == _commenterSession.Id); // Use Session.Id for lookup

        Assert.That(commentToUpdate, Is.Not.Null);
        commentToUpdate.Comment = "Actually, I strongly agree!";
        TestContext._reviewManager.UpdateComment(_commenterSession, commentToUpdate);
        TestContext._reviewCommentRepository.Flush();

        var updatedComment = TestContext._reviewCommentRepository.First(c =>
            c.ProductId == commentToUpdate.ProductId &&
            c.SellerId == commentToUpdate.SellerId &&
            c.ReviewId == commentToUpdate.ReviewId &&
            c.CommenterId == commentToUpdate.CommenterId);

        Assert.That(updatedComment, Is.Not.Null);
        Assert.That(updatedComment.Comment, Is.EqualTo("Actually, I strongly agree!"));
    }

    [Test, Order(5)]
    public void VoteReview_Upvote_Success()
    {
        // Ensure voter is logged in
        _voterSession = TestContext._userManager.LoginCustomer(_voterCustomer.NormalizedEmail, _voterCustomer.PasswordHash, out _);

        var vote = new ReviewVote
        {
            ReviewId = _testReview.Id, // Use the actual review ID
            Up = true
        };

        var addedVote = TestContext._reviewManager.Vote(_voterSession, vote);
        TestContext._reviewVoteRepository.Flush();

        Assert.That(addedVote, Is.Not.Null);
        Assert.That(addedVote.VoterId, Is.EqualTo(_voterSession.Id)); // VoterId is Session.Id
        Assert.That(addedVote.Up, Is.True);
    }

    [Test, Order(6)]
    public void VoteReview_Downvote_Success() {
        // Login as a different voter to downvote
        var anotherVoterUser = new Customer
        {
            NormalizedEmail = new Faker().Internet.Email(),
            PasswordHash = "password",
            FirstName = "Another",
            LastName = "Voter",
            Address = new Address(){City = "ads", State = "state", Neighborhood = "basd", Street = "casd", ZipCode = "asd"},
            PhoneNumber = new PhoneNumber(){CountryCode = 90,Number = "5551234567"}
        };
        anotherVoterUser = (Customer)TestContext._userManager.Register(anotherVoterUser);
        var anotherVoterSession = TestContext._userManager.LoginCustomer(anotherVoterUser.NormalizedEmail, anotherVoterUser.PasswordHash, out _);

        var vote = new ReviewVote
        {
            ReviewId = _testReview.Id, // Use the actual review ID
            Up = false
        };

        var addedVote = TestContext._reviewManager.Vote(anotherVoterSession, vote);
        TestContext._reviewVoteRepository.Flush();

        Assert.That(addedVote, Is.Not.Null);
        Assert.That(addedVote.VoterId, Is.EqualTo(anotherVoterSession.Id)); // VoterId is Session.Id
        Assert.That(addedVote.Up, Is.False);
    }

    [Test, Order(7)]
    public void GetReviewsWithAggregates_OwnVoteAndComments_Success()
    {
        // Login as the voter user to check OwnVote
        _voterSession = TestContext._userManager.LoginCustomer(_voterCustomer.NormalizedEmail, _voterCustomer.PasswordHash, out _);

        var review = TestContext._reviewManager.GetReviewWithAggregates(
            _voterCustomer, _testReview.ProductId, _testReview.SellerId, _testReview.ReviewerId, true);

        Assert.That(review, Is.Not.Null);

        // Assert Review Aggregates
        Assert.That(review.ProductId, Is.EqualTo(_testReview.ProductId));
        Assert.That(review.SellerId, Is.EqualTo(_testReview.SellerId));
        Assert.That(review.ReviewerId, Is.EqualTo(_testReview.ReviewerId));
        Assert.That(review.Rating, Is.EqualTo(_testReview.Rating));
        Assert.That(review.Comment, Is.EqualTo(_testReview.Comment));
        Assert.That(review.HasBought, Is.True);
        Assert.That(review.OwnVote, Is.EqualTo(1)); // _voterUser's session upvoted
        Assert.That(review.Votes, Is.EqualTo(0)); // 1 upvote - 1 downvote = 0
        Assert.That(review.CommentCount, Is.EqualTo(1));
        
        // Assert Comments
        Assert.That(review.Comments.Count(), Is.EqualTo(1));
        var commentWithAggregates = review.Comments.First();

        Assert.That(commentWithAggregates.CommenterId, Is.EqualTo(_commenterSession.Id)); // CommenterId is Session.Id
        Assert.That(commentWithAggregates.Comment, Is.EqualTo("Actually, I strongly agree!"));
        Assert.That(commentWithAggregates.OwnVote, Is.EqualTo(0)); // _voterUser's session did not vote on this comment
        Assert.That(commentWithAggregates.Votes, Is.EqualTo(0)); // No votes on comment yet
    }

    [Test, Order(8)]
    public void UnVoteReview_Success()
    {
        // Ensure voter is logged in
        _voterSession = TestContext._userManager.LoginCustomer(_voterCustomer.NormalizedEmail, _voterCustomer.PasswordHash, out _);

        var vote = new ReviewVote
        {
            ReviewId = _testReview.Id, // Use the actual review ID
            Up = true // This is the vote we want to remove
        };

        TestContext._reviewManager.UnVote(_voterSession, vote);
        TestContext._reviewVoteRepository.Flush();

        var review = TestContext._reviewManager.GetReviewWithAggregates(
            _voterCustomer, _testReview.ProductId, _testReview.SellerId, _testReview.ReviewerId, false);

        Assert.That(review.OwnVote, Is.EqualTo(0)); // _voterUser's vote removed
        Assert.That(review.Votes, Is.EqualTo(-1)); // Only the downvote remains
    }

    [Test, Order(9)]
    public void DeleteComment_Success()
    {
        // Ensure commenter is logged in
        _commenterSession = TestContext._userManager.LoginCustomer(_commenterCustomer.NormalizedEmail, _commenterCustomer.PasswordHash, out _);

        var commentToDelete = TestContext._reviewCommentRepository.First(c =>
            c.ProductId == _testReview.ProductId &&
            c.SellerId == _testReview.SellerId &&
            c.ReviewId == _testReview.Id && // Use the actual review ID
            c.CommenterId == _commenterSession.Id); // Use Session.Id for lookup

        Assert.That(commentToDelete, Is.Not.Null);
        TestContext._reviewManager.DeleteComment(_commenterSession, commentToDelete);
        TestContext._reviewCommentRepository.Flush();

        var reviewWithAggregates = TestContext._reviewManager.GetReviewWithAggregates(
            _reviewerCustomer, _testReview.ProductId, _testReview.SellerId, _testReview.ReviewerId, true);
        Assert.That(reviewWithAggregates.CommentCount, Is.EqualTo(0));
    }

    [Test, Order(10)]
    public void DeleteReview_Success()
    {
        LoginAsReviewer(); // Login as the reviewer to delete their review

        TestContext._reviewManager.DeleteReview(_reviewerSession, _testReview);
        TestContext._reviewRepository.Flush();

        var reviews = TestContext._reviewManager.GetReviewWithAggregates(
            _reviewerCustomer, _testReview.ProductId, _testReview.SellerId, _testReview.ReviewerId, false);

        Assert.That(reviews, Is.Null);
    }
}

using Ecommerce.Bl;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;
using Ecommerce.Entity.Projections;

namespace Ecommerce.DesktopImpl;

public partial class ReviewPage : UserControl, IPage
{
    private readonly Navigation _navigation;
    private readonly IReviewManager _reviewManager;
    public static ReviewPage Instance { get; private set; }
    public ReviewPage(Navigation navigation, IReviewManager reviewManager){
        _navigation = navigation;
        _reviewManager = reviewManager;
        InitializeComponent();
        Instance = this;
    }
    private uint Loaded;
    private int page = 1;
    public void LoadReviews(uint productId) {
        Loaded = productId;
    }
    public void Go() {
        var revTask = Task.Run(GetReviewsWithAggregates);
        revTask.ContinueWith(r => Invoke(() => LoadComments(r.Result)));
        revTask.Wait();
    }
    public void LoadComments(List<ReviewWithAggregates> reviews) {
        foreach (var rev in reviews){
            var t = $"{rev.Reviewer.FirstName} {rev.Reviewer.LastName}---Puan: {rev.Rating}\t\t\t"+ 
                    (rev.HasBought ? "     Kullanıcı bu ürünü satın aldı." : "") + $":        {rev.Comment}      {rev.Votes} kişi upladı.";
            var node = new TreeNode(t){
                Tag = rev
            };
            foreach (var comment in rev.Comments){
                var commentNode = new TreeNode($"{comment.Comment}      {comment.Votes} kişi upladı."){
                    Tag = comment
                };
                node.Nodes.Add(commentNode);
            }
            reviewView.Nodes.Add(node);
        }
        
    }

    private List<ReviewWithAggregates> GetReviewsWithAggregates() {
        return _reviewManager.GetReviewsWithAggregates(true, false, productId: Loaded, page: page );
    }

    public void Clear() {
        reviewView.Nodes.Clear();
    }
    private void sendBtn_Click(object sender, EventArgs e) {
        var rev = textBox1.Text;
        if (reviewView.SelectedNode == null)return;
        var selected = reviewView.SelectedNode;
        if (selected.Tag is ReviewComment) selected = selected.Parent;
        var review = (ProductReview)selected.Tag;
        var cmnt = _reviewManager.CommentReview(new ReviewComment(){
            Comment = rev, CommenterId = ContextHolder.Session.Id, ProductId = review.ProductId,
            ReviewerId = review.ReviewerId, SellerId = review.SellerId
        });
        Clear();
        var fetchTask = Task.Run(GetReviewsWithAggregates);
        fetchTask.ContinueWith(r=>Invoke(()=>LoadComments(r.Result))) ;
        fetchTask.Wait();
    }
}
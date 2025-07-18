using Ecommerce.Bl;
using Ecommerce.Bl.Interface;
using Ecommerce.Entity;

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
        LoadComments();
    }
    public void LoadComments() {
        var revs = _reviewManager.GetReviewsWithAggregates(true, false, productId: Loaded, page: page );
        foreach (var rev in revs){
            var t = $"{rev.Reviewer.FirstName} {rev.Reviewer.LastName}---Puan: {rev.Rating}\t\t\t"+ 
                    (rev.HasBought ? "     Kullanıcı bu ürünü satın aldı." : "") + $":        {rev.Comment}      {rev.Votes} kişi upladı.";
            var node = new TreeNode(t){
                Tag = rev
            };
            foreach (var comment in rev.Comments){
                var commentNode = new TreeNode($"\t{comment.Comment}\n\t{comment.Votes} lişi upladı."){
                    Tag = comment
                };
                node.Nodes.Add(commentNode);
            }
            reviewView.Nodes.Add(node);
        }
        
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
            Comment = rev, SessionId = ContextHolder.Session.Id, ProductId = review.ProductId,
            ReviewSessionId = review.SessionId, SellerId = review.SellerId
        });
        Clear();
        LoadReviews(Loaded);
    }
}
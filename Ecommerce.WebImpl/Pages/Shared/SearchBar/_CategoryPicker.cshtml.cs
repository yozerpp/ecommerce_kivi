using Ecommerce.Entity;

namespace Ecommerce.WebImpl.Pages.Shared.SearchBar;

public class _CategoryPicker(ICollection<Category> categories, string? targetElementId,string? inputElementId = null, string? displayElementId=null)
{
    public string? TargetElementId { get; set; } = targetElementId;
    public string? InputElementId { get; set; } = inputElementId;
    public string? DisplayElementId { get; set; } = displayElementId;
    public ICollection<Category> Categories { get; private init; } = TopologicSort(categories);
    // public int Level { get; } = categories.Max(c => ComputeLevel(c, []));
    private static ICollection<Category> TopologicSort(ICollection<Category> categories) {
        return categories.Where(p => p.ParentId == null).ToArray();
        var visited = new HashSet<Category>();
        var sorted = new List<Category>();
        foreach (var category in categories){
            Recurse(category);
        }
        return sorted.ToArray();
        void Recurse(Category category) {
            if (visited.Add(category)) sorted.Add(category);
            foreach (var categoryChild in category.Children){
                Recurse(categoryChild);
            }            
        }
    }
}
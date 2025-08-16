namespace Ecommerce.WebImpl.Pages.Shared.SearchBar;

public class _SliderPartial
{
    public string? InputName { get; init; }
    public decimal MaxValue { get; set; } = 10000000m;
    public decimal MinValue { get; set; }
    public string Step { get; set;}= "1";
}
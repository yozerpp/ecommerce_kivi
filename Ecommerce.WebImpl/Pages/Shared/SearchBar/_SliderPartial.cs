namespace Ecommerce.WebImpl.Pages.Shared.SearchBar;

public class _SliderPartial
{
    public string? InputNamePrefix { get; init; }
    public string? OnInput { get; init; }
    public decimal MaxValue { get; set; } = 10000000m;
    public decimal MinValue { get; set; }
    public string Step { get; set;}= "1";
}
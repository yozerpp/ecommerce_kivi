namespace Ecommerce.WebImpl.Pages.Shared;

public struct InputModel
{
    public string? PlaceHolder;
    public string Name;
    public string InputType = "text";
    public string? DefaultValue;
    public InputModel(string name, string inputType="text", string? defaultValue= null, string?  placeHolder= null) {
        PlaceHolder = placeHolder;
        Name = name;
        DefaultValue = defaultValue;
        InputType = inputType;
    }
}
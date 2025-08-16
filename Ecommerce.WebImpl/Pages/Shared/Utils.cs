using System.Drawing;
using Ecommerce.Entity.Common;
using Microsoft.AspNetCore.Html;

namespace Ecommerce.WebImpl.Pages.Shared;

public static class Utils
{
    public static string GetImageUrlOrDefault(string? imageUrl, bool isProductImage=true, string mimeType = "image/jpeg") {
        return !string.IsNullOrEmpty(imageUrl)
            ? (!imageUrl.StartsWith("data:") ? "data:" + mimeType + ";base64," + imageUrl : imageUrl)
            : (isProductImage?"default.jpg":"user-icon.svg");
    }
    public static string? GetRelativeTime(DateTime dateTime) {
        var now = DateTime.UtcNow + TimeSpan.FromHours(3);
        var dif =now>dateTime?(now - dateTime):(dateTime - now);
        int showed;
        if((showed = dif.Days/365)>0)
            return showed + " yıl" ;
        if((showed = dif.Days/30) > 0)
            return showed + " ay" ;
        if((showed = dif.Days) > 0)
            return showed + " gün" ;
        if((showed = dif.Hours) > 0)
            return showed + " saat" ;
        if ((showed = dif.Minutes) > 0)
            return showed + " dakika" ;
        return null;
    }
    public static IHtmlContent GenerateAssignAddressInputsFunction(int count, string inputPrefix = "addr", string elementPostfix="address") {
        var sb = new HtmlContentBuilder();
        for (int i = 0; i < count; i++){
            sb.AppendHtml($@"
            document.getElementById('{inputPrefix + i + '_'}{nameof(Address.City)}').value = document.getElementById('{nameof(Address.City)}_{elementPostfix + i}').innerText;
            document.getElementById('{inputPrefix + i + '_'}{nameof(Address.District)}').value = document.getElementById('{nameof(Address.District)}_{elementPostfix + i}').innerText;
            document.getElementById('{inputPrefix + i + '_'}{nameof(Address.Country)}').value = document.getElementById('{nameof(Address.Country)}_{elementPostfix + i}').innerText;
            document.getElementById('{inputPrefix + i + '_'}{nameof(Address.Line1)}').value = document.getElementById('{nameof(Address.Line1)}_{elementPostfix + i}').innerText;
            document.getElementById('{inputPrefix + i + '_'}{nameof(Address.ZipCode)}').value = document.getElementById('{nameof(Address.ZipCode)}_{elementPostfix + i}').innerText;
            document.getElementById('{inputPrefix + i + '_'}{nameof(Address.Line2)}').value = document.getElementById('{nameof(Address.Line2)}_{elementPostfix + i}').innerText;
            ");                    
        }
        return sb;
    }
}
namespace DiveMarketplace.ViewModels;

public class MapViewModel
{
    public string Name { get; set; } = "";
    public string? Location { get; set; }
    public string? GoogleLink => Uri.TryCreate(Location,UriKind.Absolute,out var uri) && uri.Scheme=="https" &&
        (uri.Host=="maps.app.goo.gl" || uri.Host=="goo.gl" || uri.Host=="google.com" || uri.Host.EndsWith(".google.com")) ? uri.AbsoluteUri : null;
    public string EmbedQuery
    {
        get {
            if(GoogleLink is null) return Location ?? "";
            var uri=new Uri(GoogleLink);
            var query=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            foreach(var key in new[]{"q","query"}) if(query.TryGetValue(key,out var value)) return value.ToString();
            var coordinates=System.Text.RegularExpressions.Regex.Match(uri.AbsolutePath,@"@(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)");
            if(coordinates.Success) return coordinates.Groups[1].Value+","+coordinates.Groups[2].Value;
            return Name; // Shared short links cannot be embedded directly; retain a working exact-location link.
        }
    }
    public static string? Resolve(params string?[] parts)
    {
        var value=string.Join(", ",parts.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x!.Trim()));
        return string.IsNullOrWhiteSpace(value)?null:value;
    }
}

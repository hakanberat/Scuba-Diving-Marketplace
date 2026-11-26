using System.ComponentModel.DataAnnotations;
namespace DiveMarketplace.ViewModels;
public sealed class WebAddressAttribute : ValidationAttribute
{
    public WebAddressAttribute() : base("Enter a valid website, for example https://example.com or www.example.com.") { }
    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : (value.Trim().Contains("://") ? value.Trim() : "https://" + value.Trim());
    public override bool IsValid(object? value) => value is null || string.IsNullOrWhiteSpace(value.ToString()) ||
        (Normalize(value.ToString())!.Length<=300 && Uri.TryCreate(Normalize(value.ToString()), UriKind.Absolute, out var uri) &&
         (uri.Scheme == "https" || uri.Scheme == "http") && uri.Host.Contains('.') && !uri.Host.EndsWith('.') && string.IsNullOrEmpty(uri.UserInfo));
}

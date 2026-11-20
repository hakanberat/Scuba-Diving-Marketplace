namespace DiveMarketplace.Models;

public enum PhotoTarget { Center, Activity }

public class Photo
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = "";
    public string? Caption { get; set; }
    public bool IsCover { get; set; }
}

using System.Text.Json;

namespace DiveMarketplace.Models;

public static class MarketplaceImages
{
    private const string Folder = "/images/discovery/";
    private static readonly object Gate = new();
    private static string? assignmentsPath;
    private static Dictionary<string, string> assignments = [];
    private static readonly (string File, string Tag)[] Photos =
    [
        ("dive-boat.jpg", "boat"),
        ("wreck-dive.jpg", "wreck"),
        ("scuba-diving.jpg", "course"),
        ("reef-dive.jpg", "reef"),
        ("dive-equipment.jpg", "equipment"),
        ("shore-dive.jpg", "center"),
        ("pexels-36741299.jpg", "boat"),
        ("pexels-8849678.jpg", "boat"),
        ("pexels-10391701.jpg", "wreck"),
        ("pexels-28959119.jpg", "wreck"),
        ("pexels-30620496.jpg", "reef"),
        ("pexels-32522831.jpg", "reef"),
        ("pexels-7987177.jpg", "reef"),
        ("pexels-4777692.jpg", "reef"),
        ("pexels-7001665.jpg", "reef"),
        ("pexels-31493483.jpg", "equipment"),
        ("pexels-7082808.jpg", "reef"),
        ("pexels-35269346.jpg", "reef"),
        ("pexels-20044217.jpg", "reef"),
        ("pexels-13211929.jpg", "reef"),
        ("pexels-4098819.jpg", "center"),
        ("pexels-13474746.jpg", "reef"),
        ("pexels-7970768.jpg", "reef"),
        ("pexels-8826359.jpg", "center"),
        ("pexels-3098935.jpg", "boat"),
        ("pexels-8826338.jpg", "center"),
        ("pexels-903974.jpg", "reef"),
        ("pexels-15288621.jpg", "reef"),
        ("pexels-15463393.jpg", "reef"),
        ("pexels-20051780.jpg", "course"),
        ("pexels-28274908.jpg", "equipment"),
        ("pexels-14565669.jpg", "reef"),
        ("pexels-8849641.jpg", "reef"),
        ("pexels-15763646.jpg", "reef"),
        ("pexels-18505543.jpg", "reef"),
    ];

    public static void Configure(string contentRoot)
    {
        lock (Gate)
        {
            assignmentsPath = Path.Combine(contentRoot, "App_Data", "representative-photos.json");
            assignments = File.Exists(assignmentsPath)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(assignmentsPath)) ?? []
                : [];
        }
    }

    public static string? ForActivity(int id, string? categoryName) => Assign($"activity:{id}", categoryName?.Trim().ToLowerInvariant() switch
    {
        "boat dive" or "dive tour" or "liveaboard" => "boat",
        "wreck dive" => "wreck",
        "equipment rental" or "technical dive" => "equipment",
        "shore dive" => "center",
        "try dive" or "scuba course" => "course",
        _ => "reef"
    });

    public static string? ForCenter(int id) => Assign($"center:{id}", "center");

    private static string? Assign(string key, string tag)
    {
        lock (Gate)
        {
            if (assignments.TryGetValue(key, out var existing)) return Folder + existing;
            var used = assignments.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var available = Photos.Where(p => !used.Contains(p.File)).ToList();
            // Never recycle another listing's photo when the catalog is exhausted.
            if (available.Count == 0) return null;
            var selected = available.FirstOrDefault(p => p.Tag == tag);
            if (selected.File is null) selected = available[0];
            if (assignmentsPath is not null)
            {
                var updated = new Dictionary<string, string>(assignments) { [key] = selected.File };
                Directory.CreateDirectory(Path.GetDirectoryName(assignmentsPath)!);
                var temporary = assignmentsPath + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temporary, assignmentsPath, overwrite: true);
            }
            assignments[key] = selected.File;
            return Folder + selected.File;
        }
    }
}

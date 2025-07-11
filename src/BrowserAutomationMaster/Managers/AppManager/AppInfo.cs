public class AppInfo
{
    public required string Name { get; set; } // Added required since an app won't be added if we don't know its common name
    public string? Version { get; set; }
    public string? Publisher { get; set; }
}
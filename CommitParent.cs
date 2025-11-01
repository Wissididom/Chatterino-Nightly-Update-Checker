using System.Text.Json.Serialization;

namespace ChatterinoNightlyUpdateChecker;

public class CommitParent
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    [JsonPropertyName("sha")]
    public string? Sha { get; set; }
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
}

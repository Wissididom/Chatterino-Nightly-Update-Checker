using System.Text.Json.Serialization;

namespace ChatterinoNightlyUpdateChecker;

public class CommitTree
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    [JsonPropertyName("sha")]
    public string? Sha { get; set; }
}

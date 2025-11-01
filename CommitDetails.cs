using System.Text.Json.Serialization;

namespace ChatterinoNightlyUpdateChecker;

public class CommitDetails
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    [JsonPropertyName("author")]
    public CommitAuthor? Author { get; set; }
    [JsonPropertyName("committer")]
    public CommitCommitter? Committer { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("tree")]
    public CommitTree? Tree { get; set; }
    [JsonPropertyName("commit")]
    public int CommentCount { get; set; }
    [JsonPropertyName("verification")]
    public CommitVerification? Verification { get; set; }
}

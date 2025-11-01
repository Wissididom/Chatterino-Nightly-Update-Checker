using System.Text.Json.Serialization;

namespace ChatterinoNightlyUpdateChecker;

public class CommitOverview
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    [JsonPropertyName("sha")]
    public string? Sha { get; set; }
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
    [JsonPropertyName("comments_url")]
    public string? CommentsUrl { get; set; }
    [JsonPropertyName("commit")]
    public CommitDetails? Commit { get; set; }
    [JsonPropertyName("author")]
    public GithubUser? Author { get; set; }
    [JsonPropertyName("committer")]
    public GithubUser? Committer { get; set; }
    [JsonPropertyName("parents")]
    public CommitParent[]? Parents { get; set; }
}

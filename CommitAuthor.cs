using System.Text.Json.Serialization;

namespace ChatterinoNightlyUpdateChecker;

public class CommitAuthor
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }
}

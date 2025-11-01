using System.Text.Json.Serialization;

namespace ChatterinoNightlyUpdateChecker;

public class CommitVerification
{
    [JsonPropertyName("verified")]
    public bool? Verified { get; set; }
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
    [JsonPropertyName("verified_at")]
    public DateTime? VerifiedAt { get; set; }
}

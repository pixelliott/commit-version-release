using System.Text.Json.Serialization;

public partial class GitHubCommitCreateRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("sha")]
    public string Sha { get; set; }
}

using System.Text.Json.Serialization;

public sealed class GitHubContent
{
    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("sha")]
    public string Sha { get; set; }
}

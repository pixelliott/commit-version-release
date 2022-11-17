using System.Text.Json.Serialization;

public sealed class Commit
{
    [JsonPropertyName("url")]
    public Uri Url { get; set; }

    [JsonPropertyName("author")]
    public CommitAuthor Author { get; set; }

    [JsonPropertyName("committer")]
    public CommitAuthor Committer { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}

using System.Text.Json.Serialization;

public partial class GitHubCommit
{
    [JsonPropertyName("url")]
    public Uri Url { get; set; }

    [JsonPropertyName("sha")]
    public string Sha { get; set; }

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; }

    [JsonPropertyName("html_url")]
    public Uri HtmlUrl { get; set; }

    [JsonPropertyName("comments_url")]
    public Uri CommentsUrl { get; set; }

    [JsonPropertyName("commit")]
    public Commit Commit { get; set; }

    [JsonPropertyName("author")]
    public GitHubCommitAuthor Author { get; set; }

    [JsonPropertyName("committer")]
    public GitHubCommitAuthor Committer { get; set; }
}

using System.Text.Json.Serialization;

public sealed class CommitAuthor
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; }
}

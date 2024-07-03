using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public sealed class GitHubService
{
    private readonly IHttpClientFactory HttpClientFactory;
    private readonly ActionInputs ActionInputs;

    public GitHubService(IHttpClientFactory httpClientFactory, ActionInputs actionInputs)
    {
        this.HttpClientFactory = httpClientFactory;
        this.ActionInputs = actionInputs;
    }

    private HttpClient GitHubHttpClient => HttpClientFactory.CreateClient("GitHub");

    public async Task<long?> GetDraftReleaseIdAsync()
    {
        var httpResponse = await GitHubHttpClient.GetAsync($"repos/{this.ActionInputs.Repo}/releases");

        if (httpResponse.IsSuccessStatusCode)
        {
            using var contentStream = await httpResponse.Content.ReadAsStreamAsync();

            var releases = await JsonSerializer.DeserializeAsync<IEnumerable<GitHubRelease>>(contentStream);

            return releases?.OrderByDescending(x => x.CreatedAt).Where(x => x.Draft).Select(x => (long?)x.Id).FirstOrDefault();
        }

        return null;
    }

    public async Task<long?> CreateDraftReleaseAsync(string version, string login)
    {
        var tagName = "v" + version;

        var httpResponse = await GitHubHttpClient.PostAsync($"repos/{this.ActionInputs.Repo}/releases", new StringContent(JsonSerializer.Serialize(new GitHubReleaseCreateRequest
        {
            Draft = true,
            TagName = tagName,
            Name = tagName,
            Body = $"Created at {DateTimeOffset.Now:dd/MM/yyyy HH:mm}\nContributors: @{login}\n[View all changes](https://github.com/{this.ActionInputs.Repo}/compare/{tagName}...master)\n\n## What's changed",
        }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })));

        if (httpResponse.IsSuccessStatusCode)
        {
            using var contentStream = await httpResponse.Content.ReadAsStreamAsync();

            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(contentStream);

            return release?.Id;
        }
        else
        {
            Console.WriteLine("Error" + await httpResponse.Content.ReadAsStringAsync());
        }

        return null;
    }

    public async Task<GitHubRelease?> GetReleaseAsync(long releaseId)
    {
        var httpResponse = await GitHubHttpClient.GetAsync($"repos/{this.ActionInputs.Repo}/releases/{releaseId}");

        if (httpResponse.IsSuccessStatusCode)
        {
            using var contentStream = await httpResponse.Content.ReadAsStreamAsync();

            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(contentStream);

            return release;
        }

        return null;
    }

    public async Task AmendDraftReleaseWithCommitMessage(long releaseId, GitHubCommit commit)
    {
        var release = await GetReleaseAsync(releaseId) ?? throw new NullReferenceException("Could not get release to update");

        if (string.IsNullOrWhiteSpace(release.Body))
            release.Body = string.Empty;

        var line = release.Body.Split('\n').FirstOrDefault(x => x.StartsWith("Contributors:"));

        if (line != null)
        {
            var currentContributors = line.Split(' ').Select(x => x.Trim()).ToList();
            if (!currentContributors?.Contains('@' + commit.Author.Login) ?? true)
            {
                currentContributors.Add('@' + commit.Author.Login);

                release.Body = release.Body.Replace(line, string.Join(" ", currentContributors));
            }
        }

        await GitHubHttpClient.PatchAsync($"repos/{this.ActionInputs.Repo}/releases/{releaseId}", new StringContent(JsonSerializer.Serialize(new GitHubReleaseCreateRequest
        {
            Body = $"{release.Body}\n\n[{commit.Commit.Committer.Date:dd/MM/yyyy HH:mm}] {commit.Sha[..7]}\n{(commit.Commit.Message.Length > 64 ? commit.Commit.Message.Substring(0, 61) + "..." : commit.Commit.Message)}",
            Draft = true,
            Name = release.Name,
            TagName = release.TagName,
            TargetCommitish = release.TargetCommitish,
            Prerelease = release.Prerelease,
        })));
    }

    public async Task<GitHubContent?> GetPackageJson()
    {
        var httpResponse = await GitHubHttpClient.GetAsync($"repos/{this.ActionInputs.Repo}/contents/{this.ActionInputs.PackageJsonPath}");

        if (httpResponse.IsSuccessStatusCode)
        {
            using var contentStream = await httpResponse.Content.ReadAsStreamAsync();

            var content = await JsonSerializer.DeserializeAsync<GitHubContent>(contentStream);

            return content;
        }

        return null;
    }

    public async Task UpdatePackageJson(GitHubContent content, JsonNode packageJson)
    {
        var httpResponse = await GitHubHttpClient.PutAsync($"repos/{this.ActionInputs.Repo}/contents/{this.ActionInputs.PackageJsonPath}", new StringContent(JsonSerializer.Serialize(new GitHubCommitCreateRequest
        {
            Message = $"Updated package.json version to {packageJson["version"]}",
            Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(packageJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }))),
            Sha = content.Sha
        }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })));

        if (!httpResponse.IsSuccessStatusCode)
            Console.WriteLine("Could not increment package.json version");
    }

    public async Task<GitHubCommit?> GetCommitAsync(string reference)
    {
        var httpResponse = await GitHubHttpClient.GetAsync($"repos/{this.ActionInputs.Repo}/commits/{reference}");

        if (httpResponse.IsSuccessStatusCode)
        {
            using var contentStream = await httpResponse.Content.ReadAsStreamAsync();

            var commit = await JsonSerializer.DeserializeAsync<GitHubCommit>(contentStream);

            return commit;
        }

        return null;
    }
}

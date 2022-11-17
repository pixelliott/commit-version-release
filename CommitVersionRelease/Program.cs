using System.Text;
using System.Text.Json.Nodes;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using static CommandLine.Parser;

var parser = Default.ParseArguments<ActionInputs>(() => new(), args);
parser.WithNotParsed(errors =>
{
    Console.WriteLine(string.Join(
        Environment.NewLine, errors.Select(error => error.ToString())));

    Environment.Exit(2);
});

await parser.WithParsedAsync(async options =>
{
    options.CommitReference = options.CommitReference.Trim();
    options.Repo = options.Repo.Trim();
    options.PackageJsonPath = options.PackageJsonPath.Trim();
    options.GitHubToken = options.GitHubToken.Trim();

    var sc = new ServiceCollection();
    sc.AddTransient<GitHubService>();
    sc.AddScoped(x => options);

    sc.AddHttpClient("GitHub", httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://api.github.com");

        httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/vnd.github.v3+json");
        httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "HttpRequestsSample");

        httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {options.GitHubToken}");
    });

    var sp = sc.BuildServiceProvider();

    await StartAsync(options, sp.GetRequiredService<GitHubService>());
});

static async Task StartAsync(ActionInputs inputs, GitHubService github)
{
    var draftReleaseId = await github.GetDraftReleaseIdAsync();
    var commit = await github.GetCommitAsync(inputs.CommitReference) ?? throw new NullReferenceException("Could not find commit");

    string title;
    string summary;

    if (!draftReleaseId.HasValue)
    {
        var content = await github.GetPackageJson();

        var packageJson = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(content.Content)));

        var versionParts = packageJson["version"]?.ToString().Split('.');

        packageJson["version"] = string.Join('.', versionParts.Where(x => x != versionParts.Last()).Concat(new string[] { (int.Parse(versionParts.Last()) + 1).ToString() }));

        draftReleaseId = await github.CreateDraftReleaseAsync(packageJson["version"]?.ToString(), commit.Author.Login) ?? throw new NullReferenceException("Could not find or create draft release");

        if (packageJson != null)
            await github.UpdatePackageJson(content, packageJson);

        title = $"Created draft release";
        summary = $"Created next draft release with v{packageJson["version"]}";
    }
    else
    {
        title = $"Amended draft release";
        summary = $"Updated draft release body with commit message";
    }

    await github.AmendDraftReleaseWithCommitMessage(draftReleaseId.Value, commit);

    Console.WriteLine($"::set-output name=summary-title::{title}");
    Console.WriteLine($"::set-output name=summary-details::{summary}");

    Environment.Exit(0);
}

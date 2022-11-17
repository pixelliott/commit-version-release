using CommandLine;

public class ActionInputs
{
    [Option('p', "package-json-path", Required = true, HelpText = "The package.json path. e.g. `package.json`.")]
    public string PackageJsonPath { get; set; }

    [Option('r', "repo", Required = true, HelpText = "The repository name in the format owner/repo. Assign from `github.repository`.")]
    public string Repo { get; set; }

    [Option('c', "commit-reference", Required = true, HelpText = "The reference to the commit. Assign from `github.event.head_commit.ref`")]
    public string CommitReference { get; set; }

    [Option('t', "github-token", Required = true, HelpText = "Github token. Assign from `github.token`.")]
    public string GitHubToken { get; set; }
}

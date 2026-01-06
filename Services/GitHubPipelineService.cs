using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ChangeRequesThor.Services;

public interface IGitHubPipelineService
{
    string GetReleaseId();
    string GetRepository();
    string GetBranch();
    bool IsProductionDeployment();
    string? GetJiraIssueKey();
}

public class GitHubPipelineService : IGitHubPipelineService
{
    private readonly ILogger<GitHubPipelineService> _logger;
    private static readonly Regex JiraKeyRegex = new(@"\b([A-Z]+-\d+)\b", RegexOptions.Compiled);

    public GitHubPipelineService(ILogger<GitHubPipelineService> logger)
    {
        _logger = logger;
    }

    public string GetReleaseId()
    {
        // Try different environment variables that GitHub Actions might set
        var releaseId = Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ??
                       Environment.GetEnvironmentVariable("GITHUB_SHA")?.Substring(0, 8) ??
                       Environment.GetEnvironmentVariable("RELEASE_ID") ??
                       Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ??
                       "UNKNOWN";

        _logger.LogDebug("Retrieved release ID: {ReleaseId}", releaseId);
        return releaseId;
    }

    public string GetRepository()
    {
        var repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY") ??
                        Environment.GetEnvironmentVariable("REPO_NAME") ??
                        "UNKNOWN";

        _logger.LogDebug("Retrieved repository: {Repository}", repository);
        return repository;
    }

    public string GetBranch()
    {
        var branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ??
                    Environment.GetEnvironmentVariable("GITHUB_HEAD_REF") ??
                    Environment.GetEnvironmentVariable("GITHUB_BASE_REF") ??
                    Environment.GetEnvironmentVariable("BRANCH_NAME") ??
                    "main";

        _logger.LogDebug("Retrieved branch: {Branch}", branch);
        return branch;
    }

    public bool IsProductionDeployment()
    {
        // Check if this is a production deployment
        var environment = Environment.GetEnvironmentVariable("DEPLOYMENT_ENVIRONMENT") ??
                         Environment.GetEnvironmentVariable("ENVIRONMENT") ??
                         Environment.GetEnvironmentVariable("DEPLOY_ENV") ??
                         string.Empty;

        var isProduction = environment.Equals("PRD", StringComparison.OrdinalIgnoreCase) ||
                          environment.Equals("PROD", StringComparison.OrdinalIgnoreCase) ||
                          environment.Equals("PRODUCTION", StringComparison.OrdinalIgnoreCase);

        // Also check if we're on main/master branch as a fallback
        if (!isProduction)
        {
            var branch = GetBranch();
            isProduction = branch.Equals("main", StringComparison.OrdinalIgnoreCase) ||
                          branch.Equals("master", StringComparison.OrdinalIgnoreCase);
        }

        _logger.LogDebug("Is production deployment: {IsProduction} (Environment: {Environment})", isProduction, environment);
        return isProduction;
    }

    public string? GetJiraIssueKey()
    {
        // Try to extract Jira issue key from various sources
        var sources = new[]
        {
            Environment.GetEnvironmentVariable("JIRA_ISSUE_KEY"),
            Environment.GetEnvironmentVariable("GITHUB_HEAD_REF"),
            Environment.GetEnvironmentVariable("GITHUB_REF_NAME"),
            Environment.GetEnvironmentVariable("BRANCH_NAME"),
            Environment.GetEnvironmentVariable("GITHUB_EVENT_HEAD_COMMIT_MESSAGE"),
            Environment.GetEnvironmentVariable("GITHUB_EVENT_PULL_REQUEST_TITLE")
        };

        foreach (var source in sources.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            var match = JiraKeyRegex.Match(source!);
            if (match.Success)
            {
                var jiraKey = match.Groups[1].Value;
                _logger.LogDebug("Found Jira issue key: {JiraKey} from source: {Source}", jiraKey, source);
                return jiraKey;
            }
        }

        _logger.LogDebug("No Jira issue key found in environment variables");
        return null;
    }
}
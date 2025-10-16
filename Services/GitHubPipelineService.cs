using Microsoft.Extensions.Logging;

namespace SolarWindsChangeCreator.Services;

public interface IGitHubPipelineService
{
    string GetReleaseId();
    string GetRepository();
    string GetBranch();
    bool IsProductionDeployment();
}

public class GitHubPipelineService : IGitHubPipelineService
{
    private readonly ILogger<GitHubPipelineService> _logger;

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
}
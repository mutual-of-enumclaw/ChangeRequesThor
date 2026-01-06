using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ChangeRequesThor.Services;

namespace ChangeRequesThor;

public class ChangeCreatorApplication : IHostedService
{
    private readonly IChangeAutomationService _solarWindsService;
    private readonly IGitHubPipelineService _pipelineService;
    private readonly ILogger<ChangeCreatorApplication> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public ChangeCreatorApplication(
        IChangeAutomationService solarWindsService,
        IGitHubPipelineService pipelineService,
        ILogger<ChangeCreatorApplication> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _solarWindsService = solarWindsService;
        _pipelineService = pipelineService;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Starting ChangeRequesThor Change Creator application with Jira integration");

            // Check if this is a production deployment
            if (!_pipelineService.IsProductionDeployment())
            {
                _logger.LogWarning("This is not a production deployment. Skipping change ticket creation.");
                _applicationLifetime.StopApplication();
                return;
            }

            // Get pipeline information
            var releaseId = _pipelineService.GetReleaseId();
            var repository = _pipelineService.GetRepository();
            var branch = _pipelineService.GetBranch();
            var jiraIssueKey = _pipelineService.GetJiraIssueKey();

            _logger.LogDebug("Pipeline Information - Release ID: {ReleaseId}, Repository: {Repository}, Branch: {Branch}, Jira Issue: {JiraIssueKey}", 
                releaseId, repository, branch, jiraIssueKey ?? "None");

            // Create the change ticket (now with Jira integration)
            var changeResponse = await _solarWindsService.CreateChangeTicketAsync(releaseId, repository, branch);

            if (changeResponse != null)
            {
                // Enhanced INFO level log including Jira information
                if (!string.IsNullOrWhiteSpace(jiraIssueKey))
                {
                    _logger.LogInformation("Release ID: {ReleaseId}, Jira Issue: {JiraIssueKey}, Created SolarWinds Change Ticket: {TicketNumber}", 
                        releaseId, jiraIssueKey, changeResponse.Number);
                }
                else
                {
                    _logger.LogInformation("Release ID: {ReleaseId}, Created SolarWinds Change Ticket: {TicketNumber} (No Jira issue detected)", 
                        releaseId, changeResponse.Number);
                }

                _logger.LogDebug("Change ticket created successfully. ID: {TicketId}, Number: {TicketNumber}, State: {State}", 
                    changeResponse.Id, changeResponse.Number, changeResponse.State);
            }
            else
            {
                _logger.LogError("Failed to create change ticket for release {ReleaseId}", releaseId);
                Environment.ExitCode = 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while creating the change ticket");
            Environment.ExitCode = 1;
        }
        finally
        {
            _applicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping ChangeRequesThor Change Creator application");
        return Task.CompletedTask;
    }
}
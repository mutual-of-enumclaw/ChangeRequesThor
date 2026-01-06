using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChangeRequesThor.Configuration;
using ChangeRequesThor.Models;
using System.Text;
using System.Text.Json;

namespace ChangeRequesThor.Services;

public interface IChangeAutomationService
{
    Task<ChangeResponse?> CreateChangeTicketAsync(string releaseId, string repository, string branch);
}

public class SolarWindsService : IChangeAutomationService
{
    private readonly HttpClient _httpClient;
    private readonly SolarWindsSettings _settings;
    private readonly ILogger<SolarWindsService> _logger;
    private readonly IJiraService _jiraService;
    private readonly IDescriptionEnhancementService _descriptionService;
    private readonly IGitHubPipelineService _pipelineService;

    public SolarWindsService(
        HttpClient httpClient, 
        IOptions<SolarWindsSettings> settings, 
        ILogger<SolarWindsService> logger,
        IJiraService jiraService,
        IDescriptionEnhancementService descriptionService,
        IGitHubPipelineService pipelineService)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _jiraService = jiraService;
        _descriptionService = descriptionService;
        _pipelineService = pipelineService;

        // Set up HTTP client headers
        _httpClient.BaseAddress = new Uri(_settings.ServiceUrl);
        _httpClient.DefaultRequestHeaders.Add("X-Samanage-Authorization", $"Bearer {_settings.ApiToken}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<ChangeResponse?> CreateChangeTicketAsync(string releaseId, string repository, string branch)
    {
        try
        {
            // Get Jira issue if available
            var jiraIssueKey = _pipelineService.GetJiraIssueKey();
            JiraIssue? jiraIssue = null;

            if (!string.IsNullOrWhiteSpace(jiraIssueKey))
            {
                _logger.LogDebug("Attempting to fetch Jira issue: {JiraIssueKey}", jiraIssueKey);
                jiraIssue = await _jiraService.GetIssueAsync(jiraIssueKey);
            }

            var changeRequest = await CreateChangeRequestAsync(releaseId, repository, branch, jiraIssue);
            var json = JsonSerializer.Serialize(changeRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });

            _logger.LogDebug("Creating change ticket with payload: {Payload}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/changes.json", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Change ticket creation response: {Response}", responseContent);

                var changeResponse = JsonSerializer.Deserialize<ChangeResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                _logger.LogDebug("Successfully created change ticket {TicketNumber} (ID: {TicketId}) with Jira integration: {HasJira}", 
                    changeResponse?.Number, changeResponse?.Id, jiraIssue != null);

                return changeResponse;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create change ticket. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating change ticket for release {ReleaseId}", releaseId);
            return null;
        }
    }

    private async Task<ChangeRequest> CreateChangeRequestAsync(string releaseId, string repository, string branch, JiraIssue? jiraIssue)
    {
        var now = DateTime.UtcNow;
        var plannedStart = now.AddMinutes(30); // Start in 30 minutes
        var plannedEnd = now.AddHours(2); // End in 2 hours

        // Build enhanced description
        var originalDescription = BuildBasicDescription(releaseId, repository, branch);
        var enhancedDescription = await _descriptionService.EnhanceDescriptionAsync(jiraIssue, originalDescription, releaseId, repository, branch);

        // Use Jira summary for change name if available
        var changeName = jiraIssue != null 
            ? $"Production Deployment - {jiraIssue.Key}: {jiraIssue.Fields.Summary}" 
            : $"Production Deployment - Release {releaseId}";

        return new ChangeRequest
        {
            Change = new Change
            {
                Name = changeName.Length > 100 ? changeName.Substring(0, 97) + "..." : changeName, // Truncate if too long
                Description = enhancedDescription,
                Requester = new Requester
                {
                    Email = _settings.DefaultRequestorEmail
                },
                Category = new Category
                {
                    Name = _settings.DefaultCategory
                },
                Subcategory = new Subcategory
                {
                    Name = _settings.DefaultSubcategory
                },
                Priority = _settings.DefaultPriority,
                PlanningFields = new PlanningFields
                {
                    PlannedStartDate = plannedStart.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    PlannedEndDate = plannedEnd.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            }
        };
    }

    private static string BuildBasicDescription(string releaseId, string repository, string branch)
    {
        var sb = new StringBuilder();
        sb.AppendLine("AUTOMATED PRODUCTION DEPLOYMENT");
        sb.AppendLine("================================");
        sb.AppendLine();
        sb.AppendLine($"Release ID: {releaseId}");
        sb.AppendLine($"Repository: {repository}");
        sb.AppendLine($"Branch: {branch}");
        sb.AppendLine($"Deployment Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("CHANGE DETAILS:");
        sb.AppendLine("- This is an automated production deployment initiated by the GitHub release pipeline");
        sb.AppendLine("- The deployment follows established CI/CD processes and has passed all required tests");
        sb.AppendLine("- This change is part of the regular software release cycle");
        sb.AppendLine();
        sb.AppendLine("ROLLBACK PLAN:");
        sb.AppendLine("- If issues are encountered, the previous version can be redeployed using the established rollback procedures");
        sb.AppendLine("- Application monitoring will be actively monitored for any anomalies post-deployment");
        
        return sb.ToString();
    }
}
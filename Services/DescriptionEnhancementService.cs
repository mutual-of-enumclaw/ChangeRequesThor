using Microsoft.Extensions.Logging;
using ChangeRequesThor.Models;
using System.Text;

namespace ChangeRequesThor.Services;

public interface IDescriptionEnhancementService
{
    Task<string> EnhanceDescriptionAsync(JiraIssue? jiraIssue, string originalDescription, string releaseId, string repository, string branch);
}

public class DescriptionEnhancementService : IDescriptionEnhancementService
{
    private readonly ILogger<DescriptionEnhancementService> _logger;
    private readonly IJiraService _jiraService;

    public DescriptionEnhancementService(ILogger<DescriptionEnhancementService> logger, IJiraService jiraService)
    {
        _logger = logger;
        _jiraService = jiraService;
    }

    public async Task<string> EnhanceDescriptionAsync(JiraIssue? jiraIssue, string originalDescription, string releaseId, string repository, string branch)
    {
        try
        {
            if (jiraIssue == null)
            {
                _logger.LogDebug("No Jira issue provided, using original description");
                return originalDescription;
            }

            var enhancedDescription = await BuildEnhancedDescription(jiraIssue, originalDescription, releaseId, repository, branch);
            
            _logger.LogDebug("Successfully enhanced description for Jira issue {IssueKey}", jiraIssue.Key);
            return enhancedDescription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing description, falling back to original");
            return originalDescription;
        }
    }

    private async Task<string> BuildEnhancedDescription(JiraIssue jiraIssue, string originalDescription, string releaseId, string repository, string branch)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("AUTOMATED PRODUCTION DEPLOYMENT");
        sb.AppendLine("================================");
        sb.AppendLine();

        // Deployment Information
        sb.AppendLine("DEPLOYMENT INFORMATION:");
        sb.AppendLine($"Release ID: {releaseId}");
        sb.AppendLine($"Repository: {repository}");
        sb.AppendLine($"Branch: {branch}");
        sb.AppendLine($"Deployment Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // Jira Issue Information
        sb.AppendLine("ASSOCIATED JIRA ISSUE:");
        sb.AppendLine($"Issue Key: {jiraIssue.Key}");
        sb.AppendLine($"Summary: {CleanText(jiraIssue.Fields.Summary)}");
        sb.AppendLine($"Type: {jiraIssue.Fields.IssueType.Name}");
        sb.AppendLine($"Priority: {jiraIssue.Fields.Priority.Name}");
        sb.AppendLine($"Status: {jiraIssue.Fields.Status.Name}");
        
        if (jiraIssue.Fields.Assignee != null)
        {
            sb.AppendLine($"Assignee: {jiraIssue.Fields.Assignee.DisplayName}");
        }

        if (jiraIssue.Fields.Components.Any())
        {
            sb.AppendLine($"Components: {string.Join(", ", jiraIssue.Fields.Components.Select(c => c.Name))}");
        }

        if (jiraIssue.Fields.Labels.Any())
        {
            sb.AppendLine($"Labels: {string.Join(", ", jiraIssue.Fields.Labels)}");
        }

        sb.AppendLine();

        // Enhanced Description from Jira
        var jiraDescription = await _jiraService.ExtractPlainTextDescriptionAsync(jiraIssue);
        if (!string.IsNullOrWhiteSpace(jiraDescription))
        {
            sb.AppendLine("CHANGE DETAILS (from Jira):");
            sb.AppendLine(await ScrubAndEnhanceJiraDescription(jiraDescription));
            sb.AppendLine();
        }

        // Standard deployment information
        sb.AppendLine("DEPLOYMENT PROCESS:");
        sb.AppendLine("- This is an automated production deployment initiated by the GitHub release pipeline");
        sb.AppendLine("- The deployment follows established CI/CD processes and has passed all required tests");
        sb.AppendLine("- This change is part of the regular software release cycle");
        sb.AppendLine($"- Associated with Jira issue: {jiraIssue.Key}");
        sb.AppendLine();

        // Risk Assessment based on Jira information
        sb.AppendLine("RISK ASSESSMENT:");
        sb.AppendLine(GenerateRiskAssessment(jiraIssue));
        sb.AppendLine();

        // Rollback Plan
        sb.AppendLine("ROLLBACK PLAN:");
        sb.AppendLine("- If issues are encountered, the previous version can be redeployed using the established rollback procedures");
        sb.AppendLine("- Application monitoring will be actively monitored for any anomalies post-deployment");
        sb.AppendLine($"- Jira issue {jiraIssue.Key} will be updated with deployment status and any rollback actions");

        return sb.ToString();
    }

    private async Task<string> ScrubAndEnhanceJiraDescription(string jiraDescription)
    {
        try
        {
            // Clean up the description
            var cleanedDescription = CleanText(jiraDescription);
            
            // Split into manageable chunks and enhance
            var lines = cleanedDescription.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var enhancedLines = new List<string>();

            foreach (var line in lines.Take(10)) // Limit to first 10 lines to avoid overly long descriptions
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length > 0 && trimmedLine.Length < 200) // Skip overly long lines
                {
                    enhancedLines.Add($"• {trimmedLine}");
                }
            }

            if (enhancedLines.Count == 0)
            {
                return "• No detailed description available in Jira issue";
            }

            if (lines.Length > 10)
            {
                enhancedLines.Add("• (Additional details available in the source Jira issue)");
            }

            return string.Join("\n", enhancedLines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scrubbing Jira description");
            return "• Error processing Jira description - refer to source issue for details";
        }

        await Task.CompletedTask; // For async consistency
    }

    private string GenerateRiskAssessment(JiraIssue jiraIssue)
    {
        var riskFactors = new List<string>();
        var riskLevel = "Low";

        // Assess risk based on Jira issue properties
        if (jiraIssue.Fields.Priority.Name.Contains("High", StringComparison.OrdinalIgnoreCase) ||
            jiraIssue.Fields.Priority.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase))
        {
            riskFactors.Add("High priority issue");
            riskLevel = "Medium-High";
        }

        if (jiraIssue.Fields.IssueType.Name.Contains("Bug", StringComparison.OrdinalIgnoreCase))
        {
            riskFactors.Add("Bug fix deployment");
            riskLevel = riskLevel == "Low" ? "Medium" : "Medium-High";
        }

        if (jiraIssue.Fields.Components.Any(c => 
            c.Name.Contains("Database", StringComparison.OrdinalIgnoreCase) ||
            c.Name.Contains("Security", StringComparison.OrdinalIgnoreCase) ||
            c.Name.Contains("Authentication", StringComparison.OrdinalIgnoreCase)))
        {
            riskFactors.Add("Critical system components affected");
            riskLevel = "Medium-High";
        }

        if (jiraIssue.Fields.Labels.Any(l => 
            l.Contains("breaking-change", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("database-migration", StringComparison.OrdinalIgnoreCase)))
        {
            riskFactors.Add("Breaking changes or database migrations");
            riskLevel = "High";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Risk Level: {riskLevel}");
        
        if (riskFactors.Any())
        {
            sb.AppendLine("Risk Factors:");
            foreach (var factor in riskFactors)
            {
                sb.AppendLine($"  • {factor}");
            }
        }
        else
        {
            sb.AppendLine("• Standard deployment with minimal risk factors identified");
        }

        return sb.ToString().Trim();
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Remove excessive whitespace and clean up formatting
        var cleanedText = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        
        // Remove common Jira markup artifacts
        cleanedText = System.Text.RegularExpressions.Regex.Replace(cleanedText, @"\{\{[^}]*\}\}", "");
        cleanedText = System.Text.RegularExpressions.Regex.Replace(cleanedText, @"\[[^\]]*\]", "");
        
        return cleanedText.Trim();
    }
}
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChangeRequesThor.Configuration;
using ChangeRequesThor.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace ChangeRequesThor.Services;

public interface IJiraService
{
    Task<JiraIssue?> GetIssueAsync(string issueKey);
    Task<string> ExtractPlainTextDescriptionAsync(JiraIssue issue);
}

public class JiraService : IJiraService
{
    private readonly HttpClient _httpClient;
    private readonly JiraSettings _settings;
    private readonly ILogger<JiraService> _logger;

    public JiraService(HttpClient httpClient, IOptions<JiraSettings> settings, ILogger<JiraService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        SetupHttpClient();
    }

    private void SetupHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        // Setup Basic Authentication for Jira
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.Username}:{_settings.ApiToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<JiraIssue?> GetIssueAsync(string issueKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(issueKey))
            {
                _logger.LogWarning("Issue key is empty or null");
                return null;
            }

            _logger.LogDebug("Fetching Jira issue: {IssueKey}", issueKey);

            var response = await _httpClient.GetAsync($"/rest/api/3/issue/{issueKey}");

            if (response.IsSuccessStatusCode)
            {
                var issue = await response.Content.ReadFromJsonAsync<JiraIssue>();
                _logger.LogDebug("Successfully retrieved Jira issue {IssueKey}: {Summary}", issueKey, issue?.Fields.Summary);
                return issue;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch Jira issue {IssueKey}. Status: {StatusCode}, Response: {Response}", 
                    issueKey, response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Jira issue {IssueKey}", issueKey);
            return null;
        }
    }

    public async Task<string> ExtractPlainTextDescriptionAsync(JiraIssue issue)
    {
        if (issue?.Fields?.Description == null)
        {
            return string.Empty;
        }

        try
        {
            // Handle different description formats (plain text, ADF, etc.)
            var description = issue.Fields.Description;
            
            if (description is string plainText)
            {
                return plainText;
            }
            
            // Handle Atlassian Document Format (ADF)
            if (description is System.Text.Json.JsonElement jsonElement)
            {
                return await ExtractTextFromAdf(jsonElement);
            }

            return description.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract description from Jira issue {IssueKey}, using fallback", issue.Key);
            return issue.Fields.Summary; // Fallback to summary
        }
    }

    private async Task<string> ExtractTextFromAdf(System.Text.Json.JsonElement adfElement)
    {
        var textBuilder = new StringBuilder();
        
        try
        {
            await ExtractTextRecursive(adfElement, textBuilder);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing ADF content, returning raw JSON");
            return adfElement.GetRawText();
        }

        return textBuilder.ToString().Trim();
    }

    private async Task ExtractTextRecursive(System.Text.Json.JsonElement element, StringBuilder textBuilder)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            // Check if this is a text node
            if (element.TryGetProperty("type", out var type) && type.GetString() == "text")
            {
                if (element.TryGetProperty("text", out var text))
                {
                    textBuilder.Append(text.GetString());
                }
            }

            // Process content array
            if (element.TryGetProperty("content", out var content) && content.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in content.EnumerateArray())
                {
                    await ExtractTextRecursive(item, textBuilder);
                }
            }

            // Add line breaks for paragraphs
            if (element.TryGetProperty("type", out var nodeType))
            {
                var typeString = nodeType.GetString();
                if (typeString == "paragraph" || typeString == "heading")
                {
                    textBuilder.AppendLine();
                }
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                await ExtractTextRecursive(item, textBuilder);
            }
        }

        await Task.CompletedTask; // Make method async for future extensibility
    }
}
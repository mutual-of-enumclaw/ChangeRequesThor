using System.Text.Json.Serialization;

namespace ChangeRequesThor.Models;

public class JiraIssue
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public JiraFields Fields { get; set; } = new();
}

public class JiraFields
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public object? Description { get; set; }

    [JsonPropertyName("priority")]
    public JiraPriority Priority { get; set; } = new();

    [JsonPropertyName("issuetype")]
    public JiraIssueType IssueType { get; set; } = new();

    [JsonPropertyName("assignee")]
    public JiraUser? Assignee { get; set; }

    [JsonPropertyName("reporter")]
    public JiraUser? Reporter { get; set; }

    [JsonPropertyName("status")]
    public JiraStatus Status { get; set; } = new();

    [JsonPropertyName("components")]
    public List<JiraComponent> Components { get; set; } = new();

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = new();
}

public class JiraPriority
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class JiraIssueType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class JiraUser
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; } = string.Empty;
}

public class JiraStatus
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class JiraComponent
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
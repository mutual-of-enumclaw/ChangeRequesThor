namespace ChangeRequesThor.Configuration;

public class SolarWindsSettings
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string DefaultRequestorEmail { get; set; } = string.Empty;
    public string DefaultCategory { get; set; } = string.Empty;
    public string DefaultSubcategory { get; set; } = string.Empty;
    public string DefaultPriority { get; set; } = string.Empty;
}

public class JiraSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool EnableDescriptionEnhancement { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}
namespace SolarWindsChangeCreator.Configuration;

public class SolarWindsSettings
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string DefaultRequestorEmail { get; set; } = string.Empty;
    public string DefaultCategory { get; set; } = string.Empty;
    public string DefaultSubcategory { get; set; } = string.Empty;
    public string DefaultPriority { get; set; } = string.Empty;
}

public class GitHubSettings
{
    public string Repository { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}

public class AppSettings
{
    public SolarWindsSettings SolarWinds { get; set; } = new();
    public GitHubSettings GitHub { get; set; } = new();
}
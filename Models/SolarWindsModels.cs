using System.Text.Json.Serialization;

namespace SolarWindsChangeCreator.Models;

public class ChangeRequest
{
    [JsonPropertyName("change")]
    public Change Change { get; set; } = new();
}

public class Change
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("requester")]
    public Requester Requester { get; set; } = new();

    [JsonPropertyName("category")]
    public Category Category { get; set; } = new();

    [JsonPropertyName("subcategory")]
    public Subcategory Subcategory { get; set; } = new();

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("planning_fields")]
    public PlanningFields PlanningFields { get; set; } = new();
}

public class Requester
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class Category
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class Subcategory
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class PlanningFields
{
    [JsonPropertyName("planned_start_date")]
    public string PlannedStartDate { get; set; } = string.Empty;

    [JsonPropertyName("planned_end_date")]
    public string PlannedEndDate { get; set; } = string.Empty;
}

public class ChangeResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}
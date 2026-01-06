using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ChangeRequesThor;
using ChangeRequesThor.Configuration;
using ChangeRequesThor.Services;
using Azure.Identity;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Enhance the configuration with Azure Key Vault
var preConfig = configuration.Build();
var keyVaultUrl = preConfig["KeyVault"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
}

// Build the final configuration
var finalConfig = configuration.Build();

// Create host builder
var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configure settings
        services.Configure<SolarWindsSettings>(finalConfig.GetSection("SolarWinds"));
        services.Configure<JiraSettings>(finalConfig.GetSection("Jira"));

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddConfiguration(finalConfig.GetSection("Logging"));
        });

        // Register HTTP clients
        services.AddHttpClient<IChangeAutomationService, SolarWindsService>();
        services.AddHttpClient<IJiraService, JiraService>();

        // Register services
        services.AddSingleton<IGitHubPipelineService, GitHubPipelineService>();
        services.AddSingleton<IDescriptionEnhancementService, DescriptionEnhancementService>();
        services.AddHostedService<ChangeCreatorApplication>();
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
    });

// Build and run the host
var host = hostBuilder.Build();

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    var logger = host.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "Application terminated unexpectedly");
    Environment.ExitCode = 1;
}

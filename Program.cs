using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolarWindsChangeCreator;
using SolarWindsChangeCreator.Configuration;
using SolarWindsChangeCreator.Services;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Create host builder
var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configure settings
        services.Configure<SolarWindsSettings>(configuration.GetSection("SolarWinds"));
        services.Configure<GitHubSettings>(configuration.GetSection("GitHub"));

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });

        // Register HTTP client
        services.AddHttpClient<ISolarWindsService, SolarWindsService>();

        // Register services
        services.AddSingleton<IGitHubPipelineService, GitHubPipelineService>();
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

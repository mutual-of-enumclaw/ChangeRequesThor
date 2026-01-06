# Change Creator

This application automatically creates change tickets, presently using SolarWinds, for production deployments initiated through GitHub pipelines.

## Overview

The application is designed to run as part of a GitHub release pipeline. When a deployment to production (PRD) is selected, this application will:

1. Detect the production deployment environment
2. Gather release information from environment variables
3. Create a change ticket with deployment details
4. Log the release ID and created ticket number

## Configuration

### Environment Variables

The application reads the following environment variables from the GitHub pipeline:

- `DEPLOYMENT_ENVIRONMENT` / `ENVIRONMENT` / `DEPLOY_ENV`: Set to "PRD", "PROD", or "PRODUCTION" for production deployments
- `GITHUB_REPOSITORY`: Repository name
- `GITHUB_REF_NAME`: Branch name or release tag
- `GITHUB_SHA`: Commit SHA (used as fallback for release ID)
- `RELEASE_ID`: Explicit release ID (if set)
- `GITHUB_RUN_ID`: GitHub run ID (used as fallback for release ID)

### Application Settings

The application uses `appsettings.json` and `appsettings.Production.json` for configuration:

```json
{
  "SolarWinds": {
    "ServiceUrl": "https://api.samanage.com",
    "ApiToken": "your-api-token-here",
    "DefaultRequestorEmail": "s_autocm@mutualofenum.com",
    "DefaultCategory": "Infrastructure",
    "DefaultSubcategory": "Deployment",
    "DefaultPriority": "Medium"
  }
}
```

## Usage

### In GitHub Actions

Add this step to your GitHub Actions workflow for production deployments:

```yaml
- name: Create Change Ticket
  run: |
    dotnet run --project ChangeRequesThor
  env:
    DEPLOYMENT_ENVIRONMENT: PRD
    ASPNETCORE_ENVIRONMENT: Production
```

### Local Testing

For local testing, set the required environment variables:

```bash
set DEPLOYMENT_ENVIRONMENT=PRD
set GITHUB_REPOSITORY=myorg/myrepo
set GITHUB_REF_NAME=v1.2.3
set ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

## API Integration

The application uses the SolarWinds Service Desk API:

- **Base URL**: https://api.samanage.com
- **Authentication**: Bearer token in `X-Samanage-Authorization` header
- **Endpoint**: `POST /changes.json`

## Logging

The application implements comprehensive logging:

- **INFO Level**: Only logs the release ID and created ticket number (as required)
- **DEBUG Level**: Detailed operation logs for troubleshooting
- **WARNING/ERROR Level**: Issues and failures

Example INFO log output:
```
Release ID: v1.2.3, Created Change Ticket: CHG0012345
```

## Error Handling

- If not a production deployment, the application exits gracefully without creating a ticket
- If ticket creation fails, the application exits with code 1
- All errors are logged with appropriate detail levels

## Dependencies

- .NET 9.0
- Microsoft.Extensions.* packages for dependency injection, configuration, and logging
- System.Text.Json for API serialization
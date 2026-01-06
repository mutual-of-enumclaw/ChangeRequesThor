# SolarWinds Service Desk (SWSD) Setup Notes

This document summarizes the steps and references needed to work with SWSD from our C# solution.

## Credentials
- The service account credentials are stored in **ERPM** under **Passwords → Managed Passwords**. (The page can take a moment to load.)
- The SWSD API token for `s_autocm` is stored securely—do **not** commit tokens to source control. Use environment variables or a secrets vault in development and production.

> Token (redacted):

```text
[REDACTED_TOKEN]
```

## API Documentation
- Official docs: https://apidoc.samanage.com — includes endpoints such as `createChange` and `getChangeById`.

## Base URL
- `https://api.samanage.com`

## Required Headers
```http
X-Samanage-Authorization: Bearer <your_token_here>
Accept: application/json
```

## Finding a Change ID
- Use the API (`GET /changes`) or browse to the Changes page in SWSD and open the desired change. The **Change ID** is visible in the page URL.

## Adding this file to a C# solution
1. Save this file as `SETUP.md` in your repository (e.g., under `/docs`).
2. Optionally include it in your project file so it gets copied to the output:

```xml
<ItemGroup>
  <None Include="docs/SETUP.md" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## Security Notes
- Never commit real tokens or passwords to source control. Use secrets management (e.g., environment variables, Azure Key Vault, or ERPM).
- Rotate tokens regularly and audit access.

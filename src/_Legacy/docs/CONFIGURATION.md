# LimboDancer MCP Configuration Guide

## Active runtime Host

The new runtime process is `src/LimboDancer/LimboDancer.Host`. Its initial configuration surface is:

```json
{
  "LimboDancer": {
    "ServerName": "LimboDancer",
    "ServerVersion": "0.1.0",
    "InvocationTimeoutSeconds": 30,
    "ApiKeys": [
      {
        "Key": "<secret-from-a-secure-configuration-provider>",
        "PrincipalId": "operator-or-service-identity",
        "TenantId": "11111111-1111-1111-1111-111111111111",
        "Permissions": []
      }
    ]
  }
}
```

Do not commit API keys. Supply them through environment variables, user secrets, or a deployment secret provider. The environment-variable form for the first key is `LimboDancer__ApiKeys__0__Key`; the remaining credential fields use the same double-underscore nesting convention.

The Host validates this configuration on startup. It starts with no configured credentials, but all protected MCP tool endpoints then return an authentication challenge. Tenant identity is taken from the matched credential and cannot be supplied by the MCP call payload.

| Endpoint | Authentication | Purpose |
| --- | --- | --- |
| `GET /.well-known/mcp` | Anonymous | MCP server discovery |
| `POST /mcp/server/discover` | Anonymous | Modern MCP server discovery method |
| `POST /mcp/initialize` | Anonymous | Legacy MCP initialization compatibility |
| `GET /mcp/tools` | API key | Published tool descriptors |
| `POST /mcp/tools/call` | API key | Governed directed invocation |
| `GET /health/live` | Anonymous | Process liveness |
| `GET /health/ready` | Anonymous | Observational runtime-structure readiness |

Send the API key in `X-LimboDancer-Key`. Readiness never performs schema migration or mutates authoritative State.

## Legacy configuration reference

The remaining sections describe the archived projects under `src/_Legacy/`. They are retained for archival purposes only and are not read by the new runtime Host.

## Required Configuration

### Storage (PostgreSQL)
```json
{
  "Storage": {
    "ConnectionString": "Host=localhost;Port=5432;Database=limbodancer_dev;Username=postgres;Password=postgres",
    "ApplyMigrationsAtStartup": false
  }
}
```
Alternative key (legacy): `Persistence:ConnectionString`

### Vector Search (Azure AI Search)
```json
{
  "Vector": {
    "Endpoint": "https://<your-service>.search.windows.net",
    "ApiKey": "<admin-key>",
    "IndexName": "ldm-memory",
    "VectorDimensions": 1536
  }
}
```
Alternative keys (legacy): `Search:Endpoint`, `Search:ApiKey`, `Search:Index`

### Graph Database (Cosmos Gremlin)
```json
{
  "CosmosGremlin": {
    "Host": "<account>.gremlin.cosmos.azure.com",
    "Port": 443,
    "EnableSsl": true,
    "Database": "limbodancer",
    "Graph": "history_memory_graph",
    "AuthKey": "<primary-key>",
    "ConnectionPoolSize": 8,
    "IsCosmos": true
  }
}
```

### Tenancy
```json
{
  "Tenancy": {
    "DefaultTenantId": "00000000-0000-0000-0000-000000000000",
    "DefaultPackage": "default",
    "DefaultChannel": "dev"
  }
}
```

### Authentication (McpServer.Http)
```json
{
  "Authentication": {
    "Jwt": {
      "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
      "Audience": "<API_CLIENT_ID>"
    }
  }
}
```

### Ontology API (BlazorConsole)
```json
{
  "OntologyApi": {
    "BaseUrl": "http://localhost:5179",
    "TenantHeaderName": "X-Tenant-Id",
    "TimeoutSeconds": 10
  }
}
```

## Environment-Specific Settings

### Development
- Set `ASPNETCORE_ENVIRONMENT=Development`
- Migrations can be auto-applied via `Storage:ApplyMigrationsAtStartup=true`
- Default tenant from config is used when no header present

### Production
- Ensure all connection strings use secure credentials
- Disable auto-migrations
- Configure proper CORS origins
- Use managed identities where possible

## CLI Configuration

The CLI reads from `appsettings.json` and `appsettings.Development.json` in the working directory.

Required for CLI operations:
- Storage connection for EF migrations
- Vector search credentials for index operations
- Graph database for Gremlin operations
- Tenancy defaults for development

## Running Migrations

```bash
# From CLI
ldm db migrate

# From .NET CLI
dotnet ef database update -p LimboDancer.MCP.Storage -s LimboDancer.MCP.McpServer
```

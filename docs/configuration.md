# Configuration Guide

This document describes every configuration key used by the API, how to set them for each environment, and how to protect secrets in production.

---

## Files

| File | Committed | Purpose |
|------|-----------|---------|
| `appsettings.json` | ✅ Yes | Base defaults — non-sensitive values only |
| `appsettings.Development.json` | ❌ No (`.gitignore`) | Local overrides with real Azure endpoints and agent IDs |
| Environment variables / Secret Store | — | Production secrets — never in files |

> **Rule**: if the value would expose access to Azure AI Foundry or a database, it must not be in a committed file.

---

## `appsettings.json` — Base configuration

```json
{
  "ConnectionStrings": {
    "InboxDb": "Data Source=Data/inbox.db"
  },
  "AiProvider": {
    "Type": "OpenAI",
    "ApiKey": "",
    "ModelId": "gpt-4o-mini",
    "Endpoint": null
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Microsoft.SemanticKernel": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} — {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [ "FromLogContext" ]
  },
  "WorkflowSettings": {
    "HighConfidenceThreshold": 0.85,
    "MediumConfidenceThreshold": 0.70,
    "EnableTaxonomyEvolution": true,
    "EnableHumanCollaboration": true
  }
}
```

---

## `appsettings.Development.json` — Local developer overrides

This file is listed in `.gitignore` and must **never** be committed.  
Each developer copies the template below and fills in their own values.

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  },
  "AiProvider": {
    "Type": "AzureAIFoundry",

    "Endpoint": "https://<resource>.services.ai.azure.com/api/projects/<project>",
    "ModelId": "gpt-4.1-mini",

    "ClassificationAgentId":      "Classification-Agent",
    "ClassificationAgentVersion": "5",

    "OrchestratorAgentId":      "Orchestrator-Agent",
    "OrchestratorAgentVersion": "1",

    "InvoiceAgentId":      "Invoice-Agent",
    "InvoiceAgentVersion": "1",

    "ContractAgentId":      "Contract-Agent",
    "ContractAgentVersion": "1",

    "TaxonomyEvolutionAgentId":      "Taxonomy-Evolution-Agent",
    "TaxonomyEvolutionAgentVersion": "1",

    "HumanCollaborationAgentId":      "Human-Collaboration-Agent",
    "HumanCollaborationAgentVersion": "1"
  }
}
```

### Local auth

The API uses `DefaultAzureCredential` — no API key is stored locally.  
Run `az login` once per session before starting the API.

---

## Key reference

### `ConnectionStrings`

| Key | Type | Description |
|-----|------|-------------|
| `InboxDb` | string | SQLite connection string. For production use a full-path or volume-mounted path. |

### `AiProvider`

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `Type` | string | ✅ | `AzureAIFoundry` for Azure; `OpenAI` for direct OpenAI API. |
| `Endpoint` | string | ✅ (Azure) | Project-scoped Azure AI Foundry endpoint URL. |
| `ApiKey` | string | ✅ (OpenAI) | OpenAI API key. **Never commit this value.** |
| `ModelId` | string | ✅ | Underlying model name (informational; agents are pinned in Foundry). |
| `*AgentId` | string | ✅ | Name of the Prompt Agent as deployed in Foundry. |
| `*AgentVersion` | string | ✅ | Published version snapshot. Bump when a new version is deployed. |

### `WorkflowSettings`

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `HighConfidenceThreshold` | float | `0.85` | Minimum confidence to auto-complete without human review. |
| `MediumConfidenceThreshold` | float | `0.70` | Below this value the workflow escalates to human review. |
| `EnableTaxonomyEvolution` | bool | `true` | Allows the system to propose new email categories. |
| `EnableHumanCollaboration` | bool | `true` | Enables the human-in-the-loop review flow. |

---

## Production secrets management

### Recommended approach: Azure Key Vault + Managed Identity

In production the application runs on Azure (App Service, Container Apps, or AKS).  
Use Managed Identity so no credential is ever stored in the deployment.

**1. Create the Key Vault and add secrets**

```bash
az keyvault create --name inbox-kv --resource-group <rg> --location eastus

# AiProvider
az keyvault secret set --vault-name inbox-kv \
  --name "AiProvider--Endpoint" \
  --value "https://<resource>.services.ai.azure.com/api/projects/<project>"

# Only needed if using OpenAI (not AzureAIFoundry)
az keyvault secret set --vault-name inbox-kv \
  --name "AiProvider--ApiKey" \
  --value "<openai-key>"

# Database (if using a managed SQL instead of SQLite)
az keyvault secret set --vault-name inbox-kv \
  --name "ConnectionStrings--InboxDb" \
  --value "<connection-string>"
```

> Key Vault secret names use `--` as the section separator, which maps directly to .NET's `AiProvider:Endpoint` path.

**2. Grant the app identity access**

```bash
# App Service example
PRINCIPAL=$(az webapp identity assign \
  --name inbox-api --resource-group <rg> \
  --query principalId -o tsv)

az keyvault set-policy --name inbox-kv \
  --object-id $PRINCIPAL \
  --secret-permissions get list
```

**3. Wire Key Vault into the .NET host**

```csharp
// Program.cs
if (!builder.Environment.IsDevelopment())
{
    var kvUri = builder.Configuration["KeyVaultUri"]!;
    builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new DefaultAzureCredential());
}
```

Add the Key Vault URI as a non-sensitive App Setting (not a secret):

```bash
az webapp config appsettings set \
  --name inbox-api --resource-group <rg> \
  --settings KeyVaultUri="https://inbox-kv.vault.azure.net/"
```

Non-sensitive tuning values (`WorkflowSettings`, `Serilog` levels) can remain in `appsettings.json` or as plain App Settings — they carry no security risk.

---

### Alternative: .NET User Secrets (local dev only)

If you prefer not to use a local `appsettings.Development.json`:

```bash
cd src/JF.AgenticEnterprise.Api
dotnet user-secrets init
dotnet user-secrets set "AiProvider:Endpoint" "https://..."
dotnet user-secrets set "AiProvider:ClassificationAgentId" "Classification-Agent"
```

Secrets are stored in `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json` — outside the repo, never committed.

---

### CI/CD: GitHub Actions

Store secrets in **GitHub Secrets** (Settings → Secrets → Actions), not in workflow YAML:

```yaml
- name: Deploy to Azure
  env:
    AIPROVI_ENDPOINT: ${{ secrets.AIPROVI_ENDPOINT }}
  run: |
    az webapp config appsettings set \
      --name inbox-api \
      --settings "AiProvider__Endpoint=$AIPROVI_ENDPOINT"
```

Use `__` (double underscore) as the section separator in environment variables — .NET maps these automatically to `AiProvider:Endpoint`.

---

## Security checklist

- [ ] `appsettings.Development.json` is in `.gitignore`
- [ ] No API keys or endpoints in committed files
- [ ] Production runs with Managed Identity — no stored credentials
- [ ] Key Vault access policy grants least-privilege (`get`, `list` only)
- [ ] Agent version pins are reviewed after each Foundry deployment
- [ ] `HighConfidenceThreshold` is tuned per environment before go-live

# GitHub Actions QA CI/CD

This repository includes a GitHub Actions workflow at `.github/workflows/qa-cicd.yml`.

## What it does

1. Builds all backend images on a GitHub-hosted Linux runner
2. Pushes `linux/amd64` images to Azure Container Registry
3. Creates or updates the QA Azure resources directly from the workflow
4. Deploys the new image tag to Azure Container Apps

The workflow triggers on:

- pushes to `develop`
- manual `workflow_dispatch`

## Required GitHub secrets

Add these repository secrets before running the workflow:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `JWT_SECRET`
- `DB_PASSWORD`
- `GOOGLE_MAPS_API_KEY`

## Azure OIDC setup

This workflow uses Azure OIDC, not a JSON credential blob.

Microsoft documents two supported OIDC options:

- Microsoft Entra application
- user-assigned managed identity

For your case, the practical option is a user-assigned managed identity with a federated credential for this GitHub repository.

Official docs:

- [Authenticate to Azure from GitHub Actions by OpenID Connect](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure-openid-connect)
- [Create trust between a user-assigned managed identity and GitHub Actions](https://learn.microsoft.com/en-us/entra/workload-id/workload-identity-federation-create-trust-user-assigned-managed-identity)

### GitHub secrets to create

Create these GitHub Actions secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

### How to get the values

#### Subscription ID

```bash
az account show --query id -o tsv
```

#### Tenant ID

```bash
az account show --query tenantId -o tsv
```

#### Client ID

If you are using a user-assigned managed identity:

```bash
az identity show \
  --resource-group <resource-group> \
  --name <managed-identity-name> \
  --query clientId -o tsv
```

If you need to inspect all identity fields:

```bash
az identity show \
  --resource-group <resource-group> \
  --name <managed-identity-name>
```

### One-time Azure setup required

You must create a user-assigned managed identity, assign it the required Azure role(s), and add a federated credential that trusts this GitHub repository/environment.

At a minimum, the identity needs enough rights to:

- create or update the resource group
- create or update Azure Container Registry
- create or update Azure SQL Server
- create or update Azure Container Apps resources
- register Azure resource providers

The workflow uses these values with `azure/login@v2` and requests GitHub `id-token: write` permission for OIDC token exchange.

## Default deployment values

The workflow currently assumes:

- `ACR_NAME=acrmeridianqa`
- `RESOURCE_GROUP=rg-meridian-qa`
- `LOCATION=eastasia`
- `CAE_NAME=cae-meridian-qa`
- `SQL_SERVER=sql-meridian-qa001`

If those values need to change, update `.github/workflows/qa-cicd.yml`.

## Image tags

- On a push to `develop`, the workflow uses the first 12 characters of `GITHUB_SHA`
- On manual dispatch, you can optionally provide a custom `image_tag`

The deploy job uses that same tag when updating Azure Container Apps, so the deployed apps pull the exact images built by the workflow.

## Notes

- The workflow is self-contained and does not call `build-push.sh` or `deploy-qa.sh`.
- The local bash scripts remain useful for manual bootstrap or local operator workflows, but GitHub Actions does not depend on them.

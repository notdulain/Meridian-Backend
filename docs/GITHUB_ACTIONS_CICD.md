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

- `AZURE_CREDENTIALS`
- `JWT_SECRET`
- `DB_PASSWORD`
- `GOOGLE_MAPS_API_KEY`

## AZURE_CREDENTIALS format

Use the JSON output from an Azure service principal, for example:

```json
{
  "clientId": "<app-client-id>",
  "clientSecret": "<client-secret>",
  "subscriptionId": "<subscription-id>",
  "tenantId": "<tenant-id>"
}
```

The service principal should have enough access to:

- create or update the resource group
- create or update Azure Container Registry
- create or update Azure SQL Server
- create or update Azure Container Apps resources
- register Azure resource providers

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

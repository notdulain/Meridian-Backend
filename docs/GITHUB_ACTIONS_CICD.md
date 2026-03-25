# GitHub Actions CI/CD

This repository includes environment-specific GitHub Actions workflows at `.github/workflows/qa-cicd.yml` and `.github/workflows/prod-cicd.yml`.

## What it does

1. Builds all backend images on a GitHub-hosted Linux runner
2. Pushes `linux/amd64` images to Azure Container Registry
3. Creates or updates the Azure resource group, SQL logical server, ACR, and Container Apps environment directly from the workflow
4. Deploys the new image tag to Azure Container Apps

The workflow triggers on:

- pushes to `develop`
- manual `workflow_dispatch`

## Repository secrets

The repository currently has these GitHub Actions secrets configured:

- `ACR_LOGIN_SERVER`
- `ACR_PASSWORD`
- `ACR_USERNAME`
- `AZURE_CLIENT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_TENANT_ID`
- `DB_PASSWORD`
- `GOOGLE_MAPS_API_KEY`
- `REDIS_PASSWORD`
- `JWT_SECRET`

## Secrets used by the current workflow

The current `.github/workflows/qa-cicd.yml` directly uses these secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `JWT_SECRET`
- `DB_PASSWORD`
- `GOOGLE_MAPS_API_KEY`
- `REDIS_PASSWORD`

## Secrets currently not consumed by the workflow

These secrets exist in the repo, but the current OIDC-based workflow does not read them:

- `ACR_LOGIN_SERVER`
- `ACR_USERNAME`
- `ACR_PASSWORD`

They are still useful as operational fallback values for:

- manual Docker login and push to ACR
- local recovery/bootstrap work
- future workflow changes if you decide to log Docker in with registry credentials instead of `az acr login`

## Azure OIDC setup

This workflow uses Azure OIDC, not a JSON credential blob.

Microsoft documents two supported OIDC options:

- Microsoft Entra application
- user-assigned managed identity

For your case, the practical option is a user-assigned managed identity with a federated credential for this GitHub repository.

Official docs:

- [Authenticate to Azure from GitHub Actions by OpenID Connect](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure-openid-connect)
- [Create trust between a user-assigned managed identity and GitHub Actions](https://learn.microsoft.com/en-us/entra/workload-id/workload-identity-federation-create-trust-user-assigned-managed-identity)

### GitHub secrets required for OIDC login

Create these GitHub Actions secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

### Application and deployment secrets

Create these GitHub Actions secrets for the backend deployment itself:

- `JWT_SECRET`
- `DB_PASSWORD`
- `GOOGLE_MAPS_API_KEY`

### Optional ACR secrets

These are not required by the current workflow, but may still be stored in the repo for manual use:

- `ACR_LOGIN_SERVER`
- `ACR_USERNAME`
- `ACR_PASSWORD`

### How to get the values

Before running the commands below:

```bash
az login
az account set --subscription "<subscription-name-or-id>"
```

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

#### JWT secret

Generate a strong shared signing key locally:

```bash
openssl rand -base64 48
```

Use the output as `JWT_SECRET`.

#### Database password

This is not something Azure will show you after creation. You must choose and store it yourself.

Generate a strong password locally:

```bash
openssl rand -base64 24
```

Use the generated value as `DB_PASSWORD` when provisioning Azure SQL Server and store the same value in the GitHub secret.

#### Google Maps API key

This value comes from Google Cloud, not Azure.

Steps:

1. Open the Google Cloud Console.
2. Select or create the GCP project used by RouteService.
3. Enable the Google Routes API for that project.
4. Go to `APIs & Services` → `Credentials`.
5. Create an API key or reuse an existing restricted key.
6. Copy that value into the `GOOGLE_MAPS_API_KEY` GitHub secret.

#### Redis Cloud credentials

The QA and PROD workflows do not provision Azure Cache for Redis. RouteService uses a Redis Cloud endpoint, and the workflows build the StackExchange.Redis connection string during deployment.

Current endpoint:

```text
redis-17031.c251.east-us-mz.azure.cloud.redislabs.com:17031
```

Required GitHub secret:

- `REDIS_PASSWORD`

The workflows construct the RouteService connection string in this format:

```text
redis-17031.c251.east-us-mz.azure.cloud.redislabs.com:17031,user=default,password=$REDIS_PASSWORD,ssl=True,abortConnect=False
```

#### Optional ACR values

These are only needed for manual fallback operations, not for the current OIDC workflow.

Get the registry login server:

```bash
az acr show \
  --name <acr-name> \
  --query loginServer -o tsv
```

Get the registry username:

```bash
az acr credential show \
  --name <acr-name> \
  --query username -o tsv
```

Get the registry passwords:

```bash
az acr credential show \
  --name <acr-name> \
  --query passwords
```

#### Save the values into GitHub Actions secrets

If you use the GitHub CLI, you can set the secrets directly from your terminal:

```bash
gh secret set AZURE_SUBSCRIPTION_ID --body "<subscription-id>"
gh secret set AZURE_TENANT_ID --body "<tenant-id>"
gh secret set AZURE_CLIENT_ID --body "<managed-identity-client-id>"
gh secret set JWT_SECRET --body "<jwt-secret>"
gh secret set DB_PASSWORD --body "<db-password>"
gh secret set GOOGLE_MAPS_API_KEY --body "<google-maps-api-key>"
gh secret set REDIS_PASSWORD --body "<redis-password>"
```

Optional fallback ACR secrets:

```bash
gh secret set ACR_LOGIN_SERVER --body "<acr-login-server>"
gh secret set ACR_USERNAME --body "<acr-username>"
gh secret set ACR_PASSWORD --body "<acr-password>"
```

### One-time Azure setup required

You must create a user-assigned managed identity, assign it the required Azure role(s), and add a federated credential that trusts this GitHub repository/environment.

At a minimum, the identity needs enough rights to:

- create or update the resource group
- create or update Azure Container Registry
- create or update Azure SQL logical server
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
- `REDIS_ENDPOINT=redis-17031.c251.east-us-mz.azure.cloud.redislabs.com:17031`

The seven application databases are expected to already exist on the logical server:

- `user_db`
- `meridian_delivery`
- `meridian_vehicle`
- `driver_db`
- `meridian_assignment`
- `meridian_route`
- `meridian_tracking`

If those values need to change, update `.github/workflows/qa-cicd.yml`.

## Image tags

- On a push to `develop`, the workflow uses the first 12 characters of `GITHUB_SHA`
- On manual dispatch, you can optionally provide a custom `image_tag`

The deploy job uses that same tag when updating Azure Container Apps, so the deployed apps pull the exact images built by the workflow.

## Notes

- The workflow is self-contained and does not call `build-push.sh` or `deploy-qa.sh`.
- The local bash scripts remain useful for manual bootstrap or local operator workflows, but GitHub Actions does not depend on them.
- The current workflow authenticates Docker to ACR via `az acr login`, so ACR username/password secrets are not required for the workflow to pass.
- The workflows no longer create Azure Redis resources. RouteService cache configuration comes from Redis Cloud.

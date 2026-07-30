# Manual Azure Deployment

This guide prepares the infrastructure through the Azure portal. There is no infrastructure as code in this project: resources are intentionally created manually for study purposes.

## 1. Create the Resource Group

In the portal, open **Resource groups > Create**, choose the subscription, a nearby region, and a name such as `rg-minimal-api-prod`.

A Resource Group is the logical container for resources. It simplifies permissions, costs, tags, and coordinated removal when the study project ends.

## 2. Create the App Service Plan

Open **App Service plans > Create**, select the Resource Group and region, and choose **Linux** as the operating system. For a lab, choose a SKU that meets your needs and budget.

The plan defines compute capacity and billing. Multiple Web Apps can share one plan, but they also share its resources.

## 3. Create the Web App

In **App Services > Create > Web App**, choose the Resource Group and App Service Plan you created. Define a globally unique name, such as `minimal-api-your-name`, and select the `.NET` stack with version 10 when it is available in the portal. Create the resource.

The Web App hosts the API and provides `https://<name>.azurewebsites.net`. Record its name: it is the value for `AZURE_WEBAPP_NAME`.

In **Settings > Configuration > General settings**, confirm the appropriate .NET stack. Under **Configuration > Application settings**, the workflow maintains these keys:

```text
ASPNETCORE_ENVIRONMENT = Production
Database__ConnectionString = <PostgreSQL connection string>
```

Use two underscores to represent the .NET `Database:ConnectionString` section. Do not add this connection string to production `appsettings.json`.

## 4. Create Azure Database for PostgreSQL

Create an **Azure Database for PostgreSQL flexible server** in the same Resource Group and preferably the same region. Define the server, administrator, strong password, and a suitable development SKU. Then create the `minimal_api` database.

This is Azure's managed PostgreSQL service: it manages hosting, backups, and maintenance. Configure private networking whenever possible. For a public-access lab, allow only required firewall addresses and enable access for Azure services only according to your organization policy; do not expose the server to the entire internet.

Connect to the database and run [database/init.sql](../database/init.sql) once to create the initial schema. Build a connection string with required TLS, for example:

```text
Host=<server>.postgres.database.azure.com;Port=5432;Database=minimal_api;Username=<user>;Password=<password>;Ssl Mode=Require
```

Store it only in the `DATABASE_CONNECTION` secret.

## 5. Configure GitHub authentication (OIDC)

Create a **Microsoft Entra ID App registration** for GitHub Actions. Under **Certificates & secrets > Federated credentials**, create a credential for the repository and `production` environment (a subject similar to `repo:<organization>/<repository>:environment:production`). Do not create a client secret: the workflow uses OIDC.

In the Resource Group, open **Access control (IAM) > Add role assignment** and assign the service principal the **Contributor** role (or a more restrictive role that can configure and deploy the Web App). Copy the Application (client) ID, Directory (tenant) ID, and Subscription ID.

## 6. Get the Publish Profile

In the Web App, use **Overview > Get publish profile** and download the XML file. Its complete contents are the `AZURE_PUBLISH_PROFILE` secret. It is a sensitive credential: never save it in the repository or as a tracked local file.

## GitHub Secrets

In the GitHub repository, open **Settings > Secrets and variables > Actions > New repository secret**. Create every value below:

| Secret | Value | Workflow use |
| --- | --- | --- |
| `AZURE_CLIENT_ID` | App registration Application (client) ID | OIDC login (`azure/login`) |
| `AZURE_TENANT_ID` | Directory (tenant) ID | OIDC login |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | OIDC login |
| `AZURE_WEBAPP_NAME` | Web App name, without URL | Configuration, deployment, and health check |
| `AZURE_RESOURCE_GROUP` | Resource Group name | App Service configuration |
| `AZURE_PUBLISH_PROFILE` | Complete XML downloaded from the Web App | Package upload to the Web App |
| `DATABASE_CONNECTION` | PostgreSQL connection string with TLS | `Database__ConnectionString` in App Service |

All secrets are consumed by CD. GitHub masks known secrets in logs, but do not print them in commands or messages.

## .NET Environments

For local development, use `ASPNETCORE_ENVIRONMENT=Development`. The `appsettings.Development.json` file lowers the minimum log level and Scalar/OpenAPI documentation is available. `docker-compose.yml` already applies this environment to the API.

In App Service, CD sets `ASPNETCORE_ENVIRONMENT=Production`. In this environment, the API does not expose Scalar/OpenAPI and receives its connection string through an Application Setting, which takes precedence over `appsettings.json`.

Local PowerShell example, without putting a password in a tracked file:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__ConnectionString = 'Host=localhost;Port=5432;Database=minimal_api;Username=postgres;Password=postgres'
dotnet run --project src/MinimalApi.Api
```

## Deploy and verify

Push to `main`. CI publishes the artifact; if it passes, CD configures the Web App and deploys it. Open `https://<webapp-name>.azurewebsites.net/health`: deployment is considered complete only if this endpoint returns HTTP 200.

To troubleshoot, open **Actions > CD - Azure App Service** in GitHub for workflow logs and **Monitoring > Log stream** in the Web App for application logs. Review PostgreSQL network settings if the API starts but its database health check is not healthy.

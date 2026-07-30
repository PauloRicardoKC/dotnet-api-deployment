# .NET Minimal API: CI/CD on Azure

An existing .NET 10 API, with no new business functionality, evolved to demonstrate professional CI/CD using GitHub Actions and Azure App Service. The automation validates builds and tests, produces an immutable package, and deploys it automatically after approval on `main`.

## Technologies

- .NET 10, ASP.NET Core Minimal APIs, and Clean Architecture
- PostgreSQL, Dapper, FluentValidation, and Serilog
- xUnit and FluentAssertions
- GitHub Actions for CI/CD
- Azure App Service and Azure Database for PostgreSQL
- Docker Compose for local development only

## Architecture

```text
API ---> Application ---> Domain
 |
 +---> Infrastructure ---> Application / Domain
```

`Domain` contains rules and entities; `Application` contains use cases and contracts; `Infrastructure` provides PostgreSQL persistence with Dapper; and `Api` contains HTTP endpoints, middleware, and application composition.

```text
src/                         API and application layers
tests/                       unit and integration tests
database/init.sql            initial PostgreSQL schema
.github/workflows/ci.yml     validation, tests, and artifact
.github/workflows/cd.yml     Azure deployment and health check
docs/PIPELINE.md             detailed CI/CD explanation
docs/AZURE_DEPLOY.md         manual Azure setup and secrets
```

## CI/CD flow

```text
Pull request or push to main
        |
        v
Restore -> Build -> Unit tests -> Integration tests
        |
        v
Application artifact
        |
        v
Approved push to main -> Azure App Service -> /health (HTTP 200)
```

The CD workflow starts only after a successful CI run on `main` and uses the artifact from that exact run. See [PIPELINE.md](docs/PIPELINE.en.md) for an explanation of every stage and Action.

## Run locally

Requires the .NET 10 SDK and Docker.

```bash
docker compose up --build
```

The API is available at `http://localhost:8080`, the health check at `http://localhost:8080/health`, and Scalar—available only in Development—at `http://localhost:8080/scalar/v1`.

Alternatively, start only the database and run the API:

```bash
docker compose up postgres -d
dotnet run --project src/MinimalApi.Api
dotnet test dotnet-minimal-api-foundation.slnx
```

To override the local connection without changing tracked files, use the `Database__ConnectionString` environment variable. The local environment is `Development`; in production, CD sets `ASPNETCORE_ENVIRONMENT=Production` and injects the connection string through App Service.

## Configure Azure and GitHub

Follow [AZURE_DEPLOY.md](docs/AZURE_DEPLOY.en.md) to manually create the Resource Group, App Service Plan, Web App, and Azure Database for PostgreSQL. The guide also lists all required GitHub Secrets:

`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_WEBAPP_NAME`, `AZURE_RESOURCE_GROUP`, `AZURE_PUBLISH_PROFILE`, and `DATABASE_CONNECTION`.

Values are used only as secrets/Azure settings; credentials must never be committed to the repository.

## Publish

1. Configure the infrastructure and secrets according to the Azure guide.
2. Create the GitHub `production` environment (it may require manual approval).
3. Merge or push to `main`.
4. Follow **Actions > CI** and **Actions > CD - Azure App Service**.

At the end, the pipeline calls `https://<AZURE_WEBAPP_NAME>.azurewebsites.net/health`. A response other than HTTP 200 fails the deployment and leaves an explicit message in the log.

## Endpoints

| Method | Route | Description |
| --- | --- | --- |
| GET | `/products` | Lists products |
| GET | `/products/{id}` | Gets a product |
| POST | `/products` | Creates a product |
| PUT | `/products/{id}` | Updates a product |
| DELETE | `/products/{id}` | Deletes a product |

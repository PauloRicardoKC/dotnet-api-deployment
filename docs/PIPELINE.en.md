# CI/CD Pipeline

This project uses GitHub Actions to validate every change and deploy only an approved version to Azure App Service.

## Concepts

**Continuous Integration (CI)** is the automatic and frequent validation of integrated code. In this project, it restores dependencies, builds the solution, runs tests, and packages the API.

**Continuous Delivery/Deployment (CD)** is the next stage: a validated version is made available in an environment. In this project, deployment to production happens automatically after a successful CI run on `main`.

A **workflow** is the YAML file that describes the automation. A **runner** is the temporary machine—in this case, GitHub-hosted `ubuntu-latest`—that executes the workflow steps. An **artifact** is a file or directory produced during a run and stored by GitHub Actions; here, it is the `dotnet publish` output ready for deployment, as well as TRX test results.

## Flow

```text
Push/PR to main
        |
        v
CI: checkout -> restore -> build -> unit tests -> integration tests
        |                                      |
        |                                      +-> TRX results (artifact)
        v
dotnet publish -> application artifact
        |
        v
CD (successful push to main only) -> configure App Service -> deploy -> GET /health
                                                                          |
                                                                    HTTP 200 = success
```

## CI: `.github/workflows/ci.yml`

CI runs on pushes to `main` and pull requests targeting `main`.

1. `actions/checkout@v4` downloads the commit to validate.
2. `actions/setup-dotnet@v4` installs .NET 10 and enables NuGet caching. The cache is automatically invalidated when dependency files change, reducing execution time in subsequent runs.
3. `dotnet restore` retrieves packages; `dotnet build --no-restore` builds the Release solution without repeating that work.
4. Unit and integration test projects run separately. Each generates a `.trx` file in `TestResults`, making failures easier to investigate.
5. `actions/upload-artifact@v4` stores test results even if a test fails (`if: always()`).
6. `dotnet publish` creates the API runtime output. A second `upload-artifact` stores it using the commit SHA. Only a CI run that reaches this step can create a deployment package.

The `✔` log messages make restore, build, test, and artifact milestones explicit in the job output.

## CD: `.github/workflows/cd.yml`

CD listens for completion of the workflow named `CI`. It starts only when a **push** run on `main` succeeds; pull-request artifacts are never promoted to production. This establishes a CI/CD dependency without rebuilding code: deployment downloads the artifact from the exact approved run.

1. `actions/download-artifact@v4` downloads the package using that CI run ID.
2. `azure/login@v2` authenticates without passwords through OpenID Connect (OIDC) and Microsoft Entra application IDs.
3. `azure/appservice-settings@v1` applies `ASPNETCORE_ENVIRONMENT=Production` and the connection string as App Service settings, without storing secrets in the repository.
4. `azure/webapps-deploy@v3` sends the package to the Web App using the publish profile.
5. The final script calls `https://<webapp>.azurewebsites.net/health` up to six times. Only HTTP `200` is accepted; any other status exits with code 1 and a clear `::error::` log message.

The `production` environment concurrency setting prevents simultaneous deployments to the same App Service. `cancel-in-progress: false` preserves the order of already approved commits.

## Artifacts and results

In GitHub, open a run under **Actions**. The **Artifacts** section at the bottom of the run summary lets you download:

- `minimal-api-<SHA>`: the published application package used by CD;
- `test-results-<SHA>`: `.trx` files from unit and integration tests.

The workflow retains both for 14 days. An artifact belongs to one specific run, not to the repository or the runner machine, and is removed after the configured retention period.

## GitHub secrets and environment

Create a `production` environment under **Settings > Environments**. It appears in deployment history and can enforce approval rules before the job begins. Then create the secrets described in the [Azure deployment guide](AZURE_DEPLOY.en.md#github-secrets) under **Settings > Secrets and variables > Actions**. Never put real values in YAML, `appsettings.json`, commits, or logs.

The workflows use minimum permissions: content read access in CI; artifact read access and `id-token: write` for OIDC in CD.

# Pipeline CI/CD

Este projeto usa GitHub Actions para validar cada alteração e publicar somente uma versão aprovada no Azure App Service.

## Conceitos

**Continuous Integration (CI)** é a validação automática e frequente do código integrado. Aqui ela restaura dependências, compila, executa testes e empacota a API.

**Continuous Delivery/Deployment (CD)** é a etapa posterior: uma versão validada é disponibilizada em um ambiente. Neste projeto o deploy para produção é automático após uma CI bem-sucedida na `main`.

Um **workflow** é o arquivo YAML que descreve a automação. Um **runner** é a máquina temporária, neste caso `ubuntu-latest` hospedada pelo GitHub, que executa seus passos. Um **artifact** é um arquivo ou diretório produzido durante uma execução e armazenado pelo GitHub Actions; aqui é a saída de `dotnet publish`, pronta para deploy, e também os resultados de teste em TRX.

## Fluxo

```text
Push/PR para main
        |
        v
CI: checkout -> restore -> build -> testes unitários -> integração
        |                                      |
        |                                      +-> resultados TRX (artifact)
        v
dotnet publish -> artifact da aplicação
        |
        v
CD (somente push aprovado na main) -> configura App Service -> deploy -> GET /health
                                                                    |
                                                              HTTP 200 = sucesso
```

## CI: `.github/workflows/ci.yml`

É acionada em `push` na `main` e em pull requests cujo destino é a `main`.

1. `actions/checkout@v4` baixa o commit que será validado.
2. `actions/setup-dotnet@v4` instala o .NET 10. `actions/cache@v4` restaura o cache de pacotes NuGet; sua chave considera os projetos, `NuGet.Config` e `global.json`, sendo invalidada quando dependências ou SDK mudam.
3. `dotnet restore` recupera os pacotes; `dotnet build --no-restore` compila a solução em `Release` sem repetir esse trabalho.
4. Os projetos unitário e de integração são executados separadamente. Cada um gera um arquivo `.trx` em `TestResults`, facilitando a investigação de falhas.
5. `actions/upload-artifact@v4` guarda os resultados mesmo se um teste falhar (`if: always()`).
6. `dotnet publish` monta a saída executável da API. Um segundo `upload-artifact` armazena essa saída com o SHA do commit. Só uma CI que alcança essa etapa pode gerar o pacote de deploy.

Os `echo` com `✔` deixam explícitos no log os marcos de restore, build, testes e artifact.

## CD: `.github/workflows/cd.yml`

O CD escuta a conclusão do workflow chamado `CI`. Ele só entra no job de deploy quando uma execução de **push** na `main` termina com sucesso; artifacts de pull requests jamais são promovidos para produção. Isso cria a dependência entre CI e CD sem reconstruir o código: o deploy baixa exatamente o artifact produzido pela execução aprovada.

1. `actions/download-artifact@v4` baixa o pacote pelo ID daquela execução de CI.
2. `azure/login@v2` autentica de forma sem senha usando OpenID Connect (OIDC) e os IDs do aplicativo Microsoft Entra.
3. `azure/appservice-settings@v1` aplica `ASPNETCORE_ENVIRONMENT=Production` e a connection string como configuração do App Service, sem gravar segredos no repositório.
4. `azure/webapps-deploy@v3` envia o pacote ao Web App usando o publish profile.
5. O script final consulta `https://<webapp>.azurewebsites.net/health` até seis vezes. Somente HTTP `200` é aceito; qualquer outro status encerra com código 1 e uma mensagem `::error::` clara no log.

O `concurrency` do ambiente `production` impede que dois deploys sejam feitos no mesmo App Service ao mesmo tempo. `cancel-in-progress: false` preserva a ordem dos commits já aprovados.

## Artifacts e resultados

No GitHub, abra a execução em **Actions**. A seção **Artifacts** no fim do resumo permite baixar:

- `minimal-api-<SHA>`: pacote publicado da aplicação, usado pelo CD;
- `test-results-<SHA>`: arquivos `.trx` dos testes unitários e de integração.

O workflow mantém os dois por 14 dias. O artifact pertence à execução específica, não ao repositório nem ao computador do runner, e é removido depois do período configurado.

## Secrets e ambiente GitHub

Crie o ambiente `production` em **Settings > Environments**. Ele aparece no histórico de deploys e pode receber regras de aprovação antes de o job começar. Depois, em **Settings > Secrets and variables > Actions**, crie os secrets descritos no [guia de Azure](AZURE_DEPLOY.md#github-secrets). Nunca use valores reais em YAML, `appsettings.json`, commits ou logs.

O workflow usa permissões mínimas: leitura de conteúdo na CI, leitura de artifacts e `id-token: write` no CD para OIDC.

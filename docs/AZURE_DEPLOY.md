# Deploy manual no Azure

Este guia prepara a infraestrutura pelo portal Azure. Não há infraestrutura como código neste projeto: a criação é propositalmente manual para estudo.

## 1. Criar o Resource Group

No portal, acesse **Resource groups > Create**, escolha a assinatura, uma região próxima e um nome, por exemplo `rg-minimal-api-prod`.

O Resource Group é o contêiner lógico dos recursos: facilita permissões, custos, tags e remoção coordenada ao término dos estudos.

## 2. Criar o App Service Plan

Em **App Service plans > Create**, selecione o Resource Group, região e sistema operacional **Linux**. Para laboratório, escolha uma SKU compatível com sua necessidade e orçamento.

O plano define a capacidade computacional e o modelo de cobrança. Vários Web Apps podem compartilhar um plano, mas os recursos do plano também são compartilhados.

## 3. Criar o Web App

Em **App Services > Create > Web App**, escolha o Resource Group e o App Service Plan criados. Defina um nome globalmente único, como `minimal-api-seu-nome`, e selecione a stack `.NET` com versão 10 quando disponível no portal. Crie o recurso.

O Web App hospeda a API e fornece a URL `https://<nome>.azurewebsites.net`. Anote o nome: ele é o valor de `AZURE_WEBAPP_NAME`.

Em **Settings > Configuration > General settings**, confirme a stack .NET apropriada. Em **Configuration > Application settings**, o workflow manterá estas chaves:

```text
ASPNETCORE_ENVIRONMENT = Production
Database__ConnectionString = <connection string PostgreSQL>
```

Use dois sublinhados para representar a seção `Database:ConnectionString` do .NET. Não adicione essa string ao `appsettings.json` de produção.

## 4. Criar Azure Database for PostgreSQL

Crie **Azure Database for PostgreSQL flexible server** no mesmo Resource Group e, preferencialmente, na mesma região. Defina servidor, administrador, senha forte e uma SKU de desenvolvimento adequada. Em seguida crie o banco `minimal_api`.

Esse serviço é o PostgreSQL gerenciado do Azure: cuida de hospedagem, backups e manutenção. Configure rede com acesso privado quando possível. Para um laboratório usando acesso público, permita apenas os endereços necessários e habilite a opção de acesso para serviços Azure conforme a política da organização; não abra o servidor para toda a internet.

Conecte-se ao banco e execute [database/init.sql](../database/init.sql) uma vez para criar o schema inicial. Monte a connection string com TLS obrigatório, por exemplo:

```text
Host=<servidor>.postgres.database.azure.com;Port=5432;Database=minimal_api;Username=<usuario>;Password=<senha>;Ssl Mode=Require
```

Guarde-a exclusivamente no secret `DATABASE_CONNECTION`.

## 5. Configurar autenticação do GitHub (OIDC)

Crie um **App registration** no Microsoft Entra ID para o GitHub Actions. Em **Certificates & secrets > Federated credentials**, crie uma credencial para o repositório e ambiente `production` (subject semelhante a `repo:<organizacao>/<repositorio>:environment:production`). Não crie client secret: o workflow usa OIDC.

No Resource Group, abra **Access control (IAM) > Add role assignment** e atribua ao service principal a função **Contributor** (ou uma função mais restrita que permita configurar e publicar o Web App). Copie Application (client) ID, Directory (tenant) ID e Subscription ID.

## 6. Obter o Publish Profile

No Web App, use **Overview > Get publish profile** e baixe o arquivo XML. O seu conteúdo completo será o secret `AZURE_PUBLISH_PROFILE`. Ele é uma credencial sensível; não o salve no repositório, nem como arquivo local rastreado.

## GitHub Secrets

No repositório GitHub, abra **Settings > Secrets and variables > Actions > New repository secret**. Crie todos os valores abaixo:

| Secret | Valor | Uso no workflow |
| --- | --- | --- |
| `AZURE_CLIENT_ID` | Application (client) ID do App registration | Login OIDC (`azure/login`) |
| `AZURE_TENANT_ID` | Directory (tenant) ID | Login OIDC |
| `AZURE_SUBSCRIPTION_ID` | ID da assinatura Azure | Login OIDC |
| `AZURE_WEBAPP_NAME` | Nome do Web App, sem URL | Configuração, deploy e health check |
| `AZURE_RESOURCE_GROUP` | Nome do Resource Group | Configuração do App Service |
| `AZURE_PUBLISH_PROFILE` | XML completo baixado do Web App | Upload do pacote para o Web App |
| `DATABASE_CONNECTION` | Connection string PostgreSQL com TLS | `Database__ConnectionString` no App Service |

Todos são consumidos pelo CD. O GitHub mascara secrets conhecidos nos logs, mas ainda assim não os imprima em comandos ou mensagens.

## Ambientes .NET

Para desenvolvimento local, use `ASPNETCORE_ENVIRONMENT=Development`. O arquivo `appsettings.Development.json` reduz o nível mínimo de logs e a documentação Scalar/OpenAPI fica disponível. O `docker-compose.yml` já aplica esse ambiente à API.

No App Service, o CD define `ASPNETCORE_ENVIRONMENT=Production`. Nesse ambiente a API não expõe Scalar/OpenAPI e deve receber a connection string pelo Application Setting, que prevalece sobre `appsettings.json`.

Exemplo local (PowerShell), sem colocar senha em arquivo rastreado:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__ConnectionString = 'Host=localhost;Port=5432;Database=minimal_api;Username=postgres;Password=postgres'
dotnet run --project src/MinimalApi.Api
```

## Publicar e verificar

Faça push para `main`. A CI publica o artifact; se ela passar, o CD configura o Web App e realiza o deploy. Acesse `https://<nome-do-webapp>.azurewebsites.net/health`: a publicação é considerada concluída somente se esse endpoint retornar HTTP 200.

Para diagnosticar problemas, abra **Actions > CD - Azure App Service** no GitHub para os logs e **Monitoring > Log stream** no Web App para os logs da aplicação. Revise configurações de rede do PostgreSQL caso a API suba mas o health check de banco não fique saudável.

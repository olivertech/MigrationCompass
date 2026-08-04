# MigrationCompass

O `MigrationCompass` é uma ferramenta de console em `.NET 10` voltada à análise técnica de soluções legadas e à geração de relatórios executivos para apoiar migrações até `.NET 10`.

Este documento apresenta a visão técnica e detalhada do projeto. Para uma versão institucional, consulte `README.institucional.md`.

## Objetivo

O foco do `MigrationCompass` é escanear uma solution `.NET` e responder, com evidências:

- quais projetos estão mais distantes do alvo `.NET 10`
- quais pacotes server-side podem bloquear ou encarecer a migração
- quais APIs legadas exigirão refatoração
- quais achados têm impacto mensurável em produção

## Escopo Atual

A versão atual já implementa:

- descoberta de solution e projetos `.csproj`
- leitura de `TargetFramework`, `TargetFrameworks` e `TargetFrameworkVersion`
- leitura de `PackageReference`, `Reference` e `packages.config`
- classificação de projetos desde `.NET Framework 3.x/4.x` até `.NET 9`
- scanner de APIs legadas baseado em regras JSON
- verificação de compatibilidade de pacotes com `.NET 10`
- fallback offline para cenários sem acesso ao NuGet
- relatório HTML executivo focado em bloqueadores relevantes
- suíte local de validação sem frameworks externos de teste

## Arquitetura

### Fluxo principal

1. A CLI recebe os parâmetros de entrada.
2. A solution é localizada e os projetos `.csproj` são descobertos.
3. Cada projeto é classificado pela distância até `.NET 10`.
4. Dependências, referências e arquivos `.cs` são coletados.
5. APIs legadas são avaliadas com base em `Rules/BlockingRules.json`.
6. Pacotes NuGet relevantes são avaliados contra regras de catálogo e compatibilidade real.
7. Os achados são consolidados em um relatório HTML executivo.

### Componentes centrais

- `Program.cs`
  - orquestra a execução ponta a ponta
  - resolve a solution
  - carrega regras e catálogos
  - dispara scanners e gerador de relatório

- `Services/SolutionScanner.cs`
  - descobre projetos
  - extrai TFMs, referências e pacotes
  - usa `Microsoft.Build` quando possível
  - recorre a fallback em XML para projetos clássicos ou cenários single-file

- `Services/ApiScanner.cs`
  - percorre arquivos `.cs`
  - aplica regras regex vindas de `Rules/BlockingRules.json`

- `Services/NuGetChecker.cs`
  - consulta o feed do NuGet
  - ignora pacotes irrelevantes para runtime
  - cruza pacotes com catálogo server-side
  - suporta famílias com curingas, como `Microsoft.Owin.*`

- `Reporting/HtmlReportGenerator.cs`
  - gera um HTML autocontido
  - prioriza poucos bloqueadores críticos com impacto de negócio

## Catálogo de Regras

O catálogo principal está em `Rules/BlockingRules.json`.

Ele hoje cobre dois eixos:

- APIs legadas, como:
  - `System.Web.HttpContext.Current`
  - `System.Web.Security.FormsAuthentication`
  - `System.ServiceModel.*`
  - `System.Configuration.ConfigurationManager.AppSettings`

- pacotes server-side frequentes em projetos web legados, como:
  - `Microsoft.AspNet.Mvc`
  - `Microsoft.AspNet.WebApi.*`
  - `Microsoft.AspNet.SignalR.*`
  - `Microsoft.Owin.*`
  - `Microsoft.AspNet.Identity.*`
  - `EntityFramework`
  - `NHibernate*`
  - `AutoMapper*`
  - `Serilog*`
  - `Hangfire.*`
  - `Quartz.*`
  - `log4net`
  - `NLog.*`
  - `Elmah*`

Cada regra pode incluir:

- `impact`
- `effort`
- `alternative`
- `businessImpact`
- `monthlyInactionCost`
- `docs`

## Compatibilidade de Pacotes

O scanner avalia apenas pacotes que podem afetar a migração de runtime.

### Pacotes ignorados

Pacotes client-side, de front-end ou de build não entram como bloqueadores estruturais, por exemplo:

- `jquery`
- `bootstrap`
- `modernizr`
- `webgrease`
- `microsoft.typescript.msbuild`

Essa lista é mantida em `Rules/IrrelevantPackages.json`.

### Pacotes relevantes

Para pacotes server-side, o `NuGetChecker` combina:

- regra de catálogo do projeto
- compatibilidade real de assets/TFMs no NuGet
- fallback offline quando o feed não está acessível

Isso permite separar melhor:

- ruído operacional
- alertas de atualização
- bloqueadores de modernização

## Relatório HTML

O relatório gerado contém:

- resumo executivo da solution
- pontuação de risco
- até 4 bloqueadores críticos com impacto mensurável
- panorama dos projetos escaneados
- observações sobre pacotes não verificados offline

### Fórmula de risco

```text
((BloqueadoresCriticos * 12) + (Avisos * 6)) / TotalProjetos * 10
```

Regras:

- o valor máximo é `100`
- impactos `Alto` contam como bloqueadores críticos
- impactos `Médio` e `Baixo` contam como avisos

## Estrutura do Repositório

```text
MigrationCompass/
|- MigrationCompass.csproj
|- Program.cs
|- README.md
|- README.institucional.md
|- Rules/
|- Models/
|- Services/
|- Reporting/
|- Fixtures/
'\- MigrationCompass.Specs/
```

## Stack Técnica

- `.NET 10`
- `Microsoft.Build 18.8.2`
- `NuGet.Protocol 7.6.0`
- `System.CommandLine 2.0.10`

## Como Executar

### Exibir ajuda

```powershell
dotnet run --project .\MigrationCompass.csproj -- --help
```

### Escanear uma solution

```powershell
dotnet run --project .\MigrationCompass.csproj -- --sln "C:\LegacyApps\MinhaSolucao.sln" --output ".\artifacts"
```

### Publicar como executável portátil

```powershell
dotnet publish .\MigrationCompass.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Validação Local

### Build

```powershell
dotnet build .\MigrationCompass.csproj
```

### Executar os testes locais

```powershell
dotnet run --project .\MigrationCompass.Specs\MigrationCompass.Specs.csproj
```

### Executar o scan da fixture local

```powershell
dotnet run --project .\MigrationCompass.csproj -- --sln ".\Fixtures\SampleLegacySolution\SampleLegacySolution.sln" --output ".\artifacts"
```

## Limitações Atuais

- suporte apenas a projetos `.csproj`
- saída apenas em HTML
- ausência de correções automáticas
- ausência de integração nativa com pipeline CI/CD
- scanner de APIs baseado em regex
- análise NuGet dependente de metadados remotos quando online

## Próximas Evoluções

- separar o catálogo por domínios, como `web`, `auth`, `data` e `observability`
- ampliar análise de dependências transitivas
- exportar também em JSON ou CSV
- comparar execuções históricas
- adicionar um modo opcional com análise semântica via Roslyn

## Licença

Defina aqui a licença oficial do repositório antes da publicação no GitHub.

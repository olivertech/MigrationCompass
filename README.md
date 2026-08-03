# MigrationCompass

O `MigrationCompass` &eacute; uma ferramenta de console em `.NET 10` para an&aacute;lise t&eacute;cnica de solu&ccedil;&otilde;es legadas. O projeto foi criado para apoiar jornadas de migra&ccedil;&atilde;o para `.NET 10`, identificando riscos, depend&ecirc;ncias incompat&iacute;veis, APIs cr&iacute;ticas e o esfor&ccedil;o t&eacute;cnico associado.

Este README apresenta a vis&atilde;o t&eacute;cnica e detalhada da solu&ccedil;&atilde;o. Para uma vers&atilde;o institucional voltada a stakeholders e recrutadores t&eacute;cnicos, consulte `README.institucional.md`.

## Objetivo

O foco do `MigrationCompass` &eacute; executar uma avalia&ccedil;&atilde;o local e objetiva sobre uma solution `.NET`, produzindo um relat&oacute;rio HTML que ajude equipes t&eacute;cnicas e lideran&ccedil;as a responder:

- quais projetos est&atilde;o mais distantes do alvo `.NET 10`
- quais depend&ecirc;ncias podem bloquear a moderniza&ccedil;&atilde;o
- quais APIs exigir&atilde;o refatora&ccedil;&atilde;o
- qual &eacute; o risco agregado da solution

## Escopo Atual

A vers&atilde;o atual do projeto j&aacute; implementa:

- descoberta de solution e projetos `.csproj`
- leitura de `TargetFramework` e `TargetFrameworks`
- leitura de `PackageReference`, `Reference` e `packages.config`
- classifica&ccedil;&atilde;o de projetos pela dist&acirc;ncia at&eacute; `.NET 10`
- scanner de APIs legadas baseado em regras JSON
- verifica&ccedil;&atilde;o de compatibilidade de pacotes via `api.nuget.org`
- fallback offline para cen&aacute;rios sem acesso &agrave; internet
- gera&ccedil;&atilde;o de relat&oacute;rio HTML com vis&atilde;o executiva
- su&iacute;te local de valida&ccedil;&atilde;o sem frameworks de teste adicionais

## Arquitetura

### Fluxo principal

O fluxo da aplica&ccedil;&atilde;o segue a sequ&ecirc;ncia abaixo:

1. A CLI recebe os par&acirc;metros de entrada.
2. A solution &eacute; localizada e os projetos `.csproj` s&atilde;o descobertos.
3. Cada projeto &eacute; classificado por TFM.
4. As depend&ecirc;ncias e refer&ecirc;ncias s&atilde;o coletadas.
5. Os arquivos `.cs` s&atilde;o analisados com regras de APIs legadas.
6. Os pacotes NuGet s&atilde;o avaliados quanto &agrave; compatibilidade com `.NET 10`.
7. Os achados s&atilde;o consolidados em um relat&oacute;rio HTML.

### Componentes centrais

- `Program.cs`
  - orquestra a execu&ccedil;&atilde;o ponta a ponta
  - valida argumentos
  - resolve a solution
  - dispara scanners e o gerador de relat&oacute;rio

- `Services/SolutionScanner.cs`
  - descobre projetos na solution
  - extrai TFMs, refer&ecirc;ncias e depend&ecirc;ncias
  - tenta usar `Microsoft.Build`
  - utiliza fallback em XML quando a avalia&ccedil;&atilde;o completa do projeto falha

- `Services/ApiScanner.cs`
  - percorre arquivos `.cs`
  - aplica regras regex vindas de `Rules/BlockingRules.json`

- `Services/NuGetChecker.cs`
  - consulta o feed do NuGet
  - avalia compatibilidade de pacotes com `.NET 10`
  - trata indisponibilidade remota como aviso offline

- `Reporting/HtmlReportGenerator.cs`
  - gera o relat&oacute;rio final em HTML com CSS inline

## Classifica&ccedil;&atilde;o de Projetos

Os projetos s&atilde;o classificados conforme o risco estrutural da migra&ccedil;&atilde;o para `.NET 10`:

- `.NET Framework 4.x` = risco base alto
- `.NET Core 2.x / 3.x` = risco alto
- `.NET 5 / 6 / 7` = risco m&eacute;dio
- `.NET 8 / 9` = risco menor, mas ainda eleg&iacute;veis ao scanner
- `.NET 10` = refer&ecirc;ncia informativa

Essa classifica&ccedil;&atilde;o alimenta o contexto executivo do relat&oacute;rio e ajuda na prioriza&ccedil;&atilde;o da moderniza&ccedil;&atilde;o.

## Scanner de APIs Legadas

O cat&aacute;logo de regras est&aacute; armazenado em `Rules/BlockingRules.json` e cont&eacute;m regras curadas para identificar pontos cl&aacute;ssicos de ruptura em migra&ccedil;&otilde;es, como:

- `System.Web.HttpContext.Current`
- `System.Web.Security.FormsAuthentication`
- `System.ServiceModel.*`
- `System.Configuration.ConfigurationManager.AppSettings`

Cada ocorr&ecirc;ncia registrada inclui:

- identificador da regra
- categoria
- impacto
- esfor&ccedil;o estimado
- alternativa sugerida
- link de documenta&ccedil;&atilde;o

## Compatibilidade de Pacotes

O scanner analisa pacotes diretos do projeto e pacotes vindos de `packages.config`.

Quando o acesso ao `api.nuget.org` est&aacute; dispon&iacute;vel:

- vers&otilde;es publicadas s&atilde;o consultadas
- assets do pacote s&atilde;o avaliados
- a compatibilidade com `.NET 10` &eacute; inferida por framework suportado

Quando o ambiente est&aacute; offline:

- a execu&ccedil;&atilde;o continua
- o pacote &eacute; marcado como `Nao verificado offline`
- o relat&oacute;rio preserva a rastreabilidade da limita&ccedil;&atilde;o

## Relat&oacute;rio HTML

O relat&oacute;rio gerado cont&eacute;m:

- vis&atilde;o geral da solution
- data e hora do scan
- pontua&ccedil;&atilde;o de risco
- tabela de bloqueadores cr&iacute;ticos
- avisos e observa&ccedil;&otilde;es
- resumo dos projetos analisados

### F&oacute;rmula de risco

```text
((BloqueadoresCriticos * 12) + (Avisos * 6)) / TotalProjetos * 10
```

Regras:

- o valor m&aacute;ximo &eacute; `100`
- impactos `Alto` contam como bloqueadores cr&iacute;ticos
- impactos `Medio` e `Baixo` contam como avisos

## Estrutura do Reposit&oacute;rio

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

## Stack T&eacute;cnica

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

### Regras de resolu&ccedil;&atilde;o da solution

- `--sln` &eacute; opcional apenas quando existe exatamente uma solution no diret&oacute;rio atual
- se nenhuma solution for encontrada, a execu&ccedil;&atilde;o falha
- se m&uacute;ltiplas solutions forem encontradas sem `--sln`, a execu&ccedil;&atilde;o falha

## Valida&ccedil;&atilde;o Local

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

## Limita&ccedil;&otilde;es Atuais

- suporte apenas a projetos `.csproj`
- sa&iacute;da apenas em HTML
- aus&ecirc;ncia de corre&ccedil;&otilde;es autom&aacute;ticas
- aus&ecirc;ncia de integra&ccedil;&atilde;o com pipeline CI/CD
- scanner de APIs baseado em regex
- an&aacute;lise de compatibilidade NuGet depende de metadados remotos quando online

## Pr&oacute;ximas Evolu&ccedil;&otilde;es

- exporta&ccedil;&atilde;o em JSON ou CSV
- expans&atilde;o do cat&aacute;logo de regras
- an&aacute;lise mais profunda de depend&ecirc;ncias transitivas
- compara&ccedil;&atilde;o hist&oacute;rica entre scans
- modo opcional com an&aacute;lise sem&acirc;ntica via Roslyn

## Licen&ccedil;a

Defina aqui a licen&ccedil;a oficial do reposit&oacute;rio antes da publica&ccedil;&atilde;o no GitHub.
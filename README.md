# MigrationCompass

O `MigrationCompass` é uma ferramenta de console em `.NET 10` para análise local de solutions legadas, com foco em modernização até `.NET 10`, leitura executiva de risco e geração de relatório HTML defensável para arquitetura, gestão e assessment técnico.

Para uma visão resumida e institucional, consulte `README.institucional.md`.

## Objetivo

O projeto busca responder, com evidências técnicas e linguagem gerencial:

- quão distante a solution está de `.NET 10`
- quais dependências server-side elevam risco, retrabalho ou custo de transição
- quais APIs legadas exigirão refatoração relevante
- quais sinais estruturais merecem revisão antes de qualquer programa de migração
- quais cenários estratégicos parecem mais aderentes aos sinais encontrados

## O que a ferramenta faz

- descobre automaticamente `*.sln` e projetos `*.csproj`
- lê `TargetFramework`, `TargetFrameworks` e `TargetFrameworkVersion`
- classifica projetos de `.NET Framework 3.x/4.x`, `.NET Core 2.x/3.x` e `.NET 5` até `.NET 9`
- analisa `PackageReference`, `Reference` e `packages.config`
- detecta APIs legadas com base em `Rules/BlockingRules.json`
- avalia compatibilidade de pacotes relevantes com `.NET 10`
- ignora pacotes irrelevantes para runtime, como front-end, build e scaffolding
- detecta sinais heurísticos de fragilidade estrutural organizados por `SRP`, `OCP`, `LSP`, `ISP` e `DIP`
- exclui código gerado e scaffolding do score estrutural
- gera relatório HTML executivo em PT-BR

## O que a ferramenta não faz

- não reescreve código automaticamente
- não executa Roslyn nem análise semântica profunda nesta versão
- não substitui assessment técnico detalhado de arquitetura
- não gera orçamento fechado de projeto
- não envia o código-fonte analisado para servidores do produto
- não promete compatibilidade final apenas com base em regex, catálogos ou metadados

## Privacidade e Rede

- o código analisado permanece local durante o scan
- somente metadados de pacotes podem ser consultados no `NuGet.org`, quando a validação online está habilitada
- se o ambiente estiver offline, a execução continua e marca os pacotes como `Nao verificado offline`

## Métricas Principais

### Pontuação de risco

Score de `0` a `100` com saturação gradual, calculado com base em:

- bloqueadores distintos
- avisos distintos
- diversidade de categorias críticas
- quantidade de projetos
- volume aproximado da base em `KLOC`

Essa fórmula evita saturar rapidamente em `100` apenas por repetição do mesmo sintoma.

### Índice de fragilidade estrutural

Score de `0` a `100`, em que `100` representa o pior cenário observado.

A composição atual considera quatro vetores:

- risco de migração
- densidade de sinais estruturais
- idade tecnológica
- acoplamento a legado

Leitura gerencial:

- `0 a 39` — Controlável
- `40 a 64` — Moderada
- `65 a 84` — Alta
- `85 a 100` — Crítica

## Estrutura de Saída do Relatório

O HTML gerado prioriza:

- resumo executivo da solution
- pontuação de risco
- índice de fragilidade estrutural
- bloqueadores críticos relevantes
- exposição econômica orientativa por cenário
- sinais estruturais que merecem revisão
- drivers da decisão
- caminhos estratégicos possíveis
- panorama dos projetos
- artefatos gerados ou de scaffolding identificados

## Exposição Econômica

O relatório não apresenta mais custo mensal unitário por bloqueador individual.

Em vez disso, consolida cenários orientativos, como:

- sustentação operacional
- perda de produtividade
- atraso de entregas
- infraestrutura
- segurança e conformidade

Importante:

- não é orçamento
- não é soma direta por item
- é insumo inicial para priorização e aprofundamento do assessment

As premissas ficam em `Rules/EconomicParameters.json`.

## Catálogos e Regras

- `Rules/BlockingRules.json`
  - APIs legadas
  - pacotes server-side relevantes
  - impactos de negócio
  - alternativas de modernização
- `Rules/IrrelevantPackages.json`
  - pacotes ignorados por não afetarem runtime
- `Rules/EconomicParameters.json`
  - parâmetros econômicos globais

Exemplos de famílias hoje cobertas:

- `Microsoft.AspNet.Mvc`
- `Microsoft.AspNet.WebApi.*`
- `Microsoft.AspNet.SignalR.*`
- `Microsoft.Owin.*`
- `Microsoft.AspNet.Identity.*`
- `EntityFramework`
- `NHibernate*`
- `AutoMapper*`
- `Serilog*`
- `NLog*`
- `log4net`
- `Hangfire.*`
- `Quartz.*`
- `Elmah*`

## Requisitos

### Para desenvolvimento

- Windows com `PowerShell`
- SDK `.NET 10`

### Para distribuição

Há dois modos principais:

- `framework-dependent`
  - exige runtime compatível instalado na máquina
- `self-contained single-file`
  - gera `exe` portátil maior, sem dependência de runtime pré-instalado

## Como executar

### Ajuda

```powershell
dotnet run --project .\MigrationCompass.csproj -- --help
```

### Executar apontando a solution

```powershell
dotnet run --project .\MigrationCompass.csproj -- --sln "C:\LegacyApps\MinhaSolucao.sln" --output ".\artifacts"
```

### Executar por autodetecção

Se houver apenas um arquivo `.sln` no diretório atual:

```powershell
dotnet run --project .\MigrationCompass.csproj -- --output ".\artifacts"
```

### Rodar o `.exe` dentro da pasta da solution legada

Exemplo real de uso local, com o executável publicado copiado para a pasta do legado:

```powershell
PS E:\2-PROJETOS\6IX\Projetos\GIT-API> .\MigrationCompass.exe --sln ".\ContabilAppAPI.sln" --output ".\relatorio"
```

Esse comando:

- executa o `MigrationCompass.exe` no diretório atual
- analisa a solution `.\ContabilAppAPI.sln`
- gera o HTML em `.\relatorio`

### Publish self-contained single-file

```powershell
dotnet publish .\MigrationCompass.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Exemplo Anonimizado de Uso

Entrada:

```powershell
.\MigrationCompass.exe --sln ".\SistemaLegado.sln" --output ".\relatorio"
```

Saída esperada:

- identificação dos projetos e TFMs
- bloqueadores relevantes de runtime e web legado
- leitura consultiva do cenário
- relatório HTML pronto para discussão com liderança técnica e gestão

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

## Testes

```powershell
dotnet run --project .\MigrationCompass.Specs\MigrationCompass.Specs.csproj -c Release
```

## Licenciamento Atual

No estado atual deste repositório, não há licença open source de redistribuição pública. O material deve ser tratado como uso restrito do mantenedor, salvo autorização explícita em sentido diverso.

## Observação de Posicionamento

O relatório gerado é orientativo. Ele ajuda a estruturar discovery, priorização e conversas executivas, mas não encerra sozinho uma decisão de investimento, replatforming ou reconstrução.

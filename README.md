# MigrationCompass

O `MigrationCompass` Ã© uma ferramenta de anÃ¡lise tÃ©cnica criada para apoiar programas de modernizaÃ§Ã£o de aplicaÃ§Ãµes `.NET`. Seu propÃ³sito Ã© identificar riscos, bloqueadores e esforÃ§os de migraÃ§Ã£o em soluÃ§Ãµes legadas, gerando um relatÃ³rio HTML executivo voltado Ã  tomada de decisÃ£o.

ConstruÃ­do em `.NET 10`, o projeto combina leitura de solutions, classificaÃ§Ã£o de projetos, anÃ¡lise de dependÃªncias e detecÃ§Ã£o de APIs legadas para oferecer uma visÃ£o estruturada da jornada de migraÃ§Ã£o para a plataforma alvo.

## Proposta de Valor

Em iniciativas de modernizaÃ§Ã£o, uma das maiores dificuldades nÃ£o Ã© apenas migrar cÃ³digo, mas entender com clareza:

- onde estÃ£o os maiores riscos tÃ©cnicos
- quais dependÃªncias podem comprometer cronograma e custo
- quais projetos exigirÃ£o refatoraÃ§Ãµes mais profundas
- como comunicar esse cenÃ¡rio para lideranÃ§a tÃ©cnica e executiva

O `MigrationCompass` foi desenhado para responder exatamente a essas perguntas com uma abordagem:

- local-first
- objetiva
- auditÃ¡vel
- orientada a decisÃ£o

## VisÃ£o do Produto

O projeto nasce como um scanner tÃ©cnico para avaliaÃ§Ã£o prÃ©-migraÃ§Ã£o de soluÃ§Ãµes `.NET`, com foco em ambientes corporativos onde seguranÃ§a, rastreabilidade e simplicidade operacional sÃ£o fatores crÃ­ticos.

Em vez de depender de painÃ©is externos ou fluxos distribuÃ­dos, o `MigrationCompass` concentra a anÃ¡lise em uma execuÃ§Ã£o local, produzindo um artefato final que pode ser compartilhado com:

- arquitetos de software
- tech leads
- gerentes de modernizaÃ§Ã£o
- sponsors de transformaÃ§Ã£o digital
- recrutadores tÃ©cnicos interessados em arquitetura, tooling e engenharia de plataforma

## Diferenciais

- ExecuÃ§Ã£o local sem dependÃªncia de plataforma SaaS
- RelatÃ³rio HTML pronto para consumo executivo
- Foco em migraÃ§Ã£o para `.NET 10`
- Compatibilidade com cenÃ¡rios legados e hÃ­bridos
- Estrutura simples, objetiva e fÃ¡cil de evoluir

## Principais Capacidades

### Descoberta de solution e projetos

- leitura de arquivo `.sln` informado pela CLI
- descoberta automÃ¡tica de `*.sln` no diretÃ³rio atual quando aplicÃ¡vel
- anÃ¡lise restrita a projetos `.csproj`
- exclusÃ£o de tipos nÃ£o suportados como `.vcxproj` e `.fsproj`

### ClassificaÃ§Ã£o de maturidade de migraÃ§Ã£o

Os projetos sÃ£o classificados segundo sua distÃ¢ncia estrutural atÃ© `.NET 10`:

- `.NET Framework 4.x`
- `.NET Core 2.x / 3.x`
- `.NET 5 / 6 / 7`
- `.NET 8 / 9`
- `.NET 10`

Essa classificaÃ§Ã£o ajuda a traduzir detalhe tÃ©cnico em leitura executiva de impacto.

### Leitura de dependÃªncias

O scanner coleta:

- `PackageReference`
- `Reference`
- `packages.config`

Isso permite mapear dependÃªncias modernas e legadas no mesmo ecossistema.

### DetecÃ§Ã£o de APIs legadas

O catÃ¡logo de regras embutido detecta APIs historicamente sensÃ­veis em jornadas de modernizaÃ§Ã£o, como:

- `System.Web.HttpContext.Current`
- `System.Web.Security.FormsAuthentication`
- `System.ServiceModel.*`
- `System.Configuration.ConfigurationManager.AppSettings`

Cada ocorrÃªncia Ã© enriquecida com:

- identificador da regra
- impacto
- esforÃ§o estimado
- alternativa sugerida
- referÃªncia de documentaÃ§Ã£o

### VerificaÃ§Ã£o de compatibilidade de pacotes

Quando hÃ¡ acesso ao `api.nuget.org`, o `MigrationCompass` avalia a compatibilidade de pacotes com `.NET 10` por meio da anÃ¡lise de assets e frameworks suportados.

Em ambientes sem acesso externo:

- a execuÃ§Ã£o continua normalmente
- o relatÃ³rio Ã© gerado
- os pacotes sÃ£o marcados como `Nao verificado offline`

### RelatÃ³rio executivo em HTML

O artefato final Ã© um relatÃ³rio autocontido com:

- visÃ£o executiva da solution
- pontuaÃ§Ã£o de risco
- bloqueadores crÃ­ticos
- avisos e observaÃ§Ãµes
- resumo por projeto

Esse formato foi pensado para leitura rÃ¡pida por pÃºblicos tÃ©cnicos e nÃ£o tÃ©cnicos.

## PÃºblico-Alvo

O projeto Ã© especialmente relevante para:

- arquitetos de software
- especialistas em modernizaÃ§Ã£o
- tech leads
- engenheiros de plataforma
- consultores de transformaÃ§Ã£o digital
- recrutadores tÃ©cnicos que desejam avaliar repertÃ³rio em anÃ¡lise de legado, tooling interno e arquitetura .NET

## Stack TÃ©cnica

- `.NET 10`
- `Microsoft.Build 18.8.2`
- `NuGet.Protocol 7.6.0`
- `System.CommandLine 2.0.10`

O projeto evita dependÃªncias desnecessÃ¡rias e privilegia uma arquitetura simples, extensÃ­vel e de fÃ¡cil manutenÃ§Ã£o.

## Arquitetura da SoluÃ§Ã£o

### NÃºcleo da aplicaÃ§Ã£o

- `Program.cs`
  - entrada da CLI
  - coordenaÃ§Ã£o do fluxo de anÃ¡lise

- `Services/SolutionScanner.cs`
  - descoberta da solution
  - leitura de projetos, TFMs e dependÃªncias
  - fallback controlado por XML para cenÃ¡rios onde a avaliaÃ§Ã£o completa por `Microsoft.Build` nÃ£o esteja disponÃ­vel

- `Services/ApiScanner.cs`
  - varredura de cÃ³digo com regras regex

- `Services/NuGetChecker.cs`
  - verificaÃ§Ã£o de compatibilidade de pacotes
  - tolerÃ¢ncia a cenÃ¡rios offline

- `Reporting/HtmlReportGenerator.cs`
  - geraÃ§Ã£o do relatÃ³rio HTML executivo

- `Rules/BlockingRules.json`
  - regras de bloqueio e orientaÃ§Ã£o de migraÃ§Ã£o

### ValidaÃ§Ã£o local

- `MigrationCompass.Specs/`
  - suÃ­te executÃ¡vel de validaÃ§Ã£o
  - garante cobertura dos fluxos essenciais sem adicionar frameworks extras

## Estrutura do RepositÃ³rio

```text
MigrationCompass/
â”œâ”€â”€ MigrationCompass.csproj
â”œâ”€â”€ Program.cs
â”œâ”€â”€ README.md
â”œâ”€â”€ Rules/
â”œâ”€â”€ Models/
â”œâ”€â”€ Services/
â”œâ”€â”€ Reporting/
â”œâ”€â”€ Fixtures/
â””â”€â”€ MigrationCompass.Specs/
```

## Como Executar

### Exibir ajuda

```powershell
dotnet run --project .\MigrationCompass.csproj -- --help
```

### Escanear uma solution

```powershell
dotnet run --project .\MigrationCompass.csproj -- --sln "C:\LegacyApps\MinhaSolucao.sln" --output ".\artifacts"
```

### Comportamento padrÃ£o

- `--sln` Ã© opcional apenas se existir exatamente uma solution no diretÃ³rio atual
- `--output` usa o diretÃ³rio atual por padrÃ£o
- `--format` atualmente aceita apenas `html`

## Exemplo de SaÃ­da

O relatÃ³rio Ã© salvo como:

```text
<output>\<NomeDaSolution>-relatorio-migracao.html
```

Exemplo:

```text
artifacts\SampleLegacySolution-relatorio-migracao.html
```

## PontuaÃ§Ã£o de Risco

A pontuaÃ§Ã£o de risco atual segue a fÃ³rmula:

```text
((BloqueadoresCriticos * 12) + (Avisos * 6)) / TotalProjetos * 10
```

Regras aplicadas:

- pontuaÃ§Ã£o mÃ¡xima de `100`
- itens com impacto `Alto` contam como bloqueadores crÃ­ticos
- itens com impacto `Medio` ou `Baixo` contam como avisos

## ExecuÃ§Ã£o em Ambientes Restritos

O `MigrationCompass` foi pensado para ambientes corporativos restritos.

Se o `api.nuget.org` nÃ£o estiver acessÃ­vel:

- o scan de projetos continua
- o scan de APIs continua
- o relatÃ³rio HTML continua sendo gerado
- a compatibilidade de pacotes passa para `Nao verificado offline`

## ValidaÃ§Ã£o do Projeto

O projeto possui um runner leve de testes locais.

### Build

```powershell
dotnet build .\MigrationCompass.csproj
```

### Executar testes locais

```powershell
dotnet run --project .\MigrationCompass.Specs\MigrationCompass.Specs.csproj
```

### Executar o scan da fixture de exemplo

```powershell
dotnet run --project .\MigrationCompass.csproj -- --sln ".\Fixtures\SampleLegacySolution\SampleLegacySolution.sln" --output ".\artifacts"
```

## Estado Atual

O projeto jÃ¡ possui um MVP funcional com:

- descoberta de solution
- classificaÃ§Ã£o de projetos por TFM
- leitura de dependÃªncias
- scanner de APIs legadas
- verificaÃ§Ã£o NuGet com fallback offline
- geraÃ§Ã£o de relatÃ³rio HTML
- validaÃ§Ã£o automatizada local

## LimitaÃ§Ãµes Atuais

Como todo MVP tÃ©cnico, esta versÃ£o ainda possui limites claros:

- suporte apenas a `.csproj`
- saÃ­da apenas em HTML
- ausÃªncia de correÃ§Ãµes automÃ¡ticas
- ausÃªncia de integraÃ§Ã£o com CI/CD
- scanner de API baseado em regex
- dependÃªncia de consulta remota para anÃ¡lise NuGet mais completa

## PrÃ³ximas EvoluÃ§Ãµes

EvoluÃ§Ãµes naturais para prÃ³ximas versÃµes:

- exportaÃ§Ã£o em JSON ou CSV
- enriquecimento das recomendaÃ§Ãµes de pacotes
- ampliaÃ§Ã£o do catÃ¡logo de regras
- comparaÃ§Ã£o entre mÃºltiplos scans
- anÃ¡lise mais profunda de dependÃªncias transitivas
- modo opcional com Roslyn para anÃ¡lise semÃ¢ntica

## Casos de Uso

- discovery tÃ©cnico antes de programas de modernizaÃ§Ã£o
- suporte a aprovaÃ§Ã£o de budget para migraÃ§Ã£o
- avaliaÃ§Ã£o de risco para adoÃ§Ã£o de `.NET 10`
- preparaÃ§Ã£o de material para sponsors e stakeholders

## Para Recrutadores TÃ©cnicos

Este projeto demonstra experiÃªncia prÃ¡tica em:

- arquitetura de ferramentas internas
- anÃ¡lise de sistemas legados
- engenharia de plataforma em `.NET`
- design de CLI
- geraÃ§Ã£o de relatÃ³rios tÃ©cnicos orientados a negÃ³cio
- equilÃ­brio entre profundidade tÃ©cnica e comunicaÃ§Ã£o executiva

## ContribuiÃ§Ã£o

Ao contribuir com o projeto:

- preserve a abordagem local-first
- evite dependÃªncias desnecessÃ¡rias
- mantenha o relatÃ³rio autocontido
- prefira mudanÃ§as pequenas, objetivas e verificÃ¡veis

## LicenÃ§a

Defina aqui a licenÃ§a oficial do repositÃ³rio antes da publicaÃ§Ã£o no GitHub.
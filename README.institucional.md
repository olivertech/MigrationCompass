# MigrationCompass

O `MigrationCompass` é uma ferramenta de análise de legado criada para apoiar decisões de modernização em ambientes `.NET`, traduzindo sinais técnicos em uma leitura executiva clara sobre risco, bloqueios e esforço de migração até `.NET 10`.

## Proposta de Valor

Em programas de modernização, não basta saber que o sistema é legado. É preciso entender:

- onde estão os principais bloqueadores
- quanto esse legado custa para evoluir
- quais frentes exigem mais investimento
- quais riscos podem afetar prazo, operação e estabilidade

O `MigrationCompass` foi concebido para responder a essas perguntas com um relatório objetivo, acionável e orientado à tomada de decisão.

## O que a solução entrega

- leitura automatizada de solutions e projetos `.NET`
- classificação de maturidade de migração
- identificação de APIs legadas e dependências server-side críticas
- separação entre ruído técnico e bloqueadores reais
- relatório HTML executivo pronto para discussão com arquitetura, gestão e sponsors
- faixas de custo orientativas baseadas em premissas configuráveis
- leitura consultiva com caminhos estratégicos possíveis para decisão
- avaliação heurística de fragilidades estruturais associadas a princípios SOLID
- pontuação estrutural de manutenibilidade para apoiar priorização executiva

## Destaques atuais

A versão atual do projeto já cobre cenários frequentes em aplicações web legadas, incluindo:

- `ASP.NET MVC 5`
- `ASP.NET Web API 2`
- `OWIN/Katana`
- `ASP.NET Identity`
- `SignalR`
- `Entity Framework 6`
- `NHibernate`
- `AutoMapper`
- `Serilog`, `NLog` e `log4net`
- `Hangfire`, `Quartz` e observabilidade legado

Com isso, o relatório consegue refletir não apenas problemas de compatibilidade, mas também frentes típicas de retrabalho em sistemas corporativos reais.

## Como o custo é tratado

O projeto passa a trabalhar com **faixas orientativas de custo estimado de inação**, construídas a partir de premissas explícitas de esforço técnico, composição de equipe e exposição operacional.

Isso ajuda a tornar o relatório mais confiável para conversas iniciais com liderança, sem prometer uma precisão financeira que só faria sentido em uma avaliação mais aprofundada.

## Valor para Stakeholders

O projeto ajuda lideranças a responder perguntas estratégicas:

- Quais aplicações devem entrar primeiro na jornada de modernização?
- Onde há maior risco de custo oculto?
- Quais dependências exigirão replanejamento técnico?
- Em quais pontos a inação já representa custo operacional ou risco de SLA?
- Quando ainda faz sentido migrar e quando a reconstrução gradual passa a ser uma alternativa mais racional?
- Quais sinais de complexidade estrutural do código podem encarecer ou inviabilizar a evolução do legado?

## Valor para Recrutadores Técnicos

Este repositório demonstra competências práticas em:

- arquitetura de ferramentas internas
- engenharia de plataforma em `.NET`
- análise de sistemas legados
- desenho de CLI
- geração de artefatos executivos a partir de evidências técnicas
- equilíbrio entre profundidade de engenharia e clareza de comunicação

## Diferenciais

- execução local
- foco em `.NET 10`
- baixo acoplamento
- catálogo extensível de APIs e pacotes legados
- visão executiva com impacto de negócio e custo estimado de inação

## Perfil de Uso

O `MigrationCompass` é especialmente útil para:

- arquitetos de software
- tech leads
- consultores de modernização
- sponsors de transformação digital
- recrutadores técnicos interessados em projetos de arquitetura e tooling

## Estado Atual do Projeto

Em `04/08/2026`, o projeto já possui um MVP funcional com:

- scanner de projetos `.csproj`
- análise de TFMs desde `.NET Framework 3.x/4.x` até `.NET 9`
- classificação de risco para migração ao alvo `.NET 10`
- catálogo ampliado de dependências server-side web
- modelo econômico parametrizado por faixas
- fallback offline para análise de pacotes
- relatório HTML executivo

## Próximos Passos Possíveis

- segmentar o catálogo por domínios de arquitetura
- ampliar análise de dependências transitivas
- adicionar exportação JSON/CSV
- comparar execuções históricas
- aprofundar análise semântica opcional com Roslyn

## Documento Técnico

Para detalhes de arquitetura, execução, componentes, validação e estrutura do código, consulte `README.md`.

# MigrationCompass

O `MigrationCompass` é uma ferramenta de análise local de sistemas legados `.NET` criada para apoiar decisões de modernização com uma leitura mais confiável, objetiva e executiva sobre risco, fragilidade estrutural e caminhos possíveis até `.NET 10`.

## Proposta de Valor

Em programas de modernização, o maior desafio nem sempre é identificar que o sistema é antigo. O desafio real é traduzir sinais técnicos em uma leitura que gestores, arquitetos e sponsors consigam usar para decidir com mais segurança.

O `MigrationCompass` foi desenhado para isso.

## O que ele entrega

- leitura automatizada de solutions e projetos `.NET`
- identificação de APIs legadas e dependências server-side críticas
- separação entre ruído técnico e bloqueadores relevantes
- índice de fragilidade estrutural para apoiar priorização
- exposição econômica orientativa por cenário
- relatório HTML executivo em PT-BR

## Diferenciais

- execução local
- foco específico em modernização até `.NET 10`
- leitura consultiva com linguagem menos genérica
- exclusão de scaffolding e código gerado do score estrutural
- taxonomia SOLID usada como apoio analítico, não como veredito absoluto

## Privacidade

- o código analisado não é enviado para servidores do produto
- somente metadados de pacotes podem ser consultados no `NuGet.org`, quando disponível
- o scanner continua funcionando offline, sinalizando limitações de validação quando necessário

## Para quem faz sentido

- arquitetos de software
- tech leads
- consultorias de modernização
- sponsors de transformação digital
- recrutadores técnicos avaliando profundidade de engenharia e visão de produto

## Exemplo de uso em campo

```powershell
PS E:\2-PROJETOS\6IX\Projetos\GIT-API> .\MigrationCompass.exe --sln ".\ContabilAppAPI.sln" --output ".\relatorio"
```

## Leitura importante

O relatório não substitui um assessment aprofundado. Ele funciona como instrumento inicial de triagem, priorização e preparação para uma avaliação técnica e de negócio mais detalhada.

## Documento técnico

Para detalhes de arquitetura, métricas, execução, publicação e testes, consulte `README.md`.

# MigrationCompass

MigrationCompass is a local-first `.NET 10` console scanner for legacy `.NET` solutions. It inspects solution and project files, identifies migration blockers, evaluates package compatibility, scans for risky legacy APIs, and generates an executive HTML report to support migration planning toward `.NET 10`.

## Overview

Many teams still operate solutions built on `.NET Framework 4.x`, `.NET Core 2.x/3.x`, and early unified `.NET` releases. MigrationCompass was created to help architects, technical leaders, and modernization sponsors understand:

- which projects are farther from `.NET 10`
- which packages may block or delay a migration
- which legacy APIs are likely to require refactoring
- how much migration risk exists across the solution

The tool is designed to run locally in restricted enterprise environments, without cloud dependencies or external dashboards.

## Current Status

This repository currently contains the first functional MVP of MigrationCompass, including:

- solution and `.csproj` discovery
- target framework classification
- `PackageReference`, `Reference`, and `packages.config` parsing
- legacy API scanning with curated JSON rules
- NuGet compatibility verification with offline fallback
- HTML executive report generation
- local fixture-based validation tests

## Goals

MigrationCompass focuses on enabling fast technical assessment before large migration investments are approved.

Primary goals:

- scan existing `.NET` solutions locally
- identify blockers for migration to `.NET 10`
- produce a stakeholder-friendly HTML report
- reduce hidden migration risk early in planning

## Key Features

### 1. Solution Discovery

- Accepts a `.sln` path through CLI
- Supports automatic discovery of a single `*.sln` in the current directory
- Scans only `.csproj` projects
- Ignores unsupported project types such as `.vcxproj` and `.fsproj`

### 2. Project Classification

Each project is classified by migration distance to `.NET 10`:

- `.NET Framework 4.x` = highest base risk
- `.NET Core 2.x / 3.x` = high risk
- `.NET 5 / 6 / 7` = medium risk
- `.NET 8 / 9` = lower risk, but still fully scanned
- `.NET 10` = informational reference

### 3. Package Compatibility Analysis

- Reads direct package references from project files
- Reads `packages.config` when present
- Queries `api.nuget.org` for package metadata when network access is available
- Evaluates compatibility using package assets and target framework compatibility
- Falls back to `Nao verificado offline` when remote validation is unavailable

### 4. Legacy API Scanner

The scanner uses curated rules stored in `Rules/BlockingRules.json` to detect risky APIs commonly found in migration programs, such as:

- `System.Web.HttpContext.Current`
- `System.Web.Security.FormsAuthentication`
- `System.ServiceModel.*`
- `System.Configuration.ConfigurationManager.AppSettings`

Each finding includes:

- rule identifier
- impact level
- estimated effort
- recommended alternative
- documentation link

### 5. HTML Executive Report

The report is generated as a standalone HTML file with inline CSS only.

It includes:

- solution summary
- risk score
- critical blockers table
- warnings and informational notes
- project-level migration profile

## Architecture

The project is intentionally simple and local-first.

### Main Components

- `Program.cs`
  - CLI entry point
  - orchestrates scan flow and report generation

- `Services/SolutionScanner.cs`
  - discovers projects
  - extracts TFMs, references, and package metadata
  - uses `Microsoft.Build` when possible
  - falls back to XML parsing for SDK-style project compatibility scenarios

- `Services/ApiScanner.cs`
  - scans source files using regex-based rule matching

- `Services/NuGetChecker.cs`
  - checks package compatibility with `.NET 10`
  - tolerates offline execution by downgrading remote failures to warnings

- `Reporting/HtmlReportGenerator.cs`
  - produces the final HTML report

- `Rules/BlockingRules.json`
  - embedded rule catalog for blocker detection

- `MigrationCompass.Specs/`
  - local executable spec runner without additional test framework dependencies

## Project Structure

```text
MigrationCompass/
â”œâ”€â”€ MigrationCompass.csproj
â”œâ”€â”€ Program.cs
â”œâ”€â”€ README.md
â”œâ”€â”€ Rules/
â”‚   â””â”€â”€ BlockingRules.json
â”œâ”€â”€ Models/
â”‚   â””â”€â”€ ScanModels.cs
â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ ApiScanner.cs
â”‚   â”œâ”€â”€ FlatContainerNuGetClient.cs
â”‚   â”œâ”€â”€ INuGetPackageClient.cs
â”‚   â”œâ”€â”€ MsBuildEnvironment.cs
â”‚   â”œâ”€â”€ NuGetChecker.cs
â”‚   â”œâ”€â”€ ProjectClassification.cs
â”‚   â”œâ”€â”€ ReportSummaryBuilder.cs
â”‚   â”œâ”€â”€ RuleCatalog.cs
â”‚   â””â”€â”€ SolutionScanner.cs
â”œâ”€â”€ Reporting/
â”‚   â””â”€â”€ HtmlReportGenerator.cs
â”œâ”€â”€ Fixtures/
â”‚   â””â”€â”€ SampleLegacySolution/
â””â”€â”€ MigrationCompass.Specs/
    â”œâ”€â”€ MigrationCompass.Specs.csproj
    â””â”€â”€ Program.cs
```

## Requirements

- Windows with `.NET SDK 10.0.x`
- Local filesystem access to the target solution
- Optional access to `https://api.nuget.org` for package compatibility validation

## Dependencies

The current project uses:

- `Microsoft.Build 18.8.2`
- `NuGet.Protocol 7.6.0`
- `System.CommandLine 2.0.10`

No frontend frameworks, databases, or cloud services are required.

## CLI Usage

### Help

```powershell
dotnet run --project .\MigrationCompass.csproj -- --help
```

### Scan a solution

```powershell
dotnet run --project .\MigrationCompass.csproj -- --sln "C:\LegacyApps\MinhaSolucao.sln" --output ".\artifacts"
```

### Default behavior

- `--sln` is optional only when exactly one solution exists in the current directory
- `--output` defaults to the current directory
- `--format` currently supports only `html`

## Output

The report is generated as:

```text
<output>\<SolutionName>-relatorio-migracao.html
```

Example:

```text
artifacts\SampleLegacySolution-relatorio-migracao.html
```

## Risk Score

MigrationCompass calculates a capped risk score using the current formula:

```text
((CriticalBlockers * 12) + (Warnings * 6)) / TotalProjects * 10
```

Rules:

- maximum score is `100`
- `impact = Alto` contributes to critical blockers
- `impact = Medio` or `Baixo` contributes to warnings

## Offline Behavior

MigrationCompass is designed to keep scanning even in restricted environments.

When `api.nuget.org` is unavailable:

- project scanning still runs
- API scanning still runs
- HTML report is still generated
- package compatibility is marked as `Nao verificado offline`

## Validation

The repository includes a lightweight executable spec project.

### Run build

```powershell
dotnet build .\MigrationCompass.csproj
```

### Run validation specs

```powershell
dotnet run --project .\MigrationCompass.Specs\MigrationCompass.Specs.csproj
```

### Run sample scan

```powershell
dotnet run --project .\MigrationCompass.csproj -- --sln ".\Fixtures\SampleLegacySolution\SampleLegacySolution.sln" --output ".\artifacts"
```

## Design Decisions

### Why local-first?

Many corporate modernization programs operate in environments with strict network controls. MigrationCompass avoids cloud dependencies and keeps the workflow portable and auditable.

### Why regex instead of Roslyn?

For the MVP, regex keeps the scanner smaller and easier to run with fewer moving parts. It is sufficient for an initial blocker-oriented assessment.

### Why use an MSBuild plus XML fallback approach?

`Microsoft.Build` is the preferred path for evaluating projects accurately, but SDK-style loading can fail in constrained runtime contexts. The fallback parser ensures the scanner still extracts the metadata required for migration assessment.

## Limitations

Current MVP limitations:

- only `.csproj` projects are supported
- report output is HTML only
- no automatic code fixes are applied
- no CI/CD integration is included
- API scanning is regex-based and may not capture every semantic edge case
- NuGet compatibility depends on remote metadata when online

## Roadmap Ideas

Potential next iterations:

- JSON or CSV export
- richer package recommendation heuristics
- broader blocking rule catalog
- solution trend comparison across multiple scans
- deeper TFM and transitive dependency analysis
- optional Roslyn-based semantic scanning mode

## Example Use Cases

- pre-migration discovery for enterprise monoliths
- sponsorship material for modernization funding
- risk assessment for platform upgrade programs
- technical due diligence before `.NET 10` adoption

## Contributing

If you contribute to this repository:

- keep the tool local-first
- avoid unnecessary dependencies
- preserve report portability
- prefer focused, testable additions

## License

Add the repository license here when publishing the project to GitHub.
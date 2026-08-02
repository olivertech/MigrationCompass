using MigrationCompass.Models;

namespace MigrationCompass.Services;

public static class ProjectClassification
{
    public static ProjectMigrationProfile Classify(IEnumerable<string> targetFrameworks)
    {
        var profiles = targetFrameworks.Select(ClassifySingle).OrderByDescending(profile => profile.Weight).ToArray();
        return profiles.FirstOrDefault() ?? new ProjectMigrationProfile
        {
            Classification = "Nao identificado",
            Impact = "Medio",
            Summary = "Nao foi possivel identificar o framework atual.",
            Weight = 2
        };
    }

    private static ProjectMigrationProfile ClassifySingle(string tfm)
    {
        var normalized = tfm.Trim().ToLowerInvariant();
        if (normalized.StartsWith("net4", StringComparison.Ordinal))
        {
            return new ProjectMigrationProfile
            {
                Classification = ".NET Framework 4.x",
                Impact = "Alto",
                Summary = "Projeto legado em .NET Framework 4.x com maior distancia estrutural para .NET 10.",
                Weight = 4
            };
        }

        if (normalized.StartsWith("netcoreapp2", StringComparison.Ordinal) || normalized.StartsWith("netcoreapp3", StringComparison.Ordinal))
        {
            return new ProjectMigrationProfile
            {
                Classification = ".NET Core 2.x/3.x",
                Impact = "Alto",
                Summary = "Projeto em .NET Core legado com APIs e dependencias propensas a ruptura na migracao para .NET 10.",
                Weight = 4
            };
        }

        if (normalized.StartsWith("net5", StringComparison.Ordinal) ||
            normalized.StartsWith("net6", StringComparison.Ordinal) ||
            normalized.StartsWith("net7", StringComparison.Ordinal))
        {
            return new ProjectMigrationProfile
            {
                Classification = ".NET 5-7",
                Impact = "Medio",
                Summary = "Projeto ja no ecossistema unificado, mas ainda requer revisao de breaking changes e dependencias.",
                Weight = 3
            };
        }

        if (normalized.StartsWith("net8", StringComparison.Ordinal) || normalized.StartsWith("net9", StringComparison.Ordinal))
        {
            return new ProjectMigrationProfile
            {
                Classification = ".NET 8-9",
                Impact = "Baixo",
                Summary = "Projeto proximo do alvo .NET 10, mantendo o scanner para dependencias, APIs e ajustes finais.",
                Weight = 2
            };
        }

        if (normalized.StartsWith("net10", StringComparison.Ordinal))
        {
            return new ProjectMigrationProfile
            {
                Classification = ".NET 10",
                Impact = "Informacao",
                Summary = "Projeto ja alinhado ao TFM alvo e listado apenas para contexto da solution.",
                Weight = 1
            };
        }

        return new ProjectMigrationProfile
        {
            Classification = "Outro TFM",
            Impact = "Medio",
            Summary = $"TFM '{tfm}' exige validacao manual de compatibilidade para .NET 10.",
            Weight = 2
        };
    }
}
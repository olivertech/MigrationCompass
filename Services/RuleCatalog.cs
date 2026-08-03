using System.Reflection;
using System.Text.Json;
using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Carrega o catÃ¡logo JSON de regras de bloqueio usado pelo scanner de APIs.
/// </summary>
public static class RuleCatalog
{
    /// <summary>
    /// Desserializa o arquivo de regras a partir de um arquivo fÃ­sico ou de um recurso embutido no assembly.
    /// </summary>
    public static async Task<IReadOnlyList<ApiRule>> LoadAsync(string? path, CancellationToken cancellationToken)
    {
        await using var stream = OpenRulesStream(path);
        var rules = await JsonSerializer.DeserializeAsync<List<ApiRule>>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            },
            cancellationToken);

        return rules ?? [];
    }

    /// <summary>
    /// Resolve o stream das regras priorizando arquivo fÃ­sico e usando recurso embutido como fallback para publish single-file.
    /// </summary>
    private static Stream OpenRulesStream(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            return File.OpenRead(path);
        }

        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "MigrationCompass.Rules.BlockingRules.json";
        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is not null)
        {
            return resourceStream;
        }

        throw new FileNotFoundException(
            "Nao foi possivel localizar o arquivo ou recurso embutido das regras de bloqueio.",
            path ?? resourceName);
    }
}

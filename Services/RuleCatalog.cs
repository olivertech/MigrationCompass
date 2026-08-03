using System.Text.Json;
using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Carrega o catÃ¡logo JSON de regras de bloqueio usado pelo scanner de APIs.
/// </summary>
public static class RuleCatalog
{
    /// <summary>
    /// Desserializa o arquivo de regras preservando tolerÃ¢ncia a variaÃ§Ãµes de casing no JSON.
    /// </summary>
    public static async Task<IReadOnlyList<ApiRule>> LoadAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var rules = await JsonSerializer.DeserializeAsync<List<ApiRule>>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            },
            cancellationToken);
        return rules ?? [];
    }
}
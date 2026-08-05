using System.Reflection;
using System.Text.Json;
using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Carrega as premissas econômicas padrão utilizadas para estimar ranges mensais no relatório.
/// </summary>
public static class EconomicParametersCatalog
{
    /// <summary>
    /// Desserializa as premissas a partir de arquivo físico ou recurso embutido.
    /// </summary>
    public static async Task<EconomicParameters> LoadAsync(string? path, CancellationToken cancellationToken)
    {
        await using var stream = OpenStream(path);
        var parameters = await JsonSerializer.DeserializeAsync<EconomicParameters>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            },
            cancellationToken);

        return parameters ?? throw new InvalidOperationException("Nao foi possivel carregar as premissas economicas.");
    }

    private static Stream OpenStream(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            return File.OpenRead(path);
        }

        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "MigrationCompass.Rules.EconomicParameters.json";
        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is not null)
        {
            return resourceStream;
        }

        throw new FileNotFoundException(
            "Nao foi possivel localizar as premissas economicas.",
            path ?? resourceName);
    }
}

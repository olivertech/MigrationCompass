using System.Reflection;
using System.Text.Json;

namespace MigrationCompass.Services;

/// <summary>
/// Carrega a lista de pacotes irrelevantes para migracao de runtime .NET.
/// </summary>
public static class IrrelevantPackageCatalog
{
    /// <summary>
    /// Desserializa a lista de pacotes irrelevantes a partir de arquivo fisico ou recurso embutido.
    /// </summary>
    public static async Task<IReadOnlySet<string>> LoadAsync(string? path, CancellationToken cancellationToken)
    {
        await using var stream = OpenStream(path);
        var packages = await JsonSerializer.DeserializeAsync<List<string>>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            },
            cancellationToken);

        return new HashSet<string>(packages ?? [], StringComparer.OrdinalIgnoreCase);
    }

    private static Stream OpenStream(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            return File.OpenRead(path);
        }

        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "MigrationCompass.Rules.IrrelevantPackages.json";
        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is not null)
        {
            return resourceStream;
        }

        throw new FileNotFoundException(
            "Nao foi possivel localizar a lista de pacotes irrelevantes.",
            path ?? resourceName);
    }
}

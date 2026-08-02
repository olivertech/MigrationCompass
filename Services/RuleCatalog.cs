using System.Text.Json;
using MigrationCompass.Models;

namespace MigrationCompass.Services;

public static class RuleCatalog
{
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
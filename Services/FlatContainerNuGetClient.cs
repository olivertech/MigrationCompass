using System.IO.Compression;
using System.Text.Json;

namespace MigrationCompass.Services;

public sealed class FlatContainerNuGetClient(HttpClient httpClient) : INuGetPackageClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<string>> GetVersionsAsync(string packageId, CancellationToken cancellationToken)
    {
        var lowerId = packageId.ToLowerInvariant();
        var endpoint = $"https://api.nuget.org/v3-flatcontainer/{lowerId}/index.json";
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var versions = document.RootElement.GetProperty("versions")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

        return versions;
    }

    public async Task<IReadOnlyList<string>> GetAssetFrameworkFoldersAsync(string packageId, string version, CancellationToken cancellationToken)
    {
        var lowerId = packageId.ToLowerInvariant();
        var lowerVersion = version.ToLowerInvariant();
        var endpoint = $"https://api.nuget.org/v3-flatcontainer/{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg";

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var packageBuffer = new MemoryStream();
        await responseStream.CopyToAsync(packageBuffer, cancellationToken);
        packageBuffer.Position = 0;

        using var archive = new ZipArchive(packageBuffer, ZipArchiveMode.Read, false);
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in archive.Entries)
        {
            var segments = entry.FullName.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && (segments[0].Equals("lib", StringComparison.OrdinalIgnoreCase) || segments[0].Equals("ref", StringComparison.OrdinalIgnoreCase)))
            {
                folders.Add(segments[1]);
                continue;
            }

            if (segments.Length >= 4 &&
                segments[0].Equals("runtimes", StringComparison.OrdinalIgnoreCase) &&
                segments[2].Equals("lib", StringComparison.OrdinalIgnoreCase))
            {
                folders.Add(segments[3]);
            }
        }

        return folders.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
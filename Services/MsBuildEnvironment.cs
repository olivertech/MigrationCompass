using System.Diagnostics;

namespace MigrationCompass.Services;

/// <summary>
/// Ajusta o ambiente do processo para permitir que o Microsoft.Build avalie projetos SDK-style localmente.
/// </summary>
public static class MsBuildEnvironment
{
    private static bool _configured;
    private static string? _sdkDirectory;

    /// <summary>
    /// Resolve e publica as variÃ¡veis de ambiente mÃ­nimas para a avaliaÃ§Ã£o de projetos pelo MSBuild.
    /// </summary>
    public static void Configure()
    {
        if (_configured)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MSBuildSDKsPath")))
        {
            _configured = true;
            return;
        }

        var dotnetPath = ResolveDotnetPath();
        if (dotnetPath is null)
        {
            _configured = true;
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = dotnetPath,
            Arguments = "--list-sdks",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        var output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
        process?.WaitForExit();

        var sdkLine = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        if (string.IsNullOrWhiteSpace(sdkLine))
        {
            _configured = true;
            return;
        }

        var version = sdkLine[..sdkLine.IndexOf(' ')];
        var startBracket = sdkLine.IndexOf('[');
        var endBracket = sdkLine.IndexOf(']');
        if (startBracket < 0 || endBracket <= startBracket)
        {
            _configured = true;
            return;
        }

        var sdkRoot = sdkLine[(startBracket + 1)..endBracket];
        var sdkDirectory = Path.Combine(sdkRoot, version);
        _sdkDirectory = sdkDirectory;
        var sdksPath = Path.Combine(sdkDirectory, "Sdks");
        var msbuildAssemblyPath = Path.Combine(sdkDirectory, "MSBuild.dll");
        if (Directory.Exists(sdksPath))
        {
            Environment.SetEnvironmentVariable("MSBuildSDKsPath", sdksPath);
            Environment.SetEnvironmentVariable("MSBuildExtensionsPath", sdkDirectory);
            Environment.SetEnvironmentVariable("MSBuildExtensionsPath32", sdkDirectory);
            Environment.SetEnvironmentVariable("MSBuildToolsVersion", "Current");
            Environment.SetEnvironmentVariable("MSBuildToolsPath", sdkDirectory);
            Environment.SetEnvironmentVariable("MSBuildBinPath", sdkDirectory);
            Environment.SetEnvironmentVariable("RoslynTargetsPath", sdkDirectory);
            Environment.SetEnvironmentVariable("MSBuildEnableWorkloadResolver", "false");

            if (File.Exists(msbuildAssemblyPath))
            {
                Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", msbuildAssemblyPath);
            }
        }

        _configured = true;
    }

    /// <summary>
    /// Retorna propriedades globais complementares para cenÃ¡rios em que o carregamento precisa de pistas extras.
    /// </summary>
    public static IDictionary<string, string> CreateGlobalProperties()
    {
        Configure();

        if (string.IsNullOrWhiteSpace(_sdkDirectory))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LanguageTargets"] = Path.Combine(_sdkDirectory, "Microsoft.CSharp.targets")
        };
    }

    /// <summary>
    /// Tenta localizar o executÃ¡vel do dotnet instalado na mÃ¡quina atual.
    /// </summary>
    private static string? ResolveDotnetPath()
    {
        var processPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
        {
            return processPath;
        }

        var probablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
        return File.Exists(probablePath) ? probablePath : null;
    }
}

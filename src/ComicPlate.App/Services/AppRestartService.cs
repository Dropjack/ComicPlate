using System.Diagnostics;

namespace ComicPlate.App.Services;

public sealed class AppRestartService
{
    public bool TryRestart(string[]? args = null)
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            return false;
        }

        try
        {
            if (OperatingSystem.IsMacOS() && TryRestartMacAppBundle(processPath, args))
            {
                return true;
            }

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = false,
            }.WithArguments(args));
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    public static string[] GetCurrentArguments()
    {
        return Environment.GetCommandLineArgs().Skip(1).ToArray();
    }

    private static bool TryRestartMacAppBundle(string processPath, string[]? args)
    {
        var marker = $"{Path.DirectorySeparatorChar}Contents{Path.DirectorySeparatorChar}MacOS{Path.DirectorySeparatorChar}";
        var markerIndex = processPath.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        var bundlePath = processPath[..markerIndex];
        if (!bundlePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "open",
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-n");
        startInfo.ArgumentList.Add(bundlePath);
        if (args is { Length: > 0 })
        {
            startInfo.ArgumentList.Add("--args");
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }
        }

        using var _ = Process.Start(startInfo);
        return true;
    }
}

internal static class ProcessStartInfoExtensions
{
    public static ProcessStartInfo WithArguments(this ProcessStartInfo startInfo, string[]? args)
    {
        if (args is null)
        {
            return startInfo;
        }

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return startInfo;
    }
}

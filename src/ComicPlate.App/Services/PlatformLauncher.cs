using System.Diagnostics;

namespace ComicPlate.App.Services;

public sealed class PlatformLauncher : IPlatformLauncher
{
    public void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);

        if (OperatingSystem.IsMacOS())
        {
            Start("open", path);
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            Start("explorer.exe", path);
            return;
        }

        Start("xdg-open", path);
    }

    private static void Start(string fileName, string argument)
    {
        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            ArgumentList = { argument },
            UseShellExecute = false,
        });
    }
}

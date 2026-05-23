using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ComicPlate.Core.Books;
using System.Diagnostics;

namespace ComicPlate.App.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    private readonly Window _owner;

    public FolderPickerService(Window owner)
    {
        _owner = owner;
    }

    public async Task<string?> PickOpenPathAsync()
    {
        if (OperatingSystem.IsMacOS())
        {
            var path = await TryPickFileOrFolderWithMacOpenPanelAsync();
            if (!string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
        }

        return await PickComicFileAsync();
    }

    public async Task<string?> PickComicFileAsync()
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Readable visual files")
                {
                    Patterns = ComicArchiveFormats.SupportedFormats
                        .Select(format => $"*{format.Extension}")
                        .Concat([$"*{PdfBookFormat.Extension}"])
                        .Concat([$"*{EpubBookFormat.Extension}"])
                        .Concat(SupportedPageFormats.SupportedExtensions.Select(extension => $"*{extension}"))
                        .ToArray()
                },
                new FilePickerFileType("Image PDF files")
                {
                    Patterns = [$"*{PdfBookFormat.Extension}"]
                },
                new FilePickerFileType("Image EPUB files")
                {
                    Patterns = [$"*{EpubBookFormat.Extension}"]
                },
                new FilePickerFileType("Comic archives")
                {
                    Patterns = ComicArchiveFormats.SupportedFormats
                        .Select(format => $"*{format.Extension}")
                        .ToArray()
                },
                FilePickerFileTypes.ImageAll
            ]
        });

        return files.Count == 0
            ? null
            : files[0].Path.LocalPath;
    }

    public async Task<string?> PickFolderAsync()
    {
        var folders = await _owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Folder",
            AllowMultiple = false
        });

        return folders.Count == 0
            ? null
            : folders[0].Path.LocalPath;
    }

    private static async Task<string?> TryPickFileOrFolderWithMacOpenPanelAsync()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var line in CreateMacOpenPanelScript())
            {
                process.StartInfo.ArgumentList.Add("-e");
                process.StartInfo.ArgumentList.Add(line);
            }

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            _ = await errorTask;

            if (process.ExitCode != 0)
            {
                return null;
            }

            var output = (await outputTask).Trim();
            return string.IsNullOrWhiteSpace(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }

    private static string[] CreateMacOpenPanelScript()
    {
        return
        [
            "use framework \"AppKit\"",
            "use scripting additions",
            "set panel to current application's NSOpenPanel's openPanel()",
            "panel's setCanChooseFiles:true",
            "panel's setCanChooseDirectories:true",
            "panel's setAllowsMultipleSelection:false",
            "panel's setResolvesAliases:true",
            "panel's setTitle:\"Open\"",
            "set resultCode to panel's runModal()",
            "if (resultCode as integer) is 1 then",
            "set selectedUrls to panel's URLs()",
            "if (count of selectedUrls) > 0 then",
            "set selectedUrl to item 1 of selectedUrls",
            "return (selectedUrl's path()) as text",
            "end if",
            "end if",
            "return \"\""
        ];
    }
}

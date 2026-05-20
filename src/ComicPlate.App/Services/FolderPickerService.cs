using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    private readonly Window _owner;

    public FolderPickerService(Window owner)
    {
        _owner = owner;
    }

    public async Task<string?> PickComicFileAsync()
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Comic File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Comic files")
                {
                    Patterns = ComicArchiveFormats.SupportedFormats
                        .Select(format => $"*{format.Extension}")
                        .Concat(SupportedPageFormats.SupportedExtensions.Select(extension => $"*{extension}"))
                        .ToArray()
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
            Title = "Open Comic Folder",
            AllowMultiple = false
        });

        return folders.Count == 0
            ? null
            : folders[0].Path.LocalPath;
    }
}

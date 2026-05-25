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
            Title = Text("Picker.OpenFile.Title"),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(Text("Picker.Filter.ReadableFiles"))
                {
                    Patterns = ComicArchiveFormats.SupportedFormats
                        .Select(format => $"*{format.Extension}")
                        .Concat([$"*{PdfBookFormat.Extension}"])
                        .Concat([$"*{EpubBookFormat.Extension}"])
                        .Concat(SupportedPageFormats.SupportedExtensions.Select(extension => $"*{extension}"))
                        .ToArray()
                },
                new FilePickerFileType(Text("Picker.Filter.ImagePdf"))
                {
                    Patterns = [$"*{PdfBookFormat.Extension}"]
                },
                new FilePickerFileType(Text("Picker.Filter.ImageEpub"))
                {
                    Patterns = [$"*{EpubBookFormat.Extension}"]
                },
                new FilePickerFileType(Text("Picker.Filter.ComicArchives"))
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
            Title = Text("Picker.OpenFolder.Title"),
            AllowMultiple = false
        });

        return folders.Count == 0
            ? null
            : folders[0].Path.LocalPath;
    }

    private static string Text(string key)
    {
        return LocalizationService.Current.GetString(key);
    }
}

using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ComicPlate.App.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    private readonly Window _owner;

    public FolderPickerService(Window owner)
    {
        _owner = owner;
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
}

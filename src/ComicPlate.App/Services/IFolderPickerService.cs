namespace ComicPlate.App.Services;

public interface IFolderPickerService
{
    Task<string?> PickComicFileAsync();

    Task<string?> PickFolderAsync();
}

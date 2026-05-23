namespace ComicPlate.App.Services;

public interface IFolderPickerService
{
    Task<string?> PickOpenPathAsync();

    Task<string?> PickComicFileAsync();

    Task<string?> PickFolderAsync();
}

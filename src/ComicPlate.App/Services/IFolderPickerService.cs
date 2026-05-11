namespace ComicPlate.App.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}

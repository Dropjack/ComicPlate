namespace ComicPlate.App.Services;

public sealed class UnsupportedFileAssociationService : IFileAssociationService
{
    private readonly string _message;

    public UnsupportedFileAssociationService(string message)
    {
        _message = message;
    }

    public IReadOnlyList<FileAssociationOption> GetSupportedAssociations()
    {
        return FileAssociationService.CreateOptions(
            _ => false,
            _ => false,
            _ => _message);
    }

    public FileAssociationResult Associate(string extension)
    {
        return new FileAssociationResult(false, _message);
    }

    public FileAssociationResult Disassociate(string extension)
    {
        return new FileAssociationResult(false, _message);
    }
}

namespace ComicPlate.App.Services;

public sealed class MacOSFileAssociationService : IFileAssociationService
{
    public IReadOnlyList<FileAssociationOption> GetSupportedAssociations()
    {
        return FileAssociationService.CreateOptions(
            _ => false,
            _ => false,
            _ => LocalizationService.Current.GetString("FileAssociation.Status.MacUnsupported"));
    }

    public FileAssociationResult Associate(string extension)
    {
        return new FileAssociationResult(
            false,
            LocalizationService.Current.GetString("FileAssociation.Status.MacUnsupported"));
    }

    public FileAssociationResult Disassociate(string extension)
    {
        return new FileAssociationResult(
            false,
            LocalizationService.Current.GetString("FileAssociation.Status.MacUnsupported"));
    }
}

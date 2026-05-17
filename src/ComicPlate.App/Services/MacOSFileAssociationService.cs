namespace ComicPlate.App.Services;

public sealed class MacOSFileAssociationService : IFileAssociationService
{
    public IReadOnlyList<FileAssociationOption> GetSupportedAssociations()
    {
        return FileAssociationService.CreateOptions(
            _ => false,
            _ => false,
            _ => "macOS 文件关联需要通过应用包或系统设置处理。");
    }

    public FileAssociationResult Associate(string extension)
    {
        return new FileAssociationResult(false, "macOS 文件关联需要通过应用包或系统设置处理。");
    }
}

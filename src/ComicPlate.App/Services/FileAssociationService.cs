using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public static class FileAssociationService
{
    public static IFileAssociationService CreateDefault()
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsFileAssociationService.CreateDefault();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOSFileAssociationService();
        }

        return new UnsupportedFileAssociationService("当前平台暂不支持在 ComicPlate 内修改文件关联。");
    }

    internal static IReadOnlyList<FileAssociationOption> CreateOptions(
        Func<string, bool> canAssociate,
        Func<string, bool> isAssociated,
        Func<string, string> statusText)
    {
        return ComicArchiveFormats.SupportedFormats
            .Select(format => new FileAssociationOption(
                format.Extension,
                format.DisplayName,
                canAssociate(format.Extension),
                isAssociated(format.Extension),
                statusText(format.Extension)))
            .ToArray();
    }
}

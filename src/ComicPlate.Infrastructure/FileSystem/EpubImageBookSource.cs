using System.IO.Compression;
using System.Xml.Linq;
using ComicPlate.Core.Books;

namespace ComicPlate.Infrastructure.FileSystem;

public sealed class EpubImageBookSource : IBookSource
{
    private const string ContainerPath = "META-INF/container.xml";

    private readonly string _epubPath;

    public EpubImageBookSource(string epubPath)
    {
        _epubPath = Path.GetFullPath(epubPath);
    }

    public string Id => _epubPath;

    public string DisplayName => Path.GetFileName(_epubPath);

    public BookSourceKind SourceKind => BookSourceKind.Epub;

    public Task<IReadOnlyList<PageEntry>> LoadPagesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var archive = ZipFile.OpenRead(_epubPath);
        ThrowIfEncrypted(archive);

        var packagePath = ReadPackagePath(archive);
        var packageDirectory = GetDirectoryPath(packagePath);
        var packageDocument = LoadXmlDocument(archive, packagePath);
        var manifest = ReadManifest(packageDocument, packageDirectory);
        var spineItemIds = ReadSpineItemIds(packageDocument);
        var pages = new List<PageEntry>();
        var seenImagePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var itemId in spineItemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!manifest.TryGetValue(itemId, out var htmlItem))
            {
                continue;
            }

            foreach (var imagePath in ReadImagePathsFromDocument(archive, htmlItem.FullPath, manifest))
            {
                if (!seenImagePaths.Add(imagePath))
                {
                    continue;
                }

                pages.Add(CreatePageEntry(imagePath));
            }
        }

        return Task.FromResult<IReadOnlyList<PageEntry>>(pages);
    }

    public async Task<PageEntry?> LoadCoverOrFirstPageAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var archive = ZipFile.OpenRead(_epubPath);
        ThrowIfEncrypted(archive);

        var packagePath = ReadPackagePath(archive);
        var packageDirectory = GetDirectoryPath(packagePath);
        var packageDocument = LoadXmlDocument(archive, packagePath);
        var manifest = ReadManifest(packageDocument, packageDirectory);
        var coverPath = FindCoverImagePath(packageDocument, manifest);

        if (coverPath is not null)
        {
            return CreatePageEntry(coverPath);
        }

        var pages = await LoadPagesAsync(cancellationToken);
        return pages.FirstOrDefault();
    }

    private PageEntry CreatePageEntry(string imagePath)
    {
        return new PageEntry(
            Path.GetFileName(imagePath),
            imagePath,
            PageSourceKind.EpubImage,
            async cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var archive = ZipFile.OpenRead(_epubPath);
                ThrowIfEncrypted(archive);

                var entry = archive.GetEntry(imagePath);
                if (entry is null)
                {
                    throw new FileNotFoundException("The EPUB image entry no longer exists.", imagePath);
                }

                await using var entryStream = entry.Open();
                var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                return memoryStream;
            });
    }

    private static void ThrowIfEncrypted(ZipArchive archive)
    {
        if (archive.GetEntry("META-INF/encryption.xml") is not null)
        {
            throw new InvalidDataException("Encrypted EPUB files are not supported.");
        }
    }

    private static string ReadPackagePath(ZipArchive archive)
    {
        var containerDocument = LoadXmlDocument(archive, ContainerPath);
        var rootFile = containerDocument
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "rootfile");
        var packagePath = rootFile?.Attribute("full-path")?.Value;

        if (string.IsNullOrWhiteSpace(packagePath))
        {
            throw new InvalidDataException("EPUB package document was not found.");
        }

        return NormalizeArchivePath(packagePath);
    }

    private static Dictionary<string, ManifestItem> ReadManifest(XDocument packageDocument, string packageDirectory)
    {
        return packageDocument
            .Descendants()
            .Where(element => element.Name.LocalName == "item")
            .Select(element => ManifestItem.FromElement(element, packageDirectory))
            .Where(item => item is not null)
            .ToDictionary(item => item!.Id, item => item!, StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> ReadSpineItemIds(XDocument packageDocument)
    {
        return packageDocument
            .Descendants()
            .Where(element => element.Name.LocalName == "itemref")
            .Select(element => element.Attribute("idref")?.Value)
            .Where(idref => !string.IsNullOrWhiteSpace(idref))
            .Select(idref => idref!)
            .ToArray();
    }

    private static string? FindCoverImagePath(
        XDocument packageDocument,
        IReadOnlyDictionary<string, ManifestItem> manifest)
    {
        var coverImage = manifest.Values.FirstOrDefault(item => item.IsCoverImage);
        if (coverImage is not null && IsSupportedImagePath(coverImage.FullPath))
        {
            return coverImage.FullPath;
        }

        var coverId = packageDocument
            .Descendants()
            .Where(element => element.Name.LocalName == "meta")
            .FirstOrDefault(element => element.Attribute("name")?.Value == "cover")
            ?.Attribute("content")
            ?.Value;
        if (coverId is not null
            && manifest.TryGetValue(coverId, out var coverItem)
            && IsSupportedImagePath(coverItem.FullPath))
        {
            return coverItem.FullPath;
        }

        return null;
    }

    private static IEnumerable<string> ReadImagePathsFromDocument(
        ZipArchive archive,
        string documentPath,
        IReadOnlyDictionary<string, ManifestItem> manifest)
    {
        var document = LoadXmlDocument(archive, documentPath);
        var documentDirectory = GetDirectoryPath(documentPath);

        foreach (var element in document.Descendants())
        {
            var source = element.Name.LocalName switch
            {
                "img" => element.Attribute("src")?.Value,
                "image" => element.Attribute("href")?.Value
                    ?? element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "href")?.Value,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            var imagePath = ResolveReference(documentDirectory, source);
            if (manifest.Values.Any(item => item.FullPath.Equals(imagePath, StringComparison.OrdinalIgnoreCase))
                || IsSupportedImagePath(imagePath))
            {
                yield return imagePath;
            }
        }
    }

    private static XDocument LoadXmlDocument(ZipArchive archive, string entryPath)
    {
        var entry = archive.GetEntry(NormalizeArchivePath(entryPath));
        if (entry is null)
        {
            throw new FileNotFoundException("The EPUB entry was not found.", entryPath);
        }

        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
    }

    private static string ResolveReference(string baseDirectory, string reference)
    {
        var cleanReference = reference.Split(['#', '?'], 2)[0];
        cleanReference = Uri.UnescapeDataString(cleanReference);

        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return NormalizeArchivePath(cleanReference);
        }

        return NormalizeArchivePath($"{baseDirectory}/{cleanReference}");
    }

    private static string NormalizeArchivePath(string path)
    {
        var parts = path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new Stack<string>();

        foreach (var part in parts)
        {
            if (part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                if (stack.Count > 0)
                {
                    stack.Pop();
                }

                continue;
            }

            stack.Push(part);
        }

        return string.Join('/', stack.Reverse());
    }

    private static string GetDirectoryPath(string path)
    {
        var normalized = NormalizeArchivePath(path);
        var separatorIndex = normalized.LastIndexOf('/');
        return separatorIndex < 0
            ? ""
            : normalized[..separatorIndex];
    }

    private static bool IsSupportedImagePath(string path)
    {
        return SupportedPageFormats.IsSupportedExtension(Path.GetExtension(path));
    }

    private sealed record ManifestItem(string Id, string FullPath, string MediaType, string Properties)
    {
        public bool IsCoverImage => Properties
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(property => property.Equals("cover-image", StringComparison.OrdinalIgnoreCase));

        public static ManifestItem? FromElement(XElement element, string packageDirectory)
        {
            var id = element.Attribute("id")?.Value;
            var href = element.Attribute("href")?.Value;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(href))
            {
                return null;
            }

            var fullPath = ResolveReference(packageDirectory, href);
            return new ManifestItem(
                id,
                fullPath,
                element.Attribute("media-type")?.Value ?? "",
                element.Attribute("properties")?.Value ?? "");
        }
    }
}

using System.IO.Compression;
using ComicPlate.Core.Books;
using ComicPlate.Core.Sorting;

namespace ComicPlate.Infrastructure.FileSystem;

public sealed class ZipBookSource : IBookSource
{
    private readonly string _archivePath;

    public ZipBookSource(string archivePath)
    {
        _archivePath = Path.GetFullPath(archivePath);
    }

    public string Id => _archivePath;

    public string DisplayName => Path.GetFileName(_archivePath);

    public BookSourceKind SourceKind => BookSourceKind.Zip;

    public Task<IReadOnlyList<PageEntry>> LoadPagesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var archive = ZipFile.OpenRead(_archivePath);
        var pages = archive.Entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .Where(entry => SupportedPageFormats.IsSupportedExtension(Path.GetExtension(entry.FullName)))
            .Select(CreatePageEntry)
            .OrderBy(page => page.LogicalPath, NaturalPathComparer.Instance)
            .ToArray();

        return Task.FromResult<IReadOnlyList<PageEntry>>(pages);
    }

    private PageEntry CreatePageEntry(ZipArchiveEntry entry)
    {
        var logicalPath = entry.FullName.Replace('\\', '/');

        return new PageEntry(
            entry.Name,
            logicalPath,
            PageSourceKind.ZipEntry,
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var archive = ZipFile.OpenRead(_archivePath);
                var liveEntry = archive.GetEntry(entry.FullName);

                if (liveEntry is null)
                {
                    archive.Dispose();
                    throw new FileNotFoundException("The ZIP entry no longer exists.", entry.FullName);
                }

                return Task.FromResult<Stream>(new ZipEntryReadStream(archive, liveEntry.Open()));
            });
    }

    private sealed class ZipEntryReadStream : Stream
    {
        private readonly ZipArchive _archive;
        private readonly Stream _innerStream;

        public ZipEntryReadStream(ZipArchive archive, Stream innerStream)
        {
            _archive = archive;
            _innerStream = innerStream;
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
                _archive.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

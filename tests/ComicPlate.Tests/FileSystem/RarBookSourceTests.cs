using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.Tests.FileSystem;

public sealed class RarBookSourceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"ComicPlateRarTests-{Guid.NewGuid():N}");

    public RarBookSourceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task BrokenRarReportsOpenFailure()
    {
        var rarPath = Path.Combine(_tempDirectory, "broken.cbr");
        File.WriteAllText(rarPath, "not a rar archive");
        var source = new RarBookSource(rarPath);

        await Assert.ThrowsAnyAsync<Exception>(() => source.LoadPagesAsync(CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}


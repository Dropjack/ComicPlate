using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.Tests.FileSystem;

public sealed class ImageMetadataReaderTests
{
    [Fact]
    public void ReadsPngSize()
    {
        using var stream = new MemoryStream(new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x03, 0x20,
            0x00, 0x00, 0x04, 0xB0,
            0x08, 0x02, 0x00, 0x00, 0x00
        });

        var info = ImageMetadataReader.Read(stream);

        Assert.Equal(800, info.PixelWidth);
        Assert.Equal(1200, info.PixelHeight);
    }

    [Fact]
    public void ReadsGifSize()
    {
        using var stream = new MemoryStream(new byte[]
        {
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61,
            0x20, 0x03,
            0xB0, 0x04
        });

        var info = ImageMetadataReader.Read(stream);

        Assert.Equal(800, info.PixelWidth);
        Assert.Equal(1200, info.PixelHeight);
    }
}

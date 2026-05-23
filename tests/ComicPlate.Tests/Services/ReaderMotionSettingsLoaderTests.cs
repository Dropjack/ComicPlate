using System.Text;
using ComicPlate.App.Services;

namespace ComicPlate.Tests.Services;

public sealed class ReaderMotionSettingsLoaderTests
{
    [Fact]
    public void LoadEmbeddedOrDefaultReadsReaderMotionJsonc()
    {
        var settings = ReaderMotionSettingsLoader.LoadEmbeddedOrDefault();

        Assert.True(settings.BookOpenReveal.Enabled);
        Assert.Equal(120, settings.BookOpenReveal.DistanceDip);
        Assert.Equal(220, settings.BookOpenReveal.DurationMs);
        Assert.Equal(0.32, settings.ReaderTransition.DistanceViewportRatio, precision: 3);
    }

    [Fact]
    public void LoadOrDefaultAcceptsCommentsAndTrailingCommas()
    {
        using var stream = CreateStream(
            """
            {
              // local tuning
              "bookOpenReveal": {
                "distanceDip": 180,
              },
            }
            """);

        var settings = ReaderMotionSettingsLoader.LoadOrDefault(stream);

        Assert.Equal(180, settings.BookOpenReveal.DistanceDip);
    }

    [Fact]
    public void LoadOrDefaultFallsBackForInvalidJson()
    {
        using var stream = CreateStream("{");

        var settings = ReaderMotionSettingsLoader.LoadOrDefault(stream);

        Assert.Equal(ReaderMotionSettings.Default.BookOpenReveal.DistanceDip, settings.BookOpenReveal.DistanceDip);
    }

    [Fact]
    public void LoadOrDefaultClampsOutOfRangeValues()
    {
        using var stream = CreateStream(
            """
            {
              "bookOpenReveal": {
                "distanceDip": 9999,
                "durationMs": -20
              },
              "readerTransition": {
                "distanceViewportRatio": 2,
                "minDistanceDip": -50,
                "maxDistanceDip": 9999
              }
            }
            """);

        var settings = ReaderMotionSettingsLoader.LoadOrDefault(stream);

        Assert.Equal(480, settings.BookOpenReveal.DistanceDip);
        Assert.Equal(1, settings.BookOpenReveal.DurationMs);
        Assert.Equal(1, settings.ReaderTransition.DistanceViewportRatio);
        Assert.Equal(0, settings.ReaderTransition.MinDistanceDip);
        Assert.Equal(1200, settings.ReaderTransition.MaxDistanceDip);
    }

    private static MemoryStream CreateStream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }
}

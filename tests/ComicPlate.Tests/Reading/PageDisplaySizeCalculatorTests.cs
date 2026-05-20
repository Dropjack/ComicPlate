using ComicPlate.Core.Reading;

namespace ComicPlate.Tests.Reading;

public sealed class PageDisplaySizeCalculatorTests
{
    [Fact]
    public void AutoFitFitsVerticalPagesToViewportHeight()
    {
        var size = PageDisplaySizeCalculator.Calculate(
            imageWidth: 1000,
            imageHeight: 2000,
            viewportWidth: 1200,
            viewportHeight: 800,
            FitMode.AutoFit);

        Assert.Equal(400, size.Width);
        Assert.Equal(800, size.Height);
    }

    [Fact]
    public void AutoFitFitsHorizontalPagesToViewportHeightAndMayOverflowWidth()
    {
        var size = PageDisplaySizeCalculator.Calculate(
            imageWidth: 2000,
            imageHeight: 1000,
            viewportWidth: 1200,
            viewportHeight: 800,
            FitMode.AutoFit);

        Assert.Equal(1600, size.Width);
        Assert.Equal(800, size.Height);
    }

    [Fact]
    public void FitWindowKeepsTheWholeImageVisible()
    {
        var size = PageDisplaySizeCalculator.Calculate(
            imageWidth: 1000,
            imageHeight: 2000,
            viewportWidth: 1200,
            viewportHeight: 800,
            FitMode.FitWindow);

        Assert.Equal(400, size.Width);
        Assert.Equal(800, size.Height);
    }
}

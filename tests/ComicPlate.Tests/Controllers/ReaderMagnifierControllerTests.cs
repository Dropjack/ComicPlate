using ComicPlate.App.Controllers;

namespace ComicPlate.Tests.Controllers;

public sealed class ReaderMagnifierControllerTests
{
    [Fact]
    public void DisabledMagnifierCannotBegin()
    {
        var controller = new ReaderMagnifierController();

        controller.SetEnabled(false);
        var handled = controller.Begin(hasPages: true, out var activationChanged);

        Assert.False(handled);
        Assert.False(activationChanged);
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void AdjustScaleClampsAtSupportedRange()
    {
        var controller = new ReaderMagnifierController();

        controller.Begin(hasPages: true, out _);
        controller.AdjustScale(100, out var scaleChanged);

        Assert.True(scaleChanged);
        Assert.Equal(2.5, controller.Scale);
    }

    [Fact]
    public void UpdateTransformKeepsPointerAnchoredWithinContentBounds()
    {
        var controller = new ReaderMagnifierController();

        controller.Begin(hasPages: true, out _);
        controller.UpdatePointer(x: 500, y: 250, viewportWidth: 1000, viewportHeight: 500);
        controller.UpdateTransform(
            normalLeft: 0,
            viewportWidth: 1000,
            viewportHeight: 500,
            new ReaderMagnifierContentBounds(0, 0, 1000, 500));

        Assert.Equal(-250, controller.ContentTranslateX);
        Assert.Equal(-125, controller.ContentTranslateY);
    }

    [Fact]
    public void EndDeactivatesAndResetsTranslations()
    {
        var controller = new ReaderMagnifierController();

        controller.Begin(hasPages: true, out _);
        controller.UpdatePointer(x: 500, y: 250, viewportWidth: 1000, viewportHeight: 500);
        controller.UpdateTransform(
            normalLeft: 0,
            viewportWidth: 1000,
            viewportHeight: 500,
            new ReaderMagnifierContentBounds(0, 0, 1000, 500));

        controller.End();

        Assert.False(controller.IsActive);
        Assert.Equal(1.0, controller.Scale);
        Assert.Equal(0, controller.ContentTranslateX);
        Assert.Equal(0, controller.ContentTranslateY);
    }
}

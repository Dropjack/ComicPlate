namespace ComicPlate.Core.Reading;

public static class PageDisplaySizeCalculator
{
    public static PageDisplaySize Calculate(
        double imageWidth,
        double imageHeight,
        double viewportWidth,
        double viewportHeight,
        FitMode fitMode)
    {
        if (imageWidth <= 0 || imageHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0)
        {
            return new PageDisplaySize(0, 0);
        }

        var resolvedFitMode = ResolveAutoFit(imageWidth, imageHeight, fitMode);
        var scale = resolvedFitMode switch
        {
            FitMode.FitHeight => viewportHeight / imageHeight,
            FitMode.FitWidth => viewportWidth / imageWidth,
            FitMode.FitWindow => Math.Min(viewportWidth / imageWidth, viewportHeight / imageHeight),
            FitMode.AutoFit => throw new InvalidOperationException("AutoFit must be resolved before scale calculation."),
            _ => throw new ArgumentOutOfRangeException(nameof(fitMode), fitMode, "Unknown fit mode.")
        };

        return new PageDisplaySize(imageWidth * scale, imageHeight * scale);
    }

    private static FitMode ResolveAutoFit(double imageWidth, double imageHeight, FitMode fitMode)
    {
        if (fitMode != FitMode.AutoFit)
        {
            return fitMode;
        }

        return FitMode.FitHeight;
    }
}

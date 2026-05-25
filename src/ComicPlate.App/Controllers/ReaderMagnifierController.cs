using ComicPlate.App.Services;

namespace ComicPlate.App.Controllers;

public sealed class ReaderMagnifierController
{
    private const double DefaultScale = 1.5;
    private const double MinimumScale = 1.5;
    private const double MaximumScale = 2.5;

    private readonly ReaderMagnifierMotionSettings _motionSettings;
    private double _scale = DefaultScale;
    private double _pointerX;
    private double _pointerY;

    public ReaderMagnifierController(ReaderMagnifierMotionSettings? motionSettings = null)
    {
        _motionSettings = (motionSettings ?? new ReaderMagnifierMotionSettings()).Normalize();
    }

    public bool IsEnabled { get; private set; } = true;

    public bool IsActive { get; private set; }

    public double Scale => IsActive ? _scale : 1.0;

    public double ContentTranslateX { get; private set; }

    public double ContentTranslateY { get; private set; }

    public bool SetEnabled(bool isEnabled)
    {
        if (isEnabled == IsEnabled)
        {
            return false;
        }

        IsEnabled = isEnabled;
        return true;
    }

    public bool Begin(bool hasPages, out bool activationChanged)
    {
        activationChanged = false;
        if (!IsEnabled || !hasPages)
        {
            return false;
        }

        if (!IsActive)
        {
            IsActive = true;
            activationChanged = true;
        }

        return true;
    }

    public bool End()
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        ContentTranslateX = 0;
        ContentTranslateY = 0;
        return true;
    }

    public void UpdatePointer(double x, double y, double viewportWidth, double viewportHeight)
    {
        _pointerX = ProjectPointer(x, viewportWidth, _motionSettings.PointerSensitivity);
        _pointerY = ProjectPointer(y, viewportHeight, _motionSettings.PointerSensitivity);
    }

    public bool AdjustScale(double wheelDelta, out bool scaleChanged)
    {
        scaleChanged = false;
        if (!IsActive)
        {
            return false;
        }

        var nextScale = Math.Clamp(
            _scale + (wheelDelta * _motionSettings.WheelScaleStep),
            MinimumScale,
            MaximumScale);
        if (Math.Abs(nextScale - _scale) <= 0.001)
        {
            return true;
        }

        _scale = nextScale;
        scaleChanged = true;
        return true;
    }

    public void UpdateTransform(
        double normalLeft,
        double viewportWidth,
        double viewportHeight,
        ReaderMagnifierContentBounds bounds)
    {
        if (!IsActive)
        {
            return;
        }

        var contentXUnderPointer = _pointerX - normalLeft;
        var contentYUnderPointer = _pointerY;
        var desiredLeft = _pointerX - (contentXUnderPointer * _scale);
        var desiredTop = _pointerY - (contentYUnderPointer * _scale);

        var clampedLeft = ClampScaledOffset(
            desiredLeft,
            viewportWidth,
            bounds.Left,
            bounds.Right,
            _scale);
        var clampedTop = ClampScaledOffset(
            desiredTop,
            viewportHeight,
            bounds.Top,
            bounds.Bottom,
            _scale);

        ContentTranslateX = clampedLeft - normalLeft;
        ContentTranslateY = clampedTop;
    }

    private static double ClampScaledOffset(
        double desiredOffset,
        double viewportExtent,
        double contentStart,
        double contentEnd,
        double scale)
    {
        var scaledContentStart = contentStart * scale;
        var scaledContentEnd = contentEnd * scale;
        var scaledContentExtent = scaledContentEnd - scaledContentStart;
        if (scaledContentExtent <= viewportExtent)
        {
            return ((viewportExtent - scaledContentExtent) / 2) - scaledContentStart;
        }

        var minimumOffset = viewportExtent - scaledContentEnd;
        var maximumOffset = -scaledContentStart;
        return Math.Clamp(desiredOffset, minimumOffset, maximumOffset);
    }

    private static double ProjectPointer(double pointer, double viewportExtent, double sensitivity)
    {
        var clampedPointer = Math.Clamp(pointer, 0, Math.Max(0, viewportExtent));
        var center = viewportExtent / 2;
        return Math.Clamp(
            center + ((clampedPointer - center) * sensitivity),
            0,
            Math.Max(0, viewportExtent));
    }
}

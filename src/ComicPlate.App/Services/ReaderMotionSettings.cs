namespace ComicPlate.App.Services;

public sealed class ReaderMotionSettings
{
    public BookOpenRevealMotionSettings BookOpenReveal { get; init; } = new();

    public ReaderTransitionMotionSettings ReaderTransition { get; init; } = new();

    public ReaderInputMotionSettings ReaderInput { get; init; } = new();

    public ReaderMagnifierMotionSettings Magnifier { get; init; } = new();

    public static ReaderMotionSettings Default { get; } = new();

    public ReaderMotionSettings Normalize()
    {
        return new ReaderMotionSettings
        {
            BookOpenReveal = BookOpenReveal.Normalize(),
            ReaderTransition = ReaderTransition.Normalize(),
            ReaderInput = ReaderInput.Normalize(),
            Magnifier = Magnifier.Normalize(),
        };
    }
}

public sealed class BookOpenRevealMotionSettings
{
    public bool Enabled { get; init; } = true;

    public double DistanceDip { get; init; } = 120;

    public double DurationMs { get; init; } = 220;

    public double FrameIntervalMs { get; init; } = 16;

    public double OpacityFrom { get; init; } = 0;

    public double OpacityTo { get; init; } = 1;

    public string Easing { get; init; } = "easeOutCubic";

    public BookOpenRevealMotionSettings Normalize()
    {
        return new BookOpenRevealMotionSettings
        {
            Enabled = Enabled,
            DistanceDip = ClampFinite(DistanceDip, 0, 480, 120),
            DurationMs = ClampFinite(DurationMs, 1, 1000, 220),
            FrameIntervalMs = ClampFinite(FrameIntervalMs, 1, 100, 16),
            OpacityFrom = ClampFinite(OpacityFrom, 0, 1, 0),
            OpacityTo = ClampFinite(OpacityTo, 0, 1, 1),
            Easing = string.IsNullOrWhiteSpace(Easing) ? "easeOutCubic" : Easing,
        };
    }

    private static double ClampFinite(double value, double min, double max, double fallback)
    {
        return double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    }
}

public sealed class ReaderInputMotionSettings
{
    public double WheelFreeMoveViewportRatio { get; init; } = 0.35;

    public double WheelFreeMoveMinDistanceDip { get; init; } = 120;

    public double WheelDeltaMultiplier { get; init; } = 1;

    public double TouchpadHorizontalDeltaMultiplier { get; init; } = 1;

    public ReaderInputMotionSettings Normalize()
    {
        return new ReaderInputMotionSettings
        {
            WheelFreeMoveViewportRatio = ClampFinite(WheelFreeMoveViewportRatio, 0, 2, 0.35),
            WheelFreeMoveMinDistanceDip = ClampFinite(WheelFreeMoveMinDistanceDip, 0, 720, 120),
            WheelDeltaMultiplier = ClampFinite(WheelDeltaMultiplier, 0.05, 10, 1),
            TouchpadHorizontalDeltaMultiplier = ClampFinite(TouchpadHorizontalDeltaMultiplier, 0.05, 10, 1),
        };
    }

    private static double ClampFinite(double value, double min, double max, double fallback)
    {
        return double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    }
}

public sealed class ReaderMagnifierMotionSettings
{
    public double PointerSensitivity { get; init; } = 1.15;

    public double WheelScaleStep { get; init; } = 0.05;

    public ReaderMagnifierMotionSettings Normalize()
    {
        return new ReaderMagnifierMotionSettings
        {
            PointerSensitivity = ClampFinite(PointerSensitivity, 0.1, 4, 1.15),
            WheelScaleStep = ClampFinite(WheelScaleStep, 0.001, 0.5, 0.05),
        };
    }

    private static double ClampFinite(double value, double min, double max, double fallback)
    {
        return double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    }
}

public sealed class ReaderTransitionMotionSettings
{
    public bool Enabled { get; init; } = true;

    public double DistanceViewportRatio { get; init; } = 0.32;

    public double MinDistanceDip { get; init; } = 120;

    public double MaxDistanceDip { get; init; } = 360;

    public double DurationMs { get; init; } = 150;

    public double FrameIntervalMs { get; init; } = 16;

    public string Easing { get; init; } = "easeOutCubic";

    public ReaderTransitionMotionSettings Normalize()
    {
        var minDistance = ClampFinite(MinDistanceDip, 0, 720, 120);
        var maxDistance = ClampFinite(MaxDistanceDip, minDistance, 1200, 360);
        return new ReaderTransitionMotionSettings
        {
            Enabled = Enabled,
            DistanceViewportRatio = ClampFinite(DistanceViewportRatio, 0, 1, 0.32),
            MinDistanceDip = minDistance,
            MaxDistanceDip = maxDistance,
            DurationMs = ClampFinite(DurationMs, 1, 1000, 150),
            FrameIntervalMs = ClampFinite(FrameIntervalMs, 1, 100, 16),
            Easing = string.IsNullOrWhiteSpace(Easing) ? "easeOutCubic" : Easing,
        };
    }

    private static double ClampFinite(double value, double min, double max, double fallback)
    {
        return double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    }
}

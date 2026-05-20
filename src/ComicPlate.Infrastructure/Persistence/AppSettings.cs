using ComicPlate.Core.Reading;

namespace ComicPlate.Infrastructure.Persistence;

public sealed record AppSettings
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public ReadingDirection ReadingDirection { get; init; } = ReadingDirection.RightToLeft;

    public ViewMode ViewMode { get; init; } = ViewMode.SinglePage;

    public string DefaultFitMode { get; init; } = "AutoFit";

    public AppColorTheme ColorTheme { get; init; } = AppColorTheme.MistGreen;

    public AppLanguage Language { get; init; } = AppLanguage.System;

    public int ProgressLimit { get; init; } = 500;

    public bool RestoreProgress { get; init; } = true;

    public bool AllowMultipleWindows { get; init; } = true;

    public bool RestoreWindowPlacement { get; init; } = true;

    public bool IsMagnifierEnabled { get; init; } = true;

    public WindowPlacementSettings MainWindow { get; init; } = WindowPlacementSettings.Default;

    public double? SidebarWidth { get; init; }

    public static AppSettings Default { get; } = new();
}

public sealed record WindowPlacementSettings
{
    public double Width { get; init; } = 1200;

    public double Height { get; init; } = 800;

    public int? X { get; init; }

    public int? Y { get; init; }

    public bool HasPosition => X.HasValue && Y.HasValue;

    public static WindowPlacementSettings Default { get; } = new();
}

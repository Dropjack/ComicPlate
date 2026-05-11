using ComicPlate.Core.Reading;

namespace ComicPlate.Infrastructure.Persistence;

public sealed record AppSettings(
    int Version,
    ReadingDirection ReadingDirection,
    string DefaultFitMode,
    int RecentLimit,
    bool RestoreProgress)
{
    public static AppSettings Default { get; } = new(
        Version: 1,
        ReadingDirection: ReadingDirection.RightToLeft,
        DefaultFitMode: "AutoFit",
        RecentLimit: 20,
        RestoreProgress: true);
}

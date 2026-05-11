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
        ReadingDirection: ReadingDirection.LeftToRight,
        DefaultFitMode: "Fit",
        RecentLimit: 20,
        RestoreProgress: true);
}

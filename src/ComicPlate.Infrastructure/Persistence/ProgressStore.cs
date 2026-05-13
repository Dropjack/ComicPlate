namespace ComicPlate.Infrastructure.Persistence;

public sealed record ProgressStore(
    int Version,
    Dictionary<string, ProgressEntry> Books)
{
    public static ProgressStore Empty { get; } = new(1, new Dictionary<string, ProgressEntry>(StringComparer.OrdinalIgnoreCase));
}

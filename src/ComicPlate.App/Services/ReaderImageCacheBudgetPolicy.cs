namespace ComicPlate.App.Services;

public static class ReaderImageCacheBudgetPolicy
{
    public static IReadOnlyList<int> SelectRemovalOrder(
        IEnumerable<ReaderImageCacheBudgetCandidate> candidates,
        IReadOnlySet<int> activeIndexes,
        int currentPageIndex)
    {
        return candidates
            .Where(candidate => !activeIndexes.Contains(candidate.PageIndex))
            .OrderByDescending(candidate => Math.Abs(candidate.PageIndex - currentPageIndex))
            .ThenBy(candidate => candidate.LastAccessOrder)
            .Select(candidate => candidate.PageIndex)
            .ToArray();
    }
}

public sealed record ReaderImageCacheBudgetCandidate(
    int PageIndex,
    long LastAccessOrder);

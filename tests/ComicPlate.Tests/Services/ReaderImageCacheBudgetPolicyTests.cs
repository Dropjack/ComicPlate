using ComicPlate.App.Services;

namespace ComicPlate.Tests.Services;

public sealed class ReaderImageCacheBudgetPolicyTests
{
    [Fact]
    public void SelectRemovalOrderNeverRemovesActivePages()
    {
        var candidates = new[]
        {
            new ReaderImageCacheBudgetCandidate(1, LastAccessOrder: 1),
            new ReaderImageCacheBudgetCandidate(2, LastAccessOrder: 2),
            new ReaderImageCacheBudgetCandidate(50, LastAccessOrder: 3),
        };

        var removalOrder = ReaderImageCacheBudgetPolicy.SelectRemovalOrder(
            candidates,
            new HashSet<int> { 1, 2 },
            currentPageIndex: 1);

        Assert.Equal(new[] { 50 }, removalOrder);
    }

    [Fact]
    public void SelectRemovalOrderPrefersFartherThenLeastRecentlyUsedPages()
    {
        var candidates = new[]
        {
            new ReaderImageCacheBudgetCandidate(5, LastAccessOrder: 1),
            new ReaderImageCacheBudgetCandidate(6, LastAccessOrder: 1),
            new ReaderImageCacheBudgetCandidate(50, LastAccessOrder: 10),
            new ReaderImageCacheBudgetCandidate(51, LastAccessOrder: 3),
            new ReaderImageCacheBudgetCandidate(52, LastAccessOrder: 2),
        };

        var removalOrder = ReaderImageCacheBudgetPolicy.SelectRemovalOrder(
            candidates,
            new HashSet<int> { 5 },
            currentPageIndex: 5);

        Assert.Equal(new[] { 52, 51, 50, 6 }, removalOrder);
    }
}

using ComicPlate.App.Controllers;
using ComicPlate.App.ViewModels;

namespace ComicPlate.Tests.Controllers;

public sealed class ReaderStripRefreshCoordinatorTests
{
    [Fact]
    public void BeginRefreshAdvancesCurrentVersion()
    {
        using var coordinator = new ReaderStripRefreshCoordinator(TimeSpan.FromMilliseconds(1));

        var first = coordinator.BeginRefresh();
        var second = coordinator.BeginRefresh();

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.False(coordinator.IsCurrent(first));
        Assert.True(coordinator.IsCurrent(second));
    }

    [Fact]
    public async Task QueueViewportRefreshKeepsOnlyLatestRequest()
    {
        using var coordinator = new ReaderStripRefreshCoordinator(TimeSpan.FromMilliseconds(10));
        var committed = new List<int>();

        coordinator.QueueViewportRefresh(
            new ReaderStripPlacement(1, 100),
            placement =>
            {
                committed.Add(placement!.AnchorPageIndex);
                return Task.CompletedTask;
            });
        coordinator.QueueViewportRefresh(
            new ReaderStripPlacement(2, 100),
            placement =>
            {
                committed.Add(placement!.AnchorPageIndex);
                return Task.CompletedTask;
            });

        await Task.Delay(80);

        Assert.Equal(new[] { 2 }, committed);
    }
}

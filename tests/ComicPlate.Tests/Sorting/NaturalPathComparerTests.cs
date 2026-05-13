using ComicPlate.Core.Sorting;

namespace ComicPlate.Tests.Sorting;

public sealed class NaturalPathComparerTests
{
    [Fact]
    public void SortsNumbersByNumericValue()
    {
        var input = new[] { "10.jpg", "2.jpg", "1.jpg" };

        var sorted = input.OrderBy(path => path, NaturalPathComparer.Instance).ToArray();

        Assert.Equal(new[] { "1.jpg", "2.jpg", "10.jpg" }, sorted);
    }

    [Fact]
    public void SortsPaddedNumbersNaturally()
    {
        var input = new[] { "page010.jpg", "page001.jpg", "page002.jpg" };

        var sorted = input.OrderBy(path => path, NaturalPathComparer.Instance).ToArray();

        Assert.Equal(new[] { "page001.jpg", "page002.jpg", "page010.jpg" }, sorted);
    }

    [Fact]
    public void SortsSubdirectoriesByRelativePath()
    {
        var input = new[] { "Chapter 10/001.jpg", "Chapter 2/001.jpg", "Chapter 1/002.jpg" };

        var sorted = input.OrderBy(path => path, NaturalPathComparer.Instance).ToArray();

        Assert.Equal(new[] { "Chapter 1/002.jpg", "Chapter 2/001.jpg", "Chapter 10/001.jpg" }, sorted);
    }
}

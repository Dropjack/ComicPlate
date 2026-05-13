namespace ComicPlate.Core.Reading;

public sealed class VirtualizedReaderStrip
{
    public VirtualizedReaderStrip(int neighborPageLimit)
    {
        if (neighborPageLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(neighborPageLimit), "Neighbor page limit cannot be negative.");
        }

        NeighborPageLimit = neighborPageLimit;
    }

    public int NeighborPageLimit { get; }

    public IReadOnlyList<int> CreateWindow(int pageCount, int currentPageIndex, ReadingDirection readingDirection)
    {
        return CreateWindow(pageCount, new[] { currentPageIndex }, readingDirection);
    }

    public IReadOnlyList<int> CreateWindow(
        int pageCount,
        IReadOnlyList<int> currentGroupPageIndexes,
        ReadingDirection readingDirection)
    {
        if (pageCount <= 0)
        {
            return Array.Empty<int>();
        }

        var currentGroup = currentGroupPageIndexes
            .Select(index => Math.Clamp(index, 0, pageCount - 1))
            .Distinct()
            .ToArray();
        if (currentGroup.Length == 0)
        {
            return Array.Empty<int>();
        }

        var firstLogicalIndex = currentGroup.Min();
        var lastLogicalIndex = currentGroup.Max();
        var indexes = new List<int>();

        if (readingDirection == ReadingDirection.RightToLeft)
        {
            AddIndexes(indexes, pageCount, lastLogicalIndex, direction: 1, farToNear: true);
            indexes.AddRange(currentGroup);
            AddIndexes(indexes, pageCount, firstLogicalIndex, direction: -1, farToNear: false);
        }
        else
        {
            AddIndexes(indexes, pageCount, firstLogicalIndex, direction: -1, farToNear: true);
            indexes.AddRange(currentGroup);
            AddIndexes(indexes, pageCount, lastLogicalIndex, direction: 1, farToNear: false);
        }

        return indexes;
    }

    public IReadOnlyList<VirtualizedReaderStripSlot> CreateLayout(
        IReadOnlyList<int> windowPageIndexes,
        int currentPageIndex,
        IReadOnlyDictionary<int, double> pageExtents)
    {
        return CreateLayout(windowPageIndexes, new[] { currentPageIndex }, pageExtents);
    }

    public IReadOnlyList<VirtualizedReaderStripSlot> CreateLayout(
        IReadOnlyList<int> windowPageIndexes,
        IReadOnlyCollection<int> currentPageIndexes,
        IReadOnlyDictionary<int, double> pageExtents)
    {
        var slots = new List<VirtualizedReaderStripSlot>();
        var cursor = 0.0;
        var currentPages = currentPageIndexes.ToHashSet();

        foreach (var pageIndex in windowPageIndexes)
        {
            if (!pageExtents.TryGetValue(pageIndex, out var extent) || extent <= 0)
            {
                continue;
            }

            slots.Add(new VirtualizedReaderStripSlot(
                pageIndex,
                currentPages.Contains(pageIndex),
                cursor,
                extent));
            cursor += extent;
        }

        return slots;
    }

    public double GetCenteredOffset(
        IReadOnlyList<VirtualizedReaderStripSlot> slots,
        int currentPageIndex,
        double viewportWidth)
    {
        return GetCenteredOffset(slots, new[] { currentPageIndex }, viewportWidth);
    }

    public double GetCenteredOffset(
        IReadOnlyList<VirtualizedReaderStripSlot> slots,
        IReadOnlyCollection<int> currentPageIndexes,
        double viewportWidth)
    {
        var currentSlots = slots
            .Where(slot => currentPageIndexes.Contains(slot.PageIndex))
            .ToArray();
        if (currentSlots.Length == 0)
        {
            return 0;
        }

        var startX = currentSlots.Min(slot => slot.StartX);
        var endX = currentSlots.Max(slot => slot.StartX + slot.Extent);
        var centerX = startX + ((endX - startX) / 2);
        return (viewportWidth / 2) - centerX;
    }

    public int FindNearestPageIndex(
        IReadOnlyList<VirtualizedReaderStripSlot> slots,
        double viewportWidth,
        double stripOffsetX,
        int fallbackPageIndex)
    {
        if (slots.Count == 0)
        {
            return fallbackPageIndex;
        }

        var viewportCenterInStrip = (viewportWidth / 2) - stripOffsetX;
        var nearest = slots[0];
        var nearestDistance = Math.Abs(nearest.CenterX - viewportCenterInStrip);

        for (var index = 1; index < slots.Count; index++)
        {
            var slot = slots[index];
            var distance = Math.Abs(slot.CenterX - viewportCenterInStrip);
            if (distance < nearestDistance)
            {
                nearest = slot;
                nearestDistance = distance;
            }
        }

        return nearest.PageIndex;
    }

    private void AddIndexes(List<int> indexes, int pageCount, int currentPageIndex, int direction, bool farToNear)
    {
        var pending = new List<int>();

        for (var offset = 1; offset <= NeighborPageLimit; offset++)
        {
            var index = currentPageIndex + (offset * direction);
            if (index >= 0 && index < pageCount)
            {
                pending.Add(index);
            }
        }

        if (farToNear)
        {
            pending.Reverse();
        }

        indexes.AddRange(pending);
    }
}

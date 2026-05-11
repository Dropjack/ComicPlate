using ComicPlate.Core.Books;

namespace ComicPlate.Core.Reading;

public sealed class ReaderStrip
{
    public ReaderStrip(int neighborPageLimit)
    {
        if (neighborPageLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(neighborPageLimit), "Neighbor page limit cannot be negative.");
        }

        NeighborPageLimit = neighborPageLimit;
    }

    public int NeighborPageLimit { get; }

    public IReadOnlyList<ReaderStripSlot> CreateSlots(
        IReadOnlyList<PageEntry> pages,
        int currentPageIndex,
        ReadingDirection readingDirection)
    {
        if (pages.Count == 0)
        {
            return Array.Empty<ReaderStripSlot>();
        }

        var slots = new List<ReaderStripSlot>();

        if (readingDirection == ReadingDirection.RightToLeft)
        {
            AddSlots(slots, pages, currentPageIndex, direction: 1, farToNear: true);
            slots.Add(CreateSlot(pages, currentPageIndex, isCurrent: true));
            AddSlots(slots, pages, currentPageIndex, direction: -1, farToNear: false);
        }
        else
        {
            AddSlots(slots, pages, currentPageIndex, direction: -1, farToNear: true);
            slots.Add(CreateSlot(pages, currentPageIndex, isCurrent: true));
            AddSlots(slots, pages, currentPageIndex, direction: 1, farToNear: false);
        }

        return slots;
    }

    private void AddSlots(List<ReaderStripSlot> slots, IReadOnlyList<PageEntry> pages, int currentPageIndex, int direction, bool farToNear)
    {
        var indexes = new List<int>();

        for (var offset = 1; offset <= NeighborPageLimit; offset++)
        {
            var index = currentPageIndex + (offset * direction);
            if (index >= 0 && index < pages.Count)
            {
                indexes.Add(index);
            }
        }

        if (farToNear)
        {
            indexes.Reverse();
        }

        foreach (var index in indexes)
        {
            slots.Add(CreateSlot(pages, index, isCurrent: false));
        }
    }

    private static ReaderStripSlot CreateSlot(IReadOnlyList<PageEntry> pages, int index, bool isCurrent)
    {
        return new ReaderStripSlot(index, index + 1, pages[index], isCurrent);
    }
}

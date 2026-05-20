using System.Collections.ObjectModel;
using ComicPlate.App.Controllers;
using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.App.ViewModels;

public sealed class ReaderStripItemBuilder
{
    private const double ReaderFrameVerticalPadding = 0;

    public ReaderStripItemBuildResult BuildWindowItems(
        IReadOnlyList<ReaderFrame> frames,
        ReaderFrame currentFrame,
        ReaderStripController stripController,
        ReadingDirection readingDirection)
    {
        var currentGroupPageSet = currentFrame.PageIndexes.ToHashSet();
        var windowFrames = CreateFrameWindow(
            frames,
            currentFrame.FrameIndex,
            stripController,
            readingDirection);
        var activeIndexes = windowFrames
            .SelectMany(frame => frame.PageIndexes)
            .ToHashSet();
        var items = new ObservableCollection<ReaderStripItemViewModel>();

        foreach (var frame in windowFrames)
        {
            var displaySizes = CalculateFrameDisplaySizes(frame, stripController.ViewportHeight);
            for (var framePageIndex = 0; framePageIndex < frame.Pages.Count; framePageIndex++)
            {
                var framePage = frame.Pages[framePageIndex];
                var slot = new ReaderStripSlot(
                    framePage.PageIndex,
                    framePage.DisplayIndex,
                    framePage.Page,
                    currentGroupPageSet.Contains(framePage.PageIndex));
                var item = new ReaderStripItemViewModel(slot, framePage.ImageInfo);
                item.SetViewportSize(stripController.ViewportWidth, stripController.ViewportHeight);
                item.SetDisplaySize(displaySizes[framePageIndex].Width, displaySizes[framePageIndex].Height);

                items.Add(item);
            }
        }

        return new ReaderStripItemBuildResult(items, activeIndexes);
    }

    public bool UpdateVisibleItemSizes(
        IReadOnlyList<ReaderFrame> frames,
        ObservableCollection<ReaderStripItemViewModel> items,
        ReaderStripController stripController,
        ReadingDirection readingDirection)
    {
        var currentFrame = frames.FirstOrDefault(frame => frame.IsCurrent);
        if (currentFrame is null || items.Count == 0)
        {
            return false;
        }

        var visibleItems = items.ToDictionary(item => item.PageIndex);
        foreach (var frame in CreateFrameWindow(
            frames,
            currentFrame.FrameIndex,
            stripController,
            readingDirection))
        {
            var displaySizes = CalculateFrameDisplaySizes(frame, stripController.ViewportHeight);
            for (var framePageIndex = 0; framePageIndex < frame.Pages.Count; framePageIndex++)
            {
                var framePage = frame.Pages[framePageIndex];
                if (!visibleItems.TryGetValue(framePage.PageIndex, out var item))
                {
                    return false;
                }

                item.SetViewportSize(stripController.ViewportWidth, stripController.ViewportHeight);
                item.SetDisplaySize(displaySizes[framePageIndex].Width, displaySizes[framePageIndex].Height);
            }
        }

        return true;
    }

    private static IReadOnlyList<ReaderFrame> CreateFrameWindow(
        IReadOnlyList<ReaderFrame> frames,
        int currentFrameIndex,
        ReaderStripController stripController,
        ReadingDirection readingDirection)
    {
        return stripController.CreateFrameWindow(frames, currentFrameIndex, readingDirection);
    }

    private static IReadOnlyList<PageDisplaySize> CalculateFrameDisplaySizes(
        ReaderFrame frame,
        double viewportHeight)
    {
        if (frame.Pages.Count == 0)
        {
            return Array.Empty<PageDisplaySize>();
        }

        var rawSizes = frame.Pages
            .Select(page => GetRawPageSize(page.ImageInfo))
            .ToArray();
        var availableHeight = Math.Max(160, viewportHeight - ReaderFrameVerticalPadding);
        var targetHeight = availableHeight;

        return rawSizes
            .Select(size => new PageDisplaySize(size.Width * (targetHeight / size.Height), targetHeight))
            .ToArray();
    }

    private static PageDisplaySize GetRawPageSize(PageImageInfo imageInfo)
    {
        return imageInfo.IsValid
            ? new PageDisplaySize(imageInfo.PixelWidth, imageInfo.PixelHeight)
            : new PageDisplaySize(720, 1080);
    }
}

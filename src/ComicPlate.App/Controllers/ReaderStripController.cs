using ComicPlate.Core.Reading;
using ComicPlate.App.Services;

namespace ComicPlate.App.Controllers;

public sealed class ReaderStripController
{
    private readonly VirtualizedReaderStrip _readerStrip;
    private readonly ReaderInputMotionSettings _inputSettings;
    private IReadOnlyList<VirtualizedReaderStripSlot> _layoutSlots = Array.Empty<VirtualizedReaderStripSlot>();
    private double _baseOffset;
    private double _dragOffset;
    private double _viewportHeight = 600;
    private double _viewportWidth = 800;

    public ReaderStripController(int neighborPageLimit, ReaderInputMotionSettings? inputSettings = null)
    {
        _readerStrip = new VirtualizedReaderStrip(neighborPageLimit);
        _inputSettings = (inputSettings ?? new ReaderInputMotionSettings()).Normalize();
    }

    public double ViewportHeight => _viewportHeight;

    public double ViewportWidth => _viewportWidth;

    public double TranslateX => _baseOffset + _dragOffset;

    public IReadOnlyList<VirtualizedReaderStripSlot> LayoutSlots => _layoutSlots;

    public void SetViewportSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _viewportWidth = width;
        _viewportHeight = height;
    }

    public IReadOnlyList<ReaderFrame> CreateFrameWindow(
        IReadOnlyList<ReaderFrame> frames,
        int currentFrameIndex,
        ReadingDirection readingDirection)
    {
        var indexes = _readerStrip.CreateWindow(frames.Count, currentFrameIndex, readingDirection);
        return indexes
            .Select(index => frames[index])
            .ToArray();
    }

    public void UpdateOffset(
        IReadOnlyList<int> windowPageIndexes,
        IReadOnlyDictionary<int, double> pageExtents,
        IReadOnlyCollection<int> currentGroupPageIndexes,
        ReaderStripPlacement? placement = null)
    {
        if (windowPageIndexes.Count == 0)
        {
            _baseOffset = 0;
            _layoutSlots = Array.Empty<VirtualizedReaderStripSlot>();
            return;
        }

        _layoutSlots = _readerStrip.CreateLayout(
            windowPageIndexes,
            currentGroupPageIndexes,
            pageExtents);

        if (_layoutSlots.Count == 0)
        {
            _baseOffset = 0;
            return;
        }

        _baseOffset = placement is null
            ? _readerStrip.GetCenteredOffset(_layoutSlots, currentGroupPageIndexes, _viewportWidth)
            : GetPreservedOffset(placement, currentGroupPageIndexes);
        _baseOffset = ClampOffset(_baseOffset);
    }

    public void BeginDrag()
    {
        _dragOffset = 0;
    }

    public void Drag(double horizontalDelta)
    {
        _dragOffset = horizontalDelta;
    }

    public void CancelDrag()
    {
        _dragOffset = 0;
    }

    public ReaderStripCommitResult? CommitFreeOffset(
        double targetOffset,
        int currentPageIndex,
        IReadOnlyList<ReaderFrame> frames)
    {
        if (_layoutSlots.Count == 0)
        {
            _dragOffset = 0;
            return null;
        }

        var targetPageIndex = _readerStrip.FindNearestPageIndex(
            _layoutSlots,
            _viewportWidth,
            targetOffset,
            currentPageIndex);
        var placement = new ReaderStripPlacement(
            targetPageIndex,
            GetPageScreenCenter(targetPageIndex, targetOffset));

        _baseOffset = ClampOffset(targetOffset);
        _dragOffset = 0;

        var targetFrame = frames.FirstOrDefault(frame => frame.PageIndexes.Contains(targetPageIndex));
        var currentFrame = frames.FirstOrDefault(frame => frame.IsCurrent);
        if (targetFrame is null || targetFrame == currentFrame)
        {
            return new ReaderStripCommitResult(false, currentPageIndex, placement);
        }

        return new ReaderStripCommitResult(true, targetFrame.PageIndexes.Min(), placement);
    }

    public double GetNextReadingDirectionOffsetDelta(ReadingDirection readingDirection, double inputDeltaMagnitude = 1)
    {
        var magnitude = GetFreeMoveMagnitude(inputDeltaMagnitude, _inputSettings.WheelDeltaMultiplier);
        return readingDirection == ReadingDirection.RightToLeft
            ? magnitude
            : -magnitude;
    }

    public double GetVisualLeftOffsetDelta(double inputDeltaMagnitude = 1)
    {
        return GetFreeMoveMagnitude(inputDeltaMagnitude, _inputSettings.TouchpadHorizontalDeltaMultiplier);
    }

    public double GetVisualRightOffsetDelta(double inputDeltaMagnitude = 1)
    {
        return -GetFreeMoveMagnitude(inputDeltaMagnitude, _inputSettings.TouchpadHorizontalDeltaMultiplier);
    }

    public double GetPageScreenCenter(int pageIndex, double stripOffset)
    {
        var slot = _layoutSlots.FirstOrDefault(slot => slot.PageIndex == pageIndex);
        return slot is null
            ? _viewportWidth / 2
            : stripOffset + slot.CenterX;
    }

    private double GetPreservedOffset(
        ReaderStripPlacement placement,
        IReadOnlyCollection<int> currentGroupPageIndexes)
    {
        var anchorSlot = _layoutSlots.FirstOrDefault(slot => slot.PageIndex == placement.AnchorPageIndex);
        return anchorSlot is null
            ? _readerStrip.GetCenteredOffset(_layoutSlots, currentGroupPageIndexes, _viewportWidth)
            : placement.AnchorScreenX - anchorSlot.CenterX;
    }

    private double ClampOffset(double offset)
    {
        if (_layoutSlots.Count == 0)
        {
            return 0;
        }

        var contentWidth = _layoutSlots.Max(slot => slot.StartX + slot.Extent);
        if (contentWidth <= 0)
        {
            return 0;
        }

        if (contentWidth <= _viewportWidth)
        {
            return (_viewportWidth - contentWidth) / 2;
        }

        return Math.Clamp(offset, _viewportWidth - contentWidth, 0);
    }

    private double GetFreeMoveMagnitude(double inputDeltaMagnitude, double multiplier)
    {
        var normalizedDelta = double.IsFinite(inputDeltaMagnitude)
            ? Math.Max(0, inputDeltaMagnitude)
            : 1;
        var baseMagnitude = Math.Max(
            _inputSettings.WheelFreeMoveMinDistanceDip,
            _viewportWidth * _inputSettings.WheelFreeMoveViewportRatio);
        return baseMagnitude * normalizedDelta * multiplier;
    }
}

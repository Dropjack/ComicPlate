# Known UI Polish Issues

This file records visible UX and rendering polish issues that are confirmed but intentionally not fixed immediately.

## Reader Frame Flicker During Window Resize

Status: confirmed, deferred.

When the application window is freely resized, the visible comic frame can flicker repeatedly while the reader surface recalculates layout and image resources. The current behavior is usable, but it feels less stable than NeeView, which keeps the visible page visually steady during continuous resize.

Likely area:

- reader viewport `SizeChanged` frequency;
- reader strip layout recalculation;
- image item replacement during refresh;
- decode request changes caused by continuously changing display size;
- lack of resize debounce or stable temporary bitmap reuse.

Boundary:

- Do not change drag or wheel free-offset semantics while fixing this.
- Do not make resize wait for full image decode before showing the current frame.
- Keep the reader image cache size-aware and disposable.
- Treat this as reader layout lifecycle polish, separate from the R7 decode strategy.

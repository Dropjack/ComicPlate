# Known UI Polish Issues

This file records visible UX and rendering polish issues that are confirmed but intentionally not fixed immediately.

## Reader Frame Flicker During Window Resize

Status: mitigated in R8.1, needs manual feel-test.

When the application window is freely resized, the visible comic frame can flicker repeatedly while the reader surface recalculates layout and image resources. The R8 mitigation keeps the existing strip visible during continuous resize and commits the full layout refresh after a short delay. R8.1 also updates the existing strip item sizes immediately so the frame follows the window while reusing the already-loaded bitmap.

Likely area:

- reader viewport `SizeChanged` frequency;
- reader strip layout recalculation;
- image item replacement during refresh;
- decode request changes caused by continuously changing display size;
- lack of stable temporary bitmap reuse during resize.

Boundary:

- Do not change drag or wheel free-offset semantics while fixing this.
- Do not make resize wait for full image decode before showing the current frame.
- Keep the reader image cache size-aware and disposable.
- Treat this as reader layout lifecycle polish, separate from the R7 decode strategy.

R8 change:

- first valid viewport size commits immediately;
- repeated `SizeChanged` events update the viewport and visible item geometry but delay full strip replacement;
- the current strip offset is updated immediately to preserve the current page position;
- full layout recalculation and decode request changes are committed after resize quiets for a short interval.

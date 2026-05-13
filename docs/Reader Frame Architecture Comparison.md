# Reader Frame Architecture Comparison

This note compares three reader layout models:

- ComicPlate's old page-strip model.
- NeeView's mature page-frame model.
- ComicPlate's new lightweight frame model.

It is intended as a thinking document for future architecture and edge-case review, not as a user manual.

## Problem

A manga reader cannot treat every image as a same-sized page slot.

The reader needs to handle:

- Single portrait pages.
- Two-page spreads.
- First-page cover behavior.
- Last-page odd counts.
- Wide pages that should not be paired.
- Free drag and wheel movement without magnetic snapping.
- Navigation commands that do snap to the current reading unit.
- Sidebar and window resizing.
- Memory pressure from large images.

The core object should therefore be a reading unit, not an individual page.

## Old ComicPlate Model

The old model was:

```text
PageEntry -> page index window -> horizontal strip -> current page center
```

The current anchor was a page.

This was simple and useful for early testing, but it had several weaknesses:

- Double-page mode could only be approximated as a group of page indexes.
- Wide pages were still treated as ordinary pages.
- The reader could not decide layout until after bitmap decode.
- Drag and wheel movement could reveal large empty background areas.
- Page navigation used page steps, not frame steps.
- The visual size of each page came from the decoded bitmap, so layout and memory were coupled.

This made the old model easy to understand but fragile around real manga cases.

## NeeView Model

NeeView uses a more mature frame-oriented model.

The important concepts are:

- A page is converted into a page element.
- One or more page elements become a page frame.
- A page frame may be a single page, a two-page spread, a wide single page, or a frame with a dummy partner.
- Frames are arranged by read direction.
- View transforms and bounds operate on frames and containers.
- Image metadata is read before full display decode.
- Display decode can target the required output size.

Conceptually:

```text
Page -> PageFrameElement -> PageFrame -> FrameContainer -> ViewTransform
```

The key advantage is that the reader can decide what the current visual unit is before drawing it.

This solves many cases:

- A cover can remain single.
- Two portrait pages can be paired.
- A landscape or very wide page can remain single.
- A final odd page can remain single or receive a dummy partner.
- Read direction changes visual ordering without changing logical page order.
- Bounds can be applied to the frame container rather than to a single page.

## New ComicPlate Model

ComicPlate now moves toward the same architecture, but keeps a lighter MVP implementation.

The new model is:

```text
PageEntry -> PageImageInfo -> ReaderFrame -> frame window -> strip layout -> target-size bitmap decode
```

Current pieces:

- `PageImageInfo` stores lightweight width and height.
- `ImageMetadataReader` reads common image headers without full bitmap decode.
- `ReaderFrameBuilder` creates single, spread, and wide-single frames.
- Navigation moves by frame, not by raw page step.
- The strip center is computed from the current frame's pages.
- Wheel and drag remain free movement.
- Free movement is clamped to avoid large empty background exposure.
- Reading bitmaps are decoded near their target display size.

## Frame Rules

Current frame rules are intentionally conservative:

- Page 0 is a single cover frame.
- In double-page mode, ordinary portrait pages are paired.
- In right-to-left mode, a logical pair such as `1, 2` is displayed visually as `2, 1`.
- A page with aspect ratio `width / height >= 1.25` is treated as a wide single frame.
- If a page cannot be paired safely, it remains a single frame.

These rules are not final UX. They are architecture guards.

## Centering

The anchor is no longer a single page center.

For a frame, ComicPlate calculates the visual extent of all pages in the frame:

```text
frameStart = min(page.StartX)
frameEnd = max(page.StartX + page.Width)
frameCenter = frameStart + (frameEnd - frameStart) / 2
offset = viewportCenter - frameCenter
```

For a single page, this is equivalent to centering one page.

For a spread, this centers the whole pair.

## Free Movement

ComicPlate separates two kinds of movement:

- Navigation movement: buttons, keyboard, first page, last page, view-mode changes.
- Free movement: mouse wheel and drag.

Navigation movement snaps to the current frame center.

Free movement does not snap. It only changes strip offset.

However, free movement is bounded:

- If content is wider than the viewport, the strip cannot be moved beyond its left or right content edge.
- If content is narrower than the viewport, the content is centered.

This preserves the "free movement" feel without exposing large empty background regions.

## Metadata And Memory

The old implementation decoded bitmaps first, then read `PixelSize`.

The new implementation reads image size first:

```text
PageEntry.OpenStream -> ImageMetadataReader -> PageImageInfo
```

Layout uses `PageImageInfo`.

Bitmap decode happens later, with a target display size:

```text
ReaderFrame display size -> ImagePageLoader target width/height -> Bitmap.DecodeToWidth/DecodeToHeight
```

This is closer to NeeView's memory strategy:

- Layout does not require full-size bitmap decode.
- Large images do not need to be decoded at original dimensions for normal reading.
- The bitmap cache remains limited to the visible frame window.
- Stale bitmaps are disposed after they leave the active window.

## Remaining Gaps

ComicPlate is still simpler than NeeView.

Important gaps remain:

- EXIF orientation is not applied yet.
- DPI and aspect-size metadata are not applied yet.
- WebP metadata support is minimal.
- Wide-page threshold is hardcoded.
- Dummy page policy does not exist yet.
- Wide pages larger than the viewport need more precise pan bounds.
- Sidebar resize should preserve visual position more intelligently.
- Frame-level cache keys would be cleaner than page-index-only bitmap keys.
- Animated images and unusual formats need separate policies.
- The reader does not yet expose user settings for page mode, read direction, fit mode, or wide-page behavior.

## Design Direction

ComicPlate should keep learning from NeeView's model:

```text
Page metadata first.
Frame decision second.
Layout third.
Target decode fourth.
Transform and bounds last.
```

The goal is not to copy NeeView source code.

The goal is to adopt the proven reading abstraction:

```text
The user reads frames, not files.
The UI moves frames, not raw pages.
The renderer decodes only what the current frame window needs.
```

That is the path toward lower memory use and more predictable manga behavior.

# Reader Image Decode Strategy

This note records the R7 direction for ComicPlate's reader image loading. It is a strategy comparison, not a copy of NeeView implementation code.

## NeeView Pattern

NeeView keeps image loading split into several layers:

- page/archive source opens the raw stream;
- picture metadata records pixel size, orientation, DPI-related aspect data, format, and frame information;
- view content calculates the display resource size from the current page frame;
- picture source decodes an image for that requested size;
- memory services track loaded image resources and unload pages far from the current reading position;
- load queues coalesce repeated load requests so stale view refreshes do not keep updating the UI.

The important behavior is that NeeView does not treat the whole original image as the normal display resource. It usually creates a bitmap sized for the current view target, applies high quality resize rules when enabled, and keeps memory cleanup tied to page distance and reading direction.

## ComicPlate Before R7

ComicPlate already had a separate `ReaderImageCache`, but the cache key was only the page index. That meant a bitmap decoded for one viewport could be reused after the viewport needed a larger or smaller resource.

The reader refresh also waited for every visible-window image to decode before replacing the strip items. This made a heavy archive feel slower than necessary and allowed old refresh work to compete with newer reader state.

The visible reader window was also too wide for the current memory target: twelve neighbor pages could produce a large number of decoded bitmaps.

## ComicPlate R7 Rules

R7 moves ComicPlate closer to the NeeView-style constraints while staying small:

- image decode requests are explicit objects with target pixel width and height;
- display-size decode defaults to 1.5x the UI display size;
- decode requests are capped at 4096 pixels per dimension;
- when source metadata is known, decode requests do not exceed the original source dimensions;
- cached images are reused only when their decoded size still covers the new request and is not excessively oversized;
- reader strip image loading is cancellable by refresh version;
- strip items appear before their images finish decoding, then images fill in current-page-first;
- the reader window is capped to five neighbor pages instead of twelve.

This is still not a full NeeView memory pool. The next possible step, if memory remains high, is to introduce a byte-budgeted reader image pool with page-distance eviction instead of only active-window trimming.

## Non-Goals

R7 does not change drag or wheel semantics. Drag and wheel remain free offset movement. Buttons and keyboard page commands remain snapping actions.

R7 does not solve the first-open visible resize polish issue. That belongs to reader layout lifecycle tuning, not decode strategy.

R7 does not add file association, settings UI, or a thumbnail architecture rewrite.

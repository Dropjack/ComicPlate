# 2026-05-11 Phase 1 App Shell

## Changes

- Added `ComicPlate.App` as an Avalonia desktop project.
- Connected `ComicPlate.App` to `ComicPlate.Core` and `ComicPlate.Infrastructure`.
- Added a light Windows-oriented main window shell inspired by WinUI-style utility apps.
- Added a startup page with an empty recent-books state.
- Added a reader page with:
  - left page-list sidebar,
  - central image canvas,
  - top lightweight toolbar,
  - bottom page and progress area.
- Implemented opening a folder through the Windows folder picker.
- Folder loading currently scans subfolders by default.
- Added single-page display, page list selection, and keyboard navigation:
  - Right / Space: next page,
  - Left / Backspace: previous page,
  - Home: first page,
  - End: last page,
  - Ctrl+O: open folder.
- Added empty-folder state and damaged-image placeholder behavior.
- Updated folder enumeration so unreadable child folders are skipped instead of crashing the whole load.
- Added Bookshelf concepts to Core:
  - `Bookshelf`,
  - `BookEntry`,
  - `IBookshelfSource`.
- Added `FileSystemBookshelfSource` so `Open Folder` can load a bookshelf root.
- Bookshelf entries now include first-level folders and first-level `.zip` / `.cbz` files.
- Added `ZipBookSource` for ZIP/CBZ page enumeration.
- Added `ReaderStrip` so the reader canvas can display a bounded horizontal page flow.
- Updated the reader layout toward the new target:
  - left Bookshelf panel,
  - adjacent Pages panel,
  - horizontal reader strip,
  - default RightToLeft navigation.

## Current Limits

- ZIP/CBZ page enumeration exists, but ZIP-specific UI polish and error text are still minimal.
- Recent books are only represented by a startup empty state; persistence comes later.
- Double-page mode and reading direction are not wired into the UI yet.
- The left sidebar only has Bookshelf and Pages, not the full NeeView side-panel system.
- Image caching is bounded to the reader strip, but thumbnail caching is not implemented yet.

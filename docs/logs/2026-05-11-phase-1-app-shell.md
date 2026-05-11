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

## Current Limits

- ZIP/CBZ is not implemented in this slice.
- Recent books are only represented by a startup empty state; persistence comes later.
- Double-page mode and reading direction are not wired into the UI yet.
- The left sidebar is only a page directory, not the full NeeView side-panel system.
- Image caching is minimal: only the current bitmap is kept by the view model.

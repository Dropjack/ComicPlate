# **ComicPlate Localization Key Map**

This document is the working table for ComicPlate UI localization.

Rules:

- `en.json` is the source of truth.
- Keys use semantic paths, not English sentences.
- Internal language tag is `zh-Hans`; external UI name is `中文`.
- If the system language is unsupported, fall back to `en`.
- If a key is missing in the selected language, fall back to `en`.
- Localize UI text only.
- Do not localize user content: file names, folder names, paths, archive entry names, comic titles, metadata, page numbers, zoom values, file extensions, or raw format labels.
- Format labels such as `RAR`, `CBR`, `ZIP`, `CBZ`, `PDF`, shortcut key names such as `Right`, `Left`, `Home`, and page progress values such as `1 / 20` should remain unchanged.

## **Common**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `Common.Open` | Open | 打开 | 開く | `SettingsWindow.axaml`, data folder and shortcuts buttons | General command |
| `Common.Clear` | Clear | 清除 | クリア | `SettingsWindow.axaml`, thumbnail cache button | General command |
| `Common.Restart` | Restart | 重启 | 再起動 | `ThemeRestartPromptWindow.cs` | General command |
| `Common.Later` | Later | 稍后 | 後で | `ThemeRestartPromptWindow.cs` | General command |
| `Common.ComicPlate` | ComicPlate | ComicPlate | ComicPlate | multiple | Usually does not need translation; keep as product name |

## **Restart Prompts**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `Restart.Theme.Title` | Theme changed | 主题已更改 | テーマを変更しました | `ThemeRestartPromptWindow.cs` | Shared restart prompt |
| `Restart.Theme.Message` | The new color theme will take effect after restarting ComicPlate. Restart now? | 新的颜色主题将在重启 ComicPlate 后生效。现在重启吗？ | 新しいカラーテーマは ComicPlate の再起動後に適用されます。今すぐ再起動しますか？ | `ThemeRestartPromptWindow.cs` | Shared restart prompt |
| `Restart.Language.Title` | Language changed | 语言已更改 | 言語を変更しました | `SettingsWindow.axaml.cs` | Shared restart prompt |
| `Restart.Language.Message` | The new language will take effect after restarting ComicPlate. Restart now? | 新的语言将在重启 ComicPlate 后生效。现在重启吗？ | 新しい言語は ComicPlate の再起動後に適用されます。今すぐ再起動しますか？ | `SettingsWindow.axaml.cs` | Shared restart prompt |

## **Settings**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `Settings.Title` | Settings | 设置 | 設定 | `SettingsWindow.axaml` title/nav header | Window title and sidebar title |
| `Settings.Header` | ComicPlate Settings | ComicPlate 设置 | ComicPlate 設定 | `SettingsWindow.axaml` | Main page heading |
| `Settings.StartupAndWindow` | Startup and Window | 启动与窗口 | 起動とウィンドウ | `SettingsWindow.axaml` | Nav item and section title |
| `Settings.Appearance` | Appearance | 外观 | 外観 | `SettingsWindow.axaml` | Nav item and section title |
| `Settings.Reading` | Reading | 阅读 | 読書 | `SettingsWindow.axaml` | Nav item and section title |
| `Settings.DataAndCache` | Data and Cache | 数据与缓存 | データとキャッシュ | `SettingsWindow.axaml` | Nav item and section title |
| `Settings.FileAssociations` | File Associations | 文件关联 | ファイル関連付け | `SettingsWindow.axaml` | Nav item and section title |
| `Settings.Shortcuts` | Shortcuts | 快捷键 | ショートカット | `SettingsWindow.axaml` | Nav item and section title |
| `Settings.AllowMultipleWindows.Label` | Allow multiple windows | 允许多个窗口 | 複数ウィンドウを許可 | `SettingsWindow.axaml` | Toggle label |
| `Settings.AllowMultipleWindows.Description` | Allow multiple ComicPlate windows to be open at the same time. | 允许同时打开多个 ComicPlate 窗口。 | 複数の ComicPlate ウィンドウを同時に開けるようにします。 | `SettingsWindow.axaml` | Toggle description |
| `Settings.RestoreWindowPlacement.Label` | Restore window size and position | 恢复窗口大小和位置 | ウィンドウのサイズと位置を復元 | `SettingsWindow.axaml` | Toggle label |
| `Settings.RestoreWindowPlacement.Description` | Restore the window size and position from the last time ComicPlate was closed. | 恢复上次关闭 ComicPlate 时的窗口大小和位置。 | 前回 ComicPlate を閉じたときのウィンドウのサイズと位置を復元します。 | `SettingsWindow.axaml` | Toggle description |
| `Settings.Theme.Label` | Color theme | 颜色主题 | カラーテーマ | `SettingsWindow.axaml` | Combo label |
| `Settings.Theme.Description` | Affects the command rail, Context Shelf, reader area, title bar, and progress area. Changes take effect after restart. | 影响命令栏、Context Shelf、阅读区域、标题栏和进度区域。更改将在重启后生效。 | コマンドレール、Context Shelf、リーダー領域、タイトルバー、進行状況エリアに反映されます。変更は再起動後に適用されます。 | `SettingsWindow.axaml` | Combo description |
| `Settings.Theme.MistGreen` | Mist Green | 雾绿 | ミストグリーン | `SettingsWindow.axaml` | Theme option |
| `Settings.Theme.SlateBlue` | Slate Blue | 冷灰蓝 | スレートブルー | `SettingsWindow.axaml` | Theme option |
| `Settings.Theme.WarmPaper` | Warm Paper | 暖纸米色 | ウォームペーパー | `SettingsWindow.axaml` | Theme option |
| `Settings.Theme.NightGraphite` | Night Graphite | 夜间石墨 | ナイトグラファイト | `SettingsWindow.axaml` | Theme option |
| `Settings.Language.Label` | Language | 语言 | 言語 | `SettingsWindow.axaml.cs`, `Localization/*.json` | Combo label |
| `Settings.Language.Description` | Choose the UI language. Changes take effect after restarting ComicPlate. | 选择界面语言。更改将在重启 ComicPlate 后生效。 | UI 言語を選択します。変更は ComicPlate の再起動後に適用されます。 | `SettingsWindow.axaml.cs`, `Localization/*.json` | Combo description |
| `Settings.Language.System` | System | 跟随系统 | システムに合わせる | `Localization/*.json` | zh-Hans external name is fixed |
| `Settings.Language.English` | English | English | English | `Localization/*.json` | Language option; can stay as English |
| `Settings.Language.SimplifiedChinese` | Chinese | 中文 | 中文 | `Localization/*.json` | Internal tag remains `zh-Hans`; external UI name is `中文` |
| `Settings.Language.Japanese` | Japanese | 日本語 | 日本語 | `Localization/*.json` | Language option |
| `Settings.Magnifier.Label` | Magnifier | 放大镜 | 拡大表示 | `SettingsWindow.axaml` | Toggle label |
| `Settings.Magnifier.Description` | Hold Z to magnify comic content in the reader; use the mouse wheel while magnified to adjust scale. | 按住 Z 可在阅读器中放大漫画内容；放大时使用鼠标滚轮调整倍率。 | Z を押している間、リーダー内の漫画コンテンツを拡大表示します。拡大中にマウスホイールで倍率を調整できます。 | `SettingsWindow.axaml` | Toggle description |
| `Settings.Magnifier.ShortcutHint` | Z - Magnify | Z - 放大 | Z - 拡大表示 | `SettingsWindow.axaml` | Shortcut hint |
| `Settings.DataFolder.Label` | Data folder | 数据文件夹 | データフォルダー | `SettingsWindow.axaml` | Row label |
| `Settings.DataFolder.Description` | Stores local settings, reading progress, and cache. | 存储本地设置、阅读进度和缓存。 | ローカル設定、読書の進行状況、キャッシュを保存します。 | `SettingsWindow.axaml` | Row description |
| `Settings.DataFolder.OpenInExplorer` | Open in File Explorer | 在文件资源管理器中打开 | File Explorer で開く | `SettingsWindow.axaml.cs` | Windows-specific button |
| `Settings.DataFolder.OpenInFinder` | Open in Finder | 在 Finder 中打开 | Finder で開く | `SettingsWindow.axaml.cs` | macOS-specific button |
| `Settings.ThumbnailCache.Label` | Thumbnail cache | 缩略图缓存 | サムネイルキャッシュ | `SettingsWindow.axaml` | Row label |
| `Settings.ThumbnailCache.Description` | Clear rebuildable cover thumbnails. | 清除可重建的封面缩略图。 | 再生成可能な表紙サムネイルをクリアします。 | `SettingsWindow.axaml` | Row description |
| `Settings.ThumbnailCache.Cleared` | Thumbnail cache cleared. | 缩略图缓存已清除。 | サムネイルキャッシュをクリアしました。 | `SettingsWindow.axaml.cs` | Status message |
| `Settings.FileAssociations.Description` | These associations are not enabled automatically. They only change the system default opener after you select them. | 这些关联不会自动启用。只有在你选择后，它们才会更改系统默认打开方式。 | これらの関連付けは自動では有効になりません。選択した場合にのみ、システムの既定の開き方が変更されます。 | `SettingsWindow.axaml` | Section description |
| `Settings.ExplorerContextMenu.Title` | File Explorer context menu | 文件资源管理器上下文菜单 | File Explorer コンテキストメニュー | `SettingsWindow.axaml` | Windows wording; macOS section is hidden |
| `Settings.ExplorerContextMenu.Description` | Add "Open in ComicPlate" to the File Explorer context menu. | 将“在 ComicPlate 中打开”添加到文件资源管理器上下文菜单。 | File Explorer のコンテキストメニューに「ComicPlate で開く」を追加します。 | `SettingsWindow.axaml` | Section description |
| `Settings.ExplorerContextMenu.OptionDescription` | Show "Open in ComicPlate" in the context menu. | 在上下文菜单中显示“在 ComicPlate 中打开”。 | コンテキストメニューに「ComicPlate で開く」を表示します。 | `SettingsWindow.axaml` | Option description |
| `Settings.Shortcuts.Label` | Shortcut list | 快捷键列表 | ショートカット一覧 | `SettingsWindow.axaml` | Row label |
| `Settings.Shortcuts.Description` | Shortcuts are shown in a separate window. Editing is not available yet. | 快捷键会在单独窗口中显示。暂不支持编辑。 | ショートカットは別ウィンドウに表示されます。編集はまだ利用できません。 | `SettingsWindow.axaml` | Row description |

## **Main Window And Start**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `Main.OpenComic.Tooltip` | Open comic | 打开漫画 | 漫画を開く | `MainWindow.axaml` | Command rail tooltip |
| `Main.NewWindow.Tooltip` | New window | 新建窗口 | 新規ウィンドウ | `MainWindow.axaml` | Command rail tooltip |
| `Main.Settings.Tooltip` | Settings | 设置 | 設定 | `MainWindow.axaml` | Command rail tooltip |
| `Start.OpenComics` | Open Comics | 打开漫画 | 漫画を開く | `StartView.axaml` | Start page button |
| `Start.ContinueReading` | Continue Reading | 继续阅读 | 続きから読む | `ReadingSessionController.cs` | Button text when no saved item |
| `Start.ContinueReadingWithTitle` | Continue Reading "{0}" | 继续阅读“{0}” | 「{0}」の続きを読む | `ReadingSessionController.cs` | `{0}` is comic display name; do not localize the inserted title |

## **Shelf**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `Shelf.Title` | Shelf | 书架 | シェルフ | `MainWindowViewModel.cs`, `ContextShelfView.axaml` tooltip | Pane title/tooltip |
| `Shelf.History` | History | 历史记录 | 履歴 | `MainWindowViewModel.cs`, `ContextShelfView.axaml` tooltip | Pane title/tooltip |
| `Shelf.LocateCurrent` | Locate in Shelf | 在书架中定位 | シェルフ内で表示 | `ContextShelfView.axaml` | Tooltip |
| `Shelf.UpOneLevel` | Up One Level | 返回上一级 | 1 つ上の階層へ | `ContextShelfView.axaml` | Tooltip |
| `Shelf.Hide` | Hide Shelf | 隐藏书架 | シェルフを非表示 | `MainWindowViewModel.cs` | Command rail tooltip state |
| `Shelf.Show` | Show Shelf | 显示书架 | シェルフを表示 | `MainWindowViewModel.cs` | Command rail tooltip state |
| `Shelf.Kind.Folder` | Folder | 文件夹 | フォルダー | `ContentListItemViewModel.cs` | Badge/detail; may be localized |
| `Shelf.Kind.Archive` | Archive | 压缩包 | アーカイブ | `ContentListItemViewModel.cs` | Badge; may be localized |
| `Shelf.Kind.Image` | Image | 图片 | 画像 | `ContentListItemViewModel.cs` | Detail; may be localized |
| `Shelf.Kind.ComicFolder` | Comic folder | 漫画文件夹 | 漫画フォルダー | `ContentListItemViewModel.cs` | Detail; may be localized |
| `Shelf.Kind.ZipCbz` | ZIP/CBZ | ZIP/CBZ | ZIP/CBZ | `ContentListItemViewModel.cs` | Raw format labels; do not localize |
| `Shelf.Kind.RarCbr` | RAR/CBR | RAR/CBR | RAR/CBR | `ContentListItemViewModel.cs` | Raw format labels; do not localize |

## **Reader**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `Reader.PreviousFrame.Tooltip` | Previous reading frame | 上一个阅读画面 | 前の読書フレーム | `ReaderSurfaceView.axaml` | Tooltip, repeated in several bottom overlays |
| `Reader.NextFrame.Tooltip` | Next reading frame | 下一个阅读画面 | 次の読書フレーム | `ReaderSurfaceView.axaml` | Tooltip, repeated in several bottom overlays |
| `Reader.SinglePage` | Single page | 单页 | 単ページ | `ReaderSurfaceViewModel.cs` | View mode tooltip |
| `Reader.DoublePage` | Double page | 双页 | 見開き | `ReaderSurfaceViewModel.cs` | View mode tooltip |
| `Reader.DirectionLeftToRight` | LTR | LTR | LTR | `ReaderSurfaceViewModel.cs` | Can remain as short label |
| `Reader.DirectionRightToLeft` | RTL | RTL | RTL | `ReaderSurfaceViewModel.cs` | Can remain as short label |
| `Reader.ImageDisplayError` | Could not display | 无法显示 | 表示できませんでした | `ReaderStripRefreshCoordinator.cs` | Followed by page display name on next line; do not localize inserted display name |
| `Reader.PageProgress` | {0} / {1} | {0} / {1} | {0} / {1} | `ReaderFramePageTextFormatter.cs` | Page numbers are not localized |
| `Reader.PageProgressSpread` | {0}-{1} / {2} | {0}-{1} / {2} | {0}-{1} / {2} | `ReaderFramePageTextFormatter.cs` | Page numbers are not localized |
| `Reader.ZoomScale` | {0:0.0}x | {0:0.0}x | {0:0.0}x | `ReaderSurfaceViewModel.cs` | Zoom value/unit should remain unchanged unless design changes |

## **Status And Empty States**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `Status.LoadingContents` | Loading contents... | 正在加载内容... | コンテンツを読み込み中... | `MainWindowViewModel.cs` | Loading status |
| `Status.LoadingPages` | Loading pages... | 正在加载页面... | ページを読み込み中... | `MainWindowViewModel.cs` | Loading status |
| `Status.CannotReadFolder` | ComicPlate could not read this folder. | ComicPlate 无法读取此文件夹。 | ComicPlate はこのフォルダーを読み取れませんでした。 | `MainWindowViewModel.cs` | Error status |
| `Status.FolderNoReadableContents` | This folder has no readable contents. | 此文件夹没有可读取的内容。 | このフォルダーには読み取れるコンテンツがありません。 | `MainWindowViewModel.cs` | Empty status |
| `Status.SelectItemFromCurrentFolder` | Select an item from the current folder. | 从当前文件夹中选择一个项目。 | 現在のフォルダーから項目を選択してください。 | `MainWindowViewModel.cs` | Empty/next action status |
| `Status.ComicNoReadableImages` | This comic has no readable images. | 此漫画没有可读取的图片。 | この漫画には読み取れる画像がありません。 | `MainWindowViewModel.cs` | Empty status |
| `Status.CannotReadComic` | ComicPlate could not read this comic. | ComicPlate 无法读取此漫画。 | ComicPlate はこの漫画を読み取れませんでした。 | `MainWindowViewModel.cs` | Error status |
| `Status.PathMissing` | ComicPlate could not find this path. | ComicPlate 找不到此路径。 | ComicPlate はこのパスを見つけられませんでした。 | `MainWindowViewModel.cs` | Startup/open path error |
| `Status.FileTypeUnsupported` | ComicPlate cannot open this file type yet. | ComicPlate 暂不支持打开此文件类型。 | ComicPlate はまだこのファイル形式を開けません。 | `MainWindowViewModel.cs` | Startup/open path error |
| `Status.CannotOpenPath` | ComicPlate could not open this path. | ComicPlate 无法打开此路径。 | ComicPlate はこのパスを開けませんでした。 | `MainWindowViewModel.cs` | Startup/open path error |

## **Shortcut Window**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `Shortcuts.Title` | Shortcuts | 快捷键 | ショートカット | `ShortcutWindow.axaml` | Window title and heading |
| `Shortcuts.Intro` | This window shows the current fixed shortcuts. Editing is not available yet. | 此窗口显示当前固定快捷键。暂不支持编辑。 | このウィンドウには現在の固定ショートカットが表示されます。編集はまだ利用できません。 | `ShortcutWindow.axaml` | Avoid platform-specific text unless needed |
| `Shortcuts.Group.Navigation` | Page Navigation | 页面导航 | ページナビゲーション | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Group heading |
| `Shortcuts.Group.Actions` | Actions | 操作 | アクション | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Group heading |
| `Shortcuts.NextPage` | Next page | 下一页 | 次のページ | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.PreviousPage` | Previous page | 上一页 | 前のページ | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.FirstPage` | First page | 首页 | 最初のページ | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.LastPage` | Last page | 末页 | 最後のページ | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.OpenComic` | Open comic | 打开漫画 | 漫画を開く | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.NewWindow` | New window | 新建窗口 | 新規ウィンドウ | `ShortcutRegistry.cs` | Action label |
| `Shortcuts.ToggleShelf` | Show/Hide Shelf | 显示/隐藏书架 | シェルフの表示/非表示 | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.ToggleViewMode` | Toggle single/double page | 切换单页/双页 | 単ページ/見開きを切り替え | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.ToggleReadingDirection` | Reading direction | 阅读方向 | 読み方向 | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.Fullscreen` | Full screen | 全屏 | フルスクリーン | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.Settings` | Settings | 设置 | 設定 | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |
| `Shortcuts.BackToStart` | Back to start page | 返回起始页 | スタートページに戻る | `ShortcutWindow.axaml`, `ShortcutRegistry.cs` | Action label |

Do not localize shortcut key display values unless Avalonia/platform output changes:

| Value | Notes |
|---|---|
| `Right` | Key name |
| `Left` | Key name |
| `Home` | Key name |
| `End` | Key name |
| `O` | Key name |
| `N` | Key name |
| `Tab` | Key name |
| `Q` | Key name |
| `R` | Key name |
| `F` | Key name |
| `,` | Key name |
| `W` | Key name |
| `macOS` | Platform name; usually keep as product/platform name |
| `Windows` | Platform name; usually keep as product/platform name |

## **File Associations**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `FileAssociation.Status.Associated` | Associated with ComicPlate. | 已关联到 ComicPlate。 | ComicPlate に関連付けられています。 | `WindowsFileAssociationService.cs` | Option status |
| `FileAssociation.Status.NotAssociated` | Not associated. | 未关联。 | 関連付けられていません。 | `WindowsFileAssociationService.cs` | Option status |
| `FileAssociation.Error.UnsupportedFormat` | Unsupported file format. | 不支持的文件格式。 | サポートされていないファイル形式です。 | `WindowsFileAssociationService.cs` | Error message |
| `FileAssociation.Result.Associated` | {0} is now associated with ComicPlate. | {0} 现已关联到 ComicPlate。 | {0} は ComicPlate に関連付けられました。 | `WindowsFileAssociationService.cs` | `{0}` is raw format display name such as `CBZ`; do not translate inserted value |
| `FileAssociation.Result.RegisteredNeedsWindowsConfirmation` | {0} has been registered; confirm ComicPlate in Windows default apps. | {0} 已注册；请在 Windows 默认应用中确认 ComicPlate。 | {0} は登録されました。Windows の既定のアプリで ComicPlate を確認してください。 | `WindowsFileAssociationService.cs` | Windows-specific |
| `FileAssociation.Error.AssociationFailed` | File association failed. Check system permissions. | 文件关联失败。请检查系统权限。 | ファイル関連付けに失敗しました。システム権限を確認してください。 | `WindowsFileAssociationService.cs` | Error message |
| `FileAssociation.Result.StillAssociatedByWindows` | {0} is still associated with ComicPlate by Windows default app settings; remove it in Windows default apps. | Windows 默认应用设置中仍将 {0} 关联到 ComicPlate；请在 Windows 默认应用中移除。 | Windows の既定のアプリ設定では、{0} はまだ ComicPlate に関連付けられています。Windows の既定のアプリで削除してください。 | `WindowsFileAssociationService.cs` | Windows-specific |
| `FileAssociation.Result.Disassociated` | {0} is no longer associated with ComicPlate. | {0} 已不再关联到 ComicPlate。 | {0} は ComicPlate との関連付けが解除されました。 | `WindowsFileAssociationService.cs` | Result message |
| `FileAssociation.Error.DisassociationFailed` | Removing file association failed. Check system permissions or Windows default app settings. | 移除文件关联失败。请检查系统权限或 Windows 默认应用设置。 | ファイル関連付けの削除に失敗しました。システム権限または Windows の既定のアプリ設定を確認してください。 | `WindowsFileAssociationService.cs` | Error message |
| `FileAssociation.Status.MacUnsupported` | macOS file associations must be handled through the app bundle or system settings. | macOS 文件关联必须通过 app bundle 或系统设置处理。 | macOS のファイル関連付けは、アプリバンドルまたはシステム設定で処理する必要があります。 | `MacOSFileAssociationService.cs` | macOS-specific |
| `FileAssociation.Status.PlatformUnsupported` | This platform does not support file associations from ComicPlate. | 此平台不支持从 ComicPlate 设置文件关联。 | このプラットフォームでは、ComicPlate からファイル関連付けを設定できません。 | `UnsupportedFileAssociationService.cs` construction sites | Platform-specific |
| `FileAssociation.Windows.FriendlyTypeName` | {0} comic archive | {0} 漫画压缩包 | {0} 漫画アーカイブ | `WindowsFileAssociationService.cs` registry `FriendlyTypeName` | User-visible system integration; `{0}` is raw format label |
| `FileAssociation.Windows.ProgIdDescription` | ComicPlate {0} File | ComicPlate {0} 文件 | ComicPlate {0} ファイル | `WindowsFileAssociationService.cs` registry default value | User-visible system integration; `{0}` is raw format label |

Raw file type option labels should remain unchanged:

| Value | Notes |
|---|---|
| `CBZ` | Raw format label |
| `CBR` | Raw format label |
| `ZIP` | Raw format label |
| `RAR` | Raw format label |
| `.cbz` | File extension |
| `.cbr` | File extension |
| `.zip` | File extension |
| `.rar` | File extension |

## **Explorer Context Menu**

| Key | en source | zh-Hans | ja | Current/source location | Notes |
|---|---|---|---|---|---|
| `ExplorerContextMenu.Verb.OpenInComicPlate` | Open in ComicPlate | 在 ComicPlate 中打开 | ComicPlate で開く | `WindowsExplorerContextMenuService.cs`, `SettingsWindow.axaml` | User-visible system integration text |
| `ExplorerContextMenu.Status.Registered` | Context menu registered. | 上下文菜单已注册。 | コンテキストメニューを登録しました。 | `WindowsExplorerContextMenuService.cs` | General status |
| `ExplorerContextMenu.Status.Removed` | Context menu removed. | 上下文菜单已移除。 | コンテキストメニューを削除しました。 | `WindowsExplorerContextMenuService.cs` | General status |
| `ExplorerContextMenu.Error.RegistrationFailed` | Context menu registration failed. | 上下文菜单注册失败。 | コンテキストメニューの登録に失敗しました。 | `WindowsExplorerContextMenuService.cs` | Error status |
| `ExplorerContextMenu.Error.RemovalFailed` | Context menu removal failed. | 上下文菜单移除失败。 | コンテキストメニューの削除に失敗しました。 | `WindowsExplorerContextMenuService.cs` | Error status |
| `ExplorerContextMenu.Error.SettingFailed` | Context menu setting failed. Check system permissions. | 上下文菜单设置失败。请检查系统权限。 | コンテキストメニュー設定に失敗しました。システム権限を確認してください。 | `WindowsExplorerContextMenuService.cs` | Error status |
| `ExplorerContextMenu.Status.FormatRegistered` | {0} context menu registered. | {0} 上下文菜单已注册。 | {0} のコンテキストメニューを登録しました。 | `WindowsExplorerContextMenuService.cs` | `{0}` is raw format label |
| `ExplorerContextMenu.Status.FormatRemoved` | {0} context menu removed. | {0} 上下文菜单已移除。 | {0} のコンテキストメニューを削除しました。 | `WindowsExplorerContextMenuService.cs` | `{0}` is raw format label |
| `ExplorerContextMenu.Error.FormatRegistrationFailed` | {0} context menu registration failed. | {0} 上下文菜单注册失败。 | {0} のコンテキストメニュー登録に失敗しました。 | `WindowsExplorerContextMenuService.cs` | `{0}` is raw format label |
| `ExplorerContextMenu.Error.FormatRemovalFailed` | {0} context menu removal failed. | {0} 上下文菜单移除失败。 | {0} のコンテキストメニュー削除に失敗しました。 | `WindowsExplorerContextMenuService.cs` | `{0}` is raw format label |
| `ExplorerContextMenu.Error.UnsupportedFormat` | Unsupported file format. | 不支持的文件格式。 | サポートされていないファイル形式です。 | `WindowsExplorerContextMenuService.cs` | Error status |
| `ExplorerContextMenu.Status.PlatformUnsupported` | This platform does not support registering File Explorer context menus from ComicPlate. | 此平台不支持从 ComicPlate 注册文件资源管理器上下文菜单。 | このプラットフォームでは、ComicPlate から File Explorer のコンテキストメニューを登録できません。 | `UnsupportedExplorerContextMenuService.cs` | Platform-specific |

## **Current Non-Localized Values To Preserve**

These are visible in the UI but should not become normal translation entries.

| Value/pattern | Source location | Reason |
|---|---|---|
| `ComicPlate` | multiple | Product name |
| `CP` | `MainWindow.axaml` | App mark |
| File names, folder names, paths, archive entry names, comic titles | ViewModels/services | User content |
| `{0}` inserted comic title in `Start.ContinueReadingWithTitle` | `ReadingSessionController.cs` | User content |
| Page labels and page progress values such as `1 / 20`, `1-2 / 20` | `ReaderFramePageTextFormatter.cs`, `ReaderStripItemViewModel.cs` | Page numbers are user/navigation data |
| Zoom values such as `1.0x` | `ReaderSurfaceViewModel.cs` | Numeric UI value |
| Raw format labels `RAR`, `CBR`, `ZIP`, `CBZ`, `PDF` | file/archive UI | Format labels |
| Extensions `.rar`, `.cbr`, `.zip`, `.cbz`, `.pdf` | file/archive UI | File extensions |

# 压缩包格式评估与 V1 收束

日期：2026-05-17

本文记录 ComicPlate 对 RAR/CBR、7z/CB7 和嵌套压缩包的当前决策。本文只用于规划；除非后续任务明确要求，不代表已经实现支持、不代表可以添加依赖包，也不代表可以修改文件关联行为。

## 当前 V1 结论

当前 V1 压缩包范围收束为：

- 已有：ZIP、CBZ。
- 本轮新增目标：RAR、CBR。
- 不进入当前 V1：7z、CB7。
- 不进入当前 V1：嵌套压缩包。

ComicPlate 是轻量漫画预览器/阅读器，不是通用压缩包管理器。即使支持 RAR，也只是为了兼容用户手动打开把漫画页放在 `.rar` 里的情况；文件关联和 UI 仍优先面向漫画格式。

## 当前架构状态

相关代码：

- `src/ComicPlate.App/Services/ContentOpenService.cs`
- `src/ComicPlate.Infrastructure/FileSystem/ZipBookSource.cs`
- `src/ComicPlate.Infrastructure/FileSystem/FolderBookSource.cs`
- `src/ComicPlate.Infrastructure/FileSystem/FileSystemContextShelfSource.cs`
- `src/ComicPlate.Core/Books/IBookSource.cs`
- `src/ComicPlate.Core/Books/PageEntry.cs`
- `src/ComicPlate.Core/Books/BookSourceKind.cs`
- `src/ComicPlate.Core/Books/SupportedPageFormats.cs`
- `src/ComicPlate.Core/Sorting/NaturalPathComparer.cs`
- `src/ComicPlate.App/Services/SidebarThumbnailLoader.cs`
- `src/ComicPlate.App/Services/ThumbnailCacheService.cs`
- `src/ComicPlate.App/Services/AppDataService.cs`
- `src/ComicPlate.Infrastructure/Persistence/JsonAppStateStore.cs`
- ZIP 测试：`tests/ComicPlate.Tests/FileSystem/ZipBookSourceTests.cs`

当前 `ContentOpenService` 将目录识别为 `ContentFolder`，将 `.zip` 和 `.cbz` 识别为 `BookSourceKind.Zip`，将支持图片识别为 `BookSourceKind.Image`。

当前 `ZipBookSource` 使用 `System.IO.Compression.ZipFile.OpenRead`：

- 枚举压缩包条目。
- 过滤支持图片扩展名。
- 忽略非图片文件。
- 使用 `NaturalPathComparer` 按逻辑路径自然排序。
- 为每张图片创建 `PageEntry`。
- `PageEntry.OpenStreamAsync` 每次重新打开 ZIP，复制条目到 `MemoryStream`，再返回可 seek 的流。

这个模型适合扩展到 RAR/CBR：

- `IBookSource.LoadPagesAsync` 只加载页面元数据。
- `PageEntry.OpenStreamAsync` 是惰性、可重复打开的。
- 图片加载端负责释放返回的 stream。
- `progress.json` 已经以最终 Book 规范化路径为 key。
- `session.json` 只有一个 lastSession，不需要为 RAR/CBR 增加 schema。

需要注意的边界：

- 当前 `BookSourceKind` 只有 `Zip`，实现 RAR/CBR 时可以低成本新增 `Rar`，不要为了未来格式扩成巨大 enum。
- 当前 `SidebarThumbnailLoader` 有 ZIP 专用逻辑；实现 RAR/CBR 前应加最小格式映射/工厂，让 ZIP/CBZ/RAR/CBR 共享“读取第一张可读图片作为缩略图”的行为。
- 当前 ZIP 条目会复制进内存。RAR/CBR 也可以先保持返回 seekable stream，但需要关注大图内存风险。
- 不要做通用 archive manager 抽象；只做 ComicPlate 当前支持格式映射。

## 库候选

| 候选 | 适用范围 | 风险 | 当前建议 |
| --- | --- | --- | --- |
| `System.IO.Compression` | ZIP/CBZ | 只支持 ZIP | 继续保留现有 ZIP/CBZ 行为。 |
| SharpCompress | RAR/CBR，也支持更多格式 | 需要实测 RAR 变体、加密包、CJK 文件名和性能 | 当前 RAR/CBR 首选候选。纯托管、跨平台风险较低。 |
| SevenZipSharp / native 7z wrapper | 7z/CB7 | native 依赖、macOS arm64、打包和许可复杂度 | 当前 V1 不采用。 |

不要因为某个库“支持很多格式”就扩大产品范围。ComicPlate 当前只需要 ZIP/CBZ/RAR/CBR。

## RAR/CBR 策略

- CBR 按 RAR 漫画扩展处理。
- `.cbr` 和 `.rar` 都可以作为手动打开格式支持。
- 文件关联列表可以显示 CBZ、ZIP、CBR、RAR，但所有关联默认关闭，必须由用户显式点击。
- CBR 是漫画向格式；RAR 是泛用压缩包，UI 文案不要暗示它是推荐默认关联。

错误处理：

- 加密/password RAR/CBR 当前不做密码输入 UI。
- 读不了的加密包显示简单错误状态，例如“不支持加密压缩包”。
- 损坏或不支持变体显示打开失败，不崩溃。
- 不反复重试，不阻塞 UI。

实现要求：

- 只读取支持图片条目。
- 忽略非图片文件。
- 按逻辑路径自然排序。
- 支持压缩包内子目录图片。
- 不读取嵌套压缩包。
- 不修改用户压缩包。
- 不解压到用户漫画目录。
- `OpenStreamAsync` 每次返回新的可读流；如果图片加载要求 seekable stream，则返回可 seek 的 stream。

## 7z/CB7 策略

当前 V1 不支持 7z/CB7。

原因：

- 7z 常见 solid archive，对随机访问和按页惰性读取不友好。
- native 7z wrapper 会引入打包、许可和 macOS arm64 风险。
- ComicPlate 当前目标不是覆盖所有压缩包格式。

文档和 UI 要求：

- Todo 当前 V1 不列 7z/CB7 实现项。
- Settings 不显示 7z/CB7。
- 文件关联不显示 7z/CB7。
- 不添加 7z/CB7 placeholder。

后续只有在真实需求或外部贡献明确时，再重新评估。

## 嵌套压缩包策略

当前 V1 不支持嵌套压缩包。

当前行为应保持：

- ZIP/CBZ/RAR/CBR 内的嵌套压缩包作为非图片忽略，或作为不可打开项处理。
- 不能崩溃。
- 不能递归展开。
- 不能把嵌套压缩包串进当前 Book 的 Page 流。

如果未来重新评估，必须满足：

- 最大深度默认 1。
- 解出的临时文件只允许进入 ComicPlate app data 的 temp/cache。
- 不写入用户漫画目录。
- 不修改外层压缩包。
- 有大小限制、取消机制和清理策略。
- 不能做成压缩包浏览器。

当前不为嵌套压缩包设计进度身份。

## 文件关联策略

当前 V1 文件关联设置只显示：

- CBZ
- ZIP
- CBR
- RAR

规则：

- 不自动关联任何格式。
- 安装或启动时不修改系统默认程序。
- 只有用户在设置中明确点击，才尝试修改关联。
- 不显示 7z/CB7。
- 不显示未支持格式。
- 不显示图片格式关联。
- ZIP/RAR 是泛用压缩包，不应标成推荐默认。

平台边界：

- Windows 关联逻辑应放在平台服务中，不写进 Settings 代码后置。
- macOS 文件关联通常依赖 app bundle 的 `Info.plist`/文档类型，不做脆弱的运行时 hack。
- 如果某平台当前不能安全修改关联，应在平台服务中返回明确不可用状态，而不是在 UI 里放坏按钮。

## 需要的最小实现顺序

1. 文档收束：确认当前 V1 只做 ZIP/CBZ/RAR/CBR。
2. 增加最小压缩包格式映射：`.zip`/`.cbz` -> ZIP，`.rar`/`.cbr` -> RAR。
3. 让 `ContentOpenService`、Context Shelf、缩略图、文件关联设置共用这份映射。
4. 引入 RAR/CBR 支持候选库，优先评估 SharpCompress。
5. 实现 RAR/CBR BookSource。
6. 添加 RAR/CBR 测试。
7. 添加平台文件关联服务边界。
8. 设置页只显示 CBZ、ZIP、CBR、RAR 的用户控制关联操作。
9. 之后再做真实 Windows/macOS 文件关联落地。

## 必须测试

RAR/CBR：

- `.rar` 和 `.cbr` 能被识别为支持的压缩包。
- 只加载支持图片。
- 忽略非图片。
- 自然排序数字文件名。
- 自然排序子目录路径。
- 同一页可以重复打开 stream。
- 关闭 stream 后不锁住压缩包。
- 空包、损坏包、加密包不崩溃。
- CJK 文件名。
- progress 按规范化最终 Book 路径恢复。
- Context Shelf 缩略图使用第一张可读图片。

文件关联：

- 设置列表只包含 CBZ、ZIP、CBR、RAR。
- 不包含 7z/CB7。
- 不自动执行关联。
- 不支持的格式不能被关联服务接受。
- 测试不得修改真实开发机注册表或系统默认程序。

## 明确不做

- 不实现 7z/CB7。
- 不实现嵌套压缩包。
- 不递归展开压缩包。
- 不做密码输入 UI。
- 不解压到用户漫画目录。
- 不修改用户压缩包。
- 不做压缩包浏览器。
- 不做 tabs。
- 不做 workspace restore。
- 不添加 `cache.db`。

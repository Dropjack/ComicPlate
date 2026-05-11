# Architecture Spec

本文是 ComicPlate 的架构草案。它必须服务最小闭环，不允许为了未来高级功能过度抽象。ComicPlate 读取用户漫画内容，但不修改用户漫画内容。

## 1. 技术栈

建议：

- .NET 8 或更新 LTS。
- Avalonia UI。
- C#。
- MVVM，但只用最小必要模式。
- JSON 配置。

原因：

- Avalonia 支持 macOS 和 Windows。
- C# 与 NeeView 源码同语言，便于参考概念。
- 不复用 NeeView WPF 代码，避免平台和复杂度绑定。

## 2. 项目分层

建议项目结构：

- `ComicPlate.App`：Avalonia UI、窗口、视图、ViewModel。
- `ComicPlate.Core`：Bookshelf、Book、Page、ReaderState、ReaderStrip、排序、文件来源接口。
- `ComicPlate.Infrastructure`：文件系统书架扫描、文件夹/ZIP BookSource、JSON 持久化、平台路径。
- `ComicPlate.Tests`：核心逻辑测试。

MVP 可以先用单项目起步，但命名空间按上述边界分组。等代码稳定后再拆项目。

## 3. 核心模块

### FileSource

责任：

- 根据路径打开文件夹或 ZIP。
- 返回 PageEntry 列表。
- 不负责图片解码。
- 不负责 UI。

工程命名说明：

- 文件夹漫画和 ZIP/CBZ 漫画在代码里都统一称为 Book。
- Book 内部的图片页面称为 Page。
- Bookshelf 是 Book 的列表。
- 用户界面文案可以写“漫画书”“文件夹漫画”“ZIP/CBZ 漫画”，不强迫用户理解 Book 这个工程词。

### Bookshelf

责任：

- 表示一个书架根目录。
- 递归发现根目录下可阅读的 Book。
- 支持文件夹作为文件夹 Book。
- 支持 `.zip` 和 `.cbz` 作为压缩包 Book。
- 使用明确边界避免章节目录被重复误判为独立书籍。
- 不修改用户文件。

建议模型：

```csharp
public sealed record Bookshelf(
    string RootPath,
    IReadOnlyList<BookEntry> Books);

public sealed record BookEntry(
    string Id,
    string DisplayName,
    BookSourceKind SourceKind,
    string Path);
```

注意：

- Bookshelf 是 Book 列表。
- PageList 是当前 Book 内的 Page 列表。
- 二者都可以显示在左侧栏，但不是同一个概念。
- Book 发现和 Page 收集必须分开：Bookshelf 负责发现 Book，IBookSource 负责打开一本 Book 并收集 Page。
- 文件系统里的 ZIP/CBZ 可以通过递归目录扫描被发现为 Book；ZIP/CBZ 内部不继续递归发现子 Book。
- 文件夹 Book 的边界由发现策略决定：当某个文件夹自身直接包含支持格式图片时，它可以成为 Book；它下面的子文件夹作为该 Book 的章节结构参与 Page 收集。
- 书架项后续可以有首页缩略图：文件夹 Book 取自然排序后的第一张图片，ZIP/CBZ Book 取压缩包内自然排序后的第一张图片。缩略图解码和缓存必须有上限，不能为生成书架一次性解码所有封面。

接口草案：

```csharp
public interface IBookSource
{
    string Id { get; }
    string DisplayName { get; }
    Task<IReadOnlyList<PageEntry>> LoadPagesAsync(CancellationToken cancellationToken);
}
```

### PageEntry

责任：

- 描述一页图片的位置和来源。

字段草案：

```csharp
public sealed record PageEntry(
    string DisplayName,
    string LogicalPath,
    PageSourceKind SourceKind,
    Func<CancellationToken, Task<Stream>> OpenStreamAsync);
```

注意：

- `LogicalPath` 用于排序和显示。
- `OpenStreamAsync` 让文件夹和 ZIP 都能统一读取。

### ImageLoader

责任：

- 把 PageEntry 解码成 Avalonia 可显示的图片对象。
- 管理流释放。
- 报告解码错误。

MVP 策略：

- 横向阅读带需要加载当前页附近的有限页面。
- 不允许一次性解码整本书。
- 缓存必须有明确上限，例如当前页和左右有限邻页。
- 翻页后释放超出窗口范围的 Bitmap。

### ReaderStrip

责任：

- 根据当前页、阅读方向、显示窗口大小，计算阅读带中应该出现哪些页面。
- 让当前页位于视觉中心。
- 根据 RightToLeft / LeftToRight 决定前后页排列方向。
- 为 UI 提供有限数量的可显示页面槽位。

规则：

- ReaderStrip 是阅读布局状态，不负责文件扫描。
- ReaderStrip 不直接解码图片。
- ReaderStrip 的窗口大小可以配置，但必须有上限，避免内存失控。

### ReaderState

责任：

- 当前 Book。
- 当前页索引。
- 页面总数。
- 下一页/上一页/跳转。
- 单页/双页模式。
- 阅读方向。
- 当前目录侧栏选择。
- 当前书架选择。
- 当前页面列表选择。

规则：

- 不直接读文件。
- 不直接解码图片。
- 可单元测试。

### Persistence

责任：

- 保存配置。
- 保存最近打开。
- 保存阅读进度。
- 只写 ComicPlate 自己的数据文件。

禁止：

- 修改用户图片。
- 修改用户压缩包。
- 移动、删除、重命名用户文件。

文件：

- `settings.json`
- `library.json`

位置：

- Windows：用户 AppData。
- macOS：用户 Library/Application Support。
- 开发模式可允许写到本地临时目录，但要明确。

## 4. 数据结构

`settings.json` 草案：

```json
{
  "version": 1,
  "readingDirection": "RightToLeft",
  "defaultFitMode": "Fit",
  "recentLimit": 20,
  "restoreProgress": true,
  "bookshelf": {
    "sortMode": "Name",
    "groupMode": "None"
  },
  "readerStrip": {
    "neighborPageLimit": 2
  }
}
```

`library.json` 草案：

```json
{
  "version": 1,
  "books": [
    {
      "id": "D:\\Comics\\BookA",
      "displayName": "BookA",
      "sourceKind": "Folder",
      "lastPageIndex": 41,
      "lastKnownPageCount": 180,
      "readingDirection": "RightToLeft",
      "viewMode": "DoublePage",
      "lastOpenedAt": "2026-04-25T10:30:00Z"
    }
  ]
}
```

说明：

- 配置文件格式当前仍按 JSON 草案记录。
- 书架排序、分组、过滤属于需要预留的配置区域，但 MVP 可以先只实现名称排序和无分组。
- 如果后续决定使用 TOML，应先作为单独决策记录，不和当前结构草案混用。

## 5. 错误处理

错误分层：

- Book 打不开：显示全页错误状态。
- 书架根目录打不开：显示书架错误状态。
- 书架为空：显示空书架状态。
- Book 没有图片：显示空状态。
- 单页打不开：显示错误占位页。
- 配置读失败：使用默认值并备份坏文件。
- 保存失败：不阻断阅读，但记录日志或状态。

MVP 不做复杂日志系统，但错误对象要带 message 和 exception。

## 6. 排序

必须实现自然排序。

排序输入：

- 文件夹：文件名。
- ZIP：压缩包内逻辑路径。

排序要求：

- 大小写不敏感优先。
- 数字按数字值比较。
- 结果稳定。

建议先为排序写单元测试：

- `1, 2, 10`
- `page001, page002, page010`
- `A1, a2, A10`
- 子目录路径排序。

## 7. 不提前设计的东西

不要提前设计：

- 插件接口。
- 脚本运行时。
- 可编辑命令系统。
- 复杂主题系统。
- 多窗口协调器。
- 拖拽组合/复杂面板停靠系统。
- 数据库。

如果未来确实需要，再从真实需求抽象。

## 8. 最小垂直切片

第一段代码只追求：

1. Avalonia 窗口启动。
2. 点击打开书架根目录。
3. 书架递归发现文件夹 Book 和 ZIP/CBZ Book。
4. 点击文件夹 Book 或 ZIP/CBZ Book 后收集并自然排序 Page。
5. 横向阅读带显示当前页，当前页居中。
6. 左右键按阅读方向翻页。

这条线跑通前，不做设置、不做视觉抛光、不做复杂书架管理。

这条线跑通后，第一批立即补齐：

1. 双页模式。
2. 阅读方向。
3. 基础页面列表。
4. 阅读进度保存。

## 9. Action 和右键菜单

ComicPlate 不做 NeeView 式复杂命令系统，但需要一组简单固定 Action。

示例：

- `OpenFolder`
- `OpenArchive`
- `NextPage`
- `PreviousPage`
- `GoToPage`
- `ToggleDoublePage`
- `SetReadingDirection`
- `FitToWindow`
- `RevealInFileManager`
- `CopyPath`
- `RemoveFromRecent`

规则：

- 快捷键、工具栏按钮、右键菜单都可以调用这些 Action。
- Action 不支持脚本扩展。
- Action 不支持用户复制或参数化命令。
- 右键菜单按区域固定定义。
- 所有 Action 必须遵守只读原则。

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
- `ComicPlate.Core`：Book、Page、ReadableUnit、ReaderState、ReaderStrip、排序、文件来源接口。
- `ComicPlate.Infrastructure`：可阅读单元打开、文件夹/ZIP BookSource、JSON 持久化、平台路径。
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

- 用户打开的文件夹、ZIP/CBZ 和后续支持的漫画压缩包在代码里都统一称为 Book。
- Book 内部的图片页面称为 Page。
- Book 是用户选择路径形成的可阅读单元，不是书架条目。
- 用户界面文案可以写“漫画”“文件夹”“ZIP/CBZ”，不强迫用户理解 Book 这个工程词。

### ReadableUnitOpener

责任：

- 接收用户主动选择的路径。
- 判断该路径是否能作为可阅读单元打开。
- 为该路径创建 BookEntry 和对应 IBookSource。
- 支持文件夹作为 Book。
- 支持 `.zip` 和 `.cbz` 作为 Book。
- 后续支持 RAR/CBR/7z/CB7 作为 Book。
- 支持单张图片作为单页 Book。
- 不做漫画库扫描，不自动拆分作品，不把子文件夹识别为独立书籍。
- 不修改用户文件。

建议模型：

```csharp
public sealed record BookEntry(
    string Id,
    string DisplayName,
    BookSourceKind SourceKind,
    string Path);
```

注意：

- PageList 是当前 Book 内的 Page 列表。
- BookEntry 描述当前打开范围，不描述书架中的一项。
- Book 打开和 Page 收集可以分开：ReadableUnitOpener 负责按路径选择 IBookSource，IBookSource 负责收集 Page。
- 文件夹 Book 默认递归收集内部图片，并按相对路径自然排序。
- ZIP/CBZ 内部子目录属于这本 ZIP/CBZ Book 的 Page 结构。
- 文件夹里的 ZIP/CBZ 串联阅读属于 MVP：它仍然生成同一个 Book 的 Page 流，不生成书架。

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
- MVP 可以使用三页窗口：当前页和左右邻页。单张图片 Book 只显示当前页。

规则：

- ReaderStrip 是阅读布局状态，不负责文件扫描。
- ReaderStrip 不直接解码图片。
- ReaderStrip 的窗口大小可以配置，但必须有上限，避免内存失控。

### Reader progress position

责任：

- 把 ReaderState 的当前页索引转换成 UI 进度条需要的视觉位置。
- 让进度条的深色和浅色交界点表示当前阅读位置在阅读带里的相对位置。
- 深色区域表示当前阅读位置左侧；浅色区域表示当前阅读位置右侧。
- 根据 ReadingDirection 换算视觉位置。

当前 MVP：

- 当前阅读位置等于当前页。
- LeftToRight：视觉位置等于当前页索引。
- RightToLeft：视觉位置等于最后一页索引减去当前页索引。
- 这段换算可以先放在 App 的 ViewModel，因为它直接服务 Avalonia ProgressBar 绑定。

未来状态：

- 双页模式、连续滚动或拖动预览加入后，“当前页”应升级为“当前阅读位置”。
- 如果进度条逻辑被多个 View 复用，再把换算规则下沉到 Core 的可测试服务。
- 颜色语义属于 UI 表现层；阅读位置换算属于阅读行为规则。

### PageDisplaySizeCalculator

责任：

- 根据图片原始尺寸、阅读区可用尺寸和 FitMode 计算显示尺寸。
- 默认 FitMode 为 AutoFit。
- AutoFit 下，普通竖页按阅读区高度适配，横向大图按完整窗口适配。
- 不负责图片解码，不负责 UI 控件。
- 可单元测试，避免缩放规则散落在 XAML 或 ViewModel 中。

### ReaderState

责任：

- 当前 Book。
- 当前页索引。
- 页面总数。
- 下一页/上一页/跳转。
- 单页/双页模式。
- 阅读方向。
- 当前目录侧栏选择。
- 当前页面列表选择。

规则：

- 不直接读文件。
- 不直接解码图片。
- 可单元测试。

### NavigationHistory

责任：

- 保存当前阅读流的轻量导航栈。
- 记录用户从哪个容器进入当前内容。
- 支持 Back 回到上一个 shelf 容器或上一本书所在的上层上下文。
- 区分“浏览 shelf 容器/打开 Book”和“跳转 Page”：前两者进入导航历史，Page 跳转只更新阅读进度。
- 可单元测试。

规则：

- NavigationHistory 不是书架，不保存扫描结果，不保存缩略图，不保存目录内容。
- 空启动的 Continue Reading 可以恢复少量历史 entry，让用户读完当前内容后继续 Back 回上层。
- 手动 Open 新内容时重置当前窗口的导航栈。
- Back 默认只刷新左侧 Context Shelf，不强制清空或重置主阅读面板。
- 导航栈应设置上限，例如 8 层，避免长期积累成文件浏览器历史。
- MVP 只做 Back；Forward 留到确认真实需求后再评估。

建议模型：

```csharp
public sealed record NavigationEntry(
    string Path,
    string DisplayName,
    BookSourceKind SourceKind);

public sealed class NavigationHistory
{
    public NavigationEntry? Current { get; }
    public bool CanGoBack { get; }
    public void StartAt(NavigationEntry entry);
    public void NavigateTo(NavigationEntry entry);
    public NavigationEntry? Back();
}
```

### Persistence

责任：

- 保存配置。
- 保存阅读进度。
- 保存上次阅读会话的轻量恢复状态。
- 只写 ComicPlate 自己的数据文件。

禁止：

- 修改用户图片。
- 修改用户压缩包。
- 移动、删除、重命名用户文件。

文件：

- `settings.json`
- `session.json`
- `progress.json`

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
  "defaultFitMode": "AutoFit",
    "progressLimit": 500,
  "restoreProgress": true,
  "readerStrip": {
    "neighborPageLimit": 2
  }
}
```

`progress.json` 草案：

```json
{
  "version": 1,
  "books": {
    "D:\\Comics\\BookA": {
      "path": "D:\\Comics\\BookA",
      "displayName": "BookA",
      "sourceKind": "Folder",
      "lastPageIndex": 41,
      "lastKnownPageCount": 180,
      "readingDirection": "RightToLeft",
      "viewMode": "DoublePage",
      "lastOpenedAt": "2026-04-25T10:30:00Z"
    }
  }
}
```

`progress.json` 规则：

- 记录用于同一路径再次打开时恢复页码，不展示为历史列表。
- progress key 使用最终打开的可阅读单元规范化路径，而不是启动入口路径或父容器路径。
- 打开父文件夹只加载该容器和 shelf；用户从 shelf 点进某个 ZIP/CBZ/文件夹漫画时，才用该最终 Book 路径查询并应用 progress。
- 记录数量必须有上限，MVP 默认 500 条；超过上限按 `lastOpenedAt` 删除最旧记录。
- 关闭时如果当前 Book 停在最后一页，MVP 可以删除该 Book 的 progress 记录。
- 路径变化视为新书；MVP 不做内容 hash 或跨盘符匹配。
- 多窗口同时写同一本书时，最后写入胜出。

`session.json` 草案：

```json
{
  "version": 1,
  "lastSession": {
    "current": {
      "path": "D:\\Comics\\Series\\Vol03",
      "displayName": "Vol03",
      "sourceKind": "Folder",
      "lastPageIndex": 207
    },
    "backStack": [
      {
        "path": "D:\\Comics\\Series",
        "displayName": "Series",
        "sourceKind": "Collection"
      }
    ],
    "savedAt": "2026-05-13T10:30:00Z"
  }
}
```

说明：

- 配置文件格式当前仍按 JSON 草案记录。
- 不为书架排序、分组、过滤预留配置；ComicPlate 不做漫画库管理。
- `session.json` 只保存路径、类型、页码和少量 Back 栈；不保存容器扫描结果、缩略图缓存、全库索引或 UI 展开状态。
- Continue Reading 只读取 `session.json` 的当前内容并打开；只有用户触发 Back 时才重新读取上层路径。
- 多窗口场景下，`lastSession` 表示最后更新或最后关闭的窗口，不表示窗口组或标签页系统。
- 如果后续决定使用 TOML，应先作为单独决策记录，不和当前结构草案混用。

## 5. 错误处理

错误分层：

- Book 打不开：显示全页错误状态。
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

## 8. 资源和图标规范

ComicPlate 的窗口级图标使用“源文件清楚、运行时轻量”的规则。

源文件规则：

- 图标源文件放在 `src/ComicPlate.App/Assets/Icons/`。
- 每个图标一个独立 SVG 文件，不使用一整张 PNG sprite。
- SVG 使用 `viewBox="0 0 24 24"`。
- SVG 使用 `stroke="currentColor"`、`fill="none"`。
- SVG 不包含文字、渐变、阴影、背景或位图。
- 图标应是单色线性图标，视觉上靠近 Fluent / Obsidian / VS Code 的桌面工具风格。

运行时规则：

- Avalonia 默认不直接渲染 SVG 文件；除非明确引入 SVG 渲染包，否则不要在 XAML 中直接引用 SVG 作为图片控件来源。
- 当前实现把 SVG 线条转换为 `StreamGeometry`，再用 `PathIcon` 显示。
- `PathIcon` 的颜色跟随按钮 `Foreground`，因此可以配合主题、hover 和 disabled 状态。
- 状态型按钮不换命令，只换图标可见性。例如单页/双页、LTR/RTL、Shelf show/hide。
- PNG 只适合临时预览或复杂插画，不作为 Command Rail 图标首选格式。

命名规则：

- 打开入口：`open-book.svg`。
- Shelf 状态：`shelf-show.svg`、`shelf-hide.svg`。
- 阅读模式：`single-page.svg`、`double-page.svg`。
- 阅读方向：`read-ltr.svg`、`read-rtl.svg`。
- 设置入口：`user-settings.svg`，优先使用 sliders，不使用齿轮。
- 阅读帧导航：`nav-left.svg`、`nav-right.svg`。

替换规则：

- 美术替换图标时，优先保持文件名和语义不变。
- 如果只改线条外观，不应改 ViewModel 或命令逻辑。
- 如果新增图标状态，先讨论语义，再新增资源和绑定。

## 9. 最小垂直切片

第一段代码只追求：

1. Avalonia 窗口启动。
2. 点击打开文件夹、ZIP/CBZ 或单张图片。
3. 把用户选择的路径作为 Book 打开。
4. 收集并自然排序 Page，包括文件夹内 ZIP/CBZ 合集。
5. 横向阅读带显示当前页，当前页居中。
6. 左右键按阅读方向翻页。
7. 底部进度条显示当前阅读位置在阅读带里的相对位置。

这条线跑通前，不做设置、不做视觉抛光、不做漫画库管理。

这条线跑通后，第一批立即补齐：

1. 双页模式。
2. 阅读方向设置 UI。
3. 基础页面列表。
4. 阅读进度保存。
5. 多窗口：每个窗口是一套完整独立阅读器。

## 10. Action 和右键菜单

ComicPlate 不做 NeeView 式复杂命令系统，但需要一组简单固定 Action。

示例：

- `OpenContent`
- `OpenArchive`
- `NextPage`
- `PreviousPage`
- `GoToPage`
- `ToggleDoublePage`
- `SetReadingDirection`
- `FitToWindow`
- `RevealInFileManager`
- `CopyPath`

规则：

- 快捷键、工具栏按钮、右键菜单都可以调用这些 Action。
- Action 不支持脚本扩展。
- Action 不支持用户复制或参数化命令。
- 右键菜单按区域固定定义。
- 所有 Action 必须遵守只读原则。

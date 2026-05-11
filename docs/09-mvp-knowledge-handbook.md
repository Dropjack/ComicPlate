# MVP Knowledge Handbook

这份手册记录 ComicPlate 从“能打开漫画”走到“像 NeeView 那样顺滑好用”之间，已经学到、已经决定、接下来应该继续学习和改进的内容。

它不是需求合同。需求合同仍然是：

- `02-scope-cut.md`
- `03-behavior-spec.md`
- `04-ui-ux-spec.md`
- `05-architecture-spec.md`

这份手册更像复盘地图：方便你去和 GPT 学习，也方便之后回来看“为什么当时这样做”。

## 0. 当前 MVP 状态

截至当前阶段，ComicPlate 已经能作为 MVP 阅读漫画：

- 能启动 Avalonia 桌面应用。
- 能通过 `Open Folder` 选择书架根目录。
- 书架能递归发现可阅读的 Book。
- 文件夹漫画可以作为 Book 打开。
- ZIP/CBZ 漫画可以作为 Book 打开。
- Book 里面的图片会成为 Page。
- 文件夹 Book 会递归收集子文件夹图片。
- ZIP/CBZ Book 会读取压缩包内部子目录图片。
- 非图片文件会被忽略。
- 压缩包套压缩包不做。
- 左侧有 Bookshelf 和 Pages 两个面板。
- 默认阅读方向是 RightToLeft。
- 默认缩放方向已经从固定小槽位改为 AutoFit。
- 有基础翻页按钮、页面列表和进度条。
- 有基础图片缓存和释放策略。

目前它“能看漫画”，但还没有 NeeView 的顺滑度、成熟度和大量细节。

## 1. 我们真正要学 NeeView 的什么

NeeView 不是要照抄的对象。它的源码很复杂，功能也远超 ComicPlate MVP。

ComicPlate 要学的是 NeeView 的几个核心心智：

- 漫画阅读器不是图片查看器。
- 文件夹和压缩包都是 Book。
- 图片是 Page。
- 书架显示很多 Book。
- 页面列表显示当前 Book 里的 Page。
- 阅读方向会影响翻页和页面排列。
- 阅读画布应该围绕“当前页舒服可读”设计。
- 缩放、缓存、排序、历史记录都服务阅读连续性。

暂时不学：

- 插件系统。
- 脚本系统。
- 可编辑命令系统。
- 复杂停靠面板。
- 多媒体/PDF/Susie 等高级来源。
- 复杂主题系统。

## 2. Book 和 Page

ComicPlate 的核心概念是：

- 文件夹 = 一本 Book。
- ZIP/CBZ = 一本 Book。
- Book 里面的图片 = Page。
- 书架 = 很多 Book。
- 页面列表 = 当前 Book 里的 Page。

这件事看起来简单，但它是整个项目的核心。

如果把项目理解成“打开一堆图片”，后面会很快混乱：

- 进度不知道按什么保存。
- 最近打开不知道保存文件还是文件夹。
- ZIP/CBZ 和文件夹逻辑会分裂。
- 页面列表和书架容易混成一个东西。
- 双页和阅读方向很难保持一致。

所以工程上统一叫 Book/Page，是为了让文件夹漫画和 ZIP/CBZ 漫画被同一种阅读逻辑处理。

## 3. 书架递归和 Page 递归不是一回事

这是这轮开发中最重要的边界之一。

书架递归：

- 从用户选的书架根目录开始。
- 递归寻找可阅读的 Book。
- 遇到 `.zip` 或 `.cbz`，识别成 ZIP/CBZ Book。
- 遇到直接包含图片的文件夹，识别成文件夹 Book。
- 一个文件夹一旦被识别成 Book，就不再把它下面的子文件夹重复列成 Book。

Page 递归：

- 用户打开一本 Book 后才发生。
- 如果是文件夹 Book，就递归收集这个文件夹下面的图片。
- 如果是 ZIP/CBZ Book，就收集压缩包内部所有子目录里的图片。
- 收集到的图片按逻辑路径自然排序。

一句话：

书架递归是在找“哪些东西是书”。  
Page 递归是在找“这本书里有哪些页”。

这两个递归不能混成一个。

## 4. ZIP/CBZ 为什么比文件夹复杂

文件夹图片通常是这样读取：

```text
磁盘路径 -> FileStream -> 图片解码
```

ZIP/CBZ 图片是这样读取：

```text
压缩包路径 -> ZIP 条目 -> 条目流 -> 图片解码
```

ZIP 里不是直接的一堆文件路径，而是一堆 entry：

- entry 可能是目录。
- entry 可能是图片。
- entry 可能是说明文件。
- entry 可能是另一个压缩包。
- entry 的路径可能包含子目录。

本项目的 MVP 规则：

- 只收支持图片格式。
- 忽略目录 entry。
- 忽略非图片。
- 忽略 ZIP 里的 ZIP/RAR/7z。
- 不把 ZIP 内容解压到用户目录。
- 不修改压缩包。

这轮还遇到一个真实问题：ZIP entry stream 和普通 FileStream 不完全一样。

为了让图片解码更稳定，当前实现会把 ZIP entry 复制成 `MemoryStream`，再交给图片层。这样它更像一个普通、可定位、从开头开始的图片流。

这不是最终最省内存的方案，但对于 MVP 是合理的：

- 一次只加载有限邻页。
- 不会整本 ZIP 一次性解码。
- 每页解码完成后不再保留压缩包句柄。

## 5. 自然排序

普通字符串排序会这样：

```text
1.jpg
10.jpg
2.jpg
```

漫画阅读需要自然排序：

```text
1.jpg
2.jpg
10.jpg
```

自然排序是漫画阅读器的基础体验，不是锦上添花。

排序对象不同：

- 文件夹 Book：按相对路径排序，例如 `Chapter 1/001.jpg`。
- ZIP/CBZ Book：按压缩包内部逻辑路径排序，例如 `001 绿之座/001.jpg`。
- 书架：按显示名排序，同名时按完整路径稳定排序。

够用理解：

你不需要自己写排序算法，但你要能看懂测试数据是否符合阅读直觉。

## 6. 只读原则

ComicPlate 是阅读器，不是文件管理器。

允许写：

- ComicPlate 自己的设置。
- ComicPlate 自己的进度。
- ComicPlate 自己的最近打开。
- ComicPlate 自己的日志。
- ComicPlate 自己的缓存。

禁止写：

- 用户图片。
- 用户压缩包。
- 用户漫画目录结构。
- 用户漫画文件名。

所以未来右键菜单里可以有：

- 打开。
- 复制路径。
- 在 Explorer/Finder 中显示。
- 从最近打开记录中移除。

但不应该有：

- 删除原文件。
- 移动原文件。
- 重命名原文件。
- 修改压缩包。
- 覆盖原图。

这个原则要一直守住。

## 7. 图片显示和缩放

一开始 ComicPlate 的图片看起来小，不是因为图片没有 Fit，而是因为每个页面外面套了固定小槽位。

旧问题大概是：

```text
阅读区 -> 一排固定宽度槽位 -> 图片适配槽位
```

结果是：

- 当前页只有小槽位那么大。
- 左右邻页也完整挤进屏幕。
- 看起来像缩略图横排，不像阅读器。

现在的方向是：

```text
阅读区真实尺寸 -> 图片原始尺寸 -> AutoFit -> 页面显示尺寸
```

AutoFit 规则：

- 普通竖页：优先适配阅读区高度。
- 横向大图：优先完整适配阅读区。
- 保持比例。
- 不裁切。
- 默认允许放大和缩小。

这个规则是为了适配不同屏幕：

- 25 寸 Windows 显示器。
- 14 寸 MacBook 屏幕。
- 未来可能还有不同 DPI、窗口大小、侧栏状态。

阅读器不能写死某个屏幕尺寸。它必须根据当前阅读区实际尺寸重新计算。

## 8. 内存和资源释放

漫画图片可能很大。

危险路线：

```text
打开一本书 -> 一次性把所有图片解码成 Bitmap
```

这会快速吃掉大量内存，尤其是：

- 高分辨率漫画。
- 长篇漫画。
- ZIP/CBZ。
- 双页/邻页预览。

当前 MVP 路线：

- PageEntry 只描述页面在哪里。
- 真正需要显示时才打开流。
- 只加载当前页附近有限页面。
- 翻页后释放不再需要的 Bitmap。
- ZIP entry 每次打开时复制为 MemoryStream，解码后释放。

要继续学习的词：

- stream。
- bitmap。
- cache。
- dispose。
- handle。
- memory pressure。

够用判断：

如果某个方案说“先把整本漫画都读进内存”，它大概率不适合 ComicPlate。

## 9. Avalonia 和 MVVM

当前项目中：

- `ComicPlate.App` 是 Avalonia UI。
- `MainWindow.axaml` 是界面结构。
- `MainWindow.axaml.cs` 是窗口事件桥接。
- `MainWindowViewModel.cs` 保存界面状态和命令。
- `ComicPlate.Core` 保存阅读器核心规则。
- `ComicPlate.Infrastructure` 处理真实文件系统、ZIP、持久化等现实世界接口。

MVVM 的够用理解：

- View 是界面。
- ViewModel 是界面的状态和动作。
- Core 是业务规则。
- Infrastructure 是现实世界接入。

例如：

- 按钮显示在哪里：View。
- 按钮点击后调用什么：ViewModel。
- 下一页怎么算：Core。
- 文件夹怎么扫描：Infrastructure。
- ZIP 怎么读取：Infrastructure。

如果把文件扫描直接写进按钮控件里，后面很快会乱。

## 10. Core 和 Infrastructure 的边界

Core 负责领域规则。

例如：

- Book。
- Page。
- ReadingDirection。
- ReaderState。
- FitMode。
- PageDisplaySizeCalculator。
- 支持哪些图片格式。

Infrastructure 负责接触现实世界。

例如：

- `Directory.EnumerateFiles`。
- `Path.GetExtension`。
- `ZipFile.OpenRead`。
- JSON 文件读写。
- AppData 路径。

一句有用的判断：

Core 不应该认识真实磁盘路径怎么枚举。  
Infrastructure 可以认识 `Path`、`Directory`、`ZipFile`。

但不是所有东西都绝对固定。有些规则可以粗拆，也可以严拆。

例如“支持哪些图片格式”：

- 可以先放 Infrastructure，因为扫描文件时才用。
- 也可以放 Core，因为“什么能成为漫画页”是领域规则。

ComicPlate 现在更偏教学清晰，所以让 Core 知道支持格式是合理的。

## 11. 配置文件

当前文档里已经决定一些配置项应该进入 `settings.json`：

- 默认阅读方向：`RightToLeft`。
- 默认缩放：`AutoFit`。
- 最近打开数量：`20`。
- 是否自动恢复进度：`true`。

配置读取应该有默认值。

也就是说：

1. 用户配置存在且正常，使用用户配置。
2. 用户配置不存在，使用默认配置。
3. 用户配置损坏，使用默认配置，并保留/备份坏文件。

不要让坏配置直接把软件炸给用户看。

## 12. Action 和右键菜单

Action 是应用内部固定动作。

例如：

- `OpenFolder`
- `NextPage`
- `PreviousPage`
- `GoToPage`
- `SetReadingDirection`
- `FitToWindow`
- `RevealInFileManager`
- `CopyPath`

同一个 Action 可以从不同入口触发：

- 工具栏按钮。
- 快捷键。
- 右键菜单。
- 未来菜单栏。

这样做的好处是：

- 不同入口行为一致。
- 修一个动作不用到处改。
- 测试更容易。
- 未来加快捷键不会重写业务逻辑。

ComicPlate 不做 NeeView 那种可编辑、可复制、可脚本扩展的复杂命令系统。

## 13. PlatformService

PlatformService 是隔离系统差异的防火墙。

应该放进去的东西：

- 在 Windows Explorer 中显示文件。
- 在 macOS Finder 中显示文件。
- 打开外部 URL。
- 复制到剪贴板。
- 获取 AppData / Application Support 路径。
- 系统文件选择器。
- 平台快捷键差异。
- 系统主题检测。

不一定要放进去的东西：

- `Path.Combine`
- `Path.GetFileName`
- `Directory.EnumerateFiles`

原因：

这些是 .NET 跨平台 API，本身已经屏蔽了很多差异。

真正需要 PlatformService 的，是“不同系统行为不一样”的能力。

## 14. 当前和 NeeView 的差距

ComicPlate 现在能看漫画，但还不够顺滑。

主要差距：

- 书架还很简陋。
- 没有封面缩略图。
- 没有最近打开真正持久化。
- 没有阅读进度真正持久化。
- 没有双页模式。
- 没有成熟的页面动画/滑动体验。
- 没有全屏阅读体验。
- 没有鼠标滚轮和拖拽体验。
- 没有成熟的右键菜单。
- 没有设置页。
- 没有跨平台打包验证。
- ZIP/CBZ 读取刚刚跑通，还需要真实素材持续测试。
- 图片缩放刚从“小槽位”改成 AutoFit，还需要用真实漫画调阈值。

这些差距不代表架构失败。它们是 MVP 到可长期使用版本之间的正常距离。

## 15. 下一批最值得做的模块

建议顺序：

1. 真实阅读进度保存。
2. 最近打开列表。
3. 双页模式。
4. 书架首页缩略图。
5. 全屏模式。
6. 基础右键菜单。
7. Reveal in Explorer / Finder。
8. 设置文件真正接入 UI。
9. macOS 启动和打包验证。
10. 性能观察和内存上限调整。

其中最影响“像 NeeView”的是：

- 双页模式。
- 全屏模式。
- 横向阅读带手感。
- 缩放策略。
- 最近打开和进度恢复。
- 书架封面。

## 16. 开发工作流

当前最适合本项目的节奏是：

1. 先讨论模块的产品目标。
2. 把目标写进 docs。
3. 你审文档。
4. 提交一个备份。
5. Codex 开始实现。
6. 实现后跑 build/test/format。
7. 用真实漫画试。
8. 发现问题后继续迭代。
9. 每个阶段写复盘。

这个流程比“临时做一下看看”更适合这个项目。

因为你的目标不只是得到一个工具，也是在学习一个标准项目如何长出来。

## 17. 验证习惯

每次功能改动后，至少验证：

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes --no-restore
```

注意：

- `dotnet build` 和 `dotnet test` 不要随便并行跑。
- 它们可能同时写 `obj/bin`，导致文件锁冲突。
- 如果 ComicPlate.App 正在运行，也可能锁住输出 DLL，导致构建失败。

这不是代码错，而是开发现场的资源竞争。

## 18. 提交前检查

提交前建议看：

```powershell
git status --short
git diff --stat
```

如果需要看具体变更：

```powershell
git diff
```

提交时最好按阶段分：

- 文档更新一组。
- 书架递归一组。
- ZIP 修复一组。
- UI 缩放一组。

不要把大范围无关改动混在一起。

## 19. 你现在可以重点问 GPT 的问题

适合拿去学的问题：

- Avalonia 的 XAML 和 ViewModel 是怎么绑定的？
- MVVM 为什么能让 UI 和业务逻辑分开？
- 为什么文件夹和 ZIP 都可以抽象成 Book？
- 为什么 ZIP 里的图片不能总是像普通文件一样直接读？
- Stream、MemoryStream、FileStream、Zip entry stream 有什么区别？
- 什么是 Bitmap，为什么 Bitmap 要释放？
- 为什么阅读器不能一次性解码整本漫画？
- FitWindow、FitHeight、FitWidth、AutoFit 有什么区别？
- 为什么 25 寸屏幕和 14 寸屏幕必须动态计算阅读区？
- 右到左阅读方向会影响哪些地方？
- 什么是自然排序？
- 为什么配置文件需要默认值和版本号？
- PlatformService 为什么是隔离系统差异的防火墙？

这些问题比“帮我学 C#”更精准。

## 20. 一句话复盘

ComicPlate 现在已经从“工程架子”走到了“能看漫画的 MVP”。

接下来要做的，不是继续堆功能，而是把阅读体验从“能用”推进到“顺手”：

- 更像阅读器的缩放。
- 更像书架的入口。
- 更像书本的进度恢复。
- 更像 NeeView 的双页和全屏体验。

这就是下一阶段的主线。

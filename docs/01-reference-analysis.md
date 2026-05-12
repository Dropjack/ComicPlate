# Reference Analysis

本文分析 NeeView 的产品功能结构。重点是“功能目的”和“依赖关系”，不是照搬 NeeView 的 WPF 实现。

参考来源：

- `d:\Tools\NeeView\README.md`
- `d:\Tools\NeeView\docs\en-us\userguide.md`
- `d:\Tools\NeeView\NeeView\Book`
- `d:\Tools\NeeView\NeeView\Archiver`
- `d:\Tools\NeeView\NeeView\Page`
- `d:\Tools\NeeView\NeeView\ViewContents`
- `d:\Tools\NeeView\NeeView\MainView`
- `d:\Tools\NeeView\NeeView\Command`
- `d:\Tools\NeeView\NeeView\Setting`
- `d:\Tools\NeeView\NeeView\BookHistory`
- `d:\Tools\NeeView\NeeView\BookMemento`

## 0. 核心隐喻

NeeView 的核心隐喻非常清楚：

- 文件夹和压缩包都是 **Book**。
- 图片是 **Page**。
- 阅读器不是“打开一张图”，而是“打开一本书，并在书里翻页”。

ComicPlate 应该保留 Book/Page 隐喻，但不照搬 NeeView 的书架和管理心智。新的产品方向是：

- ComicPlate 是为本地漫画内容设计的跨平台漫画预览器/阅读窗口。
- 它记住用户读到哪，但不替用户管理藏书。
- 它不需要知道“什么是漫画作品”，只需要判断“当前打开的东西能不能被线性预览”。
- 它不识别作品，只识别 **可阅读单元**：用户选择的文件夹、压缩包或图片范围，都可以被临时解释成一本 Book。

因此，ComicPlate 的核心动作不是“扫描漫画库”，而是 **Open as Book**：把用户当前选择的路径作为一个可阅读单元打开，并生成线性 Page 流。

## 1. 阅读器核心

层级：核心。

功能目的：

- 把一个输入路径变成可翻阅的页面序列。
- 管理当前页、上一页、下一页、第一页、最后一页。
- 管理单页/双页模式、阅读方向、缩放适配、窗口变化后的重新布局。

NeeView 参考结构：

- `BookFactory`：从地址和设置创建 Book。
- `BookSourceFactory`：扫描条目，生成 Page 集合，排序，并构造 BookSource。
- `BookPageCollection`：页面集合。
- `PageMode`：单页和宽页/双页。
- `PageReadOrder`：从右到左、从左到右。
- `PageStretchMode`：不缩放、适配窗口、填充、按宽/高适配等。

依赖关系：

- 依赖 FileSource/ArchiveSource 提供条目。
- 依赖 ImageLoader 解码图片。
- 依赖 ReaderState 决定当前页、显示模式、阅读方向。
- 依赖 UI 层显示结果。

ComicPlate 取法：

- 保留 Book/Page 模型。
- 先用单页阅读打通技术闭环。
- 双页模式是第一批核心体验：单页闭环跑通后立刻实现。
%% P: 我接受先测试单页这件事情，但是我认为双页是我大部分时候阅读的习惯，所以我认为这很重要，至少要在第一批做出来 %%
- 复杂页面变换、旋转、滤镜、局部放大先不做。
%% P：我同意，因为面对windows台式机/笔记本和Mac/Macbook，所以我们不用考虑为Pad/Windows Pad用户来做这些复杂功能，也完全不需要触屏功能 %%

## 2. 文件系统

层级：核心输入。

功能目的：

- 打开文件夹。
- 扫描文件夹内图片。
- 把文件名排序成用户直觉中的阅读顺序。

NeeView 参考结构：

- `FolderArchive` 把文件夹也当作 Archive。
- `ArchiveEntryCollection` 统一处理文件夹、压缩包、播放列表等来源。
- `PageSortMode` 支持文件名、类型、时间、大小、注册顺序、随机等排序。
- 单元测试中存在 `NaturalComparer` 相关测试，说明自然排序对阅读体验重要。

依赖关系：

- 依赖路径访问权限。
- 依赖图片扩展名过滤。
- 依赖自然排序。
- 依赖错误处理：空文件夹、不可访问文件夹、损坏图片。

ComicPlate 取法：

- MVP 可以先只扫描当前文件夹，但递归文件夹要保留为核心模式之一。
%% P：我也可以接受V1没有递归，但是递归也是核心，我也想要保留 %%
- 文件名使用自然排序。
- 只收集图片，不把子文件夹当页面。
- 文件夹默认作为一个可阅读单元打开：递归收集其内部支持格式图片，并按相对路径自然排序。
- 如果文件夹里包含压缩包，后续压缩包支持完成后，它们也作为这个可阅读单元的一部分进入线性 Page 流；不自动拆成书架里的多本书。

%% Q：我似乎感觉是你认为递归容易有更多的错误，不容易守住边界，不过我觉得一个错误也是错误，一大堆错误也是错误，不如我们慢慢把边界聊清楚，然后慢慢推进？%%

## 3. 压缩包

层级：核心输入扩展。

功能目的：

- 把 ZIP/CBZ 当作 Book。
- 忽略压缩包里的非图片文件。
- 在不解压到用户目录的前提下读取图片流。

NeeView 参考结构：

- `ArchiveManager` 按类型选择 Folder、Zip、SevenZip、Pdf、Media、Susie、Playlist。
- `ZipArchive` 和 `ZipArchiveExtractor` 处理 ZIP。
- `SevenZipArchive` 支持 rar/7z 等。
- `ArchiveEntryExtractorService` 支持嵌套压缩包时的临时提取。

依赖关系：

- 依赖条目排序。
- 依赖图片解码。
- 依赖缓存或流生命周期管理。
- 复杂格式会引入原生库、临时文件、密码、编码等问题。

ComicPlate 取法：

- ZIP/CBZ 是 MVP 后半段必须做。
- RAR/CBR/7z/CB7 是漫画预览器方向的重要格式扩展，放到 V1 或 V2。
- 嵌套压缩包、PDF、视频、Susie 插件永久不进入早期范围。

##YES：对，我接受这里的所有操作，非漫画功能都不是我要的，如果要看PDF，EPUB我也不需要做这么个工具，我们要做的就是文件夹内图片和压缩包图片这两个，rar和7z不再第一批我也同意，手头的所有漫画都是cbz/cbr和zip的##

## 4. 图片显示

层级：核心体验。

功能目的：

- 解码图片。
- 按窗口大小适配显示。
- 支持平滑缩放。
- 翻页时释放不再需要的图片，避免内存持续增长。

NeeView 参考结构：

- `PageContentFactory` 根据条目创建不同 PageContent。
- `BitmapPageContent`、`FilePageContent`、`ArchivePageContent`、`SvgPageContent` 等区分来源和类型。
- `ViewContent` 和 `ViewSources` 把 Page 数据变成 UI 可显示内容。
- `MainView` 处理主显示区域、鼠标拖拽、复制、放大镜、窗口等。

依赖关系：

- 依赖 ReaderState 给出当前页或双页组合。
- 依赖 ImageLoader。
- 依赖缓存策略。
- 依赖 UI 的布局尺寸变化。

ComicPlate 取法：

- MVP 只支持静态图片：jpg/jpeg/png/webp/bmp/gif 第一帧。
- 动图播放、视频、PDF、SVG 先不做。
- 默认使用“完整适配窗口”，保留原始比例。
%% Q：我希望你解释一下，这个完整适配窗口的其他情况，会不会有别的？然后第一帧我也不太懂是啥意思，不过做内存释放应该是必须的吧？功能不全都行，但是性能管理必须从一开始就要考虑到？%%

%% YES：对，我赞成不做任何动画相关 %%

## 5. 书本模式

层级：体验核心。

功能目的：

- 用户感知到自己在读一本书，而不是一堆散图。
- 进度、历史、双页、阅读方向都围绕 Book 展开。

NeeView 参考结构：

- `Book`、`BookMemento`、`BookMementoCollection`。
- `BookPageCollectMode` 支持只收图片、图片和书、全部。
- `PageReadOrder` 处理左右阅读方向。
- `PageMode` 处理单页/宽页。

%% Q：读到这里的第一个问题就是，`Book`、`BookMemento`、`BookMementoCollection`这些名字是NeeView作者自己起的函数名字？还是他起的模块的名字？还是你起的名iz？%%

依赖关系：

- 依赖路径作为 Book 身份。
- 依赖页面序列稳定。
- 依赖持久化保存进度。

ComicPlate 取法：

- 用户打开的文件夹、压缩包，或后续支持的图片范围，都会被解释为一个临时 Book。
- 先用规范化绝对路径作为 Book ID，用于在 ComicPlate 自己的外部数据文件中保存阅读状态。
- 内容 hash 暂不做，避免拖慢 MVP。

%% Q：为什么需要Book ID？我们的项目没有内存有关，所有文本都是只读的，就算我换一个路径，下次从头读不就行了？还有什么其他的需要注册ID，需要内容hash的地方吗？%%

## 6. 历史记录和阅读进度

层级：轻量必需体验。

功能目的：

- 下次打开同一本书时回到上次页。
- 空阅读面板显示最近打开。
- 让阅读器记得阅读上下文，但不替用户管理藏书。

NeeView 参考结构：

- `BookHistory` 保存路径、最后访问时间、页信息和属性。
- `BookMementoCollection` 以路径为 key 保存 Book 状态。
- `BookmarkCollection` 是更重的长期收藏能力。

依赖关系：

- 依赖 Book ID。
- 依赖配置/数据库持久化。
- 依赖路径不存在时的降级策略。

ComicPlate 取法：

- MVP 第二阶段加入最近打开和上次页。
- 先不做书签、标签、搜索历史、跨路径迁移。
- 对个人使用来说，自动恢复每本书的上次阅读页比书签更重要。

%% P：我认为需要先做个书签？还是说这个和book ID，哈希是一套体系的？不过说实话，我从来没用过书签，每一本书如果都能保持上一次关闭的页面，那就没必要有书签，至少MVP不需要，因为我用NeeView一年多了，也没用过这个功能 %%

## 7. 快捷键和输入

层级：核心操作。

功能目的：

- 让翻页、全屏、适配、打开文件夹等操作不依赖鼠标。
- 左右键映射要符合阅读方向。

NeeView 参考结构：

- `CommandTable` 管理命令表。
- `ShortcutKey`、`TouchGesture`、`MouseGesture` 支持复杂输入。
- 设置中可以编辑命令、快捷键、手势和参数。

依赖关系：

- 依赖命令系统。
- 依赖 ReaderState。
- 依赖平台快捷键差异。

ComicPlate 取法：

- 不做可编辑命令系统。
- 只做固定快捷键表。
- 左右键和阅读方向的关系写死到 Behavior Spec，后续再配置化。

%% YES：我接受这种方法，反正是给我用的，咱们俩到时候单独开一个README来讨论快捷键应该有什么，和后续拓展的方向 %%

## 8. 设置

层级：支持功能。

功能目的：

- 保存用户偏好。
- 控制阅读方向、缩放、双页、最近打开数量等。

NeeView 参考结构：

- `SettingWindowModel` 页面包括 General、FileTypes、Book、Window、MainView、Panels、Slider、Command。
- 搜索设置项。
- 大量配置项与命令系统、面板系统、文件类型系统联动。

依赖关系：

- 依赖配置文件。
- 依赖 UI 设置页。
- 依赖运行时状态刷新。

ComicPlate 取法：

- MVP 只做配置文件默认值，不做完整设置窗口。
- V1 做基础设置页。
- 设置项必须少：阅读方向、默认缩放、双页开关、最近打开数量。


%% Q：这个部分核心应该没你写的这么复杂，不过这其实就是每一个约束的硬软选择对吧？一旦某个约束是用户个性化的，就要留一个窗口。这个应该是讨论完所有的约束后，自然就能成形的部分？ 不过这个部分我打算全权交给你，到时候我在测试发现问题%%

## 9. UI 面板

层级：强体验但高复杂度。

功能目的：

- 展示当前可阅读单元的页面列表、最近打开、信息、导航、效果、书签、播放列表等。

NeeView 参考结构：

- `SidePanels`。
- 用户指南列出 Bookshelf、PageList、History、Information、Navigator、Effect、Bookmark、Playlist。
- 面板可停靠、浮动、拖拽组合。

依赖关系：

- 依赖复杂布局系统。
- 依赖命令系统。
- 依赖每个业务模块的数据。

ComicPlate 取法：

- MVP 要有基础页面列表侧栏，用来承载当前 Book/Page 的核心阅读结构。
- 不做书架侧栏；最近打开只作为打开历史，不作为漫画库管理入口。
- 右侧 metadata 编辑、复杂信息面板、效果面板不做。
- 底部缩略图可以晚于 MVP，但属于后续重要体验。
- 浮窗可以在 MVP 稳定后评估。
- 拖拽组合和复杂停靠可以永久不支持。

%% NO：这部分不行，这部分必须要做到和NeeView的基础一样，浮窗侧栏，我们可以把右面的编辑metadata的功能先不要，但是目录侧栏，必须要MVP就有，下面的缩略图可以MVP没有，但是之后也要做，我应该说：侧栏必须一上来就有，浮窗可以在测试完MVP后立刻开始着手做，拖拽组合和停靠可以永远不支持。我们做一个完成度够，但是简单漂亮的主界面。%%

## 10. 命令系统

层级：NeeView 的扩展核心，但 ComicPlate 的范围风险。

功能目的：

- 统一所有操作。
- 让快捷键、菜单、右键菜单、脚本都调用同一套命令。
- 支持命令参数、复制命令、编辑命令。

NeeView 参考结构：

- `CommandElement`、`CommandTable`、`RoutedCommandTable`。
- 设置页中有命令列表、快捷键编辑、鼠标手势、触控手势、参数编辑、冲突解决。

依赖关系：

- 依赖几乎所有业务模块。
- 依赖设置系统。
- 依赖脚本系统。

ComicPlate 取法：

- 第一年至少不做复杂命令系统。
- 只用应用内部的简单 action 枚举或方法。
- 后续若需要快捷键自定义，也只做“快捷键映射表”，不做 NeeView 式可复制命令。
- 不同区域的右键菜单需要单独写清楚，但它们只调用固定 Action，不允许用户自定义菜单或脚本扩展。

%% Q：需要你解释这部分，这部分我真的完全不懂 %%

## 11. 插件和脚本

层级：扩展生态。

功能目的：

- 用脚本扩展命令。
- 用 Susie 插件扩展图像和压缩格式。

NeeView 参考结构：

- `Script`。
- `SampleScripts`。
- `NeeView.Susie` 和 `NeeView.Susie.Server`。

依赖关系：

- 依赖命令系统。
- 依赖安全边界。
- 依赖插件生命周期和错误隔离。

ComicPlate 取法：

- 永久不做，至少第一年不碰。
- 这是很容易吞掉项目的功能。

%% YES：同意%%

## 12. 主题

层级：视觉定制。

功能目的：

- 定制窗口和控件外观。

NeeView 参考结构：

- 用户指南有 Theme。
- 设置页中 Window 相关配置。
- docs 中有主题格式说明。

依赖关系：

- 依赖样式系统。
- 依赖控件状态和视觉资源。

ComicPlate 取法：

- 不做用户可编辑主题。
- 只做跟随系统浅色/深色。
- macOS 和 Windows 视觉可以分平台微调，但不是第一版阻塞项。

%% Mac部分是26版本的，Windows就用Windows 11风格的 %%

## 13. 导出和工具类

层级：高级工具。

功能目的：

- 导出图片、打印、外部应用打开、文件管理、重命名、播放列表等。

NeeView 参考结构：

- `ExportImage`。
- `Print`。
- `External`。
- `Playlist`。
- `RenameControl`。

依赖关系：

- 依赖文件写入。
- 依赖当前页状态。
- 依赖 UI 对话框。

ComicPlate 取法：

- MVP 不做。
- V1 只考虑“在 Finder/Explorer 中显示”和“复制图片路径”。
- 导出、打印、批量文件管理长期不做。

%% YES：支持 %%

## ComicPlate 要保留的 NeeView 体验

最应该保留：

- Book/Page 的阅读心智。
- 打开文件夹或 CBZ 后立刻进入阅读。
- 把用户选择的路径作为一个可阅读单元线性预览。
- 自然排序。
- 左右键翻页。
- 阅读方向影响翻页。
- 简洁的全屏阅读。
- 进度恢复。
- 基础页面列表侧栏。
- 双页模式，尤其是日漫从右到左。

最应该避开的坑：

- 可编辑命令系统。
- 插件/脚本。
- 可停靠/拖拽组合式复杂面板。
- 多媒体/PDF/SVG/动图播放。
- 文件管理器级别的书架或漫画库管理。
- 第一版就做设置系统和主题系统。

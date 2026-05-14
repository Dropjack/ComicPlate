# ComicPlate To-Learn 本地学习资料页

> 目的：这不是系统学习 Avalonia / C# / Git 的教材，而是为了让你能监督 Codex、判断架构有没有写歪、知道每个阶段该检查什么。
>
> 项目定位：ComicPlate 是一个 macOS / Windows 双平台、只读、轻量漫画/图片阅读器。NeeView 是参考对象，不是复刻对象。

---

# 0. 使用方法

* 这份文档应该作为本地资料页保存。
* 真正执行前，先看对应章节的“检查点”。
* 不要求你手写所有代码。
* 你需要做到：

  * 看懂 Codex 在做什么。
  * 能判断它有没有把逻辑放错层。
  * 能发现危险路线。
  * 能把不确定点转成文档里的设计决策。

## 当前最重要的学习策略

* 不要按“大课”学习。
* 不要先学完整 C#、完整 Avalonia、完整 MVVM。
* 按项目问题学习：

  * 这个功能属于 UI、状态、业务规则、平台能力，还是外部库 adapter？
  * 这个行为会不会修改用户文件？
  * 这个任务会不会卡 UI？
  * 这个资源是不是需要释放？
  * 这个平台差异是不是应该进 PlatformService？
* Codex 负责生成和枚举。
* 你负责：

  * 砍范围。
  * 定边界。
  * 检查 diff。
  * 确认代码没有偏离“只读阅读器”。

---

# 1. 总体心智模型：桌面 App 是怎么运行的

## 你需要知道

* 桌面 App 运行时不是“静态图片”。
* UI 是运行时创建出来的对象。
* `.axaml` 之类的 UI 文件更像“图纸”。
* 程序启动后，框架根据图纸创建窗口、按钮、列表、图片区域。
* 用户操作会触发命令。
* 命令更新状态。
* 状态变化后，UI 刷新。

## 类比

* 游戏画面：

  * 场景对象 + 摄像机 + 灯光 + UI 状态 → 每帧渲染。
* 桌面 UI：

  * 窗口对象 + 控件 + ViewModel 状态 → 显示当前界面。

## 核心公式

* 资源：图片、字体、样式、XAML、图标。
* 状态：当前书、当前页、阅读方向、单双页、加载状态。
* 逻辑：用户点击后发生什么。
* 渲染：把当前状态画出来。

## 检查点

* Codex 是否把“界面显示”和“业务逻辑”混在一起？
* 是否把按钮点击后的一堆文件扫描逻辑直接塞进按钮事件？
* 是否有明确的状态：Loading、Empty、Reading、Error？

---

# 2. Avalonia 是什么

## 一句话

* Avalonia 是 C# / .NET 生态里的跨平台桌面 UI 框架。
* 它用来写 Windows、macOS、Linux 桌面应用。
* 它和 WPF 思路接近，但能跨平台。

## 它不是

* 不是像 Wwise 那样任何语言都能接入的中间件。
* 不是一个独立外部程序去读取你的主程序。
* 不是 macOS 或 Windows 的完全原生 UI。

## 它是

* 你 App 内部引用的一套 UI 库 / 运行时。
* 负责：

  * 创建窗口。
  * 绘制按钮、列表、图片区域。
  * 接收鼠标键盘。
  * 管理布局。
  * 处理数据绑定。
  * 支持主题。

## ComicPlate 中的关系

* ComicPlate 本身是一个 Avalonia App。
* Avalonia 负责显示和交互。
* ComicPlate 自己的代码负责：

  * 扫描文件夹。
  * 读取 ZIP/CBZ。
  * 排序页面。
  * 解码图片。
  * 保存进度。
  * 管理阅读状态。

## 够用标准

* 你能说清楚：Avalonia 负责 UI，不负责定义“什么是一本漫画”。
* 你能说清楚：ComicPlate 的业务逻辑不应该依赖按钮控件本身。

---

# 3. Avalonia 项目结构

## 最小结构

* `Program.cs`

  * 程序入口。
  * 启动 Avalonia。
  * 正常功能开发不应该频繁改这里。

* `App.axaml`

  * 应用级资源。
  * 主题、样式、全局资源。
  * 不写业务逻辑。

* `MainWindow.axaml`

  * 主窗口 UI。
  * 按钮、列表、布局、图片区域。

* `MainWindow.axaml.cs`

  * 主窗口背后的初始化代码。
  * 可以挂 DataContext。
  * 不应该塞业务逻辑。

* `ViewModel`

  * 保存界面状态。
  * 暴露命令。
  * 调用 Service。

## ComicPlate 推荐结构

* `Views/`

  * `MainWindow.axaml`
  * `ReaderView.axaml`
  * `StartView.axaml`

* `ViewModels/`

  * `MainWindowViewModel.cs`
  * `ReaderViewModel.cs`
  * `StartViewModel.cs`

* `Domain/`

  * `Book.cs`
  * `Page.cs`
  * `ReadingDirection.cs`
  * `ViewMode.cs`
  * `PageSpread.cs`

* `Application/`

  * `OpenBookUseCase.cs`
  * `NavigatePageUseCase.cs`
  * `RestoreProgressUseCase.cs`

* `Infrastructure/`

  * `FolderBookSource.cs`
  * `ZipBookSource.cs`
  * `ImageLoader.cs`
  * `JsonSettingsStore.cs`
  * `JsonProgressStore.cs`

* `Platform/`

  * `IPlatformService.cs`
  * `WindowsPlatformService.cs`
  * `MacPlatformService.cs`

## 检查点

* 按钮在哪个文件？

  * 应该在 `.axaml`。
* 按钮点击后调用谁？

  * 应该绑定 ViewModel 的 Command。
* 文件扫描写在哪？

  * 不应该在按钮事件里。
  * 应该在 `FolderBookSource` / `BookLoader` / Infrastructure。

---

# 4. MVVM 最小用法

## 一句话

* View 负责显示。
* ViewModel 负责状态和命令。
* Model / Domain / Service 负责数据和业务。

## ComicPlate 对应

* View：

  * 启动页。
  * 阅读页。
  * 目录侧栏。
  * 工具栏。
  * 设置页。

* ViewModel：

  * 当前打开的 Book。
  * 当前页 index。
  * 总页数。
  * 是否双页。
  * 阅读方向。
  * 当前缩放模式。
  * `OpenBookCommand`。
  * `NextPageCommand`。
  * `ToggleDoublePageCommand`。

* Model / Domain：

  * `Book`。
  * `Page`。
  * `PageSpread`。
  * `ReadingDirection`。
  * `FitMode`。

* Service / Infrastructure：

  * 扫描文件夹。
  * 读取 ZIP。
  * 图片解码。
  * JSON 保存。
  * 平台相关行为。

## 和你以前 Python 小工具的对应

* Python `core/` ≈ ComicPlate `Domain + Application + Infrastructure`。
* Python `gui/` ≈ Avalonia `Views + ViewModels`。
* Python `main.py` ≈ `Program.cs + App.axaml`。
* Python `config/` ≈ `JsonSettingsStore / JsonProgressStore`。

## 关键修正：Core 不是杂物箱

* 不要把所有“看起来核心”的东西都扔进 `Core`。
* 要区分：

  * `Domain`：ComicPlate 自己的阅读规则。
  * `Application`：用户操作流程。
  * `Infrastructure / Adapter`：外部库、文件系统、ZIP、JSON、图片库的包装。

## 检查点

* 扫描文件夹的代码不应该写在按钮控件里。
* ViewModel 可以协调流程，但不应该塞满所有业务细节。
* Domain 不应该知道 Avalonia。
* Infrastructure 可以知道外部库。

---

# 5. 产品角色：你现在同时是三个人

## 产品经理

* 决定做什么、不做什么。
* 控制范围。
* 守住 MVP。

## UX/UI 设计

* 决定用户怎么操作。
* 决定按钮、入口、菜单、状态反馈。
* 不一定需要高保真 Figma。

## 工程师

* 用 Avalonia / C# 把它实现出来。
* 管 Git、分支、打包、平台差异。

## Figma 是否必须

* 不必须。
* 当前阶段建议：

  * 文字 UI Spec。
  * 低保真线框。
  * 可运行原型。
* 高保真 Figma 可以以后再做。

## ComicPlate UI 第一阶段策略

* 先用框架默认主题。
* 图片区域永远是主角。
* 不追求第一版 macOS 26 / Win11 视觉完全到位。
* 先做阅读体验，再做视觉 polish。

---

# 6. Git 分支与仓库策略

## 当前结论

* 单仓库。
* 多分支。
* 不拆 repo。
* 不用 submodule。
* 不用 sparse checkout。

## 为什么不分仓库

* PlatformServices 是 ComicPlate 的一部分，不是独立产品。
* Windows 代码在 Mac 仓库里不碍事。
* Mac 代码在 Windows 仓库里不碍事。
* 代码体积很小。
* 真正占空间的是大素材、打包产物、缓存，不是几百行代码。

## 什么时候才分仓库

只有这些情况才考虑：

* 模块被多个项目复用。
* 模块有独立版本号和发布节奏。
* 模块由不同团队维护。
* 模块体积巨大，不想每次都拉。
* 模块有不同权限，不能公开。
* 模块是第三方库 fork，需要独立维护。

## 分支规则

* `main`

  * 稳定主线。
  * 必须保持可构建。

* `feature/*`

  * 新功能。
  * 例如 `feature/open-folder-reader`。

* `fix/*`

  * 修 bug。

* `docs/*`

  * 文档修改。

* `spike/*`

  * 试验分支。
  * 允许失败。
  * 失败可删除。

## 最小工作流

* 开始新任务：

  * 确认 `main` 干净。
  * 创建 `feature/task-name`。
* Codex 改代码。
* 你检查：

  * `git diff`。
  * build 是否通过。
  * 行为是否符合文档。
* 提交。
* 合回 `main`。

## 检查点

* 不要在 `main` 上让 Codex 大改。
* 一个 feature 分支可以有多个 commit。
* Branch 是隔离线。
* Commit 是存档点。
* 写坏了，删 branch 回 main。

## `.gitignore` 基本原则

* 进 Git：

  * 代码。
  * 文档。
  * 少量图标。
  * 少量测试资源。

* 不进 Git：

  * `bin/`
  * `obj/`
  * `.vs/`
  * `publish/`
  * `dist/`
  * `artifacts/`
  * `.dmg`
  * `.pkg`
  * `.msi`
  * 大漫画文件。
  * 大缓存。

---

# 7. 文件 IO

## 一句话

* 文件 IO 不是单纯“路径管理”。
* 它包括：路径规则、访问动作、失败边界、业务解释。

## ComicPlate 的文件 IO 链路

* 用户打开文件夹。
* 判断路径是否存在。
* 枚举文件。
* 筛选图片扩展名。
* 排序。
* 生成 Page 列表。
* 组成 Book。

## 绝对路径和相对路径

* 绝对路径：

  * Windows：`D:\Comics\BookA\001.jpg`
  * macOS：`/Users/name/Comics/BookA/001.jpg`

* 相对路径：

  * `001.jpg`
  * `chapter1/003.jpg`

## Wwise 类比

* Wwise SoundBank 路径需要手动设置。
* 本质是在告诉系统：从哪个根目录开始找资源。
* ComicPlate 打开文件夹时：

  * 文件夹就是 `BookRoot`。
  * 页是 Root 内部的相对路径。

## 权限失败为什么属于 IO

* 路径正确，不代表能访问。
* 可能失败的情况：

  * 没权限。
  * 文件被占用。
  * 移动硬盘断开。
  * NAS 掉线。
  * 子目录无权限。
  * 图片损坏。
  * 写配置失败。

## 业务解释

* 空文件夹：不是崩溃，是空状态。
* 图片损坏：显示错误页，允许继续翻。
* 某个子目录无权限：不能让整个软件炸。
* 历史路径不存在：提示路径不存在，不自动删除记录。

## 检查点

* 文件 IO 是否集中在 Loader / Source / Infrastructure？
* 是否区分文件夹和文件？
* 是否处理路径不存在？
* 是否处理权限失败？
* 是否把文件夹安全转成 Book / Page 数据？

---

# 8. 自然排序

## 一句话

* 普通字典序会把 `10.jpg` 排在 `2.jpg` 前面。
* 自然排序会把文件名里的数字按数字值比较。

## 你不需要做什么

* 不需要自己写算法。
* 不需要深入正则或比较器实现。

## 你需要做什么

* 要求 Codex 实现自然排序。
* 要求它写测试。
* 用测试数据验收。

## 必测样本

* `1.jpg, 2.jpg, 10.jpg`
* `001.jpg, 002.jpg, 010.jpg`
* `page1.jpg, page2.jpg, page10.jpg`
* `第1页.jpg, 第2页.jpg, 第10页.jpg`
* `chapter1/page2.jpg, chapter1/page10.jpg, chapter2/page1.jpg`

## 检查点

* 如果 `10.jpg` 在 `2.jpg` 前面，就是错。
* 子目录递归时，要按相对路径自然排序。
* ZIP 条目要按压缩包内逻辑路径自然排序。

---

# 9. 只读原则

## 一句话

* ComicPlate 是阅读器，不是文件管理器。

## 允许写入

* ComicPlate 自己的设置。
* ComicPlate 自己的阅读进度。
* ComicPlate 自己的最近打开记录。
* ComicPlate 自己的日志。
* ComicPlate 自己的缓存。

## 禁止写入

* 删除用户图片。
* 移动用户图片。
* 重命名用户图片。
* 修改压缩包。
* 覆盖原图。
* 写回 metadata。
* 在漫画源目录写 `.json`、`.cache`、隐藏配置文件。

## 菜单语义

* “从最近打开中移除”

  * 只删除 ComicPlate 记录。
  * 不删除磁盘文件。

* “清除阅读进度”

  * 只删除 ComicPlate 保存的进度。
  * 不删除漫画文件。

* “清除缓存”

  * 只删除 ComicPlate 生成的缓存。
  * 不删除原图。

## 检查点

* 任何菜单项是否可能修改用户源文件？
* 是否出现删除、移动、重命名原文件？
* 是否向漫画目录写入配置？
* 是否把“移除记录”和“删除文件”混淆？

---

# 10. 图片解码、内存释放、句柄

## 一句话

* 扫描整本书可以。
* 解码整本书不行。

## 文件大小不等于内存大小

* JPG / PNG / WebP 是压缩文件。
* 显示前要解码成 Bitmap。
* 一张 4000×6000 图片，RGBA 约 96MB。
* 一个 3MB JPG 解码后可能接近 100MB。

## Wwise 类比

* Bank / Event / metadata 可以轻量存在。
* 真正的 media、streaming buffer、voice 占内存。
* 不能把所有可能播放的音频都提前解码进内存。
* ComicPlate 也不能把整本书所有图片都提前解码成 Bitmap。

## Page 和 Bitmap 的区别

* `Page`

  * 页面信息。
  * 路径、index、来源。
  * 很轻。
  * 可以整本保留。

* `Bitmap`

  * 解码后的图像像素。
  * 很重。
  * 只能按需加载。

## 合理策略

* 当前页加载。
* 双页模式加载当前两页。
* 可预加载前后少量页面。
* 缓存必须有上限。
* 切换书本必须释放旧缓存。
* 文件流必须及时关闭。

## 句柄是什么

* 句柄是系统或库给你的资源管理引用。
* 它不是资源本身。
* 它用于之后读、写、停止、关闭、释放。

## 常见句柄 / 类句柄

* 文件句柄。
* 窗口句柄。
* 网络 socket。
* 图片 / GPU 资源。
* Wwise `PlayingID`。

## Wwise PlayingID 类比

* `UAkAudioEvent`：事件定义。
* `PostEvent`：开始播放一次。
* `AkPlayingID`：这次播放实例的管理引用。
* 它可用于停止、淡出、回调、追踪。

## ComicPlate 中的句柄问题

* 主要是资源生命周期。
* 文件流打开后要关闭。
* Bitmap 不用后要释放。
* ZIP 条目流要关闭。
* 预加载任务切书后要取消。

## 检查点

* 是否出现 `List<Bitmap>` 保存整本书？危险。
* 是否打开 FileStream 后没有释放？危险。
* Bitmap 缓存是否无限增长？危险。
* 切换书本后旧缓存是否清理？
* ImageLoader 是否负责流释放？
* ImageCache 是否负责 Bitmap 生命周期？

---

# 11. JSON 配置

## 一句话

* JSON 是人和程序都容易读写的结构化文本格式。
* 适合保存轻量设置、历史、进度。

## ComicPlate 可以用 JSON 保存

* `settings.json`

  * 阅读方向。
  * 默认缩放。
  * 主题。
  * 最近打开数量。

* `library.json` / `progress.json`

  * 最近打开。
  * 每本书读到哪一页。
  * 最后访问时间。

## JSON 基本规则

* 对象：`{}`
* 数组：`[]`
* 字段名用双引号。
* 字符串用双引号。
* 数字和布尔值不加引号。
* 最后一个字段后面不能多逗号。

## 为什么需要 version

* 未来配置结构会变。
* 旧配置需要迁移。
* 没有版本号，程序分不清：

  * 旧格式。
  * 新格式。
  * 损坏文件。

## 读失败策略

配置文件是外部输入，不可信。

必须处理：

* 文件不存在。
* JSON 格式损坏。
* 字段缺失。
* 字段类型错误。
* 字段值业务非法。
* 版本过旧。
* 版本比当前程序还新。

## 默认值策略

* 读不到文件：用默认配置。
* 文件损坏：用默认配置，并备份坏文件。
* 字段缺失：该字段用默认值。
* 字段非法：纠正到安全范围。

## 写入策略

* 写到 ComicPlate 应用数据目录。
* 不写到漫画源目录。
* 尽量先写临时文件，再替换原文件。
* 避免写到一半崩溃导致配置损坏。

## 检查点

* JSON 是否有 `version`？
* 读失败是否会导致应用启动失败？不应该。
* 是否备份损坏配置？
* 是否有默认值？
* 是否校验非法值，例如 preload 数量不能是 -999？

---

# 12. C# async / await

## 一句话

* `async/await` 是为了让耗时任务等待期间不堵住 UI 线程。

## UI 线程

* 窗口刷新、按钮响应、鼠标键盘、界面绘制都依赖 UI 线程。
* UI 线程被长任务占住，就会：

  * 窗口拖不动。
  * 按钮没反应。
  * 进度条不动。
  * 白屏。
  * 未响应。

## 哪些任务可能耗时

* 扫描大文件夹。
* 打开大 ZIP/CBZ。
* 读取移动硬盘 / NAS。
* 解码超大图片。
* 生成缩略图。
* 写入大量缓存。

## await 的真实含义

* 不是跳过后续代码。
* 是：

  * 当前流程等结果。
  * 但 UI 线程不傻等。
  * UI 可以继续刷新和响应。
  * 任务完成后再回到后续代码。

## Wwise 类比

* 同步 Load Bank：可能卡游戏。
* 异步 Load Bank：发起请求，游戏继续跑，完成后回调。
* `await` 是把回调式流程写得像顺序代码。

## CancellationToken

* 用于取消旧任务。

典型情况：

* 用户打开 A.zip。
* A 还没加载完。
* 用户又打开 B.zip。
* A 的旧任务必须取消或失效。
* 否则 A 后完成可能覆盖 B 的界面状态。

## 检查点

* 打开大文件夹是否会卡 UI？
* 图片解码是否在 UI 线程硬跑？
* 快速切书时旧任务是否取消？
* 快速翻页时旧图片加载是否可能污染新页面？
* 关闭书本后预加载是否停止？

---

# 13. ZIP / CBZ 读取

## 一句话

* CBZ 通常就是 ZIP。
* ZIP 不是文件夹的完美等价物。
* 读取时必须防御。

## ZIP 内可能有什么

* 图片。
* 目录条目。
* `.txt`。
* `.DS_Store`。
* `Thumbs.db`。
* `__MACOSX/`。
* 子目录。
* 损坏条目。
* 加密条目。

## 必须处理

* 非图片忽略。
* 目录条目跳过。
* 图片自然排序。
* 子目录内图片可读取。
* 打不开 ZIP 时显示错误，不崩溃。
* 加密 ZIP 显示“不支持”。
* 条目 stream 用完释放。
* 不一次性解码所有图片。

## MVP 行为建议

* ZIP 内所有图片视为一本书。
* 按完整条目路径自然排序。
* 路径分隔符统一成 `/` 后排序。

## 检查点

* 是否忽略非图片？
* 是否跳过目录条目？
* 是否自然排序？
* 是否处理子目录？
* 是否处理损坏 / 加密 ZIP？
* 是否释放条目流？

---

# 14. 简单 Action

## 一句话

* Action 是 ComicPlate 内部固定操作。
* 快捷键、工具栏、右键菜单只是不同入口。
* 它们应该调用同一个 Action。

## 为什么需要 Action

同一个动作不应该写三份：

* 键盘右方向键一份。
* 工具栏按钮一份。
* 右键菜单一份。

它们应该都调用：

* `NextPageCommand`
* `ToggleDoublePageCommand`
* `FitToWindowCommand`

## ComicPlate MVP Action 示例

* `OpenContent`
* `OpenArchive`
* `NextPage`
* `PreviousPage`
* `GoToFirstPage`
* `GoToLastPage`
* `GoToPage`
* `ToggleDoublePage`
* `SetReadingDirection`
* `FitToWindow`
* `RevealInFileManager`
* `CopyPath`
* `RemoveFromRecent`

## 不做 NeeView 复杂命令系统

不做：

* 用户编辑命令。
* 复制命令。
* 命令参数编辑。
* 脚本命令。
* 用户自定义菜单。

## UX 判断

* 右键菜单不是必须很多。
* NeeView 主阅读区也不一定强调右键。
* 右键更适合对象操作：

  * 最近打开项。
  * 目录侧栏项。
  * 路径 / 地址栏。

## MVP 右键建议

* 主阅读区可以极少，甚至先不做复杂菜单。
* 目录侧栏：

  * 跳转。
  * 在 Finder/Explorer 中显示。
  * 复制路径。
* 最近打开：

  * 打开。
  * 在 Finder/Explorer 中显示。
  * 复制路径。
  * 从最近打开中移除。

## 检查点

* 右键菜单项背后调用哪个 Action？
* 快捷键和菜单是否调用同一逻辑？
* 是否出现重复实现？
* 是否有修改用户文件的危险项？

---

# 15. Platform Services

## 一句话

* 平台差异必须集中封装。
* 不要让 Windows/macOS 判断散落在业务代码里。

## 为什么需要

macOS 和 Windows 在这些地方不同：

* 路径显示习惯。
* Finder / Explorer。
* AppData / Application Support。
* Ctrl / Command。
* 系统菜单栏。
* 文件关联。
* 全屏行为。
* 打包方式。

## 代码管理原则

* 单仓库。
* 代码层分平台。
* 不拆 repo。

## PlatformService 负责

* 获取应用数据目录。
* 在 Finder/Explorer 中显示文件。
* 打开外部 URL。
* 系统文件选择器。
* 剪贴板。
* 平台主修饰键：Ctrl / Command。
* 文件关联。
* 平台菜单。

## 不一定需要进 PlatformService 的东西

* 普通 `Path.Combine`。
* `Path.GetFileName`。
* `Path.GetExtension`。
* `Directory.EnumerateFiles`。
* `File.Exists`。

这些 .NET API 本身已跨平台。

## 原则

* Domain 不知道平台。
* Application 尽量不判断平台。
* Infrastructure 可以处理文件系统。
* Platform 层处理系统行为。

## 检查点

* 是否在 ViewModel 里到处 `OperatingSystem.IsWindows()`？危险。
* “在 Finder/Explorer 中显示”是否放进 PlatformService？应该。
* 获取 AppData / Application Support 是否统一封装？应该。
* Ctrl / Command 是否统一处理？应该。

---

# 16. Windows publish 与 macOS app bundle

## 你可以晚点学，但不能完全不懂

* Codex 可以帮你写命令。
* 你必须能验收产物。

## Windows publish

你需要知道：

* Debug 运行和 Release 打包不同。
* `dotnet run` 是开发运行。
* `dotnet publish` 是生成发布产物。
* 单文件发布会影响体积。
* 自包含发布更大，但目标机器不一定需要装 .NET。

你只需要会确认：

* 输出目录在哪里。
* `.exe` 是否能双击启动。
* publish 产物没有提交进 Git。

## macOS app bundle

你需要知道：

* macOS 应用通常是 `.app` bundle。
* Debug 跑起来不等于有标准 `.app`。
* 签名、公证、DMG 是后续问题。
* MVP 只要求在 macOS 上能启动打包产物。

## 检查点

* Windows 能否打 Release 包并启动？
* macOS 能否打包并启动？
* 输出是否在 `bin/Release/.../publish` 或指定 `artifacts/`？
* 打包产物是否被 `.gitignore` 排除？

---

# 17. Scope Cut：真正执行前必须再砍一次

## 当前已接受的核心方向

* ComicPlate 是只读漫画阅读器。
* 不是漫画文件管理器。
* NeeView 是参考，不是复制对象。
* Book/Page 心智必须保留。
* 双页是第一批核心体验。
* 阅读方向影响双页和左右键。
* 基础目录侧栏进入 MVP。
* 书签不做。
* 复杂命令系统不做。
* 插件/脚本不做。
* 复杂面板停靠不做。

## Scope Cut 必须检查的清单

### 阅读核心

* 打开文件夹。
* 显示第一张。
* 左右键翻页。
* 页码显示。
* 单页 Fit。
* 自然排序。
* 空状态。
* 错误页。
* 当前页状态。

### 第一批体验

* 双页。
* 阅读方向。
* 封面单独显示。
* 基础目录侧栏。
* 目录高亮当前页。
* 目录点击跳转。

### 持久化

* 是否 MVP 必须最近打开？

  * 你之前对“最近打开”有摇摆。
  * 执行前要重新定。
* 是否 MVP 必须保存进度？

  * 当前文档倾向必须。
* 进度保存按路径，不做 hash。

### 文件来源

* 文件夹。
* ZIP/CBZ。
* 递归文件夹是否 MVP 必须？

  * 当前文档倾向保留为核心模式。
  * 执行前要再确认默认是否开启。
* RAR/CBR 暂不做。
* 7z 不做。
* PDF 不做。
* GIF 动图不做。

### UI/UX

* 启动页。
* 阅读页。
* 目录侧栏。
* 工具栏是否常驻。
* 底部页码。
* 右键菜单是否 MVP 必须。
* 设置页不进 MVP。
* 全屏可 V1。

### 平台差异

* Finder / Explorer。
* Ctrl / Command。
* AppData / Application Support。
* 路径显示。
* 打包验证。

## Scope Cut 的判断句

* 没有它，软件还能读漫画吗？
* 没有它，第一版会不会明显不符合我的使用习惯？
* 它会不会把阅读器变成管理器？
* 它会不会强行引入数据库、插件、脚本、复杂命令系统？
* 它会不会让第一版拖死？

---

# 18. Codex 执行套路

## 每个模块开始前

* 先确认当前分支。
* 先确认对应文档。
* 先让 Codex 复述目标和非目标。

## 给 Codex 的通用约束

* 保持 MVVM 最小结构。
* View 只写界面和绑定。
* ViewModel 保存状态和命令。
* 扫描、排序、解码、保存放 Services / Infrastructure。
* 不修改用户漫画文件。
* 平台差异放 PlatformService。
* 不做插件、脚本、复杂命令系统。
* 不要提前设计未来大架构。
* 每完成一个模块，写变更说明到 `docs/logs/`。

## 每次 Codex 输出后检查

* Diff 里改了哪些文件？
* 是否改了不该改的入口文件？
* 是否有业务逻辑塞进 View？
* 是否有平台判断散落各处？
* 是否把整本书图片加载进内存？
* 是否有删除/移动/重命名原文件？
* 是否有测试？
* 是否更新了文档？

## 最小垂直切片顺序

* Avalonia 窗口启动。
* 打开文件夹。
* 扫描图片。
* 自然排序。
* 显示第一张。
* 左右键翻页。
* 页码显示。
* 空文件夹状态。
* 损坏图片占位。

## 单页闭环之后立刻补

* 双页。
* 阅读方向。
* 封面单独显示。
* 基础目录侧栏。
* 目录跳转。
* 进度保存。

---

# 19. 最容易出错的路线

## 架构错误

* 把 `Core` 当杂物箱。
* 把业务逻辑写进按钮事件。
* ViewModel 变成巨大万能类。
* PlatformService 不存在，平台判断到处散落。

## 范围错误

* 复制 NeeView 的复杂命令系统。
* 做插件。
* 做脚本。
* 做文件管理。
* 做书签。
* 做复杂停靠面板。
* 做主题编辑器。

## 性能错误

* 一次性解码整本书。
* Bitmap 缓存无限增长。
* 文件流不释放。
* ZIP 条目流不释放。
* 预加载任务不能取消。

## UX 错误

* 图片区域不是视觉主角。
* 工具栏太重。
* 右键菜单塞太多。
* “移除记录”和“删除文件”语义不清。
* 快捷键和阅读方向冲突。

## 持久化错误

* JSON 没版本号。
* 配置坏了应用启动失败。
* 读失败没有默认值。
* 写到漫画源目录。
* 直接覆盖配置，写坏无法恢复。

---

# 20. 你真正需要记住的 20 句话

* ComicPlate 是阅读器，不是管理器。
* NeeView 是参考，不是复制对象。
* 文件夹和压缩包都可以是 Book。
* 图片是 Page。
* 扫描整本书可以，解码整本书不行。
* View 负责显示。
* ViewModel 负责状态和命令。
* Domain 负责 ComicPlate 自己的阅读规则。
* Infrastructure 负责外部文件、ZIP、JSON、图片库。
* PlatformService 负责系统差异。
* UI 框架不是主程序外的控制器，而是 App 内部的显示与交互系统。
* Branch 是隔离线，commit 是存档点。
* 单仓库足够，不要过早拆 repo。
* JSON 必须有 version。
* 配置读失败必须有默认值。
* UI 线程不能被大任务卡住。
* CancellationToken 是取消旧任务的信号。
* Action 是统一操作，快捷键/工具栏/右键只是入口。
* 任何修改用户原文件的功能都不进 MVP。
* 每次写代码前先问：这是阅读规则、平台能力、外部库 adapter，还是 UI 状态？

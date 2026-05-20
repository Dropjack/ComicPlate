# Todo List

Todo 是“项目要做什么”。不要把学习项混进来。

当前 MVP 发布目标包括 Phase 1 到 Phase 5。Phase 1 只是最小技术闭环，不等于最终 MVP。

## Phase 0: 文档和项目护栏

- [x] 建立基础文档目录。
- [x] 写 Reference Analysis。
- [x] 写 Scope Cut。
- [x] 写 Behavior Spec。
- [x] 写 UI UX Spec。
- [x] 写 Architecture Spec。
- [x] 写 Todo List。
- [x] 写 To-Learn List。
- [x] 和用户一起审 Scope Cut，把 MVP 从书架型转为漫画预览器/阅读窗口。
- [x] 重新评估文件夹内 ZIP/CBZ 串联阅读，将其从 MVP 调整为 V1 候选。
- [x] 确认 MVP 不做快捷键设置，V1 再做快捷键预设或自定义。
- [x] 确认阅读交互原则：翻页模式是吸附，拖拽/滚动模式是自由。
- [x] 将虚拟化阅读带目标写入 `02/03/04` 文档。
- [x] 确认产品方向改为文件关联优先、启动面板、当前容器一层 Context Shelf。
- [x] 建立本地化 key map 文档，明确英文源语言、语义 key、zh-Hans 内部标签和不可本地化内容。
- [x] 回写 Scope Cut，把已经完成的 V1 能力和超出原 scope 的实现事实补齐。

## Phase 1: 最小可运行闭环

- [x] 搭建 Avalonia 项目。
- [x] 创建主窗口。
- [x] 创建启动面板。
- [x] 实现打开文件夹入口。
- [x] 实现打开单张图片入口。
- [x] 将打开的文件夹作为一个 Book。
- [x] 将打开的单张图片作为单页 Book。
- [x] 实现图片扩展名过滤。
- [x] 实现自然排序。
- [x] 实现 PageEntry 数据结构。
- [x] 显示第一张图片。
- [x] 保持单张图片居中显示。
- [x] 实现当前阅读组状态。
- [x] 实现下一页/上一页。
- [x] 绑定左右键。
- [x] 显示页码。
- [x] 处理空文件夹状态。
- [x] 处理损坏图片占位。
- [ ] 写 Phase 1 变更日志到 `docs/logs/`。

## Phase 2: 第一批阅读体验补齐

- [x] 添加基础 Context Shelf 侧栏。
- [x] Context Shelf 只显示当前层可打开项。
- [x] Context Shelf 显示文件夹/压缩包名称和缩略图。
- [x] 点击 Context Shelf 里的文件夹/压缩包打开该项或进入容器。
- [x] 底部按钮改为视觉方向：Left / 页码 / Right。
- [x] 底部进度条按当前阅读位置显示视觉位置。
- [x] 鼠标滚轮自由移动阅读带 offset。
- [x] 鼠标拖拽自由移动阅读带 offset。
- [x] Left/Right 按钮和键盘翻页保持吸附到阅读组中心。
- [x] 左侧 Context Shelf 获得焦点时，窗口级左右键仍可翻页。
- [x] 建立虚拟化阅读带计算模型。
- [x] 添加单页/双页模式状态。
- [x] 实现第一页单独显示。
- [x] 实现双页成对显示。
- [x] 实现单页阅读组移动 1 页。
- [x] 实现双页阅读组移动 1 组。
- [x] 实现 LeftToRight 排列。
- [x] 实现 RightToLeft 排列。
- [x] 根据阅读方向映射左右键。
- [x] 暴露临时阅读方向切换按钮：RTL / LTR。
- [x] 添加简单 Action 表。
- [x] 优化翻页/跳转的平滑过渡体验。
- [x] 写双页状态机测试。
- [ ] 写 Phase 2 变更日志到 `docs/logs/`。

## Phase 2.5: 进度条交互

- [x] 进度条点击/拖动释放后快速定位。
- [x] 定义 RTL/LTR 下进度条视觉位置到页码的映射。
- [x] 定义双页模式下进度条落点规则：跳到包含目标页的 frame。
- [x] 定义双页 frame 的页码范围显示，例如 `14-15 / 58`。
- [x] 定义宽页 / 横页显示规则：单图宽页仍按 1 page 计数，不拆分为跨页。
- [ ] 优化进度条拖动时的实时反馈和平滑跳转体验。
- [ ] 写 Phase 2.5 变更日志到 `docs/logs/`。

## Phase 3: 会话和每本书进度

- [x] 定义 `settings.json`。
- [x] 定义 `session.json`。
- [x] 定义 `progress.json`。
- [x] 实现用户数据目录解析。
- [x] 保存全局 lastSession。
- [x] 保存每本书 progress，并设置 500 条上限。
- [x] progress 使用最终可阅读单元规范化路径作为 key，不受启动入口影响。
- [x] 从父文件夹 shelf 点进已有 progress 的 ZIP/CBZ/文件夹漫画时恢复页码。
- [x] 关闭时如果停在最后一页，删除该书 progress。
- [x] exe 空启动展示 Continue Reading / Open Comics。
- [x] 空启动 Continue Reading 恢复上一次内容。
- [x] 再次打开同一路径时恢复该书上次页码。
- [x] 处理历史路径不存在。
- [x] 写核心持久化测试。
- [ ] 写 Phase 3 变更日志到 `docs/logs/`。

## Phase 4: ZIP/CBZ

- [x] 实现 ZIP BookSource。
- [x] 支持 `.zip` 和 `.cbz`。
- [x] 支持 ZIP/CBZ 命令行路径直接打开。
- [x] 预留文件关联启动入口，但不在 MVP 修改系统关联。
- [x] 过滤压缩包内图片。
- [x] 忽略非图片文件。
- [x] 处理压缩包内子目录。
- [x] 处理打不开的 ZIP。
- [x] 处理加密 ZIP。
- [x] 让 Continue Reading 和 per-book progress 支持 ZIP/CBZ。
- [x] 写 ZIP 排序和过滤测试。
- [ ] 写 Phase 4 变更日志到 `docs/logs/`。

## Phase 5: 当前容器和文件夹漫画入口

- [x] 文件夹打开后只扫描当前层。
- [x] 当前层图片进入主面板 Page 流。
- [x] 当前层子文件夹作为 Context Shelf entry。
- [x] 当前层 ZIP/CBZ 作为 Context Shelf entry。
- [x] 当前层只有图片时直接作为文件夹漫画阅读。
- [x] 当前层文件夹、压缩包在 Context Shelf 中自然排序或按轻量分组排序。
- [x] Context Shelf 不做面包屑和树形文件浏览器。
- [x] 处理空当前容器。
- [x] 处理权限失败的子目录。
- [x] 处理打不开的当前层压缩包。
- [x] 写当前容器扫描测试。
- [x] 写文件夹漫画入口测试。
- [ ] 写 Phase 5 变更日志到 `docs/logs/`。

## Phase 6: 基础设置、文件关联、快捷键和多窗口

- [x] 添加基础设置窗口骨架。
- [x] 设置窗口使用中文 UI。
- [x] 设置窗口采用左侧快速链接 + 右侧单页内容结构。
- [x] 设置窗口可独立拖动和缩放，不阻塞主阅读窗口。
- [x] 添加快捷键独立窗口，用同一套轻量设置 UI 风格展示固定快捷键。
- [x] 添加 Command Rail 图标化入口。
- [x] 添加 Reader 底部左右导航图标。
- [x] 接入设置保存和读取。
- [x] 接入数据目录打开按钮。
- [x] 接入缩略图缓存清理按钮。
- [x] 接入窗口大小和位置恢复。
- [x] 接入允许多开窗口设置。
- [x] 接入阅读放大镜开关。
- [x] 接入预设配色方案设置。
- [x] 实现四套内置主题：Mist Green、Slate Blue、Warm Paper、Night Graphite。
- [x] 主题变更采用重启生效提示，不做运行时热切换。
- [x] 添加文件关联设置面板的真实系统操作。
- [x] 允许用户显式关联 CBZ、ZIP、CBR、RAR；默认不自动关联。
- [x] 添加 Windows File Explorer 右键菜单设置面板。
- [x] 允许用户显式为 CBZ、ZIP、CBR、RAR 注册 `Open in ComicPlate` 右键菜单。
- [x] 实现最小压缩包格式映射：ZIP/CBZ/RAR/CBR，不做通用 archive manager。
- [x] 支持 RAR/CBR 打开、缩略图和进度恢复。
- [x] 将 7z/CB7 移出当前 V1；不显示设置项，不预留可见占位。
- [x] 确认嵌套压缩包不进入当前 V1，只忽略或按不可打开项处理。
- [x] 支持打开多个独立阅读器窗口。
- [x] 每个窗口独立打开一个 Book。
- [x] 实现全屏。
- [x] 全屏下工具栏自动隐藏。
- [x] 优化吸附翻页动画：从自由 offset 或非吸附状态切到下一 frame 时，需要有平滑的 release/attack 手感。
- [x] 优化阅读 frame 贴合：当前 frame、邻近 frame 和双页 frame 之间要严格贴合，避免出现破坏连续阅读感的空隙。
- [ ] 写 Phase 6 变更日志到 `docs/logs/`。

## Phase 6.5: UI 本地化和文案收口

- [x] 定义语言设置项 `system` / `en` / `zh-Hans` / `ja`。
- [x] 默认语言为 `system`。
- [x] 系统语言不支持时回退到 `en`。
- [x] 单个本地化 key 缺失时回退到 `en`。
- [x] 使用 `en.json` 作为 source of truth。
- [x] 使用语义路径 key，不使用英文句子作为 key。
- [x] 建立 `en.json`、`zh-Hans.json`、`ja.json`。
- [x] 外部语言显示使用 `中文`，内部标签保持 `zh-Hans`。
- [x] 语言变更采用重启生效提示，不做运行时热切换。
- [x] 添加 XAML 可用的 `{loc:Loc Key.Path}` 标记扩展。
- [x] 本地化设置页文案、按钮、说明和状态消息。
- [x] 本地化快捷键窗口文案。
- [x] 本地化主窗口 Command Rail tooltip。
- [x] 本地化 StartView 入口文案。
- [x] 本地化 Shelf tooltip、badge 和 detail 文案。
- [x] 本地化 Reader tooltip、视图模式、阅读方向和图片显示失败状态。
- [x] 本地化 ViewModel 打开/加载/空状态/失败状态消息。
- [x] 本地化文件关联和 Windows 右键菜单的用户可见状态消息。
- [x] 本地化 Windows 注册表里用户可见的 File Type 名称和右键菜单 verb。
- [x] 明确保留不本地化内容：文件名、路径、漫画标题、archive entry、页码、倍率、快捷键显示、扩展名和 raw format label。
- [x] 扫描并清理剩余用户可见硬编码 UI 字符串。
- [x] 补充设置保存、语言解析和 fallback 测试。
- [x] 将受本地化影响的测试固定为 English，避免受本机系统语言影响。
- [x] 全量测试通过：`dotnet test ComicPlate.sln`。

## Phase 7: 打包

- [x] Windows publish。
- [ ] macOS app bundle。
- [x] 应用图标。
- [x] 基础版本号。
- [ ] 本地安装/运行验证。
- [ ] 写打包说明。

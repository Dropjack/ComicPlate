# Todo List

Todo 是“项目要做什么”。不要把学习项混进来。

## Phase 0: 文档和项目护栏

- [x] 建立基础文档目录。
- [x] 写 Reference Analysis。
- [x] 写 Scope Cut。
- [x] 写 Behavior Spec。
- [x] 写 UI UX Spec。
- [x] 写 Architecture Spec。
- [x] 写 Todo List。
- [x] 写 To-Learn List。
- [ ] 和用户一起审 Scope Cut，把 MVP 再砍一轮。

## Phase 1: 最小可运行闭环

- [ ] 搭建 Avalonia 项目。
- [ ] 创建主窗口。
- [ ] 创建启动页。
- [ ] 实现打开文件夹入口。
- [ ] 实现图片扩展名过滤。
- [ ] 实现自然排序。
- [ ] 实现 PageEntry 数据结构。
- [ ] 显示第一张图片。
- [ ] 实现当前页状态。
- [ ] 实现下一页/上一页。
- [ ] 绑定左右键。
- [ ] 显示页码。
- [ ] 处理空文件夹状态。
- [ ] 处理损坏图片占位。
- [ ] 写 Phase 1 变更日志到 `docs/logs/`。

## Phase 2: 进度和最近打开

- [ ] 定义 `settings.json`。
- [ ] 定义 `library.json`。
- [ ] 实现用户数据目录解析。
- [ ] 保存最近打开。
- [ ] 保存当前页进度。
- [ ] 启动页展示最近打开。
- [ ] 点击最近打开恢复阅读。
- [ ] 处理历史路径不存在。
- [ ] 写核心持久化测试。
- [ ] 写 Phase 2 变更日志到 `docs/logs/`。

## Phase 3: ZIP/CBZ

- [ ] 实现 ZIP BookSource。
- [ ] 支持 `.zip` 和 `.cbz`。
- [ ] 过滤压缩包内图片。
- [ ] 忽略非图片文件。
- [ ] 处理压缩包内子目录。
- [ ] 处理打不开的 ZIP。
- [ ] 处理加密 ZIP。
- [ ] 让最近打开支持 ZIP/CBZ。
- [ ] 写 ZIP 排序和过滤测试。
- [ ] 写 Phase 3 变更日志到 `docs/logs/`。

## Phase 4: 双页和阅读方向

- [ ] 添加单页/双页模式状态。
- [ ] 实现第一页单独显示。
- [ ] 实现双页成对显示。
- [ ] 实现 LeftToRight 排列。
- [ ] 实现 RightToLeft 排列。
- [ ] 根据阅读方向映射左右键。
- [ ] 添加工具栏切换入口。
- [ ] 写双页状态机测试。
- [ ] 写 Phase 4 变更日志到 `docs/logs/`。

## Phase 5: 基础设置和全屏

- [ ] 添加设置页。
- [ ] 设置阅读方向。
- [ ] 设置默认适配模式。
- [ ] 设置最近打开数量。
- [ ] 实现全屏。
- [ ] 全屏下工具栏自动隐藏。
- [ ] 写 Phase 5 变更日志到 `docs/logs/`。

## Phase 6: 打包

- [ ] Windows publish。
- [ ] macOS app bundle。
- [ ] 应用图标。
- [ ] 基础版本号。
- [ ] 本地安装/运行验证。
- [ ] 写打包说明。


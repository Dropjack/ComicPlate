# Todo List

Todo 是“当前版本还需要做什么”。已完成的阶段任务不继续保留为待办项；历史决策和范围边界以 Scope、Behavior、Architecture 和 planning 文档为准。

## 当前状态

- [x] MVP/V1 阅读闭环已完成：文件夹、单图、ZIP/CBZ、RAR/CBR 可以作为 Book 打开。
- [x] Context Shelf 已按当前容器一层浏览模型实现：Shelf 只进入 Collection，不进入 Book。
- [x] Continue Reading、per-book progress、session.json、progress.json 已接入。
- [x] 多窗口基础模型已接入：每个 Reader window 独立阅读，last writer wins。
- [x] 设置页已接入：数据目录、缩略图缓存清理、文件关联、Windows 资源管理器右键菜单、快捷键入口、多窗口开关。
- [x] 缩略图缓存和 ReaderImageCache 已分离；ReaderImageCache 使用预算型 LRU。
- [x] 全屏阅读、底部 overlay、左侧 Shelf overlay 已接入。
- [x] 阅读带 frame 间距已收紧，当前版本不再保留额外 frame gap。
- [x] 进度条拖动已改为预览，松手后单次提交跳页。
- [x] Windows publish 包已纳入当前收尾流程。

## 当前不做

- [x] 当前 V1 不做 7z/CB7。
- [x] 当前 V1 不做嵌套压缩包。
- [x] 当前 V1 不做标签页、书架管理、工作区恢复。
- [x] 当前 V1 不做自动文件关联；所有系统关联必须由用户在设置中主动触发。
- [x] 当前 V1 不做 macOS Finder Service / Quick Action。

## 后续观察

这些不是当前发布阻塞项，只作为后续体验优化候选：

- 阅读切换和远距离跳页的微动画手感。
- 大本 omnibus 的更激进预加载策略。
- macOS app bundle、图标、签名、公证和 DMG。
- 更完整的发布说明和版本号策略。

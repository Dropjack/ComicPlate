# ComicPlate

[English README](README_EN.md) / [日本語 README](README_JA.md)

ComicPlate 是一个面向 Windows 和 macOS 的轻量本地漫画阅读器，使用 C# 和 Avalonia 构建。

它可以把本地文件夹和漫画压缩包作为可阅读的书打开，恢复阅读进度，并且不会修改用户的原始文件。

当前公开版本：`1.1.1`。

![ComicPlate screenshot](artworks/SamplePhoto.jpg)

截图中的示例漫画页面来自 David Revoy 创作的 *Pepper&Carrot*，使用 CC BY 4.0 授权。

## 状态

`1.0.0` 是第一个完整公开版本。

目前应用以自包含构建发布，不是安装包。Windows 和 macOS 的安装包、签名和更完整的发布流程会在后续版本继续完善。

## 支持平台

当前发布目标：

* Windows x64
* macOS Apple Silicon

## 功能

* 打开本地文件夹、ZIP / CBZ、RAR / CBR 压缩包。
* 打开单张图片作为轻量预览。
* 单页和双页阅读。
* 从右到左 / 从左到右阅读方向。
* 横向连续阅读带，支持键盘、按钮、滚轮和拖拽操作。
* 历史记录和书架。
* 继续阅读和每本书阅读进度恢复。
* 多窗口、全屏和全屏下工具栏自动隐藏。
* 内置浅色、深色和阅读友好的预设配色。
* 支持中文、英文和日语。
* Windows 文件关联和 Explorer 右键菜单的显式设置入口。

## 范围

ComicPlate 是阅读器，不是漫画库管理器。

它读取用户内容，生成页面列表，显示页面，并保存属于 ComicPlate 自己的状态，例如设置、会话、阅读进度、日志和缓存。

它可以记住阅读位置、恢复窗口状态、保存语言和主题设置，也可以在用户明确选择后注册支持格式的文件关联或右键菜单。

它没有删除、移动、重命名、重写或编辑用户的漫画文件等管理漫画文件的能力。

## 支持格式

* 文件夹图片
* `.zip` / `.cbz`
* `.rar` / `.cbr`
* `.jpg` / `.jpeg`
* `.png`
* `.webp`
* `.bmp`
* `.gif` 仅第一帧

不在当前范围内：

* PDF
* EPUB / MOBI
* 视频 / 音频
* 嵌套压缩包
* 7z / CB7
* metadata 管理
* 全库扫描
* 文件编辑或文件管理操作

## UI 模型

ComicPlate 启动时显示一个轻量入口界面，随后进入阅读窗口。

阅读窗口包含左侧 Context Shelf、中央 Reader Stage 和底部进度条。Shelf 只用于当前容器内的邻近导航，不是书架。

设置窗口只负责应用自己的行为，例如语言、主题、阅读偏好、数据目录、文件关联和快捷键说明，不承担文件管理。

## 从源码运行

要求：

* .NET SDK

```bash
dotnet restore
dotnet run --project src/ComicPlate.App
```

Debug 配置：

```bash
dotnet run --project src/ComicPlate.App -c Debug
```

## 构建

基础 Release 发布：

```bash
dotnet publish src/ComicPlate.App -c Release
```

macOS app bundle 脚本：

```bash
bash scripts/package-macos-app.sh
```

Release 输出是自包含构建。构建输出不应进入 Git。请使用 `artifacts/`、`publish/` 或其他已忽略的输出目录。

## 项目结构

```text
src/ComicPlate.App              Avalonia UI、窗口、视图、ViewModel
src/ComicPlate.Core             Book、Page、阅读状态、排序、领域规则
src/ComicPlate.Infrastructure   文件系统、压缩包、持久化、平台服务
tests/                          测试
platform/                       平台相关文件
scripts/                        构建和打包脚本
```

## 架构

ComicPlate 使用较小的 App / Core / Infrastructure 分层。

```text
App              Avalonia UI、窗口、视图、ViewModel
Core             Book、Page、阅读状态、排序、领域规则
Infrastructure  文件系统、压缩包、持久化、平台服务
```

阅读器不应该一次性解码整本书。图片解码、缓存和内存行为是阅读核心的一部分，不是后期润色。

## 路线规划

近期工作：

* 完善 Windows 和 macOS 的发布打包流程。
* 改进文件关联、签名和平台集成体验。
* 继续优化图片解码、缓存和内存行为。
* 根据真实使用反馈收紧阅读交互和设置项。

## 边界

ComicPlate 不是 PDF 阅读器、EPUB 阅读器、图片编辑器、metadata 编辑器、批量重命名工具、文件管理器或完整漫画库管理器。

## 贡献

欢迎提交 issue，尤其是关于无法读取的压缩包、错误的页面顺序、图片解码失败、双页布局问题、阅读方向问题、平台差异、内存问题和性能问题。

大型功能 PR 应先开 issue 讨论。

核心规则：ComicPlate 不应该修改用户漫画文件，也不应该变成漫画库管理器。

## License

See `LICENSE`.

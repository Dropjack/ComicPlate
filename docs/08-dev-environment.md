# Dev Environment

这份文档回答“我要装什么，什么时候装”。Architecture Spec 讲项目分层；开发环境单独放在这里。

## 当前 Windows 机器状态

已检测：

- Git 已安装：`git version 2.46.0.windows.1`
- VS Code 已安装：`1.113.0`
- .NET Runtime 已安装：`6.0.16`、`8.0.14`

缺少：

- .NET SDK。当前 `dotnet --info` 显示 `No SDKs were found.`，所以还不能创建、编译、运行 ComicPlate 的 Avalonia 项目。

## Windows 第一批必装

### .NET SDK

必须安装。

建议先安装：

- .NET 8 SDK，匹配当前文档里的技术栈。

安装后检查：

```powershell
dotnet --info
```

够用标准：

- 输出里能看到 `SDKs installed`。
- 能执行 `dotnet new --list`。

### Avalonia Templates

安装 .NET SDK 后再装。

命令：

```powershell
dotnet new install Avalonia.Templates
```

检查：

```powershell
dotnet new list avalonia
```

够用标准：

- 能看到 Avalonia App 模板。

### VS Code 扩展

建议安装：

- C# Dev Kit
- C#
- Avalonia for Visual Studio Code

够用标准：

- 打开 `.cs` 文件有语法高亮。
- 项目创建后能看到解决方案/项目结构。

## Windows 之后再装

这些不是开工前必须：

- Visual Studio。
- Rider。
- WiX / MSIX 打包工具。
- 图标制作工具。

只有当我们开始 Windows 打包时，再补打包工具。

## macOS 机器以后需要

等核心在 Windows 上跑起来，再到 macOS 上验证。

macOS 需要：

- Git。
- VS Code。
- .NET SDK。
- Avalonia Templates。
- Xcode Command Line Tools，主要用于部分构建/签名/打包流程。

macOS 够用标准：

- 能拉取同一个 ComicPlate 仓库。
- 能 `dotnet build`。
- 能 `dotnet run`。
- 能验证 Command 快捷键、Finder 行为、macOS 外观和 app bundle。

## 现在不要装的东西

暂时不要装：

- 数据库。
- Electron。
- Qt。
- WPF 专用工具。
- 插件系统相关工具。
- PDF/视频处理库。
- 打包签名全家桶。

这些会把注意力带偏。

## 当前第一步

现在真正的第一步不是装一堆工具，而是把 Scope Cut 定到能开工。

开工前只需要把这些问题定清楚：

- MVP 是否必须有最近打开。
- MVP 是否必须支持 GIF 静态第一帧，还是完全不支持 GIF。
- 递归文件夹是否默认开启，还是作为打开选项。
- 基础目录侧栏显示哪些字段。
- 主阅读区、目录侧栏、最近打开列表的右键菜单各有什么。

这些定完后，再安装 .NET SDK 和 Avalonia Templates，然后进入 Phase 1。


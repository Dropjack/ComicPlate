# ComicPlate Docs

这组文档是 ComicPlate 的项目刹车、地图和施工记录。目标不是复刻 NeeView，而是从 NeeView 中提炼“为什么它好用”，再做一个轻量、跨平台、可逐步扩展的个人漫画/图片阅读器。

建议阅读顺序：

1. [DECISIONS](DECISIONS.md)
2. [01 Reference Analysis](01-reference-analysis.md)
3. [02 Scope Cut](02-scope-cut.md)
4. [03 Behavior Spec](03-behavior-spec.md)
5. [04 UI UX Spec](04-ui-ux-spec.md)
6. [05 Architecture Spec](05-architecture-spec.md)
7. [06 Todo List](06-todo-list.md)
8. [07 To Learn List](07-to-learn-list.md)
9. [08 Dev Environment](08-dev-environment.md)
10. [Guideline](Guideline.md)
11. [logs](logs)

当前原则：

- NeeView 是参考，不是复制对象。
- ComicPlate 是只读漫画阅读器，不是漫画文件管理器。
- ComicPlate 读取用户内容，但不修改用户内容；所有写入只允许发生在 ComicPlate 自己的设置、进度、日志或缓存里。
- ComicPlate 先追求一个可运行闭环：打开文件夹，看到第一张图，按键翻页。
- 双页、阅读方向、基础目录侧栏属于第一批核心体验；单页闭环跑通后立刻补齐。
- 文档先定边界，再写代码。
- 每完成一个模块，都在 `docs/logs/` 写变更说明和当前限制。


## ComicPlate 具体方向

### 业务方向约束
- 初版 ComicPlate 只是漫画阅读器，并不是漫画文件管理器。
- 更像 NeeView 的阅读体验，但是界面更现代、更干净
- MVP 不允许删除、移动、重命名、修改图片或压缩包。
- 书签不是 MVP；每本书自动恢复上次阅读页即可。

## 批注约定

你可以继续在文档任意位置写：

```md
%% Q: 这里为什么这样定？ %%
%% P: 我偏好默认右到左。 %%
%% YES: 这个必须做。 %%
%% NO: 这个不想做。 %%
```

我处理批注时会先在聊天里回答，再把确认过的内容整理进正式文档。

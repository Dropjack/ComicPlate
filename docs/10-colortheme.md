# Color Theme Notes

本文记录 ComicPlate 配色方案的候选方向、颜色 token，以及每个 token 的使用逻辑。

当前建议最终保留四套主题：

- `Mist Green` / 雾绿 / ミストグリーン
- `Slate Blue` / 冷灰蓝 / スレートブルー
- `Warm Paper` / 暖纸米色 / ウォームペーパー
- `Night Graphite` / 夜间石墨 / ナイトグラファイト

其中：

- `Mist Green` 是 ComicPlate 原始气质。
- `Slate Blue` 是偏工具感的浅色主题。
- `Warm Paper` 是最适合阅读的浅色主题。
- `Night Graphite` 是夜间主题。

## 1. Mist Green / 雾绿

核心气质：清爽、安静、轻微自然感，是 ComicPlate 当前的默认主题。它应该像“干净的阅读工具”，而不是明显的护眼绿或装饰性绿色界面。

### Tokens

```text
BackgroundBaseColor              #EEF4F7
SurfacePanelColor                #F3F5F2
SurfaceMutedColor                #E4EAE7
SurfaceElevatedColor             #FFFFFF
SurfaceInputColor                #DCE5E1

ReaderStageColor                 #EEF4F7
ReaderPageSurfaceColor           #FFFFFF

TextPrimaryColor                 #1F2A2A
TextSecondaryColor               #667371
TextDisabledColor                #A7B1AE
TextOnAccentColor                #FFFFFF
TextInverseColor                 #FFFFFF

BorderSubtleColor                #C9D4D0

AccentColor                      #4F7F6A
AccentHoverColor                 #5F927B

ShelfHoverHighlightColor         #E8EFEC
ShelfReadingHighlightColor       #DCE6E2
ShelfNavigationHighlightColor    #CAD8D3

OverlayDarkColor                 #CC1F1F1F

MacFullscreenChromeColor         #80F9F7F5
MacFullscreenChromeBorderColor   #66FFFFFF
```

### Position Logic

`BackgroundBase` 是浅雾蓝绿，整体比纯灰更柔和，但不会让界面变成绿色主题。

`SurfacePanel` 接近灰白，给左侧面板、顶部栏、设置页卡片一个干净底色。

`SurfaceMuted` 和 `SurfaceInput` 带轻微绿灰，用来做二级区域、输入区域和弱背景。

`ReaderStage` 跟整体背景保持一致，重点是安静退后，不抢漫画页面。

`ReaderPageSurface` 保持白色，避免影响白底漫画页的观感。

文字用偏冷的墨绿黑体系：`TextPrimary` `#1F2A2A` 不像纯黑那么硬；`TextSecondary` `#667371` 保持低调但可读。

`Accent` 用低饱和雾绿 `#4F7F6A`，适合当前页进度、当前状态和主要按钮。它应该是“确认方向”的颜色，不应该大面积铺开。

这个主题的风险是容易滑向“护眼模式”。解决办法是让大面积背景保持灰白和冷静，只把绿色留给 accent 与少量高亮层级。

## 2. Slate Blue / 冷灰蓝

核心气质：干净、现代、偏工具，但不能变成企业后台。适合作为 Windows 默认候选之一。

### Tokens

```text
BackgroundBaseColor              #EEF3F8
SurfacePanelColor                #F6F8FA
SurfaceMutedColor                #E3EAF1
SurfaceElevatedColor             #FFFFFF
SurfaceInputColor                #D9E3EC

ReaderStageColor                 #EDF1F5
ReaderPageSurfaceColor           #FFFFFF

TextPrimaryColor                 #1C2733
TextSecondaryColor               #607080
TextDisabledColor                #A3AFBA
TextOnAccentColor                #FFFFFF
TextInverseColor                 #FFFFFF

BorderSubtleColor                #C8D3DE

AccentColor                      #3F6F95
AccentHoverColor                 #4E82AC

ShelfHoverHighlightColor         #E7EEF5
ShelfReadingHighlightColor       #DCE8F2
ShelfNavigationHighlightColor    #C8D8E6

OverlayDarkColor                 #CC1C2228

MacFullscreenChromeColor         #80F7FAFC
MacFullscreenChromeBorderColor   #66FFFFFF
```

### Position Logic

`BackgroundBase` 是浅冷灰蓝，给整个 App 一个冷静底色。

`SurfacePanel` 接近白，用来托起左侧面板、顶部栏、设置页卡片。

`SurfaceMuted` 和 `SurfaceInput` 带一点蓝灰，用来做输入框、二级面板、弱背景。

`ReaderStage` 故意不蓝，只是轻微冷灰。这样漫画页不会被蓝色污染。

`ReaderPageSurface` 保持白色，因为多数漫画页本身就是白底，页面背板越中性越好。

文字用蓝黑体系：`TextPrimary` `#1C2733` 比纯黑柔和；`TextSecondary` `#607080` 带蓝灰感。

`Accent` 用中低饱和蓝 `#3F6F95`，不刺眼，能用于进度条、当前状态、主要按钮。

这个主题的问题是容易“太普通”。解决办法是让 Shelf 的高亮有一点蓝灰层级，而不是全都白。

## 3. Night Graphite / 夜间石墨

核心气质：低亮度、稳定、长时间阅读。不要做纯黑，也不要做 VS Code 那种高对比程序员主题。

### Tokens

```text
BackgroundBaseColor              #171B1F
SurfacePanelColor                #1F252A
SurfaceMutedColor                #273037
SurfaceElevatedColor             #2D353C
SurfaceInputColor                #222B32

ReaderStageColor                 #121518
ReaderPageSurfaceColor           #1A1D20

TextPrimaryColor                 #E6ECEF
TextSecondaryColor               #A4B0B7
TextDisabledColor                #657178
TextOnAccentColor                #FFFFFF
TextInverseColor                 #FFFFFF

BorderSubtleColor                #35414A

AccentColor                      #6FA6B8
AccentHoverColor                 #82B8CA

ShelfHoverHighlightColor         #263038
ShelfReadingHighlightColor       #2C3A43
ShelfNavigationHighlightColor    #354A56

OverlayDarkColor                 #CC000000

MacFullscreenChromeColor         #80222A30
MacFullscreenChromeBorderColor   #335F6A72
```

### Position Logic

`BackgroundBase` 用深石墨色，不是纯黑。纯黑会让白色漫画页太炸，也会让 UI 边界过硬。

`SurfacePanel` 比背景亮一点，左侧面板、顶部栏能自然浮出来。

`SurfaceElevated` 再亮一点，给弹窗、菜单、浮层使用。

`ReaderStage` 是全套里最深的区域之一，因为阅读舞台应该退后。

`ReaderPageSurface` 不用纯黑，而是 `#1A1D20`。如果图片没加载、透明图、页面背板出现时，它不会像黑洞一样突兀。

文字不要纯白。`TextPrimary` `#E6ECEF` 是浅灰白，长时间看比 `#FFFFFF` 舒服。

`TextSecondary` `#A4B0B7` 保证状态栏、路径、说明文字还读得清。

`TextDisabled` `#657178` 足够弱，但不至于完全看不见。

`Accent` 用浅青蓝 `#6FA6B8`，因为深色主题下绿色容易变“终端感”，纯蓝容易变“系统设置感”。青蓝更安静，也和漫画阅读器气质更接近。

这个主题最重要的风险是：`BorderSubtle` 不能太亮。深色 UI 一旦线太亮，会变成网格。

## 4. Warm Paper / 暖纸米色

核心气质：纸张、温和、长期阅读。不能做旧报纸，也不能黄到像护眼模式。

### Tokens

```text
BackgroundBaseColor              #F4EFE7
SurfacePanelColor                #FAF7F1
SurfaceMutedColor                #E9E1D4
SurfaceElevatedColor             #FFFDF8
SurfaceInputColor                #E3D9CA

ReaderStageColor                 #F0EAE1
ReaderPageSurfaceColor           #FFFDF8

TextPrimaryColor                 #2D2924
TextSecondaryColor               #756D62
TextDisabledColor                #B1A89B
TextOnAccentColor                #FFFFFF
TextInverseColor                 #FFFFFF

BorderSubtleColor                #D5CABB

AccentColor                      #7A6A45
AccentHoverColor                 #8B7A52

ShelfHoverHighlightColor         #EFE8DD
ShelfReadingHighlightColor       #E5DACB
ShelfNavigationHighlightColor    #D8C8B4

OverlayDarkColor                 #CC1E1A15

MacFullscreenChromeColor         #80FFF9EF
MacFullscreenChromeBorderColor   #66FFFFFF
```

### Position Logic

`BackgroundBase` 是浅暖米灰，不是黄。

`SurfacePanel` 接近纸白，用来让侧栏和设置页保持干净。

`SurfaceMuted` 和 `SurfaceInput` 偏暖灰，用来做列表底、输入框、二级区域。

`ReaderStage` 用 `#F0EAE1`，比页面白稍微暗一点，这样白色漫画页能被托出来。

`ReaderPageSurface` 用 `#FFFDF8`，接近温和纸白，不是纯白。这套主题下它可以比蓝色主题更“纸”。

文字用暖黑，不用纯黑。`TextPrimary` `#2D2924` 很关键，它让米色主题不脏、不硬。

`TextSecondary` `#756D62` 是暖灰棕，适合路径、副标题、格式说明。

`Accent` 用 `#7A6A45`，偏橄榄棕。这个颜色不会太“咖啡馆”，也不会太老式。

如果用绿色 `Accent`，米色主题会变成“护眼阅读器”；如果用橙色，会变廉价。所以棕橄榄是比较稳的选择。

这个主题的风险是最容易做脏。解决办法是：页面底色保持干净，文字不要太棕，边框不要太黄。

## 5. Shared Placement Rules

### App Shell

包括窗口内部背景、顶部栏、左侧主导航、状态区。

| Theme | Direction |
| --- | --- |
| Mist Green | 浅雾蓝绿，清爽、安静。 |
| Slate Blue | 浅冷灰蓝，清楚、现代。 |
| Night Graphite | 深石墨灰，低亮度。 |
| Warm Paper | 暖米灰，纸感但不泛黄。 |

对应 token：

- `BackgroundBase`
- `SurfacePanel`
- `SurfaceMuted`
- `BorderSubtle`

### Reader

包括漫画背后的舞台、页面背板、页面之间空隙。

| Theme | Direction |
| --- | --- |
| Mist Green | 轻微冷绿灰，不明显绿。 |
| Slate Blue | 冷中性灰，不明显蓝。 |
| Night Graphite | 深中性灰，不纯黑。 |
| Warm Paper | 暖灰纸色，不明显黄。 |

对应 token：

- `ReaderStage`
- `ReaderPageSurface`
- `BorderSubtle`

这里不要自由发挥太多。Reader 是最该克制的区域。

### Shelf

包括左侧书架列表、hover、当前阅读、定位高亮。

| Theme | Direction |
| --- | --- |
| Mist Green | 雾绿灰高亮，柔和但有层级。 |
| Slate Blue | 蓝灰高亮，清楚但安静。 |
| Night Graphite | 深青灰高亮，层级靠亮度差。 |
| Warm Paper | 暖米灰高亮，避免过黄。 |

对应 token：

- `ShelfHoverHighlight`
- `ShelfReadingHighlight`
- `ShelfNavigationHighlight`

层级应该是：

- `ShelfHoverHighlight` 最轻
- `ShelfReadingHighlight` 中等
- `ShelfNavigationHighlight` 最明显

### Text

浅色主题不是共用一个黑色，而是各自有倾向。

| Theme | Direction |
| --- | --- |
| Mist Green | 冷墨绿黑、绿灰。 |
| Slate Blue | 蓝黑、蓝灰。 |
| Warm Paper | 暖黑、暖灰。 |
| Night Graphite | 浅灰白、中灰。 |

对应 token：

- `TextPrimary`
- `TextSecondary`
- `TextDisabled`
- `TextOnAccent`
- `TextInverse`

不要用纯黑和纯白做主文字。纯黑在浅色主题里太硬，纯白在深色主题里太刺。

### Accent

`Accent` 只负责方向，不负责装饰。

| Theme | Direction |
| --- | --- |
| Mist Green | 低饱和雾绿。 |
| Slate Blue | 低饱和蓝。 |
| Night Graphite | 浅青蓝。 |
| Warm Paper | 橄榄棕。 |

对应 token：

- `Accent`
- `AccentHover`
- `TextOnAccent`

使用位置：

- 当前页进度
- 当前选中
- 主按钮
- active 状态
- hover 到重要操作

不要用在：

- `ReaderStage`
- 页面背板
- 大面积侧栏背景
- 普通文本
- 普通分隔线

## 6. User-Facing Names

如果要在设置里显示给用户，不要叫技术名。

### English

- Mist Green
- Slate Blue
- Warm Paper
- Night Graphite

### Chinese

- 雾绿
- 冷灰蓝
- 暖纸米色
- 夜间石墨

### Japanese

- ミストグリーン
- スレートブルー
- ウォームペーパー
- ナイトグラファイト

# 抽牌预览（Draw Peek）

战斗中将鼠标移到顶部计时器左侧的卡牌小图标上，按顺序查看下回合预计抽到的整组牌（通常为 5 张）。抽牌后自动刷新，支持中文卡名和升级标记。

## 安装前需要准备

| 项目 | 是否必需 | 说明 |
| --- | --- | --- |
| 《杀戮尖塔2》 | 必需 | 最低目标版本 v0.107.1；已通过该版本编译和兼容接口检查，尚未完成实机验证。v0.111.0 为此前实机测试环境，本次改动尚未在该版本回归。 |
| RitsuLib（Mod ID：STS2-RitsuLib） | 必需 | 最低版本 0.6.9，且须与游戏版本兼容。在 Steam 创意工坊订阅并在游戏中启用。DrawPeek 的设置页面和开关依赖它，未安装或未启用时无法正常加载。 |
| DrawPeek（抽牌预览） | 必需 | 安装并启用本模组；手动安装时，将 DrawPeek.dll 和 DrawPeek.json 一起放入游戏目录的 mods/DrawPeek 文件夹。 |
| RandomForeseer（随机数预测） | 可选 | 不安装也能使用顶部悬停抽牌预览和内置的基础地图奖励预测。已加载且接口兼容时，会优先使用它增强卡牌奖励预测。 |

建议顺序：先安装并启用 RitsuLib，再安装 DrawPeek，重启游戏后使用。

普通玩家直接使用编译好的模组文件即可，无需安装 .NET SDK、Godot 编辑器或额外下载 Harmony。只有自行修改、编译源码时才需要下方“环境”中列出的开发工具。

## 语言支持

模组界面根据游戏语言自动切换：中文语言使用简体中文，英文及其他语言使用英文。覆盖战斗抽牌预览、地图奖励提示、设置页标题与开关说明。卡牌、事件、药水和遗物名称继续使用游戏自身的本地化文本。

在游戏设置中切换语言后，战斗预览会自动刷新，已打开的地图预览会重新生成；设置页文案在重新打开或刷新页面时更新。无需单独设置模组语言。

## 使用

1. 从 Steam 启动游戏。
2. 打开 RitsuLib 的模组设置（Ctrl+Shift+F9）。
3. 选择“抽牌预览 (Draw Peek)”，切换“启用抽牌预览”。开关立即生效并自动保存。
4. 战斗中鼠标悬停图标自动展开；点击图标固定面板，移开鼠标也持续显示；再次点击取消固定，恢复悬停显示。打开暂停、设置及其他覆盖菜单时暂时隐藏预览，关闭菜单后恢复并保留固定状态。关闭模组会取消固定，重启游戏后默认不固定。

列表按当前抽牌数量修正效果估计下回合的抽牌数量，并显示抽牌堆中对应的有序卡名。抽牌堆不足的部分标注“待洗牌确定”。保留手牌、回合开始效果及继续出牌可能改变最终结果。地图预测使用独立随机数副本和遗物池副本，不执行真实抽牌、洗牌或领取奖励。

## 地图奖励预测

悬停未走过且当前可前往（闪动）的节点显示预测，移开收起；已走过节点保留原版历史提示。总开关同时控制抽牌和地图预览。

- 战斗：预测基础金币、药水和三张卡牌候选；精英另显示遗物。已内置基础卡牌候选预测，无需额外模组。加载 RandomForeseer 时优先使用其独立预测上下文；特殊奖励修改仍可能影响结果。
- 问号：预测房间类型；事件显示预计名称，事件选项的具体奖励仍需进入事件后查看，尚未实现地图上完整列出所有选项结果。
- 宝箱：预测公共遗物候选，不预定多人归属。新手首箱固定遗物和特殊开箱效果可能替换结果。
- 商店和休息处：显示用途，暂不预测库存或具体选项收益。

这些是基于当前状态的预测，不是锁定结果。未领取的前一场奖励、新手固定奖励、战斗结算触发、遗物奖励加成、事件替换和第三方模组可能影响结果；尚未穷举验证。每次预测核对随机数、概率、遗物池、事件与房间队列是否变化并写入日志。

## 更新记录 / Changelog

### 1.4.0

- 新增游戏 v0.107.1 兼容处理，最低版本调整为 0.107.1；需安装并启用支持对应游戏版本的 RitsuLib 0.6.9 或以上版本。
- 适配旧版随机数复制、药水奖励计算与战斗卡牌标记；保留新接口的运行时识别路径。
- v0.107.1 构建与兼容接口检查通过，900 次随机抽取验证通过；尚未实机验证奖励预测或回归 v0.111.0。
- Added compatibility handling for game v0.107.1 and lowered the minimum game version to 0.107.1. Requires RitsuLib 0.6.9 or later with support for the selected game version.
- Adapted RNG cloning, potion reward rolls, and combat card flags for older APIs while retaining runtime detection of newer APIs.
- Build and compatibility checks passed against v0.107.1, including 900 matching RNG draws. In-game reward predictions and v0.111.0 regression testing remain unverified.

### 1.3.2

- 修复顶栏卡牌图标被遮挡的问题。
- 打开暂停、设置及其他覆盖菜单时暂时隐藏抽牌图标和预览面板；关闭菜单后恢复显示，并保留点击固定状态。
- Fixed the top-bar card icon being obscured.
- The card icon and preview panel hide while pause, settings, or other modal screens are open. They return afterward with the pinned state preserved.

### 1.3.0

- 新增完整中英文界面，战斗抽牌预览、地图奖励提示和设置页文案自动跟随游戏语言切换；中文语言使用简体中文，英文及其他语言使用英文。
- 卡牌、事件、药水和遗物名称继续使用游戏自身的本地化文本。
- Added Chinese and English UI for combat draw previews, map reward hints, and settings. Text follows the game language: Chinese locales use Simplified Chinese; English and other locales use English.
- Card, event, potion, and relic names continue to use the game's own localization.

## 维护约定

每次修改项目内容，必须在同一次变更中同步更新本 README 与工程根目录的 `workshop.json` 简介，并将 `workshop.json` 同步至 `E:\software\ModUploader\DrawPeekWorkshop\workshop.json`。更新说明应覆盖中文和英文，准确记录功能、使用方式、依赖或兼容性变化；不影响玩家使用的维护变更也应简要注明。开发者执行细则见 `AGENTS.md`。

## 环境

当前构建基准为本机游戏 v0.107.1，依赖 RitsuLib 0.6.9。自行构建需要支持 net9.0 目标框架的 .NET SDK（本机使用 .NET SDK 10.0.203）、本机游戏程序集和 RitsuLib 程序集。无需 Godot 编辑器和 PCK。

```powershell
# 仅编译和打包
powershell -ExecutionPolicy Bypass -File .\build.ps1
# 关闭游戏后编译并安装
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install
# 关闭游戏后编译、安装并通过 Steam 启动
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Launch
```

脚本支持 -GameDir、-RitsuLibDir 和 -RitsuCompatVersion 覆盖路径及适配版本。默认引用 RitsuLib 的 compat/0.107.1 分支。构建时必须使用目标游戏版本的程序集；其他游戏版本需使用对应的 RitsuLib 适配分支并重新检查兼容性。

兼容接口检查：`pwsh -File .\tests\Verify-Compatibility.ps1`。此检查不启动游戏、不修改存档，不能替代实机验证。

安装目录：`E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\DrawPeek`，包含 DrawPeek.dll 和 DrawPeek.json。

关闭游戏后移除 mods\DrawPeek 可卸载。

## 文件与命名

- 稳定 Mod ID、程序集名、命名空间：DrawPeek。
- 显示名称：抽牌预览 (Draw Peek)，版本：1.4.0。
- Source/ModEntry.cs：入口、设置页面注册、日志。
- Source/Settings.cs：开关持久化。
- Source/CombatOverlay.cs：战斗悬浮界面、只读预览与生命周期。
- DrawPeek.json：名称、依赖、版本。

配置：`%APPDATA%\SlayTheSpire2\DrawPeek\settings.json`

日志：`%APPDATA%\SlayTheSpire2\DrawPeek\DrawPeek.log`

游戏日志：`%APPDATA%\SlayTheSpire2\logs\godot.log`

设置接口参考：[RitsuLib 官方文档](https://sts2-ritsulib.ritsukage.com/guide/mod-settings)。实际编译引用本机框架程序集。


## 上传创意工坊 / Workshop upload

启动 Steam 并登录发布账号后，双击 `E:\software\ModUploader\UploadDrawPeek.bat`。脚本上传同目录的 `DrawPeekWorkshop`，结束后显示退出码并用 `pause` 保留窗口。工程根目录保存脚本副本；实际使用时应放在 `ModUploader.exe` 旁边。本次仅添加上传快捷脚本，不改变模组功能。

Start Steam and sign in to the publishing account, then double-click `E:\software\ModUploader\UploadDrawPeek.bat`. It uploads the adjacent `DrawPeekWorkshop`, displays the exit code, and pauses before closing. A copy is kept in the project; place it beside `ModUploader.exe` to use it. This maintenance change only adds an upload shortcut and does not change mod behavior.

也可在上传工具目录手动执行 / Or run in the uploader directory:

```powershell
.\ModUploader.exe upload -w ".\DrawPeekWorkshop"
```

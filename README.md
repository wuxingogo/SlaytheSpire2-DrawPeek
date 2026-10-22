# 抽牌预览（Draw Peek）

战斗中将鼠标移到顶部计时器左侧的卡牌小图标上，按顺序查看下回合预计抽到的整组牌（通常为 5 张）。抽牌后自动刷新，支持中文卡名和升级标记。

## 安装前需要准备

| 项目 | 是否必需 | 说明 |
| --- | --- | --- |
| 《杀戮尖塔2》 | 必需 | 当前版本适配并测试于游戏 v0.111.0；其他版本需重新确认兼容性。 |
| RitsuLib（Mod ID：STS2-RitsuLib） | 必需 | 最低版本 0.6.9，且须与游戏版本兼容。在 Steam 创意工坊订阅并在游戏中启用。DrawPeek 的设置页面和开关依赖它，未安装或未启用时无法正常加载。 |
| DrawPeek（抽牌预览） | 必需 | 安装并启用本模组；手动安装时，将 DrawPeek.dll 和 DrawPeek.json 一起放入游戏目录的 mods/DrawPeek 文件夹。 |
| RandomForeseer（随机数预测） | 可选 | 不安装也能使用顶部悬停抽牌预览和内置的基础地图奖励预测。已加载且接口兼容时，会优先使用它增强卡牌奖励预测。 |

建议顺序：先安装并启用 RitsuLib，再安装 DrawPeek，重启游戏后使用。

普通玩家直接使用编译好的模组文件即可，无需安装 .NET SDK、Godot 编辑器或额外下载 Harmony。只有自行修改、编译源码时才需要下方“环境”中列出的开发工具。

## 使用

1. 从 Steam 启动游戏。
2. 打开 RitsuLib 的模组设置（Ctrl+Shift+F9）。
3. 选择“抽牌预览 (Draw Peek)”，切换“启用抽牌预览”。开关立即生效并自动保存。
4. 战斗中鼠标悬停图标自动展开；点击图标固定面板，移开鼠标也持续显示；再次点击取消固定，恢复悬停显示。关闭模组会取消固定，重启游戏后默认不固定。

列表按当前抽牌数量修正效果估计下回合的抽牌数量，并显示抽牌堆中对应的有序卡名。抽牌堆不足的部分标注“待洗牌确定”。保留手牌、回合开始效果及继续出牌可能改变最终结果。地图预测使用独立随机数副本和遗物池副本，不执行真实抽牌、洗牌或领取奖励。

## 地图奖励预测

悬停未走过且当前可前往（闪动）的节点显示预测，移开收起；已走过节点保留原版历史提示。总开关同时控制抽牌和地图预览。

- 战斗：预测基础金币、药水和三张卡牌候选；精英另显示遗物。已内置基础卡牌候选预测，无需额外模组。加载 RandomForeseer 时优先使用其独立预测上下文；特殊奖励修改仍可能影响结果。
- 问号：预测房间类型；事件显示预计名称，事件选项的具体奖励仍需进入事件后查看，尚未实现地图上完整列出所有选项结果。
- 宝箱：预测公共遗物候选，不预定多人归属。新手首箱固定遗物和特殊开箱效果可能替换结果。
- 商店和休息处：显示用途，暂不预测库存或具体选项收益。

这些是基于当前状态的预测，不是锁定结果。未领取的前一场奖励、新手固定奖励、战斗结算触发、遗物奖励加成、事件替换和第三方模组可能影响结果；尚未穷举验证。每次预测核对随机数、概率、遗物池、事件与房间队列是否变化并写入日志。

## 环境

本机适配游戏 v0.111.0，依赖 RitsuLib 0.6.9。自行构建需要支持 net9.0 目标框架的 .NET SDK（本机使用 .NET SDK 10.0.101）、本机游戏程序集和 RitsuLib 程序集。无需 Godot 编辑器和 PCK。

```powershell
# 仅编译和打包
powershell -ExecutionPolicy Bypass -File .\build.ps1
# 关闭游戏后编译并安装
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install
# 关闭游戏后编译、安装并通过 Steam 启动
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Launch
```

脚本支持 -GameDir 和 -RitsuLibDir 覆盖路径。框架引用 compat/0.111.0 分支；游戏或框架 API 更新后需重新检查兼容性。

安装目录：`G:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\DrawPeek`，包含 DrawPeek.dll 和 DrawPeek.json。

关闭游戏后移除 mods\DrawPeek 可卸载。

## 文件与命名

- 稳定 Mod ID、程序集名、命名空间：DrawPeek。
- 显示名称：抽牌预览 (Draw Peek)，版本：1.2.1。
- Source/ModEntry.cs：入口、设置页面注册、日志。
- Source/Settings.cs：开关持久化。
- Source/CombatOverlay.cs：战斗悬浮界面、只读预览与生命周期。
- DrawPeek.json：名称、依赖、版本。

配置：`%APPDATA%\SlayTheSpire2\DrawPeek\settings.json`

日志：`%APPDATA%\SlayTheSpire2\DrawPeek\DrawPeek.log`

游戏日志：`%APPDATA%\SlayTheSpire2\logs\godot.log`

设置接口参考：[RitsuLib 官方文档](https://sts2-ritsulib.ritsukage.com/guide/mod-settings)。实际编译引用本机框架程序集。

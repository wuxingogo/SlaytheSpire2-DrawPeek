# 抽牌预览（Draw Peek）

战斗中将鼠标移到顶部计时器左侧的卡牌小图标上，以单行横排牌面查看下一回合预计抽到的牌，从左到右为抽牌顺序。抽牌后自动刷新，牌面使用游戏本地化名称、费用、效果和升级状态。

## 安装前需要准备

| 项目 | 是否必需 | 说明 |
| --- | --- | --- |
| 《杀戮尖塔2》 | 必需 | 最低目标版本 v0.107.1；已通过该版本编译和兼容接口检查。 |
| RitsuLib | 不需要 | v2.0.0 起为独立版；其他模组可能仍需要。 |
| DrawPeek（抽牌预览） | 必需 | 安装并启用本模组；手动安装时，将 DrawPeek.dll 和 DrawPeek.json 一起放入游戏目录的 mods/DrawPeek 文件夹。 |
| RandomForeseer（随机数预测） | 可选 | 不安装也能使用顶部悬停抽牌预览和内置的基础地图奖励预测。已加载且接口兼容时，会优先使用它增强卡牌奖励预测。 |

独立版直接安装并启用 DrawPeek 即可。RandomForeseer 仍为可选增强，它自身的前置依赖需另行满足。

普通玩家直接使用编译好的模组文件即可，无需安装 .NET SDK、Godot 编辑器或额外下载 Harmony。只有自行修改、编译源码时才需要下方“环境”中列出的开发工具。

## 语言支持

模组界面根据游戏语言自动切换：中文语言使用简体中文，英文及其他语言使用英文。覆盖战斗抽牌预览、地图奖励提示、设置页标题与开关说明。卡牌、事件、药水和遗物名称继续使用游戏自身的本地化文本。

在游戏设置中切换语言后，战斗预览会自动刷新，已打开的地图预览会重新生成；设置页文案在重新打开或刷新页面时更新。无需单独设置模组语言。

## 使用

1. 从 Steam 启动游戏。
2. 打开游戏原生模组页面，点击右上方的抽牌预览设置。
3. 选择“抽牌预览 (Draw Peek)”，分别设置“启用抽牌预览”“启用地图预测”和“启用事件预测”。独立设置窗口沿用已有配置，开关独立生效并自动保存。
4. 战斗中鼠标悬停图标自动展开；点击图标固定面板，移开鼠标也持续显示；再次点击取消固定，恢复悬停显示。打开暂停、设置及其他覆盖菜单时暂时隐藏预览，关闭菜单后恢复并保留固定状态。关闭抽牌预览会取消固定，重启游戏后默认不固定。

## 战斗抽牌范围 / Combat draw scope

按下一回合预计抽牌数量显示，不再固定 5 张。当前抽牌堆不足时，用随机数副本预测弃牌堆洗牌；两组牌之间显示竖线，只有一组时不显示线。以现在结束回合为基础，将普通手牌计入未来弃牌堆，排除保留和虚无牌，并受剩余手牌容量限制。支持原版额外抽牌、抽牌减少、部分回合计数遗物与 PerfectFit；无法确定的洗牌位置显示无字卡背。抽牌和出牌结算期间保留上一帧，稳定后直接刷新。

Preview the expected next-turn draw count instead of a fixed five. When the draw pile runs short, predict the discard reshuffle using cloned RNG and separate the two groups with a vertical line; no line appears for a single group. Assume the turn ends now: ordinary hand cards enter the future discard pile, retained and Ethereal cards are excluded, and retained cards limit hand capacity. Supports native draw bonuses/reductions, supported turn-counter relics, and PerfectFit. Uncertain reshuffle slots use blank card backs. Keep the previous frame during resolution and refresh when settled.

预测基于当前状态，不会执行真实抽牌、回合末效果或修改真实随机数。继续出牌、敌方行动、抽牌触发、生成牌、选择效果、联机共享随机数和第三方模组可能改变结果；当前不完整模拟这些触发链。手牌中的特殊回合末效果及未知洗牌规则会使洗牌部分退回卡背。首次无缓存时等待稳定牌面，零抽牌或无牌可抽时收起。

Predictions do not execute real draws or turn-end effects or advance live RNG. Further plays, enemy actions, draw triggers, generated cards, choices, shared multiplayer RNG, and third-party effects can change the result and are not fully simulated. Special end-of-turn hand effects or unknown shuffle rules use backs for the reshuffled portion. First use waits for a stable frame; zero draws or exhausted piles hide the row.

## 独立开关 / Independent toggles

原生模组页面中的抽牌预览设置提供以下三个开关：

- 启用抽牌预览：控制战斗中当前牌堆的后续抽牌顺序及地图选点时的下一场基础起手预览。
- 启用地图预测：控制地图上可前往节点的房间、奖励及事件初始选项信息。
- 启用事件预测：控制进入事件后，按钮及变化选牌界面的随机升级、变化和受支持随机获牌预览。

三个开关互不依赖，自动保存，无需重启。新配置默认全部开启；旧配置中缺失的新开关沿用原开关状态，随后可单独调整。

The Draw Peek Settings button in the native Mods screen provides three independent toggles:

- Enable Draw Peek: the next cards from current combat piles and the next combat base opening hand on the map.
- Enable Map Predictions: room, reward, and initial event option information for reachable map nodes.
- Enable Event Predictions: random upgrade, transformation, and supported card reward previews on event buttons and in transformation selection screens.

Each toggle is saved independently without restarting. New configurations enable all three by default. Missing toggles in older configurations inherit the previous toggle state and can then be changed separately.

## 地图起手预览 / Map opening hand preview

地图选点时也显示顶部抽牌图标，悬停或点击固定可查看下一场战斗的基础起手牌；进入战斗后自动切回当前牌堆的后续抽牌顺序。支持原版初始洗牌、固有牌与沉底优先级、已支持遗物的起手数量修正，以及多人前序牌组的洗牌消耗。由“抽牌预览”开关控制，独立于地图节点预测。

先悬停普通、精英或首领节点，再移到图标，预览会保留最近悬停的可前往战斗节点类型；精英节点可计入 Booming Conch 的起手加成。关闭地图后清除节点目标。暂停、设置及覆盖菜单仍会隐藏图标和面板，并保留固定状态。

按当前牌组和洗牌状态计算，不推进真实随机数、不改变牌序。支持 Bag of Preparation、Ring of the Snake/Drake、Big Mushroom、Fiddle、Pael's Blood、Snecko Eye 的抽牌数量，以及 Pendulum、Pollinous Core 的计数规则。途中增删/变化卡牌、开场生成牌、自动打出、换牌、特殊洗牌与第三方效果未进行完整模拟，可能改变最终手牌。战斗中临时打开地图时仍显示当前战斗的当前牌堆的后续抽牌顺序。

The top-bar draw icon also appears while choosing map nodes. Hover or pin it to preview the next combat base opening hand; combat switches it back to the next cards from current piles. Handles the native initial shuffle, innate/bottom priority, supported relic draw modifiers, and preceding players sharing the shuffle stream. Controlled by Draw Peek independently of map node predictions.

Hover a reachable normal, elite, or boss node before moving to the icon to retain that target type; elite targets include Booming Conch's opening bonus. Closing the map clears the target. Pause, settings, and modal menus hide the overlay while preserving its pinned state.

Uses the current deck and a cloned shuffle RNG without changing live deck order. Supports draw counts from Bag of Preparation, Ring of the Snake/Drake, Big Mushroom, Fiddle, Pael's Blood, Snecko Eye, and the counters of Pendulum and Pollinous Core. Deck changes along the route, generated cards, auto-play, redraws, special shuffle rules, and third-party effects are not fully simulated and may change the hand. Opening the map during an ongoing combat still shows the next cards from the current combat piles.

## 事件卡牌结果预览 / Event card previews

新增“被寄生的自动机械”两个选项的基础卡牌奖励预测：悬停“研究”查看随机能力牌，悬停“触摸核心”查看随机 0 费牌。使用玩家奖励随机数副本，遵循角色已解锁卡池、联机限制和事件基础稀有度，不消耗真实随机数；由“启用事件预测”控制。额外奖励替换与第三方模组修改可能影响结果。

Added base card reward previews for Infested Automaton: hover Study to preview a random Power, or Touch Core to preview a random zero-cost card. Uses a cloned player rewards RNG with the character unlock pool, multiplayer restrictions, and event base rarity odds, without advancing live RNG. Controlled by Enable Event Predictions. Additional reward replacements and third-party modifications may affect the result.

悬停受支持的随机升级事件按钮，按钮侧边会用原版卡牌图像显示预测结果。卡名、牌面和升级状态跟随游戏语言，移开鼠标由原版悬浮提示自动收起，由“启用事件预测”独立控制。

- 随机升级：支持 Doors of Light and Dark 的 LIGHT、Endless Conveyor 的 OBSERVE_CHEF / SPICY_SNAPPY、Tablet of Truth 的 DECIPHER，以及 Reflections 的 TOUCH_A_MIRROR（依次展示降级、升级结果）。
- 手动选择目标的随机变化：点击事件选项进入选牌界面，悬停目标卡牌即可查看变化结果；多张变化按当前选择顺序计算。支持 Aroma of Chaos、Morphic Grove、Symbiote、Whispering Hollow、Endless Conveyor 与 Trial 的标准随机变化选项。只有一个合法目标时，也能直接在事件按钮旁预览。
- 需要玩家选择的目标不会提前当作随机目标猜测。先战斗再升级、第三方事件及第三方卡牌不在当前预测范围内；特殊奖励修改可能影响结果。

Hover a supported random-upgrade event button to see predicted card images beside it using the native hover UI. Card text and upgrade status follow the game language. Moving away closes the preview, and the Enable Event Predictions toggle controls this feature independently.

- Random upgrades: supports LIGHT in Doors of Light and Dark, OBSERVE_CHEF / SPICY_SNAPPY in Endless Conveyor, DECIPHER in Tablet of Truth, and TOUCH_A_MIRROR in Reflections (downgrades followed by upgrades).
- Player-selected random transforms: enter the card-selection screen and hover a target to preview its replacement. Multiple transforms follow the current selection order. Supports standard random-transform options in Aroma of Chaos, Morphic Grove, Symbiote, Whispering Hollow, Endless Conveyor, and Trial. When only one target is legal, its result can also appear beside the event button.
- Player choices are not guessed as random targets. Upgrades after combat, third-party events, and third-party cards are outside the current scope; special reward modifiers may affect results.

## 地图奖励预测

悬停未走过且当前可前往（闪动）的节点显示预测，移开收起；已走过节点保留原版历史提示。由“启用地图预测”独立控制，包含地图上的事件名称和初始选项说明。

- 战斗：预测基础金币、药水和三张卡牌候选；精英另显示遗物。已内置基础卡牌候选预测，无需额外模组。加载 RandomForeseer 时优先使用其独立预测上下文；特殊奖励修改仍可能影响结果。
- 问号：预测房间类型；事件显示预计名称、初始选项名称和效果说明。选项来自游戏本地化文本，不会提前执行事件或生成奖励。没有可读取文本的事件提示进入查看。
- 宝箱：预测公共遗物候选，不预定多人归属；没有奖励时显示“奖励：空箱子”。新手首箱固定遗物和特殊开箱效果可能替换结果。
- 商店和休息处：显示用途，暂不预测库存或具体选项收益。

这些是基于当前状态的预测，不是锁定结果。未领取的前一场奖励、新手固定奖励、战斗结算触发、遗物奖励加成、事件替换和第三方模组可能影响结果。每次预测核对随机数、概率、遗物池、事件与房间队列是否变化并写入日志。

## 更新记录 / Changelog

### 2.0.0

- 独立版无需 RitsuLib；在原生模组页面点击抽牌预览设置，保留三个独立开关、自动保存和旧配置。RandomForeseer 可选，其自身依赖需单独满足。
- Standalone: no RitsuLib required. Open Draw Peek Settings in the native Mods screen; all three toggles, automatic saving, and existing configuration are preserved. RandomForeseer remains optional with its own requirements.

### 1.9.0

- 恢复洗牌后抽牌预测，加入分组竖线；按下一回合预计抽牌数显示并考虑保留手牌上限。保持结算期间持续显示。
- Restored reshuffle previews with a vertical divider; use the expected next-turn draw count and retained-hand capacity while preserving continuous display during resolution.

### 1.8.2

- 暂停战斗重新洗牌预测，只显示当前抽牌堆中最多 5 张已确定的牌。牌堆不足时不补预测或卡背，空牌堆收起牌面；实际洗牌完成后恢复显示新牌序。
- Suspended combat reshuffle predictions. Show only up to five known cards in the current draw pile, without extra predictions or card backs. Empty piles hide the row; after a real shuffle, previews resume from the new order.

### 1.8.1

- 修复洗牌预览提前计入手牌的问题。战斗中展示当前真实抽牌堆与弃牌堆接下来的最多 5 张牌序；抽牌、洗牌及相关结算期间暂时隐藏牌面，完成后刷新，保留图标与固定状态。
- Fixed hand cards incorrectly entering reshuffle previews. Combat previews show up to five next cards from the current draw and discard piles. Card faces hide during draws, shuffles, and related resolution, then refresh; the icon and pin state remain.

### 1.8.0

- 抽牌堆为空或不足时，继续预览弃牌堆重新洗牌后的抽牌；计入普通回合末弃牌、保留/虚无及手牌上限，支持 PerfectFit 置顶。使用随机数副本；特殊回合末效果或不支持的洗牌规则仍显示卡背。
- When the draw pile is empty or short, previews continue into the reshuffled discard pile. Accounts for normal end-turn discards, Retain/Ethereal, hand capacity, and PerfectFit priority using cloned RNG. Unresolved special turn-end effects or unsupported shuffle rules keep card-back placeholders.

### 1.7.0

- 抽牌预览改为单行横排卡牌，按从左到右的抽牌顺序显示；移除标题、文字列表和附加说明，待洗牌位置显示无字卡背。牌多时自动缩放，保留悬停、固定及菜单隐藏，地图起手使用相同样式。
- Draw previews now show one horizontal row of card faces in left-to-right draw order, without headings, lists, or extra captions. Unknown shuffle slots use blank card backs. Cards scale to fit; hover, pinning, menu hiding, and map opening-hand previews share the same layout.

### 1.6.1

- 修复从战斗奖励界面打开地图时，隐藏在地图下方的奖励面板导致抽牌图标消失的问题。地图打开/关闭时立即刷新；暂停、设置等覆盖界面仍隐藏预览，并保留固定状态。
- Fixed the draw icon disappearing when the map opens above a reward screen retained in the overlay stack. Refreshes immediately on map open/close; pause and settings screens still hide the preview while preserving its pinned state.

### 1.6.0

- 地图选点时也显示顶部抽牌图标，悬停或点击固定可查看下一场战斗的基础起手牌；进入战斗后自动切回下回合抽牌。支持原版初始洗牌、固有牌与沉底优先级、已支持遗物的起手数量修正，以及多人前序牌组的洗牌消耗。由“抽牌预览”开关控制，独立于地图节点预测。
- The top-bar draw icon also appears while choosing map nodes. Hover or pin it to preview the next combat base opening hand; combat switches it back to next-turn draws. Handles the native initial shuffle, innate/bottom priority, supported relic draw modifiers, and preceding players sharing the shuffle stream. Controlled by Draw Peek independently of map node predictions.

### 1.5.2

- 新增“被寄生的自动机械”两个选项的基础卡牌奖励预测：悬停“研究”查看随机能力牌，悬停“触摸核心”查看随机 0 费牌。使用玩家奖励随机数副本，遵循角色已解锁卡池、联机限制和事件基础稀有度，不消耗真实随机数；由“启用事件预测”控制。额外奖励替换与第三方模组修改可能影响结果。
- Added base card reward previews for Infested Automaton: hover Study to preview a random Power, or Touch Core to preview a random zero-cost card. Uses a cloned player rewards RNG with the character unlock pool, multiplayer restrictions, and event base rarity odds, without advancing live RNG. Controlled by Enable Event Predictions. Additional reward replacements and third-party modifications may affect the result.

### 1.5.1

- 新增地图预测和事件预测独立开关；原抽牌预览开关只控制战斗抽牌。两个设置入口共用配置，支持中英文、自动保存及旧配置迁移。
- Added independent map and event prediction toggles. The existing draw toggle now controls combat draws only. Both settings entry points share localized controls, saved preferences, and migration of older settings.

### 1.5.0

- 新增事件按钮侧边的随机升级卡牌图像预览，以及选牌界面悬停目标的随机变化结果预览，支持中英文和多张变化顺序。
- 使用随机数副本与未登记进对局的卡牌副本；不执行事件选项、真实升级、变化或奖励命令。
- Added native card-image previews for supported random-upgrade event buttons and random-transform targets in the selection screen, including localization and multi-card selection order.
- Uses cloned RNG and unregistered preview cards without executing event choices or live upgrade, transform, or reward commands.

### 1.4.2

- 删除事件选项预览底部的问号和可选项说明；空宝箱显示“奖励：空箱子”。
- Removed the question-mark and choice disclaimer beneath event options. Empty chests now show “Reward: Empty chest”.

### 1.4.1

- 地图事件预测新增初始选项名称和效果说明，自动跟随游戏语言。
- 预览只读本地化文本，不执行事件初始化、选项生成或奖励流程。无法读取时保留事件名称并提示进入查看。
- Map event previews now include initial option names and effect descriptions in the game language.
- Previews read localization text only, without initializing events, generating options, or awarding rewards. If option text is unavailable, the event name remains visible with a prompt to enter it.

### 1.4.0

- 新增游戏 v0.107.1 兼容处理，最低版本调整为 0.107.1；需安装并启用支持对应游戏版本的 RitsuLib 0.6.9 或以上版本。
- 适配旧版随机数复制、药水奖励计算与战斗卡牌标记；保留新接口的运行时识别路径。
- v0.107.1 构建与兼容接口检查通过，900 次随机抽取验证通过。
- Added compatibility handling for game v0.107.1 and lowered the minimum game version to 0.107.1. Requires RitsuLib 0.6.9 or later with support for the selected game version.
- Adapted RNG cloning, potion reward rolls, and combat card flags for older APIs while retaining runtime detection of newer APIs.
- Build and compatibility checks passed against v0.107.1, including 900 matching RNG draws.

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

每次修改项目内容，必须在同一次变更中同步更新本 README 与工程根目录的 `workshop.json` 简介，并将 `workshop.json` 同步至 `G:\project\SlaytheSpire2Demo\ModUploader\DrawPeekWorkshop\workshop.json`。更新说明应覆盖中文和英文，准确记录功能、使用方式、依赖或兼容性变化；不影响玩家使用的维护变更也应简要注明。开发者执行细则见 `AGENTS.md`。

## 环境

当前本机构建基准为游戏 v0.111.0，无需 RitsuLib。自行构建需要支持 net9.0 目标框架的 .NET SDK（本机使用 .NET SDK 10.0.203）和本机游戏程序集。无需 Godot 编辑器和 PCK。

```powershell
# 编译、打包并同步工坊上传目录
powershell -ExecutionPolicy Bypass -File .\build.ps1
# 关闭游戏后编译并安装
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install
# 关闭游戏后编译、安装并通过 Steam 启动
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Launch
```

脚本支持 -GameDir 和 -WorkshopDir 指定游戏及发布目录。独立版不需要 RitsuLib 编译引用。其他游戏版本应重新检查兼容性。

兼容接口检查：`pwsh -File .\tests\Verify-Compatibility.ps1`。此检查不启动游戏、不修改存档，不能替代实机验证。

安装目录：`G:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\DrawPeek`，包含 DrawPeek.dll 和 DrawPeek.json。

关闭游戏后移除 mods\DrawPeek 可卸载。

## 文件与命名

- 稳定 Mod ID、程序集名、命名空间：DrawPeek。
- 显示名称：抽牌预览 (Draw Peek)，版本：2.0.0。
- Source/ModEntry.cs：入口、设置页面注册、日志。
- Source/Settings.cs：开关持久化。
- Source/CombatOverlay.cs：战斗悬浮界面、只读预览与生命周期。
- DrawPeek.json：名称、依赖、版本。

配置：`%APPDATA%\SlayTheSpire2\DrawPeek\settings.json`

日志：`%APPDATA%\SlayTheSpire2\DrawPeek\DrawPeek.log`

游戏日志：`%APPDATA%\SlayTheSpire2\logs\godot.log`

设置窗口直接使用游戏自带 Godot 控件；语言直接读取游戏本地化管理器。


## 上传创意工坊 / Workshop upload

v1.5.2 发布配置修正：精简中英文工坊简介，保留功能说明及历次更新记录，避免描述超过 Steam 的 8000 字节限制。按解析后的 `description` 的 UTF-8 字节数计算，中文字符通常占多个字节，并非 JSON 文件大小或字符数。上传 BAT 会先检查 JSON 和简介长度，达到 8000 字节时停止上传并保留窗口；请先精简简介再重试。本次仅调整发布文案与上传检查，模组版本保持 1.5.2。

v1.5.2 publishing correction: condensed the bilingual Workshop description while retaining feature notes and release history to fit Steam's 8000-byte limit. Measure the parsed description in UTF-8 bytes, not JSON file size or character count. The upload BAT validates JSON and stops at 8000 bytes or more, leaving the window open. Shorten the description before retrying. This changes publishing copy and validation only; the mod remains v1.5.2.

接口依据 / API reference: [Steamworks SetItemDescription](https://partner.steamgames.com/doc/api/ISteamUGC#SetItemDescription).

工坊配置默认可见性为公开（`public`），下次成功上传后应用到 Steam 条目。

Workshop visibility defaults to public (`public`) and is applied to the Steam item on the next successful upload.

启动 Steam 并登录发布账号后，双击 `G:\project\SlaytheSpire2Demo\ModUploader\UploadDrawPeek.bat`。脚本上传同目录的 `DrawPeekWorkshop`，结束后显示退出码并用 `pause` 保留窗口。工程根目录保存脚本副本；实际使用时应放在 `ModUploader.exe` 旁边。

Start Steam and sign in to the publishing account, then double-click `G:\project\SlaytheSpire2Demo\ModUploader\UploadDrawPeek.bat`. It uploads the adjacent `DrawPeekWorkshop`, displays the exit code, and pauses before closing. A copy is kept in the project; place it beside `ModUploader.exe` to use it.

也可在上传工具目录手动执行 / Or run in the uploader directory:

```powershell
.\ModUploader.exe upload -w ".\DrawPeekWorkshop"
```


工坊文案已精简，移除上传脚本维护说明及待实机验证措辞；实际检查记录保留在 `VALIDATION.md`。

Workshop copy has been streamlined by removing upload-script maintenance notes and pending in-game validation wording. Check records remain in `VALIDATION.md`.

## 发布目录同步 / Release workspace synchronization

每次更新后运行 `powershell -ExecutionPolicy Bypass -File .\build.ps1`。成功构建后自动将 DLL、模组 JSON 和工坊配置同步到相邻的 `..\ModUploader\DrawPeekWorkshop`，校验版本与 SHA-256，并检查简介小于 8000 UTF-8 字节。可用 `-WorkshopDir` 指定其他已有上传工作区；缺失工作区或校验失败会报错，不会显示同步成功。直接执行 `dotnet build` 不触发同步。

工坊简介和更新说明以工程根目录 `workshop.json` 为准；标题、可见性、标签、依赖及其他字段保留上传目录里的设置，并回写工程配置。现有 `mod_id.txt`、封面和截图保留，继续更新原条目。构建不会上传 Steam；准备好后双击上传工具目录的 `UploadDrawPeek.bat`。本次为发布流程维护，模组版本保持 1.8.2。

Run `build.ps1` after each update to build, package, and synchronize DLL/manifest/Workshop configuration to the sibling `ModUploader/DrawPeekWorkshop` workspace. Versions, SHA-256 hashes, and the description size are checked. Override the existing workspace with `-WorkshopDir`. Missing workspaces or failed checks stop synchronization. Direct `dotnet build` does not synchronize.

Release text comes from the project's `workshop.json`; publisher-managed fields are preserved from the upload workspace and written back to the project. The existing item ID, cover, and screenshots remain intact. Builds never upload to Steam: run the uploader's `UploadDrawPeek.bat` separately when ready. This publishing workflow maintenance retains mod version 1.8.2.

## 1.8.3

- 修复抽牌或从抽牌堆直接打出牌时预览闪烁：结算期间保留已有牌面，结束后直接刷新。首次打开且尚无稳定牌面时等待结算；新战斗不沿用旧牌面。结算后空牌堆仍收起。
- Fixed preview flicker during draws and plays from the draw pile: retain existing faces during resolution, then refresh in place. First use waits for a stable preview; new combats never reuse old faces. Settled empty piles still hide the row.


## 安装偏好 / Installation preference

更新默认只编译、检查并同步工坊上传目录，不自动安装到游戏目录。只有用户明确要求安装时才执行安装；退出游戏不代表授权安装。

Updates only build, validate, and synchronize the Workshop upload workspace by default. Installation into the game requires an explicit user request; closing the game does not authorize installation.

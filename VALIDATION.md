# 验证记录

日期：2026-10-22。环境：Windows，Slay the Spire 2 v0.111.0，RitsuLib 0.6.9。

## 已完成

- Release 构建：0 警告、0 错误。
- 多次通过 Steam 启动；游戏日志确认 DrawPeek 初始化成功。
- RitsuLib 设置页可见，中文名称、说明和启用开关正常。
- 关闭开关写入 settings.json；重启游戏后仍为关闭。
- 战斗内启用，无需重启即可显示按钮。
- 展开显示“下一张 · 打击”，中文字体正常。
- 正常出牌不受影响；结束回合抽牌后，剩余数量从 8 变为 3，预览同步更新。
- 预览展开时关闭功能，按钮和面板一起隐藏；重新启用正常。
- 最终安装 DLL/JSON 与 dist 构建产物哈希一致。

## 范围

测试的是单人铁甲战士战斗；未穷举其他角色、联机、第三方特殊抽牌效果或所有分辨率。
空牌堆显示“等待洗牌”而不预测洗牌结果，满手牌显示提示；这些分支经过代码检查，尚未在本次实机流程单独覆盖。

测试前的 profile1 备份在 .legacy/profile1-before-validation-20261022-213046。
验证抽牌后退出游戏，比较 current_run.save，变化仅为 num_reloads（重新载入次数）；对局进度字段未变化。

## v1.1.0 整组预览（2026-10-22）

- Release 构建 0 警告、0 错误，已安装并通过 Steam 启动。
- 实机展开显示“下回合抽牌 · 预计 5 张”，按顺序列出打击、打击、打击、防御、痛击；中文和布局正常。
- 安装 DLL 与 dist 文件 SHA256 一致。
- 使用游戏当前 ModifyHandDraw 数量计算接口；不会调用抽牌或洗牌操作。
- 抽牌堆不足部分显示“待洗牌确定”。回合开始效果、保留、阻止抽牌等情况仍可能改变结果，界面已注明估计性质。本次未穷举特殊效果或重复结束回合验证。
- 上述 v1.0 的“下一张”和满手牌提示说明仅适用于旧版本，v1.1 已改为下回合整组列表。

## v1.1.1 悬停交互

移除点击开关，改为按钮 MouseEntered 即时展开，定时检查鼠标离开后收起；展开时按钮与面板之间的空隙也包含在区域内。Release 构建 0 警告、0 错误；已安装并重启游戏。悬停交互尚未实机逐项验证。

## v1.2.1 固定面板与精简文字

- 图标使用切换按钮：点击固定，再点恢复悬停；关闭模组取消固定。
- 删除抽牌底部估计说明，以及地图预测中的通用说明，保留奖励信息和不可用状态。
- Release 构建 0 警告、0 错误，安装 DLL 与构建产物一致。
- 本次尚未实机验证点击固定/取消固定。


## v1.3.0 中英文界面（2026-10-23）

- 所有模组自有战斗、地图和设置页文案增加中英文，根据 RitsuLib 解析的当前游戏语言动态选择。中文语言码 zhs/zht 使用简体中文，其他语言回退英文。
- 卡牌、事件、药水和遗物名称继续使用游戏本地化接口。
- 编译后程序集在独立进程中以模拟 LocManager 单例验证 eng → zhs → zht → fra → eng → zhs 文案选择与回退，全部通过；未修改真实游戏设置或存档。
- Release 构建 0 警告、0 错误；本次未实机验证中英文界面布局或设置页切换。

## v1.3.1 暂停菜单层级（2026-10-23）

- 移除抽牌预览根节点的 ZIndex=100，改为全局 UI 的第一个子节点，遵循原版菜单和遮罩的绘制与输入顺序，固定状态保持不变。
- Release 构建成功，0 警告、0 错误。尚未实机验证暂停、设置及恢复游戏的显示和输入。
- 新增 in_game3、ingame4 的 JPEG 工坊预览副本，保留原 PNG。

## v1.3.2 图标恢复与菜单遮挡（2026-10-23）

- 已从原版 run.tscn 和 top_bar.tscn 确认：将预览置于 GlobalUi 首个子节点，会被随后绘制的 TopBar 背景遮住；撤销 v1.3.1 的该调整。
- 恢复 ZIndex=100，暂停/设置子菜单、capstone 或 overlay 屏幕打开时隐藏整个预览，关闭后恢复；不清除 ButtonPressed 固定状态。
- 订阅原版菜单变化信号即时刷新，并在退出场景时解绑，保留定时刷新兜底。
- 使用本机游戏程序集编译成功：0 警告、0 错误。本次尚未实机验证图标、菜单与固定状态的交互。


## v1.4.0 游戏 0.107.1 适配（2026-10-23）

- 本机 release_info.json 确认当前游戏为 v0.107.1，改用 RitsuLib compat/0.107.1 引用。
- 原代码直接编译报 7 个错误：Rng.ToSerializable 缺失、PotionRewardOdds.Roll 多一个 AscensionManager 参数、IsFromCombat 标记缺失；已在 GameCompat 中兼容。
- Release 构建 0 警告、0 错误。tests/Verify-Compatibility.ps1 使用真实旧版游戏程序集验证 9 组 seed/counter，共 900 次序列对比，副本生成不推进原始 RNG；旧药水接口及 AscensionManager 创建检查通过。
- 最低游戏版本设为 0.107.1。保留新版本接口运行时分支，但当前机器缺少 0.111.0 程序集，未完成本次新版本回归。
- 未启动游戏、未修改游戏安装目录或存档；0.107.1 界面、暂停交互和奖励预测仍待实机验证。


## v1.4.1 事件初始选项预览（2026-10-23）

- 读取 EventModel.InitialDescription 的初始页面键，从对应 LocTable 提取初始选项标题及描述；不调用 BeginEvent、GenerateInitialOptions、CalculateVars 或选项回调，不修改事件 Owner/Rng。
- 去除游戏富文本标记，动态或嵌套 SmartFormat 参数以 ? 显示；静态初始候选不代表当前状态一定可选，界面包含说明。
- tests/Verify-EventOptions.ps1 使用本机 PCK 提取的 eng/zhs 事件表（.inspect 下，不纳入发布），两种语言各覆盖 57 个事件、147 个初始选项，包含无事件回退及嵌套变量清理检查，全部通过。
- 0.107.1 Release 编译 0 警告、0 错误。尚未实机验证事件面板布局、第三方事件或特殊初始页面。


## v1.4.2 事件说明与空宝箱（2026-10-23）

- 移除事件选项底部关于问号与实际可选项的提示；动态参数占位处理保留。
- 对照本机 0.107.1 的 TreasureRoomRelicSynchronizer.BeginRelicPicking：Hook.ShouldGenerateTreasure 为所有玩家返回 false 时不生成宝箱遗物；预览在该分支显示中英文空箱子奖励。保留非空遗物池耗尽时的原版 FallbackRelic 行为。
- 本次未实机验证空宝箱分支。


## v1.5.0 事件卡牌图像预览（2026-10-23）

- 本次本机游戏已切回 v0.111.0（release_info.json），使用对应 RitsuLib 适配分支构建，0 警告、0 错误；本次未重新验证 0.107.1。
- 对照原版事件代码实现 Doors/Conveyor/Tablet/Reflections 随机升级顺序；先战斗后奖励不作提前预测。手动随机变化白名单仅支持默认变化池，不执行 CardFactory.CreateRandomCardForTransform（会注册进 CardScope），而是只读池选取并生成未注册副本。
- 在 NHoverTipSet.CreateAndShow 的 IEnumerable 重载中仅替换方法局部参数，不修改 EventOption.HoverTips，不使用额外 UI 层级。保留原有悬浮信息和收起机制。
- tests/Verify-EventCardPrediction.ps1 使用真实游戏模型与内存中独立测试牌组，验证 Doors、Conveyor 两选项、Reflections 共四分支的结果数量、重复预测稳定性、真实 RNG 状态和升级等级不变；另验证变化副本所有者/升级状态/未加入牌组、白名单回退及悬浮接口签名。全部通过，不修改存档或真实游戏设置。
- PowerShell 宿主的 CoreCLR 10.0.11 不在本机 Harmony 支持范围，无法在该宿主执行原版 UI 方法补丁；仅验证目标签名，不将其视为游戏内补丁或 UI 验证。游戏实际使用其自身运行时。
- 尚未实机验证卡牌布局、鼠标/手柄交互、变化顺序和特殊奖励修改。第三方卡牌及事件不执行预测。

## v1.5.1 独立预测开关（2026-10-23）

- 在现有 RitsuLib 注册设置页增加地图预测与事件预测开关，分别接入地图显示/隐藏和事件卡牌悬浮入口；原 Enabled 只控制战斗抽牌。
- 使用隔离临时目录检查实际 Settings.cs 的保存/加载逻辑（仅替换配置目录并使用日志桩）：新配置默认值、旧 Enabled 开/关迁移、全部 8 种组合保存与重载、部分缺失字段迁移通过。未修改玩家设置。
- 使用本机 v0.111.0 游戏程序集与 RitsuLib compat/0.111.0 完成 Release 构建，0 错误；2 条 NU1900 警告来自无法连接 NuGet 漏洞数据服务，并非编译错误。
- 本次未实机验证两个设置入口、地图隐藏和事件悬浮交互；未重新验证 v0.107.1。
- Independent draw, map, and event controls passed isolated migration and persistence checks for all eight combinations. Release build succeeded against the local v0.111.0 assemblies; live UI interaction and v0.107.1 were not revalidated.

## v1.5.2 被寄生的自动机械奖励预测（2026-10-23）

- 原因：此前事件预测白名单只覆盖部分升级/变化选项，没有 INFESTED_AUTOMATON 的 STUDY 与 TOUCH_CORE。
- 对照本机 v0.111.0 的 InfestedAutomaton、CardCreationOptions、CardFactory 与 PlayerOddsSet：这两个选项使用玩家 Rewards 随机流而非事件 RNG；先按事件基础稀有度抽取，再从角色已解锁且符合联机限制的能力牌/原始耗能为 0 且非 X 费卡池中取牌；NoUpgradeRoll 不进行随机升级。
- 新增基础奖励预览，不执行 CardFactory.CreateForReward、获牌命令或奖励修改回调，仅创建未登记的卡牌副本。额外奖励替换及第三方修改不在此预测范围，README 和工坊简介已说明。
- tests/Verify-EventRewardPrediction.ps1 使用真实游戏卡牌和原版私有 RollForRarity + NextItem 作为对照，400 组种子/已推进状态/卡池/稀有度偏移组合通过；覆盖缺少普通稀有度、重复悬停、真实随机数不变及空/无合法稀有度卡池。已有事件卡牌预测回归检查通过。
- v0.111.0 / RitsuLib compat/0.111.0 Release 构建成功，0 错误；2 条 NU1900 警告为 NuGet 漏洞检查服务连接失败。本次未实机验证悬浮图像和获牌对照，未重新验证 0.107.1。
- Added base reward previews for both Infested Automaton options. Four hundred comparisons with native rarity/card selection passed without advancing live RNG. Build and existing event prediction regression checks passed; live UI/reward behavior and 0.107.1 were not revalidated.

## v1.5.2 上传参数错误排查（2026-10-23）

- 用户上传日志显示 17:55:32 与 17:56:22 在提交阶段返回 Invalid Parameter；此前内容及五张图片均完成传输。
- 发布配置可正常解析，description 为 5787 字符、8123 个 UTF-8 字节，超过 Steamworks k_cchPublishedDocumentDescriptionMax = 8000 字节。标题为 24 字节，changeNote 为 280 字节，所有图片均小于 1 MiB。
- 将简介压缩至 5568 字节，保留双语功能说明和 v1.3.0 至 v1.5.2 更新记录。发布内容版本保持 1.5.2。
- UploadDrawPeek.bat 新增 JSON/UTF-8 简介长度检查；实际调用 BAT 内相同 PowerShell 命令验证当前简介通过、原 8123 字节简介拒绝、7999/8000 字节边界和非法 JSON 正确处理，未执行上传。
- Steam 日志未明确指出具体字段；简介超限为已确认的参数违规，修正后的服务器提交结果仍需重新上传确认。
- Confirmed an oversized description (8123 UTF-8 bytes) after Invalid Parameter failures at commit. Condensed to 5568 bytes and verified the BAT preflight boundary/error handling. No mod binary changed and no Steam upload was performed.

## v1.6.0 地图下一场起手预览（2026-10-23）

- 扩展现有顶部图标：非战斗、地图打开且可旅行时显示下一场基础起手；战斗中仍显示当前战斗的下回合抽牌。保留固定模式、暂停/设置/覆盖菜单隐藏逻辑与独立抽牌开关。
- 对照 v0.111.0 Player.PopulateCombatState、CardPile.RandomizeOrderInternal、CombatManager.SetupPlayerTurn：使用初始 UnstableShuffle；复制共享 Shuffle RNG，并按玩家顺序重放前序牌组的洗牌消耗。先移动沉底附魔，再逐张 MoveToTop 对应的固有反序；不改变原始牌序或 RNG。
- 按只读规则计算已支持遗物的第一回合抽牌量，包括准备背包、蛇/龙之戒、蘑菇、Fiddle、Pael's Blood、Snecko Eye、Pendulum、Pollinous Core；最近悬停的可前往精英节点支持 Booming Conch。关闭地图清除目标，不依赖地图奖励开关。
- tests/Verify-OpeningHand.ps1 与原版 UnstableShuffle、CardPile.MoveToBottomInternal/MoveToTopInternal 对照 600 组，覆盖空/单牌、正常牌组、固有反序、沉底与固有冲突、多名前序玩家、已推进随机状态、重复预测，以及真实牌序和 RNG 不变。已支持起手数量与原版第一回合规则对照通过，另验证精英分支与手牌上限。
- 仅预测当前牌组的基础起手；不执行战斗创建、抽牌命令或真实战斗钩子。开场生成牌、自动打出、换牌、特殊洗牌及第三方效果未完整模拟，界面/双语说明标明基础结果可能变化。
- v0.111.0 + RitsuLib compat/0.111.0 Release 构建成功，0 错误；2 条 NU1900 警告来自 NuGet 漏洞服务不可达。本次未实机验证地图图标、固定状态、菜单及节点切换，也未重新验证 0.107.1。
- Workshop description 为 6582 UTF-8 字节，低于上传限制。
- Added a map opening-hand mode with isolated initial shuffle and supported first-turn draw rules. Six hundred native order comparisons and relic count checks passed. Build succeeded; live UI and v0.107.1 were not revalidated.

## v1.6.1 地图图标间歇隐藏修复（2026-10-23）

- 玩家日志确认已加载 v1.6.0 且抽牌开关开启；可读取到地图起手成功记录，没有对应的 Preview update failed。旧日志没有逐次可见性原因，因此不能从日志还原每次隐藏时的具体状态。
- 对照 NOverlayStack：ScreenCount 返回栈内面板数量；地图 Opened 调用 HideOverlays，只隐藏栈顶奖励屏幕而不清空栈，StackIsCovered 在地图打开时为 true。v1.6.0 的 ScreenCount == 0 条件会误把地图下方保留的奖励屏幕当作前台遮挡。
- 地图打开且实际可见时不再因后台 overlay 数量隐藏抽牌图标；仍保留抽牌开关、战斗/可选点地图、旅行状态以及暂停/设置等 capstone/submenu 屏蔽。非地图场景继续使用 overlay 屏蔽。
- 订阅地图 Opened/Closed 信号即时刷新，在根节点 TreeExiting 解绑；新增仅状态变化时记录的可见性日志，便于区分旅行锁定、前台菜单和其他隐藏原因。
- v0.111.0 + RitsuLib compat/0.111.0 Release 构建成功，0 错误，NuGet 漏洞服务连接失败产生 2 条 NU1900 警告。发布配置 JSON 与 7057 字节简介检查通过。未实机验证奖励后开图和菜单切换，也未重新验证 v0.107.1。
- Fixed a visibility gate that counted reward screens hidden beneath the map as foreground blockers. Map open/close now refreshes immediately, with transition-only visibility diagnostics. Build and publishing metadata checks passed; live UI behavior remains unverified.

## v1.7.0 单行抽牌牌面（2026-10-23）

- 将战斗下回合与地图基础起手从文字列表改为单行横排原生卡牌牌面，按抽牌顺序从左到右显示；无面板标题、序号及附加说明。待洗牌位置为无字卡背，空结果隐藏面板。
- 复用原版 card_hover_tip.tscn / NCard，卡牌使用 MutableClone；原版渲染会清理/更新 DynamicVars，因此不将真实牌组中的模型直接交给渲染器。重复牌保留独立位置，不登记新牌、不执行抽牌命令。视图节点复用并刷新副本，鼠标与焦点输入禁用。
- 布局只使用一行，按可用宽高压缩间距和等比缩放，并将面板位置限制在界面内；保留悬停、固定、地图奖励界面隐藏修复与暂停菜单遮挡逻辑。
- v0.111.0 + RitsuLib compat/0.111.0 Release 编译通过，0 错误、2 条 NU1900 警告（漏洞检查服务无法连接）。既有起手预测回归测试通过：600 组原版洗牌/牌序对照及遗物抽牌数量、精英分支、手牌上限检查。
- 工坊简介 7474 UTF-8 字节，小于 8000 字节；版本统一为 1.7.0。当前无法在离线测试进程中运行 Godot 原生 UI，横排布局、窗口缩放、鼠标操作和游戏内语言切换尚未实机验证；未重新验证 v0.107.1。
- Replaced text previews with one horizontal row of native card faces backed by cloned models, with blank backs for unknown shuffle slots. Release build and 600 native opening-order comparisons plus draw-count regression checks passed. Live Godot UI, resizing, interactions, language switching, and v0.107.1 were not revalidated.

## v1.8.0 战斗重新洗牌预测（2026-10-23）

- 对照 v0.111.0 CardPileCmd.Draw/Shuffle：抽牌堆用尽后，将弃牌堆副本按原版 StableShuffle（先 CompareTo 排序，再 Fisher-Yates）和复制的共享 Shuffle RNG 排列，接到剩余抽牌堆后。与开局 UnstableShuffle 区分；本次已抽到手里的牌不再次加入洗牌池，两堆耗尽时停止。
- 按“现在结束回合”的基础状态计入普通手牌弃置，排除虚无牌、保留牌，并按保留数量限制手牌空间；原版 RunicPyramid、RingingTriangle、RetainHandPower、WellLaidPlansPower 仅调用已核对的只读 ShouldFlush。敌方回合不再次模拟手牌弃置。
- 支持 PerfectFit，按预测时未来弃牌堆的监听顺序逐张置顶；不执行真实 ModifyShuffleOrder、回合末、抽牌或奖励命令。不支持的洗牌/弃牌/虚无规则以及有特殊回合末效果的手牌保留未知卡背，等待状态刷新。
- tests/Verify-CombatReshuffle.ps1：480 组与原版 StableShuffle 和 PerfectFit.ModifyShuffleOrder 对照，覆盖空/不足/充足抽牌堆、空/单牌/大弃牌堆、重复牌、已推进随机数、数量截断、重复预览、真实 RNG 与牌序不变；普通/保留/虚无/固有、全手保留、敌方回合、Burn 与未知卡背分支检查通过。
- 原有起手预测 600 组洗牌/牌序及遗物抽牌数量回归检查通过。v0.111.0 + RitsuLib compat/0.111.0 Release 构建 0 错误、2 条 NU1900 警告（NuGet 漏洞数据服务不可达）。工坊简介 7811 UTF-8 字节，版本统一为 1.8.0。
- 预测不会完整模拟敌方、开场或抽牌触发链、其他玩家后续消耗共享随机数、第三方效果；玩家继续出牌亦会改变当前预测。尚未实机对照下一回合抽牌和 UI，也未重新验证 v0.107.1。
- Added base next-turn discard reshuffle prediction with native stable shuffling, cloned RNG, normal hand flushing, Retain/Ethereal, hand capacity, and PerfectFit. All 480 reshuffle comparisons and 600 opening-hand regression comparisons passed. Build succeeded; live UI/gameplay and v0.107.1 remain unverified. Future effects and shared multiplayer RNG consumption are outside the simulation.

## v1.8.1 当前牌堆与异步刷新修复（2026-10-23）

- 根因：v1.8.0 仅按 CurrentSide=Player 假设结束回合，提前把手牌混入洗牌池；Start 阶段也属于玩家方，自动抽牌中途会把新入手的牌再次纳入预测。旧测试只覆盖给定牌池的 Predict 和独立 ProjectHand，未覆盖运行时选池路径。
- 删除回合末手牌投影，Build 直接走可测试的 BuildFromPiles，仅传入真实 DrawPile/DiscardPile，保留原版 StableShuffle 和 PerfectFit 规则。预览改为接下来的最多 5 张牌序，不再用 ModifyHandDraw 或假设未来手牌数量；实际效果抽牌数量与游戏手牌上限由游戏决定。
- 在 CardPileCmd.Draw(decimal, Player, bool) 与 Shuffle 的同步入口开始计数，包装其 Task 到完整结束时才在 finally 释放；嵌套、取消、异步异常和同步异常均保持计数平衡。只读刷新保护另覆盖 Start/AutoPrePlay、回合结束、PlayPile 与原版卡牌/药水效果结算。阶段/效果接口以反射处理版本差异，期间隐藏牌面而保留图标与固定状态，完成后由 0.2 秒定时刷新恢复。
- Verify-CombatReshuffle.ps1 的 480 组原版 StableShuffle/PerfectFit 对照通过；新增真实 BuildFromPiles 路径，覆盖 None/Start/AutoPrePlay/Play、空/普通/满手牌、Burn/保留/虚无，以及手牌/Play/Exhaust 不混入和空池回退。
- Verify-DrawRefresh.ps1 直接调用实际补丁 Prefix/Postfix/Finalizer，验证嵌套抽牌与洗牌、玩家隔离、结果对象保持、异步完成/失败/取消与同步异常释放，并核对原版接口签名。此测试不等同于实机 Harmony 分发及 UI 验证。
- v0.111.0 + RitsuLib compat/0.111.0 Release 编译 0 错误、2 条 NU1900 警告（漏洞服务不可达）。简介 7846 UTF-8 字节，小于 8000；版本统一为 1.8.1。本次未实机验证 UI/自动抽牌/控制台抽牌，也未重新验证 v0.107.1。
- Removed speculative hand flushing and now preview up to five next cards from live draw/discard piles. Async command guards hide intermediate card faces until nested draws/shuffles and related resolution finish. Native shuffle comparisons, actual pile-selection regressions, and patch callback lifecycle checks passed. Live Harmony/UI behavior and v0.107.1 were not revalidated.

## v1.8.2 暂停战斗重新洗牌预测（2026-10-23）

- 按用户要求移除战斗弃牌堆的洗牌预测路径。CombatDrawPreview 只截取真实 DrawPile 的前最多 5 张，不读取弃牌堆、不推演手牌弃置、不计算洗牌 RNG，也不补未知卡背。
- 空抽牌堆收起牌面；实际洗牌结束后，沿用 v1.8.1 的异步结算保护和定时刷新，显示真实新牌序。地图起手预览未改动。
- 更新既有 Verify-CombatReshuffle.ps1 为暂停预测的回归检查：抽牌堆 0/1/3/5/7 张时，即使手牌、弃牌堆、打出区与消耗堆各有 7 张，也只显示已知抽牌堆前缀，无占位，无牌堆修改。检查通过。
- v0.111.0 + RitsuLib compat/0.111.0 Release 构建通过，0 错误、2 条 NU1900 警告（漏洞服务不可达）；版本 1.8.2，双语简介 7924 UTF-8 字节，小于 8000。未进行新一轮实机 UI 或 v0.107.1 验证。
- Suspended combat reshuffle prediction and show only the known draw-pile prefix, without discarded cards or placeholders. Existing empty-row handling and async resolution guards restore the view after actual shuffles. Build and focused known-pile regression checks passed; live UI was not revalidated.

## 发布目录迁移与构建同步（2026-10-23）

- 更新本机路径到 G 盘，默认以游戏 v0.111.0 和 RitsuLib compat/0.111.0 构建。build.ps1 成功打包后调用 Sync-Workshop.ps1，同步到相邻 ModUploader/DrawPeekWorkshop；保留原工坊 ID 3824980334、封面、截图及发布者配置字段。
- Release 构建通过，0 警告、0 错误。DLL/manifest SHA-256 一致；两份 workshop.json 均可解析且哈希一致，简介 7926 UTF-8 字节。Windows PowerShell 同步入口复验通过。未上传 Steam、未安装到游戏或进行实机验证。
- Verify-CombatReshuffle.ps1 通过；Verify-Compatibility.ps1 第 16 行失败：旧检查使用的 Rng 构造函数在本机程序集不存在。此检查需另行适配，不能据此声称全部兼容检查通过。本次没有修改游戏预测逻辑。
- Migrated local paths and added automatic post-build Workshop synchronization. Release build, package/config hash checks, Windows PowerShell synchronization, and known-draw-pile regression passed. The legacy compatibility test failed at its RNG constructor lookup. No Steam upload or live game validation was performed.
## v1.8.3 预览结算闪烁修复（2026-10-23）

- CombatOverlay 不再在 IsPreviewSettling 时隐藏面板，保留已经渲染的卡牌副本，稳定后刷新。不读取结算中间牌堆；没有缓存时不显示空面板。新 PlayerCombatState 清空旧预览，地图起手不会充当新战斗的缓存。
- Release 编译 0 警告、0 错误。抽牌异步生命周期与已知牌堆回归检查通过；这些检查不覆盖 Godot 实机画面。发布目录 DLL/JSON 与工坊配置同步校验通过，简介 7787 UTF-8 字节，原工坊 ID 保留。
- 游戏仍运行，未替换游戏安装目录文件，未进行煎饼打击或破灭的实机验证，未上传 Steam。
- Kept the last rendered clones visible during resolution and refreshed after settling, with cache isolation across combats and no blank initial panel. Build and existing async lifecycle/known-pile regressions passed. Workshop staging was synchronized. The running game was not replaced; live UI verification and Steam upload were not performed.
## v1.9.0 下一回合数量与洗牌分隔（2026-10-23）

- 恢复原版 StableShuffle 的副本预测，支持 PerfectFit；以当前结束回合为基础追加普通手牌，排除保留和虚无，敌方回合不再次弃手牌。未知洗牌规则和特殊回合末手牌效果使洗牌部分显示卡背。不执行真实结算，不改真实随机数或牌堆。
- NextDrawCount 读取原版已支持能力/遗物的未来抽牌规则：DrawCardsNextTurn、Clarity、MachineLearning、ToolsOfTheTrade、Demesne、Tyranny、MindRot，Fiddle、PaelsBlood、SneckoEye、RingOfTheDrake、Pendulum、PollinousCore、Pocketwatch；计算下一次计数并排除仅第一回合生效的加成，限制到保留手牌后的空位。第三方效果、完整回合末/敌方/抽牌触发链和联机 RNG 后续变化不在完整模拟范围。
- DrawForecast 传递洗牌边界；DrawCardRow 在两组均非空时预留 16 像素间隔并绘制 2 像素竖线。全部来自同一组时无分隔线。保持 v1.8.3 结算期间上一帧持续显示。
- Release 构建 0 警告、0 错误；360 组原版洗牌/数量/边界/随机数隔离检查、PerfectFit 原版 hook 对照、手牌投影及抽牌增减/上限/遗物计数检查通过。异步抽牌生命周期回归和 600 组地图起手对照通过。
- 已同步 ModUploader/DrawPeekWorkshop 并核对 DLL/JSON/config；简介 7910 UTF-8 字节。未安装、未启动游戏、未上传 Steam。未实机验证布局和交互，未重新验证 v0.107.1。
- Restored cloned reshuffle forecasts with a vertical group divider and projected next-turn draw counts, respecting retained-hand capacity and supported native modifiers. Release build and shuffle/count/async/opening-hand regressions passed. Workshop staging was synchronized; no installation, live UI validation, Steam upload, or v0.107.1 revalidation was performed.
## v2.0.0 独立版（2026-10-23）

- 移除全部 RitsuLib 程序集引用、构建参数、模组 manifest 必需依赖，以及工坊配置中的 RitsuLib 条目。保留上传工作区的其他字段、封面和条目 ID。
- 使用原生 NModdingScreen.OnSubmenuOpened 补丁提供独立设置按钮和 PopupPanel，三个开关复用原来的 Settings 保存/迁移逻辑；关闭父页面时关闭弹窗。中文/英文直接读取 LocManager.Language。
- Release 构建 0 警告、0 错误。读取编译 DLL 的程序集引用确认没有 RitsuLib；模组 manifest 无前置依赖，两份工坊配置无 RitsuLib 依赖。异步抽牌回归与 360 组洗牌/抽牌数量检查通过。
- 工坊文件同步及哈希检查通过，简介 7740 字节。未安装、未上传 Steam，未实机验证设置窗口布局、输入和语言切换；未重新验证 v0.107.1。RandomForeseer 若启用仍须满足它自己的前置要求。
- Standalone build: no RitsuLib assembly references or required mod dependency. Native Mods screen settings reuse persisted configuration; localization uses the game directly. Build and prediction/async regressions passed, publishing files synchronized. No installation, upload, live settings UI verification, or v0.107.1 revalidation was performed.
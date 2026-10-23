using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace DrawPeek;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public const string Id = "DrawPeek";
    public static string DisplayName => L10n.Text("抽牌预览 (Draw Peek)", "Draw Peek");

    public static void Initialize()
    {
        Settings.Load();
        RitsuLibFramework.RegisterModSettings(Id, page => page
            .WithTitle(ModSettingsText.Dynamic(() => DisplayName))
            .WithModDisplayName(ModSettingsText.Dynamic(() => DisplayName))
            .WithDescription(ModSettingsText.Dynamic(() => L10n.Text("分别控制战斗抽牌、地图节点和事件卡牌结果预览。开关独立生效并自动保存，无需重启游戏。", "Control combat draw, map node, and event card result previews independently. Changes are saved automatically without restarting the game.")))
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Dynamic(() => L10n.Text("通用", "General")))
                .AddToggle("enabled", ModSettingsText.Dynamic(() => L10n.Text("启用抽牌预览", "Enable Draw Peek")),
                    new ModSettingsCallbackValueBinding<bool>(Id, "enabled", SaveScope.Global,
                        () => Settings.Enabled, Settings.SetEnabled, Settings.Save),
                    ModSettingsText.Dynamic(() => L10n.Text("控制战斗中当前抽牌堆的已知牌序，以及地图选点时的下一场基础起手预览。", "Show the known next cards in the current combat draw pile and the next combat base opening hand while choosing map nodes.")))
                .AddToggle("mapPredictionEnabled", ModSettingsText.Dynamic(() => L10n.Text("启用地图预测", "Enable Map Predictions")),
                    new ModSettingsCallbackValueBinding<bool>(Id, "mapPredictionEnabled", SaveScope.Global,
                        () => Settings.MapPredictionEnabled, Settings.SetMapPredictionEnabled, Settings.Save),
                    ModSettingsText.Dynamic(() => L10n.Text("悬停当前可前往的地图节点，显示预计房间、奖励和事件初始选项。", "Hover a reachable map node to preview its room, rewards, and initial event options.")))
                .AddToggle("eventPredictionEnabled", ModSettingsText.Dynamic(() => L10n.Text("启用事件预测", "Enable Event Predictions")),
                    new ModSettingsCallbackValueBinding<bool>(Id, "eventPredictionEnabled", SaveScope.Global,
                        () => Settings.EventPredictionEnabled, Settings.SetEventPredictionEnabled, Settings.Save),
                    ModSettingsText.Dynamic(() => L10n.Text("悬停受支持的事件按钮或变化选牌目标，显示随机升级、变化及受支持随机获牌的结果。", "Hover supported event buttons or transformation targets to preview random upgrade, transformation, and supported random card rewards.")))));
        new Harmony("local.drawpeek").PatchAll(typeof(ModEntry).Assembly);
        Log($"Initialized v1.8.2; settings registered; enabled={Settings.Enabled}; map={Settings.MapPredictionEnabled}; event={Settings.EventPredictionEnabled}");
    }

    internal static void Log(string message)
    {
        Console.WriteLine($"[{Id}] {message}");
        try
        {
            Directory.CreateDirectory(Settings.Folder);
            File.AppendAllText(Path.Combine(Settings.Folder, "DrawPeek.log"),
                $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

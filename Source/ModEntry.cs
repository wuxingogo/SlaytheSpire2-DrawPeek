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
            .WithDescription(ModSettingsText.Dynamic(() => L10n.Text("鼠标悬停在顶部计时器左侧的卡牌图标上，查看下回合预计抽到的整组牌。开关立即生效并自动保存。", "Hover over the card icon to the left of the top timer to preview the cards expected next turn. Changes apply immediately and are saved automatically.")))
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Dynamic(() => L10n.Text("通用", "General")))
                .AddToggle("enabled", ModSettingsText.Dynamic(() => L10n.Text("启用抽牌预览", "Enable Draw Peek")),
                    new ModSettingsCallbackValueBinding<bool>(Id, "enabled", SaveScope.Global,
                        () => Settings.Enabled, Settings.SetEnabled, Settings.Save),
                    ModSettingsText.Dynamic(() => L10n.Text("关闭后隐藏抽牌图标和地图奖励预览；再次开启无需重启游戏。", "Disabling hides the card icon and map reward previews. Enable again without restarting the game.")))));
        new Harmony("local.drawpeek").PatchAll(typeof(ModEntry).Assembly);
        Log("Initialized v1.3.2; settings registered; enabled=" + Settings.Enabled);
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

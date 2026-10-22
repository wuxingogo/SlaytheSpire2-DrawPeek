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
    public const string DisplayName = "抽牌预览 (Draw Peek)";

    public static void Initialize()
    {
        Settings.Load();
        RitsuLibFramework.RegisterModSettings(Id, page => page
            .WithTitle(ModSettingsText.Literal(DisplayName))
            .WithModDisplayName(ModSettingsText.Literal(DisplayName))
            .WithDescription(ModSettingsText.Literal("鼠标悬停在顶部计时器左侧的卡牌图标上，查看下回合预计抽到的整组牌。开关立即生效并自动保存。"))
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Literal("通用"))
                .AddToggle("enabled", ModSettingsText.Literal("启用抽牌预览"),
                    new ModSettingsCallbackValueBinding<bool>(Id, "enabled", SaveScope.Global,
                        () => Settings.Enabled, Settings.SetEnabled, Settings.Save),
                    ModSettingsText.Literal("关闭后隐藏抽牌图标和地图奖励预览；再次开启无需重启游戏。"))));
        new Harmony("local.drawpeek").PatchAll(typeof(ModEntry).Assembly);
        Log("Initialized v1.2.1; settings registered; enabled=" + Settings.Enabled);
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

using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace DrawPeek;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public const string Id = "DrawPeek";
    public static string DisplayName => L10n.Text("抽牌预览 (Draw Peek)", "Draw Peek");

    public static void Initialize()
    {
        Settings.Load();
        new Harmony("local.drawpeek").PatchAll(typeof(ModEntry).Assembly);
        Log($"Initialized v2.0.0; standalone settings; enabled={Settings.Enabled}; map={Settings.MapPredictionEnabled}; event={Settings.EventPredictionEnabled}");
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

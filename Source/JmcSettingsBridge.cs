using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace DrawPeek;

// Optional integration: do not reference or load JmcModLib ourselves.
[HarmonyPatch(typeof(NSettingsScreen), nameof(NSettingsScreen._Ready))]
internal static class JmcSettingsBridge
{
    private static bool registered;
    private static bool registering;

    [HarmonyPrefix]
    private static void Prefix() => TryRegister();

    internal static void TryRegister()
    {
        if (registered || registering) return;
        var manager = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("JmcModLib.Config.ConfigManager", false))
            .FirstOrDefault(t => t != null);
        if (manager == null) return;
        try
        {
            var method = manager.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == "RegisterConfig" && m.IsGenericMethodDefinition)
                .MakeGenericMethod(typeof(bool));
            registering = true;
            Register(method, "enabled", "启用抽牌预览 / Enable Draw Peek", () => Settings.Enabled, Settings.SetEnabled, 0);
            Register(method, "mapPredictionEnabled", "启用地图预测 / Enable Map Predictions", () => Settings.MapPredictionEnabled, Settings.SetMapPredictionEnabled, 1);
            Register(method, "eventPredictionEnabled", "启用事件预测 / Enable Event Predictions", () => Settings.EventPredictionEnabled, Settings.SetEventPredictionEnabled, 2);
            registered = true;
            ModEntry.Log("Registered three toggles in JmcModLib Mod Settings.");
        }
        catch (Exception ex)
        {
            ModEntry.Log("Optional JmcModLib settings integration unavailable: " + ex.GetBaseException().Message);
        }
        finally { registering = false; }
    }

    private static void Register(MethodInfo method, string key, string label, Func<bool> getter, Action<bool> setter, int order)
    {
        // Jmc loads its own saved copy during registration. Our existing settings
        // remain authoritative, shared with the standalone window.
        Action<bool> apply = value => { if (!registering) setter(value); };
        var values = new Dictionary<string, object?> {
            ["displayName"] = label, ["getter"] = getter, ["setter"] = apply,
            ["group"] = "DefaultGroup", ["storageKey"] = key,
            ["order"] = order, ["restartRequired"] = false,
            ["assembly"] = typeof(ModEntry).Assembly
        };
        var args = method.GetParameters().Select(p => values.TryGetValue(p.Name!, out var value)
            ? value : p.HasDefaultValue ? p.DefaultValue : throw new MissingMethodException("Unsupported required parameter: " + p.Name)).ToArray();
        method.Invoke(null, args);
    }
}

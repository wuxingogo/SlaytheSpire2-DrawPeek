using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace DrawPeek;

[HarmonyPatch(typeof(NModdingScreen), nameof(NModdingScreen.OnSubmenuOpened))]
internal static class StandaloneSettingsPatch
{
    private static void Postfix(NModdingScreen __instance)
    {
        if (__instance.GetNodeOrNull<Button>("DrawPeekSettingsButton") is { } existing)
        {
            existing.Text = L10n.Text("抽牌预览设置", "Draw Peek Settings");
            return;
        }
        var button = new Button { Name = "DrawPeekSettingsButton", Text = L10n.Text("抽牌预览设置", "Draw Peek Settings") };
        __instance.AddChild(button);
        button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        button.OffsetLeft = -360;
        button.OffsetRight = -40;
        button.OffsetTop = 24;
        button.OffsetBottom = 76;
        button.AddThemeFontSizeOverride("font_size", 24);
        var popup = new PopupPanel { Name = "DrawPeekSettings", Exclusive = true };
        __instance.AddChild(popup);
        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 24);
        popup.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 16);
        margin.AddChild(column);
        var title = new Label();
        title.AddThemeFontSizeOverride("font_size", 28);
        column.AddChild(title);
        var draw = new CheckButton();
        var map = new CheckButton();
        var events = new CheckButton();
        foreach (var toggle in new[] { draw, map, events })
        {
            toggle.AddThemeFontSizeOverride("font_size", 24);
            column.AddChild(toggle);
        }
        draw.Toggled += Settings.SetEnabled;
        map.Toggled += Settings.SetMapPredictionEnabled;
        events.Toggled += Settings.SetEventPredictionEnabled;
        var close = new Button();
        column.AddChild(close);
        close.Pressed += popup.Hide;
        __instance.VisibilityChanged += () => { if (!__instance.IsVisibleInTree()) popup.Hide(); };
        button.Pressed += () =>
        {
            title.Text = ModEntry.DisplayName;
            draw.Text = L10n.Text("启用抽牌预览", "Enable Draw Peek");
            map.Text = L10n.Text("启用地图预测", "Enable Map Predictions");
            events.Text = L10n.Text("启用事件预测", "Enable Event Predictions");
            close.Text = L10n.Text("完成（自动保存）", "Done (saved automatically)");
            draw.SetPressedNoSignal(Settings.Enabled);
            map.SetPressedNoSignal(Settings.MapPredictionEnabled);
            events.SetPressedNoSignal(Settings.EventPredictionEnabled);
            popup.PopupCentered(new Vector2I(560, 340));
            draw.GrabFocus();
        };
    }
}

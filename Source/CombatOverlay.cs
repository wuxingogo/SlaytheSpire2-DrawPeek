using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes;

namespace DrawPeek;

[HarmonyPatch(typeof(NRunTimer), nameof(NRunTimer._Ready))]
internal static class CombatOverlayPatch
{
    private static void Postfix(NRunTimer __instance)
    {
        // A deferred add avoids modifying children while Godot is still initializing the room.
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsInsideTree()) return;
            try { CombatOverlay.Attach(__instance); }
            catch (Exception ex) { ModEntry.Log("Overlay creation failed: " + ex); }
        }).CallDeferred();
    }
}

internal static class CombatOverlay
{
    public static void Attach(NRunTimer room)
    {
        var ui = NRun.Instance?.GlobalUi;
        if (ui == null) return;
        if (ui.HasNode("DrawPeekOverlay")) return;
        var root = new Control { Name = "DrawPeekOverlay", MouseFilter = Control.MouseFilterEnum.Ignore };
        ui.AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.ZIndex = 100;
        var button = new Button { Position = new Vector2(-56, 0), Size = new Vector2(44, 44), FocusMode = Control.FocusModeEnum.None, ToggleMode = true };
        button.AddThemeFontSizeOverride("font_size", 24);
        button.AddThemeColorOverride("font_color", new Color("f2db9b"));
        root.AddChild(button);
        // Two small overlapping cards form the icon without requiring an asset pack.
        foreach (var offset in new[] { new Vector2(10, 8), new Vector2(17, 13) })
        {
            var card = new Panel { Position = offset, Size = new Vector2(18, 25), MouseFilter = Control.MouseFilterEnum.Ignore };
            card.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("293544"), BorderColor = new Color("f2db9b"), BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2, CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 });
            button.AddChild(card);
        }
        button.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        var panel = new PanelContainer { Position = new Vector2(-340, 54), Size = new Vector2(328, 100), Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color("18232eef"), BorderColor = new Color("b79c60"),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 16, ContentMarginBottom = 16 });
        root.AddChild(panel);
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(288, 100) };
        label.AddThemeFontSizeOverride("font_size", 22);
        panel.AddChild(label);
        label.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        bool expanded = false;
        string lastPreview = "";
        bool failed = false;
        void Refresh()
        {
            try
            {
                root.Visible = Settings.Enabled && CombatManager.Instance.IsInProgress;
                if (!root.Visible) {
                    expanded = false;
                    panel.Visible = false;
                    if (!Settings.Enabled) button.SetPressedNoSignal(false);
                    return;
                }
                var timerPosition = root.GetGlobalTransform().AffineInverse() * room.GlobalPosition;
                button.Position = timerPosition + new Vector2(-56, 0);
                panel.Position = timerPosition + new Vector2(-340, 54);
                // Keep the panel open across the gap between the button and its content.
                var hoverArea = button.GetGlobalRect();
                if (expanded) hoverArea = hoverArea.Merge(panel.GetGlobalRect());
                expanded = button.ButtonPressed || hoverArea.HasPoint(button.GetGlobalMousePosition());
                panel.Visible = expanded;
                if (!expanded) return;
                var combat = CombatManager.Instance.DebugOnlyGetState();
                var player = LocalContext.GetMe(combat);
                var state = player?.PlayerCombatState;
                var cards = state?.DrawPile.Cards;
                string preview;
                if (cards == null || combat == null || player == null) preview = "等待战斗数据…";
                else
                {
                    // Query the same count modifiers used by the game, without executing draw actions.
                    int count = Math.Clamp((int)Math.Ceiling(Hook.ModifyHandDraw(combat, player,
                        CombatManager.baseHandDrawCount, out _)), 0, CardPile.MaxCardsInHand);
                    var lines = new List<string> { $"下回合抽牌 · 预计 {count} 张", "" };
                    for (int i = 0; i < count; i++)
                        lines.Add(i < cards.Count ? $"{i + 1}. {cards[i].Title}" : $"{i + 1}. 待洗牌确定");
                    lines.Add($"\n抽牌堆剩余 {cards.Count} 张");
                    if (count > cards.Count) lines.Add("洗牌后的牌序暂不可预知");
                    preview = string.Join('\n', lines);
                }
                label.Text = preview;
                if (lastPreview != preview) { lastPreview = preview; ModEntry.Log("Preview: " + preview.Replace('\n', ' ')); }
            }
            catch (Exception ex)
            {
                if (!failed) { failed = true; ModEntry.Log("Preview update failed: " + ex); }
                label.Text = "预览暂不可用";
            }
        }
        button.MouseEntered += Refresh;
        button.Toggled += pinned => { ModEntry.Log("Preview pinned=" + pinned); Refresh(); };
        var timer = new Godot.Timer { WaitTime = 0.2, Autostart = true };
        timer.Timeout += Refresh;
        root.AddChild(timer);
        Refresh();
        ModEntry.Log("Combat overlay attached.");
    }
}

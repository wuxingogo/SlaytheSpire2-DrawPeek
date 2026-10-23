using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

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
        // Keep the icon above the top bar. Modal visibility is handled below;
        // placing this behind the top bar also hides the icon itself.
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
        var panel = new Panel { Position = new Vector2(-340, 54), Size = new Vector2(328, 100), Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color("18232eef"), BorderColor = new Color("b79c60"),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 16, ContentMarginBottom = 16 });
        root.AddChild(panel);
        var row = new DrawCardRow(panel);
        bool expanded = false;
        string lastPreview = "";
        bool failed = false;
        string lastVisibility = "";
        void Refresh()
        {
            try
            {
                bool inCombat = CombatManager.Instance.IsInProgress;
                var map = NMapScreen.Instance;
                bool mapOpen = map != null && map.IsOpen && map.IsVisibleInTree();
                bool choosingNode = !inCombat && mapOpen
                    && map!.IsTravelEnabled && !map.IsTraveling;
                // Opening the map hides the overlay stack without removing its
                // reward screens. A non-empty stack only blocks us off the map.
                root.Visible = Settings.Enabled && (inCombat || choosingNode)
                    && !ui.CapstoneContainer.InUse
                    && !ui.SubmenuStack.Stack.SubmenusOpen
                    && (mapOpen || ui.Overlays.ScreenCount == 0);
                string visibility = $"visible={root.Visible}; combat={inCombat}; map={mapOpen}; travelEnabled={map?.IsTravelEnabled}; traveling={map?.IsTraveling}; capstone={ui.CapstoneContainer.InUse}; submenu={ui.SubmenuStack.Stack.SubmenusOpen}; overlays={ui.Overlays.ScreenCount}; enabled={Settings.Enabled}";
                if (visibility != lastVisibility)
                {
                    lastVisibility = visibility;
                    ModEntry.Log("Preview visibility: " + visibility);
                }
                if (!root.Visible) {
                    expanded = false;
                    panel.Visible = false;
                    if (!Settings.Enabled) button.SetPressedNoSignal(false);
                    return;
                }
                var timerPosition = root.GetGlobalTransform().AffineInverse() * room.GlobalPosition;
                button.Position = timerPosition + new Vector2(-56, 0);
                PlacePanel();
                // Keep the panel open across the gap between the button and its content.
                var hoverArea = button.GetGlobalRect();
                if (expanded) hoverArea = hoverArea.Merge(panel.GetGlobalRect());
                expanded = button.ButtonPressed || hoverArea.HasPoint(button.GetGlobalMousePosition());
                panel.Visible = expanded;
                if (!expanded) return;
                if (choosingNode)
                {
                    ShowPreview(OpeningHandPreview.BuildCards(), PileType.Deck);
                    return;
                }
                var combat = CombatManager.Instance.DebugOnlyGetState();
                var player = LocalContext.GetMe(combat);
                var state = player?.PlayerCombatState;
                var cards = state?.DrawPile.Cards;
                if (cards == null || combat == null || player == null)
                    ShowPreview(Array.Empty<CardModel?>(), PileType.Draw);
                else
                {
                    // Do not sample partial piles or an already-advanced shuffle
                    // RNG while a command is still transferring cards.
                    if (GameCompat.IsPreviewSettling(player)) { panel.Visible = false; return; }
                    ShowPreview(CombatDrawPreview.Build(player, 5), PileType.Draw);
                }
            }
            catch (Exception ex)
            {
                if (!failed) { failed = true; ModEntry.Log("Preview update failed: " + ex); }
                panel.Visible = false;
            }
        }
        void PlacePanel()
        {
            var bounds = root.Size;
            panel.Position = new Vector2(
                Math.Clamp(button.Position.X + button.Size.X - panel.Size.X, 12, Math.Max(12, bounds.X - panel.Size.X - 12)),
                Math.Clamp(button.Position.Y + button.Size.Y + 10, 12, Math.Max(12, bounds.Y - panel.Size.Y - 12)));
        }
        void ShowPreview(IReadOnlyList<CardModel?> cards, PileType pile)
        {
            row.Show(cards, pile, root.Size - new Vector2(24, button.Position.Y + button.Size.Y + 34));
            PlacePanel();
            string preview = pile + ": " + string.Join(", ", cards.Select(c => c?.Id.ToString() ?? "card back"));
            if (lastPreview != preview) { lastPreview = preview; ModEntry.Log("Preview: " + preview); }
        }
        // Update immediately when menus change, preserving ButtonPressed so a
        // pinned preview returns on closing the menu. Release signal handlers
        // when the overlay leaves the tree.
        ui.CapstoneContainer.Changed += Refresh;
        ui.SubmenuStack.Stack.StackModified += Refresh;
        ui.Overlays.Changed += Refresh;
        ui.MapScreen.Opened += Refresh;
        ui.MapScreen.Closed += Refresh;
        root.TreeExiting += () => {
            if (GodotObject.IsInstanceValid(ui.CapstoneContainer)) ui.CapstoneContainer.Changed -= Refresh;
            if (GodotObject.IsInstanceValid(ui.SubmenuStack.Stack)) ui.SubmenuStack.Stack.StackModified -= Refresh;
            if (GodotObject.IsInstanceValid(ui.Overlays)) ui.Overlays.Changed -= Refresh;
            if (GodotObject.IsInstanceValid(ui.MapScreen))
            {
                ui.MapScreen.Opened -= Refresh;
                ui.MapScreen.Closed -= Refresh;
            }
        };
        button.MouseEntered += Refresh;
        button.Toggled += pinned => { ModEntry.Log("Preview pinned=" + pinned); Refresh(); };
        var timer = new Godot.Timer { WaitTime = 0.2, Autostart = true };
        timer.Timeout += Refresh;
        root.AddChild(timer);
        Refresh();
        ModEntry.Log("Combat overlay attached.");
    }
}

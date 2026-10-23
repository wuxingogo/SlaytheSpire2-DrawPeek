using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace DrawPeek;

// Uses the native tooltip scene, but lays out every slot in one horizontal row.
// Never uses playable card holders or the global tooltip stack.
internal sealed class DrawCardRow
{
    private readonly Control panel;
    private readonly List<(Control View, NCard? Card)> slots = new();
    internal bool HasCards => slots.Count > 0;
    private ColorRect? divider;

    internal DrawCardRow(Control panel) => this.panel = panel;

    internal void Show(IReadOnlyList<CardModel?> cards, PileType pile, Vector2 available, int shuffleIndex = -1)
    {
        bool split = shuffleIndex > 0 && shuffleIndex < cards.Count;
        if (divider != null) divider.Visible = split;
        bool rebuild = cards.Count != slots.Count
            || cards.Where((card, i) => (card == null) != (slots[i].Card == null)).Any();
        if (rebuild)
        {
            foreach (var slot in slots)
            {
                panel.RemoveChild(slot.View);
                slot.View.QueueFree();
            }
            slots.Clear();
            foreach (var model in cards)
            {
                if (model == null)
                {
                    var back = CreateBack();
                    panel.AddChild(back);
                    slots.Add((back, null));
                }
                else
                {
                    var view = PreloadManager.Cache.GetScene("res://scenes/ui/card_hover_tip.tscn").Instantiate<Control>();
                    panel.AddChild(view);
                    IgnoreInput(view);
                    slots.Add((view, view.GetNode<NCard>("%Card")));
                }
            }
        }
        for (int i = 0; i < cards.Count; i++)
        {
            if (slots[i].Card is not { } node || cards[i] is not { } model) continue;
            // NCard updates DynamicVars when rendering: isolate those changes
            // from the real deck and refresh costs/upgrades/localization in place.
            node.Model = (CardModel)model.MutableClone();
            node.UpdateVisuals(pile, CardPreviewMode.Normal);
        }
        if (cards.Count == 0) { panel.Visible = false; return; }

        var size = new Vector2(slots.Max(s => s.View.Size.X), slots.Max(s => s.View.Size.Y));
        const float padding = 12;
        float separatorWidth = split ? 16 : 0;
        float width = Math.Max(1, available.X - padding * 2 - separatorWidth);
        float height = Math.Max(1, available.Y - padding * 2);
        // Compress gaps first, then scale all cards equally; never wrap or crop.
        float scale = Math.Min(0.72f, Math.Min(height / size.Y, width / (size.X * cards.Count)));
        float gap = cards.Count < 2 ? 0 : Math.Clamp((width - size.X * scale * cards.Count) / (cards.Count - 1), 0, 8);
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].View.Scale = Vector2.One * scale;
            slots[i].View.Position = new Vector2(padding + i * (size.X * scale + gap) + (split && i >= shuffleIndex ? separatorWidth : 0), padding);
        }
        panel.Size = new Vector2(padding * 2 + size.X * scale * cards.Count + gap * (cards.Count - 1) + separatorWidth, padding * 2 + size.Y * scale);
        if (split)
        {
            if (divider == null)
            {
                divider = new ColorRect { Color = new Color("b79c60"), MouseFilter = Control.MouseFilterEnum.Ignore };
                panel.AddChild(divider);
            }
            divider.Visible = true;
            divider.Position = new Vector2(padding + shuffleIndex * (size.X * scale + gap) - gap / 2 + separatorWidth / 2 - 1, padding);
            divider.Size = new Vector2(2, size.Y * scale);
        }
    }

    private static Control CreateBack()
    {
        var back = new Panel { Size = NCard.defaultSize, MouseFilter = Control.MouseFilterEnum.Ignore };
        back.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color("293544"), BorderColor = new Color("b79c60"),
            BorderWidthLeft = 4, BorderWidthRight = 4, BorderWidthTop = 4, BorderWidthBottom = 4,
            CornerRadiusTopLeft = 18, CornerRadiusTopRight = 18, CornerRadiusBottomLeft = 18, CornerRadiusBottomRight = 18 });
        var inset = new Panel { Position = new Vector2(24, 24), Size = NCard.defaultSize - new Vector2(48, 48), MouseFilter = Control.MouseFilterEnum.Ignore };
        inset.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color("18232e"), BorderColor = new Color("756546"),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12 });
        back.AddChild(inset);
        return back;
    }

    private static void IgnoreInput(Node node)
    {
        if (node is Control control)
        {
            control.MouseFilter = Control.MouseFilterEnum.Ignore;
            control.FocusMode = Control.FocusModeEnum.None;
        }
        foreach (var child in node.GetChildren()) IgnoreInput(child);
    }
}

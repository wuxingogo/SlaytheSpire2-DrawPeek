using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace DrawPeek;

internal static class CombatDrawPreview
{
    internal static IReadOnlyList<CardModel?> Build(Player player, int count)
    {
        var state = player.PlayerCombatState;
        return state == null ? Array.Empty<CardModel?>() : BuildFromPiles(state, count);
    }

    internal static IReadOnlyList<CardModel?> BuildFromPiles(PlayerCombatState state, int count)
    {
        // Reshuffle prediction is suspended. Only show cards already in Draw;
        // once the real shuffle finishes, normal refresh picks up the new order.
        return state.DrawPile.Cards.Take(Math.Clamp(count, 0, CardPile.MaxCardsInHand))
            .Cast<CardModel?>().ToArray();
    }
}

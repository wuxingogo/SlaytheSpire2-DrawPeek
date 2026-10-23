using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace DrawPeek;

internal static class EventRewardPrediction
{
    internal static IReadOnlyList<CardModel> ForAutomaton(Player player, bool power)
    {
        // Mirror InfestedAutomaton's non-combat options, including unlock and
        // multiplayer restrictions. Do not execute reward hooks or CardFactory:
        // those may register cards, advance live RNG, or trigger reward effects.
        var options = CardCreationOptions.ForNonCombatWithDefaultOdds(
            new[] { player.Character.CardPool },
            power ? c => c.Type == CardType.Power
                : c => c.EnergyCost is { Canonical: 0, CostsX: false });
        if (!power) options = options.WithFlags(CardCreationFlags.NoCardPoolModifications);
        var excluded = player.RunState.Players.Count > 1
            ? CardMultiplayerConstraint.SingleplayerOnly : CardMultiplayerConstraint.MultiplayerOnly;
        var pool = options.GetPossibleCards(player).Where(c => c.MultiplayerConstraint != excluded).ToArray();
        var canonical = SelectRewardCard(pool, player.PlayerRng.Rewards);
        if (canonical == null || canonical.GetType().Assembly != typeof(CardModel).Assembly)
            return Array.Empty<CardModel>();
        var preview = canonical.ToMutable();
        preview.Owner = player;
        // Both options set NoUpgradeRoll, so the awarded card starts unupgraded.
        return new[] { preview };
    }

    internal static CardModel? SelectRewardCard(IReadOnlyList<CardModel> pool, Rng liveRewards)
    {
        if (pool.Count == 0) return null;
        var rng = GameCompat.CloneRng(liveRewards);
        // Non-combat default odds use base rarity chances, ignoring the current
        // pity offset. Rarity and card selection consume the SAME rewards stream,
        // not the event RNG used by upgrades and transformations.
        var rarity = new CardRarityOdds(rng).RollWithBaseOdds(CardRarityOddsType.RegularEncounter);
        var seen = new HashSet<CardRarity>();
        while (!pool.Any(card => card.Rarity == rarity))
        {
            if (rarity == CardRarity.None || !seen.Add(rarity)) return null;
            rarity = rarity.GetNextHighestRarityWithWrapping();
        }
        return rng.NextItem(pool.Where(card => card.Rarity == rarity));
    }
}

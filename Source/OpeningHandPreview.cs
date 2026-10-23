using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace DrawPeek;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Close))]
internal static class OpeningHandMapClosePatch
{
    private static void Postfix() => OpeningHandPreview.ClearTarget();
}

internal static class OpeningHandPreview
{
    private static NMapPoint? target;

    internal static void SetTarget(NMapPoint point)
    {
        if (point.State == MapPointState.Travelable) target = point;
    }

    internal static void ClearTarget() => target = null;

    private static RoomType? TargetRoom()
    {
        if (!GodotObject.IsInstanceValid(target) || !target!.IsVisibleInTree()
            || target.State != MapPointState.Travelable) return null;
        return target.Point.PointType switch {
            MapPointType.Monster => RoomType.Monster,
            MapPointType.Elite => RoomType.Elite,
            MapPointType.Boss => RoomType.Boss,
            _ => null
        };
    }

    internal static IReadOnlyList<CardModel> BuildCards()
    {
        var run = RunManager.Instance.DebugOnlyGetState();
        var player = run == null ? null : LocalContext.GetMe(run);
        if (run == null || player == null)
            return Array.Empty<CardModel>();

        var room = TargetRoom();
        // CombatManager initializes players in run order using a shared shuffle
        // stream. Replay earlier players' Fisher-Yates draws on the copy only.
        var preceding = new List<int>();
        bool found = false;
        foreach (var participant in run.Players)
        {
            if (ReferenceEquals(participant, player)) { found = true; break; }
            preceding.Add(participant.Deck.Cards.Count);
        }
        if (!found) return Array.Empty<CardModel>();
        var cards = PredictOrder(player.Deck.Cards, run.Rng.Shuffle, preceding);
        int innate = cards.Count(IsOpeningInnate);
        int count = Math.Min(cards.Count, Math.Clamp(Math.Max(OpeningDrawCount(player.Relics, room), innate), 0, CardPile.MaxCardsInHand));
        return cards.Take(count).ToArray();
    }

    internal static List<CardModel> PredictOrder(IReadOnlyList<CardModel> deck, Rng liveShuffle, IEnumerable<int> precedingDeckSizes)
    {
        var rng = GameCompat.CloneRng(liveShuffle);
        foreach (int size in precedingDeckSizes)
            for (int remaining = size; remaining > 1;)
                rng.NextInt(remaining--);
        var cards = deck.ToList();
        cards.UnstableShuffle(rng); // Initial shuffle is NOT StableShuffle.
        var bottom = cards.Where(c => c.Enchantment?.ShouldStartAtBottomOfDrawPile ?? false).ToList();
        foreach (var card in bottom) { cards.Remove(card); cards.Add(card); }
        // The original moves each innate card to index zero in enumeration order,
        // reversing the innate subset. A stable partition would predict incorrectly.
        foreach (var card in cards.Where(IsOpeningInnate).ToArray())
        {
            cards.Remove(card);
            cards.Insert(0, card);
        }
        return cards;
    }

    private static bool IsOpeningInnate(CardModel card) => card.Keywords.Contains(CardKeyword.Innate)
        && !(card.Enchantment?.ShouldStartAtBottomOfDrawPile ?? false);

    internal static int OpeningDrawCount(IEnumerable<RelicModel> relics, RoomType? room)
    {
        decimal count = CombatManager.baseHandDrawCount;
        foreach (var relic in relics)
        {
            if (relic.GetType().Assembly != typeof(RelicModel).Assembly) continue;
            // Read known first-turn rules without setting live TurnNumber or
            // calling hooks that depend on the previous combat's stale state.
            switch (relic.GetType().Name)
            {
                case "BagOfPreparation": case "RingOfTheSnake":
                case "Fiddle": case "PaelsBlood": case "SneckoEye":
                    count += relic.DynamicVars.Cards.BaseValue;
                    break;
                case "RingOfTheDrake":
                    if (relic.DynamicVars["Turns"].BaseValue >= 1) count += relic.DynamicVars.Cards.BaseValue;
                    break;
                case "BigMushroom": count -= relic.DynamicVars.Cards.BaseValue; break;
                case "BoomingConch":
                    if (room == RoomType.Elite) count += relic.DynamicVars.Cards.BaseValue;
                    break;
                case "Pendulum": case "PollinousCore":
                    int seen = (int)relic.GetType().GetProperty("TurnsSeen")!.GetValue(relic)! + 1;
                    int interval = relic.DynamicVars["Turns"].IntValue;
                    bool activates = relic.GetType().Name == "Pendulum" ? interval > 0 && seen % interval == 0 : seen >= interval;
                    if (activates) count += relic.DynamicVars.Cards.BaseValue;
                    break;
            }
        }
        return Math.Clamp((int)Math.Ceiling(count), 0, CardPile.MaxCardsInHand);
    }
}

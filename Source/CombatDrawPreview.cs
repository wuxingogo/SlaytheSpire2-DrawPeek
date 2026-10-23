using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace DrawPeek;

internal sealed record DrawForecast(IReadOnlyList<CardModel?> Cards, int ShuffleIndex);

internal static class CombatDrawPreview
{
    internal static DrawForecast Build(Player player)
    {
        var state = player.PlayerCombatState;
        var combat = player.Creature.CombatState;
        if (state == null || combat == null) return new(Array.Empty<CardModel?>(), -1);
        bool endingHand = combat.CurrentSide.ToString() == "Player";
        bool flush = true;
        bool uncertain = false;
        foreach (var model in combat.IterateHookListeners())
        {
            if (Overrides(model, "ShouldFlush"))
            {
                if (IsNative(model) && model.GetType().Name is "RunicPyramid" or "RingingTriangle" or "RetainHandPower" or "WellLaidPlansPower")
                    flush &= model.ShouldFlush(player);
                else uncertain = true;
            }
            if (Overrides(model, "ModifyShuffleOrder") && !(IsNative(model) && model.GetType().Name == "PerfectFit")) uncertain = true;
        }
        var (futureDiscard, retained, uncertainHand) = ProjectHand(state, endingHand, flush);
        uncertain |= uncertainHand;
        int count = NextDrawCount(player.Relics, player.Creature.Powers, state.TurnNumber + 1, retained);
        return Predict(state.DrawPile.Cards, futureDiscard, player.RunState.Rng.Shuffle, count, uncertain);
    }

    private static bool IsNative(AbstractModel model) => model.GetType().Assembly == typeof(CardModel).Assembly;
    private static bool Overrides(AbstractModel model, string method) => model.GetType().GetMethod(method)?.DeclaringType != typeof(AbstractModel);

    internal static (List<CardModel> Discard, int Retained, bool Uncertain) ProjectHand(PlayerCombatState state, bool endingHand, bool flush)
    {
        var discard = state.DiscardPile.Cards.ToList();
        int retained = 0;
        bool uncertain = false;
        foreach (var card in state.Hand.Cards)
        {
            if (!endingHand) { retained++; continue; }
            if (card.HasTurnEndInHandEffect) { uncertain = true; continue; }
            if (card.Keywords.Contains(CardKeyword.Ethereal)) continue;
            if (!flush || card.ShouldRetainThisTurn) retained++;
            else discard.Add(card);
        }
        return (discard, retained, uncertain);
    }

    internal static DrawForecast Predict(IReadOnlyList<CardModel> draw, IReadOnlyList<CardModel> discard, Rng liveRng, int count, bool uncertain = false)
    {
        count = Math.Clamp(count, 0, CardPile.MaxCardsInHand);
        var result = draw.Take(count).Cast<CardModel?>().ToList();
        int boundary = -1;
        if (result.Count < count && (discard.Count > 0 || uncertain))
        {
            boundary = result.Count;
            if (uncertain)
                result.AddRange(Enumerable.Repeat<CardModel?>(null, count - result.Count));
            else
            {
                var shuffled = discard.ToList();
                shuffled.StableShuffle(GameCompat.CloneRng(liveRng));
                // Native shuffle listeners visit the future discard pile in order.
                foreach (var card in discard)
                    if (card.Enchantment?.GetType().Name == "PerfectFit" && IsNative(card.Enchantment))
                    { shuffled.Remove(card); shuffled.Insert(0, card); }
                result.AddRange(shuffled.Take(count - result.Count));
            }
        }
        return new(result, boundary);
    }

    internal static int NextDrawCount(IEnumerable<RelicModel> relics, IEnumerable<PowerModel> powers, int nextTurn, int retained)
    {
        decimal count = CombatManager.baseHandDrawCount;
        // Native listeners apply powers before relics. Evaluate upcoming-turn rules
        // without temporarily changing live TurnNumber or invoking lifecycle hooks.
        foreach (var power in powers)
        {
            if (!IsNative(power)) continue;
            switch (power.GetType().Name)
            {
                case "DrawCardsNextTurnPower": case "MachineLearningPower": case "ToolsOfTheTradePower":
                case "DemesnePower": case "TyrannyPower": count += power.Amount; break;
                case "ClarityPower": if (power.Amount > 0) count++; break;
                case "MindRotPower": count = Math.Max(0, count - power.Amount); break;
            }
        }
        foreach (var relic in relics)
        {
            if (!IsNative(relic) || relic.IsMelted) continue;
            switch (relic.GetType().Name)
            {
                case "Fiddle": case "PaelsBlood": case "SneckoEye": count += relic.DynamicVars.Cards.BaseValue; break;
                case "RingOfTheDrake": if (nextTurn <= relic.DynamicVars["Turns"].BaseValue) count += relic.DynamicVars.Cards.BaseValue; break;
                case "Pendulum": case "PollinousCore":
                    int seen = (int)relic.GetType().GetProperty("TurnsSeen")!.GetValue(relic)! + 1;
                    int interval = relic.DynamicVars["Turns"].IntValue;
                    if (relic.GetType().Name == "Pendulum" ? interval > 0 && seen % interval == 0 : seen >= interval) count += relic.DynamicVars.Cards.BaseValue;
                    break;
                case "Pocketwatch":
                    int played = (int)relic.GetType().GetField("_cardsPlayedThisTurn", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(relic)!;
                    if (nextTurn > 1 && played <= relic.DynamicVars["CardThreshold"].BaseValue) count += relic.DynamicVars.Cards.BaseValue;
                    break;
            }
        }
        return Math.Clamp((int)Math.Ceiling(count), 0, Math.Max(0, CardPile.MaxCardsInHand - retained));
    }
}

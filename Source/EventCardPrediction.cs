using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Random;

namespace DrawPeek;

// Extend the native hover UI instead of creating an overlay above menus.
// Only the local argument is changed; event choices and original hover lists stay intact.
[HarmonyPatch(typeof(NHoverTipSet), nameof(NHoverTipSet.CreateAndShow),
    new[] { typeof(Control), typeof(IEnumerable<IHoverTip>), typeof(HoverTipAlignment) })]
internal static class EventCardHoverPatch
{
    private static void Prefix(Control owner, ref IEnumerable<IHoverTip> hoverTips)
    {
        if (!Settings.EventPredictionEnabled) return;
        try
        {
            IReadOnlyList<CardModel> cards = Array.Empty<CardModel>();
            if (owner is NEventOptionButton button && !button.Option.IsLocked)
                cards = EventCardPrediction.ForOption(button.Event, button.Option.TextKey);
            else if (owner is NGridCardHolder holder)
                cards = EventCardPrediction.ForTransformCard(holder);
            if (cards.Count > 0)
                hoverTips = hoverTips.Concat(cards.Select(card => HoverTipFactory.FromCard(card))).ToArray();
        }
        catch (Exception ex) { ModEntry.Log("Event card prediction unavailable: " + ex.Message); }
    }
}

internal static class EventCardPrediction
{
    private static readonly FieldInfo EventField = AccessTools.Field(typeof(NEventRoom), "_event");
    private static readonly FieldInfo SelectedField = AccessTools.Field(typeof(NDeckTransformSelectScreen), "_selectedCards");

    internal static bool IsVanilla(EventModel model) => model.GetType().Assembly == typeof(EventModel).Assembly;

    internal static IReadOnlyList<CardModel> ForOption(EventModel model, string key)
    {
        if (!IsVanilla(model) || model.Owner == null) return Array.Empty<CardModel>();
        if (key is "INFESTED_AUTOMATON.pages.INITIAL.options.STUDY"
            or "INFESTED_AUTOMATON.pages.INITIAL.options.TOUCH_CORE")
            return EventRewardPrediction.ForAutomaton(model.Owner, key.EndsWith(".STUDY", StringComparison.Ordinal));
        if (model.Rng == null) return Array.Empty<CardModel>();
        // Do not speculate about choices that execute a combat before upgrading.
        var rng = GameCompat.CloneRng(model.Rng);
        var deck = model.Owner.Deck.Cards;
        var result = new List<CardModel>();
        var candidates = deck.Where(c => c.IsUpgradable).ToList();
        if (key == "DOORS_OF_LIGHT_AND_DARK.pages.INITIAL.options.LIGHT")
        {
            foreach (var card in candidates.StableShuffle(rng).Take(model.DynamicVars.Cards.IntValue))
                result.Add(UpgradeCopy(card));
        }
        else if (key == "ENDLESS_CONVEYOR.pages.INITIAL.options.OBSERVE_CHEF"
            || key == "ENDLESS_CONVEYOR.pages.ALL.options.SPICY_SNAPPY")
        {
            if (candidates.Count > 0) result.Add(UpgradeCopy(rng.NextItem(candidates)!));
        }
        else if (key.StartsWith("TABLET_OF_TRUTH.pages.", StringComparison.Ordinal)
            && key.Contains(".options.DECIPHER", StringComparison.Ordinal))
        {
            // A fatal option never reaches its upgrade action.
            if (model.DynamicVars["DecipherMaxHpLoss"].BaseValue >= model.Owner.Creature.MaxHp) return result;
            int count = (int)AccessTools.Field(model.GetType(), "_decipherCount").GetValue(model)!;
            if (count == 4) result.AddRange(candidates.Select(UpgradeCopy));
            else if (candidates.Count > 0) result.Add(UpgradeCopy(rng.NextItem(candidates)!));
        }
        else if (key == "REFLECTIONS.pages.INITIAL.options.TOUCH_A_MIRROR")
        {
            // Shadow copies retain deck order, including cards downgraded before
            // the four upgrades. Nothing is removed from or added to the real deck.
            var shadows = deck.Select(Copy).ToList();
            var downgradable = shadows.Where(c => c.IsUpgraded).ToList();
            for (int i = 0; i < 2 && downgradable.Count > 0; i++)
            {
                var card = rng.NextItem(downgradable)!;
                downgradable.Remove(card);
                card.DowngradeInternal();
                result.Add(Copy(card));
            }
            var upgradable = shadows.Where(c => c.IsUpgradable).ToList();
            for (int i = 0; i < 4 && upgradable.Count > 0; i++)
            {
                var card = rng.NextItem(upgradable)!;
                upgradable.Remove(card);
                result.Add(UpgradeCopy(card));
            }
        }
        else if (IsTransformOption(key))
        {
            // A player-selected target is not predictable before that choice.
            // When only one target is legal, its output can already be shown.
            var targets = deck.Where(c => c.IsTransformable).ToArray();
            if (targets.Length == 1)
            {
                result.Add(Copy(targets[0]));
                result.Add(TransformCopy(targets[0], rng));
            }
        }
        return result;
    }

    internal static bool IsTransformOption(string key) => key is
        "AROMA_OF_CHAOS.pages.INITIAL.options.LET_GO" or
        "MORPHIC_GROVE.pages.INITIAL.options.GROUP" or
        "SYMBIOTE.pages.INITIAL.options.KILL_WITH_FIRE" or
        "WHISPERING_HOLLOW.pages.INITIAL.options.HUG" or
        "ENDLESS_CONVEYOR.pages.ALL.options.JELLY_LIVER" or
        "TRIAL.pages.NONDESCRIPT.options.INNOCENT";

    internal static IReadOnlyList<CardModel> ForTransformCard(NGridCardHolder holder)
    {
        NDeckTransformSelectScreen? screen = null;
        for (Node? parent = holder.GetParent(); parent != null; parent = parent.GetParent())
            if (parent is NDeckTransformSelectScreen transform) { screen = transform; break; }
        if (screen == null || NEventRoom.Instance == null) return Array.Empty<CardModel>();
        var model = EventField.GetValue(NEventRoom.Instance) as EventModel;
        if (model == null || !IsVanilla(model) || model.Owner == null || model.IsFinished
            || !model.CurrentOptions.Any(option => IsTransformOption(option.TextKey))) return Array.Empty<CardModel>();
        var source = holder.CardModel;
        if (!source.IsTransformable || source.Owner != model.Owner || !model.Owner.Deck.Cards.Contains(source))
            return Array.Empty<CardModel>();
        var selected = (IEnumerable<CardModel>)SelectedField.GetValue(screen)!;
        var rng = GameCompat.CloneRng(model.Rng);
        // Each prior selection consumes a roll before this card is transformed.
        // Use the same HashSet enumeration order returned by the original screen.
        foreach (var prior in selected)
        {
            if (ReferenceEquals(prior, source)) break;
            NextTransform(prior, rng);
        }
        return new[] { TransformCopy(source, rng) };
    }

    private static CardModel Copy(CardModel source)
    {
        // Avoid mod card callbacks with arbitrary side effects in preview code.
        if (source.GetType().Assembly != typeof(CardModel).Assembly)
            throw new NotSupportedException("Third-party card prediction is not supported.");
        return (CardModel)source.MutableClone();
    }

    private static CardModel UpgradeCopy(CardModel source)
    {
        var copy = Copy(source);
        copy.UpgradeInternal();
        copy.FinalizeUpgradeInternal();
        return copy;
    }

    private static CardModel NextTransform(CardModel source, Rng rng) =>
        rng.NextItem(CardFactory.GetDefaultTransformationOptions(source, false))
            ?? throw new InvalidOperationException("No eligible transformation result.");

    private static CardModel TransformCopy(CardModel source, Rng rng)
    {
        // CardFactory.CreateRandomCardForTransform registers a card with CardScope.
        // Select from its read-only pool instead and create an unregistered copy.
        return MaterializeTransform(source, NextTransform(source, rng));
    }

    internal static CardModel MaterializeTransform(CardModel source, CardModel canonical)
    {
        if (canonical.GetType().Assembly != typeof(CardModel).Assembly)
            throw new NotSupportedException("Third-party transformation prediction is not supported.");
        var copy = canonical.ToMutable();
        copy.Owner = source.Owner;
        return copy;
    }
}

using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Combat;

namespace DrawPeek;

/// <summary>Isolates APIs that changed between 0.107.1 and 0.111.0.</summary>
internal static class GameCompat
{
    private static readonly MethodInfo? SerializeRng = typeof(Rng).GetMethod("ToSerializable", Type.EmptyTypes);
    private static readonly PropertyInfo? PlayerPhase = typeof(PlayerCombatState).GetProperty("Phase");
    private static readonly MethodInfo? ExecutingEffect = typeof(CombatManager).GetMethod("IsExecutingCardOrPotionEffect", new[] { typeof(Player) });
    private static readonly MethodInfo PotionRoll = typeof(PotionRewardOdds).GetMethods()
        .Single(m => m.Name == "Roll" && m.GetParameters().Length is 2 or 3);

    internal static readonly CardCreationFlags CombatCardFlags =
        Enum.TryParse<CardCreationFlags>("IsFromCombat", out var flags) ? flags : default;

    internal static bool IsPreviewSettling(Player player)
    {
        var manager = CombatManager.Instance;
        string? phase = PlayerPhase?.GetValue(player.PlayerCombatState)?.ToString();
        return DrawPreviewActivity.IsBusy(player)
            || phase is "Start" or "AutoPrePlay"
            || manager.EndingPlayerTurnPhaseOne || manager.EndingPlayerTurnPhaseTwo
            || (player.PlayerCombatState?.PlayPile.Cards.Count ?? 0) > 0
            || ExecutingEffect?.Invoke(manager, new object[] { player }) is true;
    }

    internal static Rng CloneRng(Rng source)
    {
        // New versions can preserve their complete serialized RNG state.
        if (SerializeRng != null)
        {
            var state = SerializeRng.Invoke(source, null)!;
            return (Rng)Activator.CreateInstance(typeof(Rng), state)!;
        }
        // 0.107.1 represents RNG state as seed + counter. Never advance source.
        var seed = typeof(Rng).GetProperty("Seed")!.GetValue(source)!;
        var counter = typeof(Rng).GetProperty("Counter")!.GetValue(source)!;
        return (Rng)Activator.CreateInstance(typeof(Rng), seed, counter)!;
    }

    internal static bool RollPotion(PotionRewardOdds odds, Player player, RunState run, RoomType room)
    {
        var parameters = PotionRoll.GetParameters();
        if (parameters.Length == 2)
            return (bool)PotionRoll.Invoke(odds, new object[] { player, room })!;
        // Older versions take a read-only ascension context explicitly.
        var ascension = Activator.CreateInstance(parameters[1].ParameterType, run.AscensionLevel)!;
        return (bool)PotionRoll.Invoke(odds, new object[] { player, ascension, room })!;
    }
}

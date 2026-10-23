using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace DrawPeek;

// Track the entire async command, including nested draws and shuffles. A
// synchronous Harmony postfix alone would release the guard before any awaits.
internal static class DrawPreviewActivity
{
    private static readonly object gate = new();
    private static readonly Dictionary<Player, int> depths = new();

    internal static bool IsBusy(Player player)
    {
        lock (gate) return depths.ContainsKey(player);
    }

    internal static IDisposable Begin(Player player)
    {
        lock (gate) depths[player] = depths.GetValueOrDefault(player) + 1;
        return new Scope(player);
    }

    private sealed class Scope(Player player) : IDisposable
    {
        private int disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            lock (gate)
            {
                int remaining = depths[player] - 1;
                if (remaining == 0) depths.Remove(player);
                else depths[player] = remaining;
            }
        }
    }

    internal static async Task Track(Task task, IDisposable scope)
    {
        try { await task; }
        finally { scope.Dispose(); }
    }

    internal static async Task<IEnumerable<CardModel>> TrackDraw(Task<IEnumerable<CardModel>> task, IDisposable scope)
    {
        try { return await task; }
        finally { scope.Dispose(); }
    }
}

[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Draw), new[] {
    typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool) })]
internal static class DrawPreviewDrawPatch
{
    private static void Prefix(Player player, out IDisposable __state) => __state = DrawPreviewActivity.Begin(player);
    private static void Postfix(ref Task<IEnumerable<CardModel>> __result, IDisposable __state)
        => __result = DrawPreviewActivity.TrackDraw(__result, __state);
    private static void Finalizer(Exception? __exception, IDisposable? __state)
    {
        if (__exception != null) __state?.Dispose();
    }
}

[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Shuffle))]
internal static class DrawPreviewShufflePatch
{
    private static void Prefix(Player player, out IDisposable __state) => __state = DrawPreviewActivity.Begin(player);
    private static void Postfix(ref Task __result, IDisposable __state)
        => __result = DrawPreviewActivity.Track(__result, __state);
    private static void Finalizer(Exception? __exception, IDisposable? __state)
    {
        if (__exception != null) __state?.Dispose();
    }
}

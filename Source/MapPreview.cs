using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace DrawPeek;

[HarmonyPatch(typeof(NMapPoint), "OnFocus")]
internal static class MapPreviewFocusPatch
{
    private static void Postfix(NMapPoint __instance) => MapPreview.Show(__instance);
}

[HarmonyPatch(typeof(NMapPoint), "OnUnfocus")]
internal static class MapPreviewExitPatch
{
    private static void Postfix(NMapPoint __instance) => MapPreview.Hide(__instance);
}

internal static class MapPreview
{
    private static PanelContainer? panel;
    private static NMapPoint? owner;

    internal static void Hide(NMapPoint point)
    {
        if (owner != point) return;
        if (GodotObject.IsInstanceValid(panel)) panel!.QueueFree();
        panel = null;
        owner = null;
    }

    internal static void Show(NMapPoint point)
    {
        if (!Settings.Enabled || point.State != MapPointState.Travelable) return;
        if (owner != null) Hide(owner);
        try
        {
            var run = RunManager.Instance.DebugOnlyGetState();
            var player = LocalContext.GetMe(run);
            if (player == null || run == null || NMapScreen.Instance == null) return;
            string before = PredictionState(player, run);
            var text = Predict(point.Point, player, run);
            if (before != PredictionState(player, run))
                throw new InvalidOperationException("Prediction unexpectedly changed tracked run state.");
            owner = point;
            panel = new PanelContainer { Name = "DrawPeekMapPreview", MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 100 };
            panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
                BgColor = new Color("18232ef5"), BorderColor = new Color("b79c60"),
                BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
                ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 12, ContentMarginBottom = 12 });
            var label = new Label { Text = text, CustomMinimumSize = new Vector2(380, 0), AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
            label.AddThemeFontSizeOverride("font_size", 21);
            panel.AddChild(label);
            NMapScreen.Instance.AddChild(panel);
            label.ApplyLocaleFontSubstitution(FontType.Regular, "font");
            panel.Position = NMapScreen.Instance.GetGlobalTransform().AffineInverse() * point.GlobalPosition + new Vector2(70, -60);
            var current = panel;
            Callable.From(() => {
                if (!GodotObject.IsInstanceValid(current)) return;
                var bounds = NMapScreen.Instance.Size;
                current.Position = new Vector2(Math.Clamp(current.Position.X, 12, Math.Max(12, bounds.X - current.Size.X - 12)),
                    Math.Clamp(current.Position.Y, 80, Math.Max(80, bounds.Y - current.Size.Y - 12)));
            }).CallDeferred();
            string language = L10n.Language;
            var timer = new Godot.Timer { WaitTime = .2, Autostart = true };
            timer.Timeout += () => {
                if (!Settings.Enabled || !point.IsVisibleInTree() || point.State != MapPointState.Travelable) Hide(point);
                else if (language != L10n.Language) Show(point);
            };
            panel.AddChild(timer);
            ModEntry.Log("Map preview: " + text.Replace('\n', ' '));
            ModEntry.Log("Map preview state check: RNG, odds, relic pools and visited events unchanged.");
        }
        catch (Exception ex) { ModEntry.Log("Map preview failed: " + ex); }
    }

    private static string PredictionState(Player player, RunState run) => JsonSerializer.Serialize(new {
        RunRng = run.Rng.ToSerializable(), PlayerRng = player.PlayerRng.ToSerializable(),
        PotionOdds = player.PlayerOdds.PotionReward.CurrentValue, CardOdds = player.PlayerOdds.CardRarity.CurrentValue,
        SharedRelics = run.SharedRelicGrabBag.ToSerializable(), Relics = player.RelicGrabBag.ToSerializable(),
        Events = run.VisitedEventIds.Select(id => id.ToString()).ToArray(),
        Rooms = ((RoomSet)AccessTools.Field(typeof(ActModel), "_rooms").GetValue(run.Act)!).ToSave()
    }, new JsonSerializerOptions { IncludeFields = true });

    private static string Predict(MapPoint point, Player player, RunState run)
    {
        var lines = new List<string> { L10n.Text("下一节点 · 奖励预测", "Next node · Reward preview"), "" };
        RoomType type;
        if (point.PointType == MapPointType.Unknown)
        {
            var source = run.Odds.UnknownMapPoint;
            var odds = new UnknownMapPointOdds(GameCompat.CloneRng(run.Rng.UnknownMapPoint)) {
                MonsterOdds = source.MonsterOdds, EliteOdds = source.EliteOdds,
                TreasureOdds = source.TreasureOdds, ShopOdds = source.ShopOdds };
            type = odds.Roll(RunManager.BuildRoomTypeBlacklist(run.CurrentMapPointHistoryEntry, point.Children), run);
            lines.Add(L10n.Text("问号预计为：", "Expected ? room: ") + RoomName(type));
        }
        else type = point.PointType switch {
            MapPointType.Monster => RoomType.Monster, MapPointType.Elite => RoomType.Elite,
            MapPointType.Boss => RoomType.Boss, MapPointType.Treasure => RoomType.Treasure,
            MapPointType.Shop => RoomType.Shop, MapPointType.RestSite => RoomType.RestSite,
            MapPointType.Ancient => RoomType.Event, _ => RoomType.Unassigned };

        switch (type)
        {
            case RoomType.Monster:
            case RoomType.Elite:
            case RoomType.Boss:
                CombatRewards(lines, player, run, type);
                break;
            case RoomType.Event:
                var rooms = (RoomSet)AccessTools.Field(typeof(ActModel), "_rooms").GetValue(run.Act)!;
                EventModel? next = point.PointType == MapPointType.Ancient ? run.Act.Ancient : null;
                for (int i = 0; next == null && i < rooms.events.Count; i++) {
                    var candidate = rooms.events[(rooms.eventsVisited + i) % rooms.events.Count];
                    if (candidate.IsAllowed(run) && !run.VisitedEventIds.Contains(candidate.Id)) next = candidate;
                }
                lines.Add(next == null ? L10n.Text("事件尚不可确定", "Event not yet determined") : L10n.Text("事件：", "Event: ") + next.Title.GetFormattedText());
                break;
            case RoomType.Treasure:
                var rng = GameCompat.CloneRng(run.Rng.TreasureRoomRelics);
                var bag = RelicGrabBag.FromSerializable(run.SharedRelicGrabBag.ToSerializable());
                foreach (var recipient in run.Players) {
                    if (!Hook.ShouldGenerateTreasure(run, recipient)) continue;
                    var relic = bag.PullFromFront(RelicFactory.RollRarity(rng), run) ?? RelicFactory.FallbackRelic;
                    lines.Add(L10n.Text("宝箱遗物：", "Chest relic: ") + relic.Title.GetFormattedText());
                }
                break;
            case RoomType.Shop: lines.Add(L10n.Text("商店：库存暂不可预览", "Shop: inventory preview unavailable")); break;
            case RoomType.RestSite: lines.Add(L10n.Text("休息处：恢复生命 / 升级卡牌", "Rest site: heal / upgrade a card")); break;
            default: lines.Add(L10n.Text("此节点暂无奖励预测。", "No reward preview available for this node.")); break;
        }
        return string.Join('\n', lines);
    }

    private static void CombatRewards(List<string> lines, Player player, RunState run, RoomType type)
    {
        var encounter = run.Act.PullNextEncounter(type); // Reads the queue; does not advance it.
        lines.Add(RoomName(type) + L10n.Text("胜利后的基础奖励：", " — Base victory rewards:"));
        var rng = GameCompat.CloneRng(player.PlayerRng.Rewards);
        var potionOdds = new PotionRewardOdds(player.PlayerOdds.PotionReward.CurrentValue, rng);
        bool potion = GameCompat.RollPotion(potionOdds, player, run, type);
        int gold = rng.NextInt(encounter.MinGoldReward, encounter.MaxGoldReward + 1);
        lines.Add(L10n.Text($"基础金币：{gold}", $"Base gold: {gold}"));
        lines.Add(potion ? L10n.Text("药水：", "Potion: ") + PotionFactory.CreateRandomPotionOutOfCombat(player, rng).Title.GetFormattedText() : L10n.Text("药水：无", "Potion: none"));
        try
        {
            // Optional integration with the installed predictor. Its context owns cloned RNG and rarity odds.
            var contextType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("RandomForeseer.RandomForeseerCode.OutOfCombat.RunPredictionContext")).FirstOrDefault(t => t != null);
            if (contextType == null) {
                lines.Add(L10n.Text("卡牌候选（基础）：\n", "Card choices (base):\n") + string.Join('\n', BaseCards(player, type, rng).Select(c => "• " + c)));
                if (type == RoomType.Elite) {
                    var baseRelic = RelicGrabBag.FromSerializable(player.RelicGrabBag.ToSerializable()).PullFromFront(RelicFactory.RollRarity(rng), run) ?? RelicFactory.FallbackRelic;
                    lines.Add(L10n.Text("遗物：", "Relic: ") + baseRelic.Title.GetFormattedText());
                }
                return;
            }
            var context = Activator.CreateInstance(contextType, player)!;
            var rngSet = contextType.GetProperty("Rng")!.GetValue(context)!;
            var contextRng = (Rng)rngSet.GetType().GetProperty("Rewards")!.GetValue(rngSet)!;
            // Replay only the base reward rolls on this independent context in the game's generation order.
            var contextOdds = (PotionRewardOdds)contextType.GetProperty("PotionRewardOdds")!.GetValue(context)!;
            bool contextPotion = GameCompat.RollPotion(contextOdds, player, run, type);
            contextRng.NextInt(encounter.MinGoldReward, encounter.MaxGoldReward + 1);
            if (contextPotion) PotionFactory.CreateRandomPotionOutOfCombat(player, contextRng);
            var predictor = contextType.Assembly.GetType("RandomForeseer.RandomForeseerCode.OutOfCombat.CardRewardPrediction")!;
            var method = predictor.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "PredictCards" && m.GetParameters()[0].ParameterType == contextType);
            var cards = (IEnumerable<CardModel>)method.Invoke(null, new object?[] { context, 3, CardCreationOptions.ForRoom(player, type).WithFlags(GameCompat.CombatCardFlags), null, null })!;
            lines.Add(L10n.Text("卡牌候选：\n", "Card choices:\n") + string.Join('\n', cards.Select(c => "• " + c.Title)));
            if (type == RoomType.Elite) {
                var relic = RelicGrabBag.FromSerializable(player.RelicGrabBag.ToSerializable()).PullFromFront(RelicFactory.RollRarity(contextRng), run) ?? RelicFactory.FallbackRelic;
                lines.Add(L10n.Text("遗物：", "Relic: ") + relic.Title.GetFormattedText());
            }
        }
        catch (Exception ex) { lines.Add(L10n.Text("卡牌预测暂不可用（预测组件不兼容）。", "Card prediction unavailable (incompatible predictor).")); ModEntry.Log("Reward predictor: " + ex.Message); }
    }

    private static List<string> BaseCards(Player player, RoomType roomType, Rng rng)
    {
        var result = new List<string>();
        var used = new HashSet<ModelId>();
        var odds = new CardRarityOdds(player.PlayerOdds.CardRarity.CurrentValue, rng);
        for (int i = 0; i < 3; i++) {
            var options = CardCreationOptions.ForRoom(player, roomType).WithFlags(GameCompat.CombatCardFlags);
            options = Hook.ModifyCardRewardCreationOptions(player.RunState, player, options);
            var pool = options.GetPossibleCards(player).Where(c => !used.Contains(c.Id) &&
                c.MultiplayerConstraint != (player.RunState.Players.Count > 1 ? CardMultiplayerConstraint.SingleplayerOnly : CardMultiplayerConstraint.MultiplayerOnly)).ToArray();
            IEnumerable<CardModel> candidates;
            if (options.RarityOdds == CardRarityOddsType.Uniform)
                candidates = pool.Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient);
            else {
                var rarity = odds.Roll(options.RarityOdds);
                var seen = new HashSet<CardRarity>();
                while (!pool.Any(c => c.Rarity == rarity)) {
                    if (!seen.Add(rarity)) throw new InvalidOperationException("No eligible reward rarity.");
                    rarity = rarity.GetNextHighestRarityWithWrapping();
                }
                candidates = pool.Where(c => c.Rarity == rarity);
            }
            var canonical = rng.NextItem(candidates) ?? throw new InvalidOperationException("No eligible reward card.");
            used.Add(canonical.Id);
            var card = canonical.ToMutable();
            card.Owner = player;
            bool upgraded = false;
            if (!options.Flags.HasFlag(CardCreationFlags.NoUpgradeRoll)) {
                decimal roll = (decimal)rng.NextFloat();
                decimal chance = canonical.Rarity == CardRarity.Rare ? 0m : player.RunState.CurrentActIndex *
                    AscensionHelper.GetValueIfAscension(AscensionLevel.Scarcity, .125m, .25m);
                upgraded = card.IsUpgradable && roll <= Hook.ModifyCardRewardUpgradeOdds(player.RunState, player, card, chance);
            }
            result.Add(card.Title + (upgraded ? "+" : ""));
        }
        return result;
    }

    private static string RoomName(RoomType type) => type switch {
        RoomType.Monster => L10n.Text("普通战斗", "Normal combat"), RoomType.Elite => L10n.Text("精英战斗", "Elite combat"), RoomType.Boss => L10n.Text("首领战斗", "Boss combat"),
        RoomType.Event => L10n.Text("事件", "Event"), RoomType.Treasure => L10n.Text("宝箱", "Treasure"), RoomType.Shop => L10n.Text("商店", "Shop"),
        RoomType.RestSite => L10n.Text("休息处", "Rest site"),
        RoomType.Unassigned => L10n.Text("未知房间", "Unknown room"), _ => type.ToString() };
}

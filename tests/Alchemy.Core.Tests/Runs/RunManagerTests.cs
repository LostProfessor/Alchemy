using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Combat;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;
using Alchemy.Core.Map;
using Alchemy.Core.Relics;
using Alchemy.Core.Rooms;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.Rewards;
using Xunit;

namespace Alchemy.Core.Tests.Runs;

public class RunManagerTests
{
    // ── 辅助 ─────────────────────────────────────────────

    private static void KillAllEnemies(RunManager manager)
    {
        var combat = manager.ActiveCombat!;
        foreach (var enemy in combat.Enemies.ToList())
        {
            combat.DealDamage(new DamageContext(manager.Player, enemy, 99999));
        }
    }

    /// <summary>推进一步：根据当前阶段执行对应动作。</summary>
    private static void Step(RunManager manager)
    {
        switch (manager.Phase)
        {
            case RunPhase.OnMap:
                manager.MoveToNextRoom();
                break;
            case RunPhase.Reward:
                manager.ClaimRewardCurrency(); // 默认全拿：货币 → 额外遗物 → 三选一袋
                manager.ClaimBonusRelic();
                manager.PickReward(manager.PendingRewards![0]);
                break;
            case RunPhase.BossRelicChoice:
                manager.ChooseBossRelic(0); // 首领遗物默认选第一件
                break;
            case RunPhase.InRoom:
                if (manager.ActiveCombat != null)
                {
                    KillAllEnemies(manager);
                    manager.EndCombat(victory: true);
                }
                else if (manager.CurrentRoom is RestSiteRoom)
                {
                    manager.PerformRest(RestChoice.Sleep);
                }
                else if (manager.CurrentRoom is EventRoom)
                {
                    manager.PerformEventChoice(0);
                }
                else if (manager.CurrentRoom is TreasureRoom)
                {
                    manager.ClaimTreasure();
                }
                else if (manager.CurrentRoom is ShopRoom)
                {
                    manager.LeaveShop();
                }
                else
                {
                    throw new Exception("未知房间类型停在 InRoom");
                }

                break;
        }
    }

    /// <summary>自动打完一整局：进房→杀敌→奖励→下一房，直到通关。</summary>
    private static void PlayThrough(RunManager manager)
    {
        int guard = 0;
        while (manager.Phase is RunPhase.OnMap or RunPhase.InRoom or RunPhase.Reward or RunPhase.BossRelicChoice)
        {
            if (++guard > 500)
            {
                throw new Exception("主循环未在预算内结束");
            }

            Step(manager);
        }
    }

    /// <summary>自动结算当前房间（不在地图上移动）。</summary>
    private static void ResolveCurrentRoom(RunManager manager)
    {
        if (manager.ActiveCombat != null)
        {
            KillAllEnemies(manager);
            manager.EndCombat(victory: true);
        }
        else if (manager.CurrentRoom is RestSiteRoom)
        {
            manager.PerformRest(RestChoice.Sleep);
        }
        else if (manager.CurrentRoom is EventRoom)
        {
            manager.PerformEventChoice(0);
        }
        else if (manager.CurrentRoom is TreasureRoom)
        {
            manager.ClaimTreasure();
        }
        else if (manager.CurrentRoom is ShopRoom)
        {
            manager.LeaveShop();
        }

        if (manager.Phase == RunPhase.Reward)
        {
            manager.ClaimRewardCurrency();
            manager.ClaimBonusRelic();
            manager.PickReward(manager.PendingRewards![0]);
        }
    }

    /// <summary>BFS：从 from 找一条沿子节点到满足 goal 节点的路径（不含 from，含目标）。</summary>
    private static List<MapPoint>? FindPath(StandardActMap map, MapPoint from, Func<MapPoint, bool> goal)
    {
        var prev = new Dictionary<MapPoint, MapPoint>();
        var visited = new HashSet<MapPoint> { from };
        var queue = new Queue<MapPoint>();
        queue.Enqueue(from);
        MapPoint? found = null;
        while (queue.Count > 0 && found == null)
        {
            var current = queue.Dequeue();
            if (goal(current))
            {
                found = current;
                break;
            }

            foreach (var child in current.Children)
            {
                if (visited.Add(child))
                {
                    prev[child] = current;
                    queue.Enqueue(child);
                }
            }
        }

        if (found == null)
        {
            return null;
        }

        var path = new List<MapPoint>();
        for (var p = found; !ReferenceEquals(p, from); p = prev[p])
        {
            path.Add(p);
        }

        path.Reverse();
        return path;
    }

    /// <summary>自动推进直到 stop 条件满足（或步数耗尽）。</summary>
    private static void PlayUntil(RunManager manager, Func<RunManager, bool> stop)
    {
        int guard = 0;
        while (!stop(manager) && guard++ < 1000)
        {
            Step(manager);
        }
    }

    // ── 种子随机 ─────────────────────────────────────────

    private static List<int> Draw(RunRandom r, int count)
    {
        var list = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(r.Next().Next(10000));
        }

        return list;
    }

    [Fact]
    public void RunRandom_SameSeed_SameStream()
    {
        var a = new RunRandom(5);
        var b = new RunRandom(5);

        Assert.Equal(Draw(a, 8), Draw(b, 8));
    }

    [Fact]
    public void RunRandom_DifferentSeed_DifferentStream()
    {
        var a = new RunRandom(5);
        var b = new RunRandom(6);

        Assert.NotEqual(Draw(a, 8), Draw(b, 8));
    }

    // ── 主循环 ───────────────────────────────────────────

    [Fact]
    public void StartRun_SetsPhaseOnMap_AndGeneratesRooms()
    {
        var manager = new RunManager(42);
        Assert.Equal(RunPhase.NotStarted, manager.Phase);

        manager.StartRun();

        Assert.Equal(RunPhase.OnMap, manager.Phase);
        Assert.NotNull(manager.PeekNextRoom());
    }

    [Fact]
    public void MoveToNextRoom_BeforeStart_Throws()
    {
        var manager = new RunManager(1);

        Assert.Throws<InvalidOperationException>(() => manager.MoveToNextRoom());
    }

    [Fact]
    public void SameSeed_SameRoomSequence_AndSameRun()
    {
        var a = new RunManager(7, startingHp: 999);
        a.StartRun();
        PlayThrough(a);

        var b = new RunManager(7, startingHp: 999);
        b.StartRun();
        PlayThrough(b);

        Assert.Equal(a.VisitedRooms.Select(r => r.Id), b.VisitedRooms.Select(r => r.Id));
        Assert.Equal(a.VisitedRooms.Select(r => r.Type), b.VisitedRooms.Select(r => r.Type));
        Assert.Equal(a.Run.Pocket.Counts, b.Run.Pocket.Counts); // 奖励也一致
    }

    [Fact]
    public void PlayThrough_CompletesRun_WithRewards()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();

        PlayThrough(manager);

        Assert.Equal(RunPhase.Completed, manager.Phase);
        Assert.True(manager.Run.Pocket.TotalCount > 0, "整局应至少获得过药材奖励");
        Assert.True(manager.VisitedRooms.Count > 1);
    }

    [Fact]
    public void EndCombat_Defeat_EndsRunDefeated()
    {
        var manager = new RunManager(42, startingHp: 30);
        manager.StartRun();

        int guard = 0;
        while (manager.Phase == RunPhase.OnMap)
        {
            if (++guard > 50)
            {
                throw new Exception("未在预算内遇到战斗房");
            }

            manager.MoveToNextRoom();
            if (manager.Phase == RunPhase.InRoom && manager.ActiveCombat != null)
            {
                manager.EndCombat(victory: false);
                break;
            }
        }

        Assert.Equal(RunPhase.Defeated, manager.Phase);
        Assert.Null(manager.ActiveCombat);
    }

    [Fact]
    public void PickReward_AppliesToPocket_ReturnsToMap()
    {
        var manager = new RunManager(1, startingHp: 999);
        manager.StartRun();

        // 推进到第一场战斗并胜利 → Reward 阶段
        while (manager.Phase == RunPhase.OnMap)
        {
            manager.MoveToNextRoom();
            if (manager.Phase == RunPhase.InRoom && manager.ActiveCombat != null)
            {
                KillAllEnemies(manager);
                manager.EndCombat(victory: true);
                break;
            }
        }

        Assert.Equal(RunPhase.Reward, manager.Phase);
        Assert.NotNull(manager.PendingRewards);
        Assert.Equal(3, manager.PendingRewards!.Count);

        var bag = manager.PendingRewards[0];
        int before = manager.Run.Pocket.TotalCount;
        int expected = bag.Items.OfType<IngredientReward>().Sum(i => i.Count);

        manager.ClaimRewardCurrency(); // 先领货币，否则奖励未结算不回到地图
        manager.PickReward(bag);

        Assert.Equal(RunPhase.OnMap, manager.Phase);
        Assert.Null(manager.PendingRewards);
        Assert.Equal(before + expected, manager.Run.Pocket.TotalCount);
    }

    [Fact]
    public void AbandonRun_SetsPhaseAbandoned()
    {
        var manager = new RunManager(1);
        manager.StartRun();

        manager.AbandonRun();

        Assert.Equal(RunPhase.Abandoned, manager.Phase);
        Assert.Null(manager.CurrentRoom);
    }

    [Fact]
    public void RestRoom_Sleep_HealsPlayer_ThenBackToMap()
    {
        var manager = new RunManager(42, startingHp: 100);
        manager.StartRun();

        int guard = 0;
        while (manager.Phase is RunPhase.OnMap or RunPhase.InRoom or RunPhase.Reward)
        {
            if (++guard > 200)
            {
                throw new Exception("未在预算内遇到火堆");
            }

            if (manager.Phase == RunPhase.InRoom && manager.CurrentRoom is RestSiteRoom)
            {
                int before = manager.Player.CurrentHp;
                manager.PerformRest(RestChoice.Sleep);

                Assert.Equal(RunPhase.OnMap, manager.Phase);
                Assert.True(manager.CurrentRoom.Completed);
                Assert.True(manager.Player.CurrentHp >= before); // 睡觉至少不回退
                return;
            }

            Step(manager);
        }

        throw new Exception("未遇到火堆");
    }

    [Fact]
    public void CombatReward_AddsCurrency_OnVictory()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();

        PlayThrough(manager);

        Assert.Equal(RunPhase.Completed, manager.Phase);
        Assert.True(manager.Run.Currency > 0, "每次战斗胜利都应给货币奖励");
    }

    [Fact]
    public void EliteReward_GrantsBonusRelic()
    {
        var rng = new BattleRandom(3);

        var roll = CombatRewards.RollRewards(rng, Array.Empty<string>(), isElite: true, isBoss: false);

        Assert.True(roll.Currency >= 50 && roll.Currency <= 80, "精英货币应在 50~80");
        Assert.NotNull(roll.BonusRelic);
    }

    [Fact]
    public void BossReward_NoDirectRelic_OnlyCurrency()
    {
        var rng = new BattleRandom(3);

        // 首领战后不再直发遗物（改跨层三选一）
        var roll = CombatRewards.RollRewards(rng, Array.Empty<string>(), isElite: false, isBoss: true, bossId: "witch_boss");

        Assert.True(roll.Currency >= 100 && roll.Currency <= 150, "首领货币应在 100~150");
        Assert.Null(roll.BonusRelic);
    }

    [Fact]
    public void NormalReward_NoBonusRelic()
    {
        var rng = new BattleRandom(3);

        var roll = CombatRewards.RollRewards(rng, Array.Empty<string>(), isElite: false, isBoss: false);

        Assert.True(roll.Currency >= 25 && roll.Currency <= 45, "普通货币应在 25~45");
        Assert.Null(roll.BonusRelic);
    }

    [Fact]
    public void BossCombat_GrantsBossRelic_ThenCompletes()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();

        PlayThrough(manager);

        Assert.Equal(RunPhase.Completed, manager.Phase);
        Assert.True(manager.Run.Relics.Relics.Any(r => r.Rarity == RelicRarity.Boss), "通关后应拥有首领遗物");
    }

    // ── 地图分支选择 ──────────────────────────────────────

    [Fact]
    public void TryMoveTo_ValidChild_Moves()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();
        var next = manager.AvailableNextPoints.First();

        Assert.True(manager.TryMoveTo(next));
        Assert.Equal(next, manager.CurrentMapPoint);
        Assert.Equal(RunPhase.InRoom, manager.Phase);
    }

    [Fact]
    public void TryMoveTo_NonChild_ReturnsFalse()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();
        var unreachable = manager.Map!.AllPoints.First(p => !manager.AvailableNextPoints.Contains(p));

        Assert.False(manager.TryMoveTo(unreachable));
        Assert.Equal(RunPhase.OnMap, manager.Phase);
        Assert.Null(manager.CurrentRoom);
    }

    [Fact]
    public void TryMoveTo_FromStart_CanPickAnyRow1Point()
    {
        var manager = new RunManager(7, startingHp: 999);
        manager.StartRun();
        var row1 = manager.Map!.PointsInRow(1);
        Assert.True(row1.Count > 0);

        var target = row1.Count > 1 ? row1[^1] : row1[0]; // 故意选非第一个
        Assert.True(manager.TryMoveTo(target));
        Assert.Equal(target, manager.CurrentMapPoint);
    }

    [Fact]
    public void AvailableNextPoints_AreChildrenOfCurrent()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();

        Assert.Equal(manager.CurrentMapPoint!.Children, manager.AvailableNextPoints);
    }

    [Fact]
    public void VisitedMapPoints_TracksPath()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();
        var first = manager.AvailableNextPoints.First();

        Assert.True(manager.TryMoveTo(first));

        Assert.Contains(first, manager.VisitedMapPoints);
        Assert.Single(manager.VisitedMapPoints);
    }

    [Fact]
    public void TryMoveTo_CanChoosePathToTreasure()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();
        var map = manager.Map!;
        var treasure = map.AllPoints.First(p => p.Type == MapPointType.Treasure);
        var path = FindPath(map, map.Start, p => p == treasure);

        Assert.NotNull(path);
        Assert.True(path!.Count > 0, "应存在到宝藏的路径");

        foreach (var step in path)
        {
            Assert.True(manager.TryMoveTo(step), $"应能移动到 {step}");
            ResolveCurrentRoom(manager);
            Assert.Equal(RunPhase.OnMap, manager.Phase);
        }

        // 已走到并领取宝藏（ClaimTreasure 后 CurrentRoom 仍是宝藏房）
        Assert.IsType<TreasureRoom>(manager.CurrentRoom);
        Assert.True(manager.CurrentRoom.Completed);
    }

    [Fact]
    public void Treasure_CanClaimPartsIndividually_SkipRest_ThenFinish()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();
        var map = manager.Map!;
        var treasure = map.AllPoints.First(p => p.Type == MapPointType.Treasure);
        var path = FindPath(map, map.Start, p => p == treasure);

        Assert.NotNull(path);
        foreach (var step in path!)
        {
            Assert.True(manager.TryMoveTo(step));
            if (manager.CurrentRoom is TreasureRoom)
            {
                break; // 进入宝箱（未领任何东西）就停
            }

            ResolveCurrentRoom(manager);
        }

        var room = Assert.IsType<TreasureRoom>(manager.CurrentRoom);
        var reward = room.Treasure!;
        Assert.False(reward.AllClaimed);
        Assert.False(reward.CurrencyClaimed);
        Assert.False(reward.BagClaimed);
        Assert.Equal(RunPhase.InRoom, manager.Phase);

        int currencyBefore = manager.Run.Currency;
        int relicsBefore = manager.Run.Relics.Count;
        int pocketBefore = manager.Run.Pocket.TotalCount;

        // 只领货币 → 仍在房内（房未完成），遗物/袋未动
        Assert.True(manager.ClaimTreasureCurrency());
        Assert.Equal(currencyBefore + reward.Currency, manager.Run.Currency);
        Assert.True(reward.CurrencyClaimed);
        Assert.False(reward.BagClaimed);
        Assert.False(reward.AllClaimed);
        Assert.Equal(RunPhase.InRoom, manager.Phase);
        Assert.False(room.Completed);

        // 领药材袋（跳过遗物）→ 遗物保持未领
        Assert.True(manager.ClaimTreasureBag());
        Assert.True(manager.Run.Pocket.TotalCount > pocketBefore);
        Assert.False(reward.AllClaimed);
        if (reward.Relic != null)
        {
            Assert.False(reward.RelicClaimed);
            Assert.Equal(relicsBefore, manager.Run.Relics.Count); // 遗物被放弃未入账
        }

        // 放弃剩余离开 → 完成房间回地图
        manager.FinishTreasure();
        Assert.Equal(RunPhase.OnMap, manager.Phase);
        Assert.True(room.Completed);
    }

    [Fact]
    public void Treasure_ClaimAll_FillsEverything_AndFinishes()
    {
        var manager = new RunManager(7, startingHp: 999);
        manager.StartRun();
        var map = manager.Map!;
        var treasure = map.AllPoints.First(p => p.Type == MapPointType.Treasure);
        var path = FindPath(map, map.Start, p => p == treasure);

        Assert.NotNull(path);
        foreach (var step in path!)
        {
            Assert.True(manager.TryMoveTo(step));
            if (manager.CurrentRoom is TreasureRoom)
            {
                break;
            }

            ResolveCurrentRoom(manager);
        }

        var room = Assert.IsType<TreasureRoom>(manager.CurrentRoom);
        var reward = room.Treasure!;

        Assert.True(manager.ClaimTreasureCurrency());
        if (reward.Relic != null)
        {
            Assert.True(manager.ClaimTreasureRelic());
        }

        Assert.True(manager.ClaimTreasureBag());
        Assert.True(reward.AllClaimed);
        Assert.Equal(RunPhase.InRoom, manager.Phase); // 全领但尚未离开

        manager.FinishTreasure();
        Assert.Equal(RunPhase.OnMap, manager.Phase);
        Assert.True(room.Completed);
        Assert.True(manager.Run.Pocket.TotalCount > 0);
    }

    [Fact]
    public void Shop_BuyIngredientBag_ChargesCurrency_AddsToPocket_NoDoubleBuy()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();
        manager.Run.Currency = 5000; // 够买任何东西
        var map = manager.Map!;
        var shop = map.AllPoints.First(p => p.Type == MapPointType.Shop);
        var path = FindPath(map, map.Start, p => p == shop);

        Assert.NotNull(path);
        foreach (var step in path!)
        {
            Assert.True(manager.TryMoveTo(step));
            if (manager.CurrentRoom is ShopRoom)
            {
                break; // 进入商店就停
            }

            ResolveCurrentRoom(manager);
        }

        var room = Assert.IsType<ShopRoom>(manager.CurrentRoom);
        int bagIndex = room.Stock.FindIndex(i => i.IsIngredientBag);
        Assert.True(bagIndex >= 0, "商店应有药材袋商品");
        var item = room.Stock[bagIndex];
        Assert.True(item.Price > 0);

        int pocketBefore = manager.Run.Pocket.TotalCount;
        int currencyBefore = manager.Run.Currency;

        Assert.True(manager.TryBuyItem(bagIndex));
        Assert.Equal(currencyBefore - item.Price, manager.Run.Currency);
        Assert.True(manager.Run.Pocket.TotalCount > pocketBefore); // 药材入口袋
        Assert.True(item.Sold);

        // 已售不可重复购买
        Assert.False(manager.TryBuyItem(bagIndex));

        manager.LeaveShop();
        Assert.Equal(RunPhase.OnMap, manager.Phase);
        Assert.True(room.Completed);
    }

    // ── 多大大层推进 ──────────────────────────────────────

    [Fact]
    public void StartRun_ActIndex_Is1()
    {
        var manager = new RunManager(42);
        manager.StartRun();

        Assert.Equal(1, manager.ActIndex);
    }

    [Fact]
    public void BossClear_AdvancesToNextAct_KeepsHp()
    {
        var manager = new RunManager(42, startingHp: 100);
        manager.StartRun();

        // 打到第 1 大层首领战胜利（Reward 阶段、未领奖）
        PlayUntil(manager, m => m.Phase == RunPhase.Reward && m.IsBossRewardPending);
        Assert.Equal(1, manager.ActIndex);
        int hpAtBoss = manager.Player.CurrentHp;

        manager.ClaimRewardCurrency();
        manager.ClaimBonusRelic();
        manager.PickReward(manager.PendingRewards![0]); // 领完战斗奖励 → 进入首领遗物三选一

        Assert.Equal(RunPhase.BossRelicChoice, manager.Phase);
        Assert.Equal(1, manager.ActIndex);

        manager.ChooseBossRelic(0); // 选一件首领遗物 → 进入第 2 大层

        Assert.Equal(2, manager.ActIndex);
        Assert.Equal(RunPhase.OnMap, manager.Phase);
        Assert.Equal(hpAtBoss, manager.Player.CurrentHp); // 跨大层不回血
        Assert.NotNull(manager.Map);
        Assert.Empty(manager.VisitedRooms); // 新大层地图从零开始
    }

    [Fact]
    public void BossRelicChoice_CandidatesFromThatBossPool()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();

        // 打到第 1 层首领战后（Reward 阶段），领完战斗奖励
        PlayUntil(manager, m => m.Phase == RunPhase.Reward && m.IsBossRewardPending);
        manager.ClaimRewardCurrency();
        manager.ClaimBonusRelic();
        manager.PickReward(manager.PendingRewards![0]);

        Assert.Equal(RunPhase.BossRelicChoice, manager.Phase);
        Assert.NotNull(manager.PendingBossRelicChoices);
        // 第 1 层首领 = goblin_king，其专属池只有 goblin_crown
        foreach (var relic in manager.PendingBossRelicChoices!)
        {
            Assert.Equal("goblin_crown", relic.Id);
        }
    }

    [Fact]
    public void StartRun_PreparesOpeningRelicChoices()
    {
        var manager = new RunManager(42);
        manager.StartRun();

        Assert.NotNull(manager.PendingOpeningRelicChoices);
        Assert.InRange(manager.PendingOpeningRelicChoices!.Count, 1, 3);
        // 候选来自普通池，不应与职业遗物重复
        var jobRelic = manager.Run.Relics.Relics.First().Id;
        Assert.DoesNotContain(manager.PendingOpeningRelicChoices, r => r.Id == jobRelic);

        int before = manager.Run.Relics.Count;
        manager.ChooseOpeningRelic(0);
        Assert.Equal(before + 1, manager.Run.Relics.Count);
        Assert.Null(manager.PendingOpeningRelicChoices);
        Assert.Equal(RunPhase.OnMap, manager.Phase); // 选完仍在地图阶段，可直接进房
    }

    [Fact]
    public void FinalBoss_Win_GoesStraightToCompleted()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();

        // 最终 Boss 打赢即通关：不经过 Reward / BossRelicChoice
        PlayUntil(manager, m => m.Phase == RunPhase.Completed);

        Assert.Equal(RunPhase.Completed, manager.Phase);
        Assert.Equal(RunManager.TotalActs, manager.ActIndex);
    }

    [Fact]
    public void PlayThrough_CompletesFiveActs()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();

        PlayThrough(manager);

        Assert.Equal(RunPhase.Completed, manager.Phase);
        Assert.Equal(RunManager.TotalActs, manager.ActIndex);
    }

    [Fact]
    public void AbandonCombat_ClearsCombatAndReturnsToMap()
    {
        var manager = new RunManager(5);
        manager.StartRun();

        // 一路推进直到进入一个战斗房（中途非战斗房顺手结算）
        int guard = 0;
        while (guard++ < 500)
        {
            if (manager.Phase == RunPhase.OnMap)
            {
                manager.MoveToNextRoom();
                continue;
            }

            if (manager.Phase == RunPhase.InRoom && manager.ActiveCombat != null)
            {
                break; // 到达战斗房，停在战斗中
            }

            if (manager.Phase == RunPhase.InRoom)
            {
                ResolveCurrentRoom(manager);
                continue;
            }

            if (manager.Phase == RunPhase.Reward)
            {
                manager.PickReward(manager.PendingRewards![0]);
                continue;
            }

            throw new Exception($"意外阶段：{manager.Phase}");
        }

        Assert.NotNull(manager.ActiveCombat);
        Assert.Equal(RunPhase.InRoom, manager.Phase);

        manager.AbandonCombat();

        Assert.Null(manager.ActiveCombat);
        Assert.Null(manager.Player.Combat);
        Assert.Equal(RunPhase.OnMap, manager.Phase);

        // 回地图后能继续移动（不卡死）
        var next = manager.AvailableNextPoints.FirstOrDefault();
        Assert.NotNull(next);
        Assert.True(manager.TryMoveTo(next!));
    }

    [Fact]
    public void DifferentActs_UseDifferentBosses()
    {
        var boss1 = Alchemy.Core.Encounters.EncounterFactory.BossForAct(1);
        var boss5 = Alchemy.Core.Encounters.EncounterFactory.BossForAct(5);

        Assert.NotEqual(boss1.BossId, boss5.BossId);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alchemy.Core.Combat;
using Alchemy.Core.GameData;
using Alchemy.Core.Hooks;
using Alchemy.Core.Rooms;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.History;
using Alchemy.Core.Runs.Saves;
using Xunit;

namespace Alchemy.Core.Tests.Runs;

public class RunSaveTests
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

    private static void MoveToNextAndResolve(RunManager manager)
    {
        manager.MoveToNextRoom();
        ResolveCurrentRoom(manager);
    }

    // ── 经历记录 RunHistory ──────────────────────────────

    [Fact]
    public void History_RecordsOrderedEvents()
    {
        var history = new RunHistory();
        history.RecordRoomEntered(1, "combat_1_1", "进入");
        history.RecordHpChange(1, "combat_1_1", 30, 22);
        history.RecordIngredientConsumed(1, "combat_1_1", "glowcap");
        history.RecordReward(1, "combat_1_1", "货币+12", amount: 12);
        history.RecordRoomCompleted(1, "combat_1_1", "完成");

        Assert.Equal(5, history.Entries.Count);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, history.Entries.Select(e => e.Order).ToArray());
        Assert.Equal(HistoryEventType.RoomEntered, history.Entries[0].Type);
        Assert.Equal(HistoryEventType.HpChanged, history.Entries[1].Type);
        Assert.Equal(30, history.Entries[1].HpBefore);
        Assert.Equal(22, history.Entries[1].HpAfter);
        Assert.Equal(-8, history.Entries[1].Amount);
        Assert.Equal(HistoryEventType.IngredientConsumed, history.Entries[2].Type);
        Assert.Equal("glowcap", history.Entries[2].ItemId);
        Assert.Equal(12, history.Entries[3].Amount);
        Assert.Equal(HistoryEventType.RoomCompleted, history.Entries[4].Type);
    }

    [Fact]
    public void History_Restore_ContinuesOrder()
    {
        var history = new RunHistory();
        history.RecordRoomEntered(1, "r1", "a");
        history.RecordRoomCompleted(1, "r1", "b");

        var restored = new RunHistory();
        restored.Restore(history.Entries.ToList());
        Assert.Equal(2, restored.Entries.Count);

        restored.RecordRoomEntered(1, "r2", "c");
        Assert.Equal(3, restored.Entries.Count);
        Assert.Equal(2, restored.Entries[^1].Order); // 顺序号继续递增
    }

    // ── SaveService JSON ─────────────────────────────────

    [Fact]
    public void SaveService_JsonRoundTrip_PreservesAllFields()
    {
        var data = new RunSaveData
        {
            Version = 1,
            MasterSeed = 123,
            StreamCounter = 7,
            ActIndex = 2,
            CurrentMapSeed = 999,
            CurrentMapCol = 3,
            CurrentMapRow = 4,
            PlayerMaxHp = 35,
            PlayerCurrentHp = 20,
            Currency = 88,
            Pocket = new Dictionary<string, int> { ["glowcap"] = 2, ["ash"] = 1 },
            RelicIds = new List<string> { "iron_bracer" },
            History = new List<HistoryEntry>
            {
                new HistoryEntry
                {
                    Order = 0,
                    Act = 1,
                    RoomId = "combat_1_1",
                    Type = HistoryEventType.RoomEntered,
                    Message = "进入",
                    Amount = 0,
                    HpBefore = 30,
                    HpAfter = 30,
                    ItemId = "",
                },
            },
        };

        var json = SaveService.Serialize(data);
        Assert.Contains("\"Pocket\"", json);
        Assert.Contains("\"History\"", json);

        var back = SaveService.Deserialize(json)!;
        Assert.NotNull(back);
        Assert.Equal(1, back.Version);
        Assert.Equal(123, back.MasterSeed);
        Assert.Equal(7, back.StreamCounter);
        Assert.Equal(2, back.ActIndex);
        Assert.Equal(999, back.CurrentMapSeed);
        Assert.Equal(3, back.CurrentMapCol);
        Assert.Equal(4, back.CurrentMapRow);
        Assert.Equal(35, back.PlayerMaxHp);
        Assert.Equal(20, back.PlayerCurrentHp);
        Assert.Equal(88, back.Currency);
        Assert.Equal(2, back.Pocket["glowcap"]);
        Assert.Equal(1, back.Pocket["ash"]);
        Assert.Equal(new[] { "iron_bracer" }, back.RelicIds);
        Assert.Single(back.History);
        Assert.Equal(HistoryEventType.RoomEntered, back.History[0].Type);
        Assert.Equal("combat_1_1", back.History[0].RoomId);
        Assert.Equal(30, back.History[0].HpBefore);
    }

    [Fact]
    public void SaveService_FileRoundTrip()
    {
        var data = new RunSaveData { MasterSeed = 5, PlayerCurrentHp = 30 };
        var path = Path.Combine(Path.GetTempPath(), $"alchemy_save_{Guid.NewGuid():N}.json");
        try
        {
            SaveService.SaveToFile(data, path);
            Assert.True(File.Exists(path));

            var back = SaveService.LoadFromFile(path);
            Assert.NotNull(back);
            Assert.Equal(5, back.MasterSeed);
            Assert.Equal(30, back.PlayerCurrentHp);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    // ── RunManager 存档/读档 ─────────────────────────────

    [Fact]
    public void SaveAndLoad_PreservesRunState_AndCanContinue()
    {
        var manager = new RunManager(42, startingHp: 40);
        manager.StartRun();
        MoveToNextAndResolve(manager); // 房 1
        MoveToNextAndResolve(manager); // 房 2
        MoveToNextAndResolve(manager); // 房 3

        var data = manager.CreateSaveData();

        // 保存前快照"下一个随机流"（验证读档后随机确定性）
        int expectedNext = manager.Random.Next().Next(10000);

        var json = SaveService.Serialize(data);
        var backData = SaveService.Deserialize(json)!;
        var loaded = RunManager.LoadFromSaveData(backData);

        Assert.Equal(manager.ActIndex, loaded.ActIndex);
        Assert.Equal(manager.Player.MaxHp, loaded.Player.MaxHp);
        Assert.Equal(manager.Player.CurrentHp, loaded.Player.CurrentHp);
        Assert.Equal(manager.Run.Currency, loaded.Run.Currency);
        Assert.Equal(
            manager.Run.Pocket.Counts.OrderBy(kv => kv.Key),
            loaded.Run.Pocket.Counts.OrderBy(kv => kv.Key));
        Assert.Equal(
            manager.Run.Relics.Relics.Select(r => r.Id),
            loaded.Run.Relics.Relics.Select(r => r.Id));
        Assert.Equal(manager.History.Entries.Count, loaded.History.Entries.Count);

        Assert.Equal(RunPhase.OnMap, loaded.Phase);
        Assert.NotNull(loaded.Map);
        Assert.NotNull(loaded.CurrentMapPoint);

        // 随机流确定性恢复
        Assert.Equal(expectedNext, loaded.Random.Next().Next(10000));

        // 读档后可继续推进
        MoveToNextAndResolve(loaded);
        Assert.Equal(RunPhase.OnMap, loaded.Phase);
        Assert.True(loaded.History.Entries.Count > manager.History.Entries.Count);
    }

    [Fact]
    public void SaveAndLoad_UnknownRelicId_IsSkipped()
    {
        var data = new RunSaveData
        {
            MasterSeed = 3,
            ActIndex = 1,
            RelicIds = new List<string> { "does_not_exist" },
            PlayerMaxHp = 30,
            PlayerCurrentHp = 30,
        };

        var loaded = RunManager.LoadFromSaveData(data);
        Assert.Empty(loaded.Run.Relics.Relics); // 未知遗物跳过，不崩溃
        Assert.Equal(RunPhase.OnMap, loaded.Phase);
    }

    // ── 历史完整性与炼药记录 ─────────────────────────────

    [Fact]
    public void Playthrough_RecordsRoomEnteredAndCompletedAndHp()
    {
        var manager = new RunManager(7);
        manager.StartRun();
        MoveToNextAndResolve(manager); // 第一房（战斗）

        var entries = manager.History.Entries;
        Assert.Contains(entries, e => e.Type == HistoryEventType.RoomEntered);
        Assert.Contains(entries, e => e.Type == HistoryEventType.RoomCompleted);
        Assert.Contains(entries, e => e.Type == HistoryEventType.HpChanged);

        // 战斗房必然产出货币奖励
        Assert.Contains(entries, e => e.Type == HistoryEventType.RewardObtained && e.Amount > 0);

        // 每个已完成的房间都有对应的"进入"记录
        foreach (var roomId in entries
                     .Where(e => e.Type == HistoryEventType.RoomCompleted)
                     .Select(e => e.RoomId))
        {
            Assert.Contains(entries, e => e.Type == HistoryEventType.RoomEntered && e.RoomId == roomId);
        }
    }

    [Fact]
    public void Brewing_RecordsIngredientConsumed()
    {
        var manager = new RunManager(11);
        manager.StartRun();
        manager.MoveToNextRoom(); // 第一房是战斗
        Assert.NotNull(manager.ActiveCombat);

        // 开局赠送 2 个 glowcap；再加 1 个用于验证消耗扣减
        int before = manager.Run.Pocket.CountOf("glowcap");
        manager.Run.Pocket.Add("glowcap", 1);
        var ingredient = Ingredients.Default.All.First(i => i.Id == "glowcap");
        var session = manager.CreateBrewingSession(manager.ActiveCombat!);
        session.StartBrew(BaseLiquids.Aqua);
        Assert.True(session.TryStartAddIngredient(ingredient));

        Assert.Contains(manager.History.Entries,
            e => e.Type == HistoryEventType.IngredientConsumed && e.ItemId == "glowcap");
        Assert.Equal(before, manager.Run.Pocket.CountOf("glowcap")); // 消耗掉的是额外加的 1 个
    }

    // ── 奖励界面存档/读档（Reward / BossRelicChoice）──────────────────

    private static void Step(RunManager manager)
    {
        switch (manager.Phase)
        {
            case RunPhase.OnMap:
                manager.MoveToNextRoom();
                break;
            case RunPhase.Reward:
                manager.ClaimRewardCurrency();
                manager.ClaimBonusRelic();
                manager.PickReward(manager.PendingRewards![0]);
                break;
            case RunPhase.BossRelicChoice:
                manager.ChooseBossRelic(0);
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

                break;
        }
    }

    private static void PlayUntil(RunManager manager, Func<RunManager, bool> stop)
    {
        int guard = 0;
        while (!stop(manager) && guard++ < 1000)
        {
            Step(manager);
        }
    }

    private static RunManager SaveRoundTrip(RunManager manager) =>
        RunManager.LoadFromSaveData(SaveService.Deserialize(SaveService.Serialize(manager.CreateSaveData()))!);

    [Fact]
    public void SaveDuringReward_LoadRestoresRewardPhase_AndCanStillClaim()
    {
        var manager = new RunManager(1, startingHp: 999);
        manager.StartRun();

        // 推进到第一场战斗并胜利 → Reward 阶段（先不领）
        PlayUntil(manager, m => m.Phase == RunPhase.Reward);
        Assert.Equal(RunPhase.Reward, manager.Phase);
        Assert.NotNull(manager.PendingRewards);
        Assert.Equal(3, manager.PendingRewards!.Count);
        int? currency = manager.PendingRewardCurrency;
        Assert.NotNull(currency);

        var loaded = SaveRoundTrip(manager);

        // 读档后回到同一奖励界面，待领块原样放回
        Assert.Equal(RunPhase.Reward, loaded.Phase);
        Assert.Equal(3, loaded.PendingRewards!.Count);
        Assert.Equal(currency, loaded.PendingRewardCurrency);

        // 读档后照常领取 → 回地图，货币入账
        loaded.ClaimRewardCurrency();
        loaded.PickReward(loaded.PendingRewards[0]);
        Assert.Equal(RunPhase.OnMap, loaded.Phase);
        Assert.Equal(manager.Run.Currency + currency!.Value, loaded.Run.Currency);
    }

    [Fact]
    public void SaveDuringBossReward_Load_ThenClaimStillLeadsToBossRelicChoice()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();

        // 打到第 1 大层首领战胜利（Reward 阶段、未领奖）
        PlayUntil(manager, m => m.Phase == RunPhase.Reward && m.IsBossRewardPending);
        Assert.True(manager.IsBossRewardPending);
        Assert.Equal(1, manager.ActIndex);

        var loaded = SaveRoundTrip(manager);

        Assert.Equal(RunPhase.Reward, loaded.Phase);
        Assert.True(loaded.IsBossRewardPending);

        // 读档后领完战斗奖励 → 仍能凭 Boss 池 id 进入首领遗物三选一
        loaded.ClaimRewardCurrency();
        loaded.ClaimBonusRelic();
        loaded.PickReward(loaded.PendingRewards![0]);
        Assert.Equal(RunPhase.BossRelicChoice, loaded.Phase);
        Assert.NotNull(loaded.PendingBossRelicChoices);

        loaded.ChooseBossRelic(0);
        Assert.Equal(2, loaded.ActIndex);
        Assert.Equal(RunPhase.OnMap, loaded.Phase);
    }

    [Fact]
    public void SaveDuringBossRelicChoice_LoadRestoresChoices_AndAdvances()
    {
        var manager = new RunManager(42, startingHp: 999);
        manager.StartRun();

        // 打到首领战后进入 BossRelicChoice（候选已备好）
        PlayUntil(manager, m => m.Phase == RunPhase.Reward && m.IsBossRewardPending);
        manager.ClaimRewardCurrency();
        manager.ClaimBonusRelic();
        manager.PickReward(manager.PendingRewards![0]);
        Assert.Equal(RunPhase.BossRelicChoice, manager.Phase);

        var expectedIds = manager.PendingBossRelicChoices!.Select(r => r.Id).ToList();
        Assert.NotEmpty(expectedIds);

        var loaded = SaveRoundTrip(manager);

        // 读档后直接回到首领遗物三选一，候选一致
        Assert.Equal(RunPhase.BossRelicChoice, loaded.Phase);
        Assert.Equal(expectedIds, loaded.PendingBossRelicChoices!.Select(r => r.Id).ToList());

        loaded.ChooseBossRelic(0);
        Assert.Equal(2, loaded.ActIndex);
        Assert.Equal(RunPhase.OnMap, loaded.Phase);
    }

    // ── 事件房扣血致死 ─────────────────────────────────

    [Fact]
    public void EventChoice_ThatKillsPlayer_EndsRunDefeated()
    {
        // 默认事件目录含 废弃药架(abandoned_shelf)：第 2 个选项"冒险深入"扣 12 血。
        // 遍历种子直到进入该事件房，压低血量验证"扣到 0 → 直接判负、不回地图、不完成房间"。
        for (int seed = 1; seed <= 300; seed++)
        {
            var manager = new RunManager(seed, startingHp: 999);
            manager.StartRun();

            int guard = 0;
            while (guard++ < 400)
            {
                if (manager.Phase == RunPhase.OnMap)
                {
                    manager.MoveToNextRoom();
                    continue;
                }

                if (manager.Phase == RunPhase.InRoom
                    && manager.CurrentRoom is EventRoom er
                    && er.Event?.Id == "abandoned_shelf")
                {
                    manager.Player.CurrentHp = 5; // 12 伤 → 0 血致死
                    manager.PerformEventChoice(1); // 冒险深入（失去 12 生命）

                    Assert.Equal(RunPhase.Defeated, manager.Phase);
                    Assert.Equal(0, manager.Player.CurrentHp);
                    Assert.Null(manager.CurrentRoom); // EndRun 清理当前房（房未完成）
                    return;
                }

                if (manager.Phase == RunPhase.InRoom)
                {
                    ResolveCurrentRoom(manager); // 战斗/火堆/宝箱/商店/其它事件 → 结算回地图
                    continue;
                }

                if (manager.Phase == RunPhase.Reward)
                {
                    manager.ClaimRewardCurrency();
                    manager.ClaimBonusRelic();
                    manager.PickReward(manager.PendingRewards![0]);
                    continue;
                }

                break; // Defeated/Completed（不该出现，换种子）
            }
        }

        Assert.Fail("300 个种子都没在事件房遇到 废弃药架 的扣血选项，测试环境异常");
    }

    [Fact]
    public void EventChoice_NonLethal_StillCompletesRoom()
    {
        for (int seed = 1; seed <= 300; seed++)
        {
            var manager = new RunManager(seed, startingHp: 999);
            manager.StartRun();

            int guard = 0;
            while (guard++ < 400)
            {
                if (manager.Phase == RunPhase.OnMap)
                {
                    manager.MoveToNextRoom();
                    continue;
                }

                if (manager.Phase == RunPhase.InRoom && manager.CurrentRoom is EventRoom er && er.Event != null)
                {
                    int before = manager.Player.CurrentHp;
                    manager.PerformEventChoice(0); // 选任意事件的第 1 个选项

                    if (manager.Phase == RunPhase.Defeated)
                    {
                        break; // 恰好选到致死项（hp 高基本不会）→ 换种子
                    }

                    // 未致死 → 正常完成房间回地图
                    Assert.Equal(RunPhase.OnMap, manager.Phase);
                    Assert.True(manager.CurrentRoom.Completed);
                    return;
                }

                if (manager.Phase == RunPhase.InRoom)
                {
                    ResolveCurrentRoom(manager);
                    continue;
                }

                if (manager.Phase == RunPhase.Reward)
                {
                    manager.ClaimRewardCurrency();
                    manager.ClaimBonusRelic();
                    manager.PickReward(manager.PendingRewards![0]);
                    continue;
                }

                break;
            }
        }

        Assert.Fail("300 个种子都没进过事件房");
    }
}

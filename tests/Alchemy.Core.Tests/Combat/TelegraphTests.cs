using System.Collections.Generic;
using Alchemy.Core.Combat;
using Alchemy.Core.Encounters;
using Xunit;

namespace Alchemy.Core.Tests.Combat;

/// <summary>计时动作的预兆（telegraph）信号：执行前 T 秒触发一次，供表现层播预备动画。</summary>
public class TelegraphTests
{
    [Fact]
    public void Telegraph_FiresOnce_WhenRemainingCrossesLead()
    {
        var (combat, player, _) = CombatFixture.Create();
        var fired = new List<ActionTelegraph>();
        combat.ActionTelegraphed += t => fired.Add(t);

        combat.BeginTimedAction(player, "test_action", 5f, _ => { }, telegraphSeconds: 1.5f);

        combat.AdvanceTime(3f); // 剩 2.0s → 未到预兆
        Assert.Empty(fired);

        combat.AdvanceTime(0.5f); // 剩 1.5s → 触发
        Assert.Single(fired);
        Assert.Equal("test_action", fired[0].ActionId);
        Assert.Same(player, fired[0].Actor);
        Assert.InRange(fired[0].SecondsUntilExecute, 1.49f, 1.51f);

        combat.AdvanceTime(1.5f); // 完成 → 不二次触发
        Assert.Single(fired);
    }

    [Fact]
    public void Telegraph_NotFired_WhenLeadIsZero()
    {
        var (combat, player, _) = CombatFixture.Create();
        bool fired = false;
        combat.ActionTelegraphed += _ => fired = true;

        combat.BeginTimedAction(player, "no_telegraph", 1f, _ => { }); // 默认 telegraph=0
        combat.AdvanceTime(5f);

        Assert.False(fired);
    }

    [Fact]
    public void Telegraph_FiresImmediately_WhenLeadLongerThanDuration()
    {
        var (combat, player, _) = CombatFixture.Create();
        int count = 0;
        combat.ActionTelegraphed += _ => count++;

        combat.BeginTimedAction(player, "short", 1f, _ => { }, telegraphSeconds: 1.5f);
        combat.AdvanceTime(0.1f); // 剩 0.9s ≤ 1.5s → 立即触发

        Assert.Equal(1, count);
    }

    [Fact]
    public void EnemyAction_TelegraphsBeforeExecuting()
    {
        var (combat, _, enemy) = CombatFixture.Create();
        combat.EnemyTelegraphSeconds = 1.5f;

        var fired = new List<ActionTelegraph>();
        combat.ActionTelegraphed += t => fired.Add(t);

        var intention = new Intention("attack", "攻击", 10f, IntentionActionType.Attack, Damage: 5);
        combat.ScheduleEnemyAction(enemy, new[] { intention });

        combat.AdvanceTime(8f); // 剩 2.0s → 尚未预兆
        Assert.Empty(fired);

        combat.AdvanceTime(0.5f); // 剩 1.5s → 预兆（但还没打）
        Assert.Single(fired);
        Assert.Same(enemy, fired[0].Actor);
        Assert.StartsWith("enemy_move_", fired[0].ActionId);
        Assert.Equal(30, combat.Player!.CurrentHp); // 尚未执行

        combat.AdvanceTime(1.5f); // 到点 → 执行攻击
        Assert.Equal(25, combat.Player!.CurrentHp);
    }

    [Fact]
    public void Telegraph_PerIntentionOverride_BeatsGlobalDefault()
    {
        var (combat, _, enemy) = CombatFixture.Create();
        combat.EnemyTelegraphSeconds = 5f; // 战斗默认 5s

        int count = 0;
        combat.ActionTelegraphed += _ => count++;

        // 该意图单独指定 2s：应在剩 2s 时触发（而不是 5s）
        var intention = new Intention("heavy", "力劈华山", 10f, IntentionActionType.Attack, Damage: 5, TelegraphSeconds: 2f);
        combat.ScheduleEnemyAction(enemy, new[] { intention });

        combat.AdvanceTime(7.9f); // 剩 2.1s > 2s → 尚未预兆
        Assert.Equal(0, count);

        combat.AdvanceTime(0.1f); // 剩 2.0s → 触发
        Assert.Equal(1, count);
    }

    [Fact]
    public void Telegraph_NegativeValue_InheritsGlobalDefault()
    {
        var (combat, _, enemy) = CombatFixture.Create();
        combat.EnemyTelegraphSeconds = 4f;

        int count = 0;
        combat.ActionTelegraphed += _ => count++;

        // TelegraphSeconds 默认 -1 → 继承战斗默认 4s
        var intention = new Intention("atk", "攻击", 10f, IntentionActionType.Attack, Damage: 5);
        combat.ScheduleEnemyAction(enemy, new[] { intention });

        combat.AdvanceTime(5.9f); // 剩 4.1s > 4s → 尚未预兆
        Assert.Equal(0, count);

        combat.AdvanceTime(0.1f); // 剩 4.0s → 触发
        Assert.Equal(1, count);
    }
}

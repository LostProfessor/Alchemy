using System.Collections.Generic;
using Alchemy.Core.Combat;
using Alchemy.Core.Encounters;
using Alchemy.Core.Hooks;
using Xunit;

namespace Alchemy.Core.Tests.Combat;

/// <summary>破招（打断）：可打断的敌人出手在预兆窗口内被一次达阈值伤害打断 → 取消出手 + 瘫痪 → 接下一个意图。</summary>
public class InterruptTests
{
    /// <summary>可打断的重击：间隔 10s、预兆 3s、阈值 20、瘫痪 2s。</summary>
    private static Intention Heavy(float telegraph = 3f, int threshold = 20, float stagger = 2f) =>
        new("heavy", "力劈华山", 10f, IntentionActionType.Attack, Damage: 8,
            TelegraphSeconds: telegraph, Interruptible: true, InterruptDamage: threshold, StaggerSeconds: stagger);

    [Fact]
    public void Interrupt_Triggers_WhenDamageMeetsThreshold_InWindow()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        var interrupted = new List<ActionInterrupted>();
        combat.ActionInterrupted += i => interrupted.Add(i);

        combat.ScheduleEnemyAction(enemy, new[] { Heavy() });
        combat.AdvanceTime(7.1f); // 剩 2.9s：预兆已发、尚未结算
        Assert.Empty(interrupted);

        combat.DealDamage(new DamageContext(player, enemy, 20)); // 刚好达阈值（≥）

        Assert.Single(interrupted);
        Assert.Same(enemy, interrupted[0].Actor);
        Assert.Equal("heavy", interrupted[0].Intention.Id);
        Assert.Equal(20, interrupted[0].Damage);
        Assert.True(combat.IsEnemyStaggered(enemy));
        Assert.DoesNotContain(combat.PendingActions, a => a.Id.StartsWith("enemy_move_"));
    }

    [Fact]
    public void Interrupt_StaggerEnds_ThenStartsNextIntention()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        combat.ScheduleEnemyAction(enemy, new[]
        {
            Heavy(stagger: 2f),
            new Intention("second", "追击", 10f, IntentionActionType.Attack, Damage: 4),
        });

        combat.AdvanceTime(7.1f);
        combat.DealDamage(new DamageContext(player, enemy, 25));
        Assert.True(combat.IsEnemyStaggered(enemy));

        int hpAfterHit = player.CurrentHp; // 被自己打的那 25 是对敌人，玩家未掉血
        Assert.Equal(30, hpAfterHit);

        combat.AdvanceTime(2f); // 瘫痪结束 → 进入下一个意图（追击）

        Assert.False(combat.IsEnemyStaggered(enemy));
        Assert.Equal(hpAfterHit, player.CurrentHp); // 被打断的招没打出来
        Assert.Equal("second", combat.GetCurrentIntention(enemy)!.Id);

        combat.AdvanceTime(10f); // 追击结算（4 伤害）
        Assert.Equal(hpAfterHit - 4, player.CurrentHp);
    }

    [Fact]
    public void Interrupt_NotTriggered_WhenDamageBelowThreshold()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        bool fired = false;
        combat.ActionInterrupted += _ => fired = true;

        combat.ScheduleEnemyAction(enemy, new[] { Heavy() });
        combat.AdvanceTime(7.1f);

        combat.DealDamage(new DamageContext(player, enemy, 19)); // 差 1 点

        Assert.False(fired);
        Assert.False(combat.IsEnemyStaggered(enemy));
        Assert.Contains(combat.PendingActions, a => a.Id.StartsWith("enemy_move_"));

        int before = player.CurrentHp; // 到点仍会出手（8 伤害）
        combat.AdvanceTime(2.9f);
        Assert.Equal(before - 8, player.CurrentHp);
    }

    [Fact]
    public void Interrupt_NotTriggered_BeforeTelegraph()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        bool fired = false;
        combat.ActionInterrupted += _ => fired = true;

        combat.ScheduleEnemyAction(enemy, new[] { Heavy() });
        combat.AdvanceTime(5f); // 剩 5s > 3s → 预兆还没发，窗口未开

        combat.DealDamage(new DamageContext(player, enemy, 25));

        Assert.False(fired);
        Assert.False(combat.IsEnemyStaggered(enemy));
    }

    [Fact]
    public void Interrupt_NotTriggered_ForNonInterruptible()
    {
        var (combat, player, enemy) = CombatFixture.Create();
        bool fired = false;
        combat.ActionInterrupted += _ => fired = true;

        var normal = new Intention("atk", "攻击", 10f, IntentionActionType.Attack, Damage: 5); // 不可打断
        combat.ScheduleEnemyAction(enemy, new[] { normal });
        combat.AdvanceTime(8.6f); // 预兆已发（默认 1.5s）

        combat.DealDamage(new DamageContext(player, enemy, 25));

        Assert.False(fired);
        Assert.False(combat.IsEnemyStaggered(enemy));
    }

    [Fact]
    public void Interrupt_Counts_SourcelessDamage_LikeDot()
    {
        var (combat, _, enemy) = CombatFixture.Create();
        bool fired = false;
        combat.ActionInterrupted += _ => fired = true;

        combat.ScheduleEnemyAction(enemy, new[] { Heavy() });
        combat.AdvanceTime(7.1f);

        combat.DealDamage(new DamageContext(null, enemy, 25)); // 无来源（烈毒等 DoT）也算

        Assert.True(fired);
        Assert.True(combat.IsEnemyStaggered(enemy));
    }

    [Fact]
    public void Interruptible_Intention_DefaultsToLongerTelegraph()
    {
        var (combat, _, enemy) = CombatFixture.Create();
        combat.EnemyTelegraphSeconds = 1.5f; // 普通默认

        var fired = new List<ActionTelegraph>();
        combat.ActionTelegraphed += t => fired.Add(t);

        // 不指定 TelegraphSeconds(-1) 的可打断意图 → 用更长的默认 2.5s
        combat.ScheduleEnemyAction(enemy, new[] { Heavy(telegraph: -1f) });

        combat.AdvanceTime(7.4f); // 剩 2.6s > 2.5s → 尚未
        Assert.Empty(fired);

        combat.AdvanceTime(0.1f); // 剩 2.5s → 触发
        Assert.Single(fired);
    }
}

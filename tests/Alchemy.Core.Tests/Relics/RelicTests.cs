using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;
using Alchemy.Core.Relics;
using Alchemy.Core.Runs;
using Xunit;

namespace Alchemy.Core.Tests.Relics;

public class RelicTests
{
    private static CombatState NewCombat(out Creature player, out Creature enemy, params IHookListener[] relics)
    {
        var combat = new CombatState(123);
        player = new Creature("玩家", 30, isPlayer: true);
        enemy = new Creature("敌人", 30);
        combat.AddAlly(player);
        combat.AddEnemy(enemy);
        combat.AttachRelics(relics);
        combat.Start();
        return combat;
    }

    [Fact]
    public void IronBracer_GrantsToughnessAtCombatStart()
    {
        var combat = NewCombat(out var player, out _, new IronBracer());

        Assert.True(player.HasEffect(EffectId.Toughness));
        Assert.Equal(1, player.GetEffectOrThrow(EffectId.Toughness).Layers);
    }

    [Fact]
    public void SteelWill_BonusBlock_OnPlayerOnly()
    {
        var combat = NewCombat(out var player, out var enemy, new SteelWill());

        combat.GainBlock(enemy, 3);
        Assert.Equal(3, enemy.Block); // 敌人不加

        combat.GainBlock(player, 3);
        Assert.Equal(4, player.Block); // 玩家 +1
    }

    [Fact]
    public void ThornCloak_GrantsThornyAtCombatStart()
    {
        var combat = NewCombat(out var player, out _, new ThornCloak());

        Assert.Equal(2, player.GetEffectOrThrow(EffectId.Thorny).Layers);
    }

    [Fact]
    public void ManaCrystal_Active_ChargesResetPerCombat()
    {
        var crystal = new ManaCrystal();

        // 第一场
        var combat1 = new CombatState(1);
        var p1 = new Creature("玩家", 30, isPlayer: true);
        combat1.AddAlly(p1);
        combat1.AttachRelics(new IHookListener[] { crystal });
        combat1.Start();
        combat1.DealDamage(new DamageContext(null, p1, 10)); // 20

        Assert.True(crystal.Activate(combat1, p1));
        Assert.Equal(25, p1.CurrentHp);

        Assert.False(crystal.Activate(combat1, p1)); // 本场已用
        Assert.Equal(25, p1.CurrentHp);

        // 第二场：重置次数
        var combat2 = new CombatState(2);
        var p2 = new Creature("玩家", 30, isPlayer: true);
        combat2.AddAlly(p2);
        combat2.AttachRelics(new IHookListener[] { crystal });
        combat2.Start();
        combat2.DealDamage(new DamageContext(null, p2, 10)); // 20

        Assert.True(crystal.Activate(combat2, p2));
        Assert.Equal(25, p2.CurrentHp);
    }

    [Fact]
    public void RelicInventory_AddHasRemove()
    {
        var inventory = new RelicInventory();
        var relic = new IronBracer();

        Assert.False(inventory.Has("iron_bracer"));

        inventory.Add(relic);
        Assert.True(inventory.Has("iron_bracer"));
        Assert.Equal(1, inventory.Count);

        inventory.Remove(relic);
        Assert.False(inventory.Has("iron_bracer"));
        Assert.Equal(0, inventory.Count);
    }

    [Fact]
    public void RunState_ExposesRelicInventory()
    {
        var run = new RunState();
        run.Relics.Add(new ThornCloak());

        Assert.True(run.Relics.Has("thorn_cloak"));
    }
}

using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;

namespace Alchemy.Core.Relics;

// ── 示例遗物（占位设计，后续可数据化）─────────────────────────────

/// <summary>铁皮手环（被动）：战斗开始时，玩家获得 1 层坚韧（非药水效果）。</summary>
public sealed class IronBracer : Relic
{
    public IronBracer() : base("iron_bracer", "铁皮手环", RelicRarity.Common)
    {
    }

    protected override void OnCombatStart(CombatState combat)
    {
        PlayerOf(combat)?.AddEffect(EffectId.Toughness, 1);
    }
}

/// <summary>钢铁意志（被动）：玩家获得格挡时额外 +1。</summary>
public sealed class SteelWill : Relic
{
    public SteelWill() : base("steel_will", "钢铁意志", RelicRarity.Uncommon)
    {
    }

    protected override void OnBeforeBlockGained(BlockContext c)
    {
        if (c.Target.IsPlayer)
        {
            c.Amount += 1;
        }
    }
}

/// <summary>荆棘斗篷（被动）：战斗开始时，玩家获得 2 层棘皮。</summary>
public sealed class ThornCloak : Relic
{
    public ThornCloak() : base("thorn_cloak", "荆棘斗篷", RelicRarity.Uncommon)
    {
    }

    protected override void OnCombatStart(CombatState combat)
    {
        PlayerOf(combat)?.AddEffect(EffectId.Thorny, 2);
    }
}

/// <summary>法力水晶（主动）：回复 5 生命，每场战斗 1 次。</summary>
public sealed class ManaCrystal : Relic
{
    private int _usesThisCombat;

    public ManaCrystal() : base("mana_crystal", "法力水晶", RelicRarity.Rare, hasActive: true)
    {
    }

    protected override void OnCombatStart(CombatState combat) => _usesThisCombat = 0;

    public override bool CanActivate(CombatState combat, Creature user) => _usesThisCombat < 1;

    public override bool Activate(CombatState combat, Creature user)
    {
        if (!CanActivate(combat, user))
        {
            return false;
        }

        _usesThisCombat++;
        combat.Heal(user, 5);
        return true;
    }
}

/// <summary>木符（被动）：战斗开始时，玩家获得 2 格挡。</summary>
public sealed class WoodenAmulet : Relic
{
    public WoodenAmulet() : base("wooden_amulet", "木符", RelicRarity.Common)
    {
    }

    protected override void OnCombatStart(CombatState combat)
    {
        var player = PlayerOf(combat);
        if (player != null)
        {
            combat.GainBlock(player, 2);
        }
    }
}

/// <summary>幸运草（被动）：战斗开始时，玩家获得 1 层精确。</summary>
public sealed class LuckyClover : Relic
{
    public LuckyClover() : base("lucky_clover", "幸运草", RelicRarity.Common)
    {
    }

    protected override void OnCombatStart(CombatState combat)
    {
        PlayerOf(combat)?.AddEffect(EffectId.Precise, 1);
    }
}

/// <summary>温暖睡袋（被动·火堆）：睡觉回复量 +50%。</summary>
public sealed class WarmBedroll : Relic, IRestHealModifier
{
    public WarmBedroll() : base("warm_bedroll", "温暖睡袋", RelicRarity.Common)
    {
    }

    public int ModifyRestHeal(int amount) => amount + amount / 2;
}

/// <summary>符文王冠（首领）：战斗开始时，玩家获得 2 层不朽。</summary>
public sealed class Runecrown : Relic
{
    public Runecrown() : base("runecrown", "符文王冠", RelicRarity.Boss)
    {
    }

    protected override void OnCombatStart(CombatState combat)
    {
        PlayerOf(combat)?.AddEffect(EffectId.Immortal, 2);
    }
}

/// <summary>凤凰之心（首领）：每场战斗第一次死亡时，以 1 点生命复活。</summary>
public sealed class PhoenixHeart : Relic
{
    private bool _usedThisCombat;

    public PhoenixHeart() : base("phoenix_heart", "凤凰之心", RelicRarity.Boss)
    {
    }

    protected override void OnCombatStart(CombatState combat) => _usedThisCombat = false;

    protected override void OnCreatureDied(Creature creature)
    {
        if (!creature.IsPlayer || _usedThisCombat)
        {
            return;
        }

        _usedThisCombat = true;
        creature.CurrentHp = 1; // 复活
    }
}

/// <summary>石之心（首领）：战斗开始时，玩家获得 3 格挡。</summary>
public sealed class Stoneheart : Relic
{
    public Stoneheart() : base("stoneheart", "石之心", RelicRarity.Boss)
    {
    }

    protected override void OnCombatStart(CombatState combat)
    {
        var player = PlayerOf(combat);
        if (player != null)
        {
            combat.GainBlock(player, 3);
        }
    }
}

/// <summary>哥布林王冠（首领）：战斗开始时，所有敌人获得 1 层易感。</summary>
public sealed class GoblinCrown : Relic
{
    public GoblinCrown() : base("goblin_crown", "哥布林王冠", RelicRarity.Boss)
    {
    }

    protected override void OnCombatStart(CombatState combat)
    {
        foreach (var enemy in combat.Enemies)
        {
            enemy.AddEffect(EffectId.Vulnerable, 1);
        }
    }
}

/// <summary>死亡之钟（首领）：战斗开始时，所有敌人获得 1 层迟钝（计时动作更慢）。</summary>
public sealed class DeathBell : Relic
{
    public DeathBell() : base("death_bell", "死亡之钟", RelicRarity.Boss)
    {
    }

    protected override void OnCombatStart(CombatState combat)
    {
        foreach (var enemy in combat.Enemies)
        {
            enemy.AddEffect(EffectId.Sluggish, 1);
        }
    }
}

/// <summary>深渊之心（首领）：战斗开始时，玩家获得 1 层祝福（每动作回 1 血）。</summary>
public sealed class AbyssHeart : Relic
{
    public AbyssHeart() : base("abyss_heart", "深渊之心", RelicRarity.Boss)
    {
    }

    protected override void OnCombatStart(CombatState combat)
    {
        PlayerOf(combat)?.AddEffect(EffectId.Blessing, 1);
    }
}

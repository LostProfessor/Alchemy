using Alchemy.Core.Effects;
using Alchemy.Core.Encounters;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 敌人意图资源：一次行动（攻击/防御/施加效果）+ 执行间隔，可在 Inspector 编辑，存进敌人 .tres。
/// </summary>
[GlobalClass]
public partial class IntentionResource : Resource
{
    [Export] public string DisplayName { get; set; } = "攻击";

    [Export] public float IntervalSeconds { get; set; } = 25f;

    [Export] public IntentionActionType ActionType { get; set; } = IntentionActionType.Attack;

    [Export] public int Damage { get; set; }

    [Export] public int Block { get; set; }

    [Export] public EffectId Effect { get; set; } = EffectId.None;

    [Export] public int EffectLayers { get; set; }

    [Export] public bool TargetSelf { get; set; }

    /// <summary>预兆提前量（秒）：出手前多少秒发预兆；-1=自动（可打断 2.5s / 普通 1.5s）。</summary>
    [Export] public float TelegraphSeconds { get; set; } = -1f;

    /// <summary>是否可被打断（破招）：为 true 时，预兆发出后受到一次 ≥ InterruptDamage 的伤害会被打断。</summary>
    [Export] public bool Interruptible { get; set; }

    /// <summary>打断所需的最小单次伤害（固定数值，仅可打断的技能有意义）。</summary>
    [Export] public int InterruptDamage { get; set; }

    /// <summary>被打断后的瘫痪时长（秒）。</summary>
    [Export] public float StaggerSeconds { get; set; } = 2f;

    public Intention ToIntention() => new(
        $"{ActionType}_{Damage}_{Block}_{Effect}_{EffectLayers}",
        DisplayName,
        IntervalSeconds,
        ActionType,
        Damage,
        Block,
        Effect,
        EffectLayers,
        TargetSelf,
        TelegraphSeconds,
        Interruptible,
        InterruptDamage,
        StaggerSeconds);
}

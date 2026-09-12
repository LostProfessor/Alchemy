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

    /// <summary>预兆提前量（秒）：出手前多少秒发预兆；-1=用战斗默认（1.5s）。可打断的长前摇技能应设更大值。</summary>
    [Export] public float TelegraphSeconds { get; set; } = -1f;

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
        TelegraphSeconds);
}

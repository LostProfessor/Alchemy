using Alchemy.Core.Effects;

namespace Alchemy.Core.Encounters;

/// <summary>意图动作类型。</summary>
public enum IntentionActionType
{
    /// <summary>对玩家造成伤害。</summary>
    Attack,

    /// <summary>给自己加格挡。</summary>
    Defend,

    /// <summary>施加效果（TargetSelf=true 对自己，否则对玩家）。</summary>
    ApplyEffect,
}

/// <summary>
/// 敌人意图：队列中的一次行动，执行后间隔 <see cref="IntervalSeconds"/> 秒进入下一次。
/// 敌人按意图队列循环执行（展示给玩家读敌）。
/// </summary>
public sealed record Intention(
    string Id,
    string DisplayName,
    float IntervalSeconds,
    IntentionActionType ActionType,
    int Damage = 0,
    int Block = 0,
    EffectId Effect = EffectId.None,
    int EffectLayers = 0,
    bool TargetSelf = false,
    float TelegraphSeconds = -1f,
    bool Interruptible = false,
    int InterruptDamage = 0,
    float StaggerSeconds = 2f);

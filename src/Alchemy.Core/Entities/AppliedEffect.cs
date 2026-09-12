using Alchemy.Core.Effects;

namespace Alchemy.Core.Entities;

/// <summary>
/// 生物身上的效果实例（运行时状态）：定义 + 层数 + 剩余时长。
/// 药水调制时操作的是"效果条目"（不结算）；只有施加到生物上才变成 AppliedEffect 参与结算。
/// </summary>
public sealed class AppliedEffect
{
    public EffectId Id { get; init; }

    public Creature Owner { get; init; } = null!;

    public int Layers { get; set; }

    /// <summary>剩余时长（秒）。计时类效果（烈毒/药物依赖/不朽）才有值。</summary>
    public float? DurationRemaining { get; set; }

    public EffectDefinition Definition => EffectRegistry.Get(Id);

    public override string ToString() =>
        $"{Id} x{Layers}{(DurationRemaining.HasValue ? $" ({DurationRemaining:0.0}s)" : string.Empty)}";
}

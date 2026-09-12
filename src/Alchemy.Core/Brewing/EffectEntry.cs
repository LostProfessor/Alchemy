using Alchemy.Core.Effects;

namespace Alchemy.Core.Brewing;

/// <summary>
/// 药水效果条目：5 格队列/栈中的一格，记录效果种类与层数。
/// 同一效果多次加入时保留为独立槽位（保持 5 格语义），投掷/饮用时按种类聚合层数。
/// </summary>
public sealed class EffectEntry
{
    public EffectId Effect { get; set; }

    public int Layers { get; set; }

    public EffectEntry()
    {
    }

    public EffectEntry(EffectId effect, int layers)
    {
        Effect = effect;
        Layers = layers;
    }

    public override string ToString() => $"{Effect} x{Layers}";
}

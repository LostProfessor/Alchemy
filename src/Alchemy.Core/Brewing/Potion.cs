using System.Collections.Generic;
using Alchemy.Core.Effects;

namespace Alchemy.Core.Brewing;

/// <summary>
/// 一瓶药水 = 基底 + 5 格效果条目（队列/栈，取决于基底模式）。
/// 颜色 = 基底颜色 + Σ(每格效果 RGB 增量 × 层数)，clamp 到 0~255。
/// </summary>
public sealed class Potion
{
    /// <summary>一瓶药水最多同时拥有 5 个效果（策划案规则）。</summary>
    public const int MaxSlots = 5;

    public BaseLiquid Base { get; }

    public List<EffectEntry> Entries { get; } = new();

    public Potion(BaseLiquid baseLiquid) => Base = baseLiquid;

    public int Count => Entries.Count;

    public bool IsEmpty => Entries.Count == 0;

    /// <summary>当前药水颜色：基底色 + 各效果增量（越界 clamp）。</summary>
    public PotionColor Color
    {
        get
        {
            var delta = ColorDelta.Zero;
            foreach (var entry in Entries)
            {
                delta += EffectRegistry.Get(entry.Effect).ColorDelta * entry.Layers;
            }

            return Base.BaseColor + delta;
        }
    }

    /// <summary>按效果种类聚合总层数（供投掷/饮用时结算，效果定义层数上限在此阶段不裁剪）。</summary>
    public IReadOnlyDictionary<EffectId, int> AggregateLayers()
    {
        var result = new Dictionary<EffectId, int>();
        foreach (var entry in Entries)
        {
            result.TryGetValue(entry.Effect, out var current);
            result[entry.Effect] = current + entry.Layers;
        }

        return result;
    }

    /// <summary>清空炼药台（战斗结束时不保留药水）。</summary>
    public void Clear() => Entries.Clear();
}

using System;
using System.Collections.Generic;
using Alchemy.Core.Effects;

namespace Alchemy.Core.Brewing;

/// <summary>
/// 各种规则共享的词条操作原语。
/// </summary>
internal static class BrewingOps
{
    /// <summary>移除指定效果最多 amount 层（不足则全移），返回实际移除层数。</summary>
    public static int RemoveLayers(List<EffectEntry> entries, EffectId effect, int amount)
    {
        int remaining = amount;
        for (int i = entries.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (entries[i].Effect != effect)
            {
                continue;
            }

            int take = Math.Min(entries[i].Layers, remaining);
            entries[i].Layers -= take;
            remaining -= take;
            if (entries[i].Layers <= 0)
            {
                entries.RemoveAt(i);
            }
        }

        return amount - remaining;
    }

    /// <summary>从"尾部"（队列=队尾最近加入，栈=栈顶）移除 count 格，返回实际移除数。</summary>
    public static int RemoveLast(List<EffectEntry> entries, int count)
    {
        int removed = 0;
        while (removed < count && entries.Count > 0)
        {
            entries.RemoveAt(entries.Count - 1);
            removed++;
        }

        return removed;
    }

    /// <summary>反转所有效果极性（增益↔减益），层数不变、不会抵消。</summary>
    public static void ReversePolarity(List<EffectEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (EffectRegistry.TryGetOpposite(entry.Effect, out var opposite))
            {
                entry.Effect = opposite;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Alchemy.Core.Runs.Rewards;

/// <summary>一个奖励袋：若干奖励条目的组合。</summary>
public sealed class RewardBag
{
    public IReadOnlyList<RewardItem> Items { get; }

    public RewardBag(IReadOnlyList<RewardItem> items) => Items = items;

    /// <summary>两个袋内容是否完全相同（顺序无关）。</summary>
    public static bool ContentEquals(RewardBag a, RewardBag b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a.Items.Count != b.Items.Count)
        {
            return false;
        }

        var ka = a.Items.Select(i => i.SortKey).OrderBy(k => k, StringComparer.Ordinal);
        var kb = b.Items.Select(i => i.SortKey).OrderBy(k => k, StringComparer.Ordinal);
        return ka.SequenceEqual(kb);
    }
}

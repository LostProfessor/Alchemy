using System.Collections.Generic;

namespace Alchemy.Core.Runs;

/// <summary>
/// 药材口袋：存放玩家目前拥有的所有药材（局内累积、一次性消耗、无 CD）。
/// 每场战斗后通过"三选一奖励袋"随机补充（见奖励系统，三袋内容不得完全一致）。
/// 属于 RunState 的一部分，随存档持久化。
/// </summary>
public sealed class IngredientPocket
{
    private readonly Dictionary<string, int> _counts = new();

    /// <summary>药材 ID → 数量。</summary>
    public IReadOnlyDictionary<string, int> Counts => _counts;

    public int CountOf(string ingredientId) => _counts.TryGetValue(ingredientId, out var count) ? count : 0;

    public bool Contains(string ingredientId) => CountOf(ingredientId) > 0;

    public void Add(string ingredientId, int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        _counts[ingredientId] = CountOf(ingredientId) + count;
    }

    /// <summary>炼药取用即消耗。数量不足返回 false。</summary>
    public bool TryConsume(string ingredientId, int count = 1)
    {
        if (count <= 0)
        {
            return true;
        }

        if (CountOf(ingredientId) < count)
        {
            return false;
        }

        _counts[ingredientId] -= count;
        if (_counts[ingredientId] == 0)
        {
            _counts.Remove(ingredientId);
        }

        return true;
    }

    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (var kv in _counts)
            {
                total += kv.Value;
            }

            return total;
        }
    }
}

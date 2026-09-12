using System.Collections.Generic;
using System.Linq;

namespace Alchemy.Core.Runs;

/// <summary>
/// 职业定义（纯 C#）：开局由它决定 基底玩法/专属遗物/初始药材。
/// 来源可被 content/jobs/*.tres（JobResource → ToJobDefinition）整体替换，方便新增职业。
/// </summary>
public sealed record JobDefinition(
    string Id,                                       // researcher / elf / et
    string DisplayName,                              // 研究员 / 精灵 / E.T.
    string BaseLiquidId,                             // 关联基底（aqua/oil/slime，玩法与职业解耦）
    string RelicId,                                  // 开局专属遗物（aqua_craft…）
    IReadOnlyList<(string Id, int Count)> StarterIngredients); // 开局药材

/// <summary>
/// 职业目录：默认内置三职业；运行时可用内容资源整体替换。
/// <see cref="Resolve"/> 兼容旧传参（job id 或基底 id 均可解析到对应职业）。
/// </summary>
public static class JobCatalog
{
    private static readonly Dictionary<string, JobDefinition> _default = new()
    {
        ["researcher"] = new("researcher", "研究员", "aqua", "aqua_craft",
            new (string Id, int Count)[] { ("glowcap", 2), ("moss", 2), ("bitterroot", 2), ("ash", 2) }),
        ["elf"] = new("elf", "精灵", "oil", "oil_craft",
            new (string Id, int Count)[] { ("bitterroot", 3), ("ash", 3), ("cindercrystal", 1) }),
        ["et"] = new("et", "E.T.", "slime", "slime_craft",
            new (string Id, int Count)[] { ("snakeberry", 2), ("phosphor", 2), ("muddleweed", 2) }),
    };

    private static IReadOnlyDictionary<string, JobDefinition> _current = _default;

    /// <summary>按 job id 解析职业；也接受旧基底 id（aqua/oil/slime）反查（向后兼容）；未知/空 → 兜底研究员。</summary>
    public static JobDefinition Resolve(string? jobIdOrBaseLiquid)
    {
        if (!string.IsNullOrEmpty(jobIdOrBaseLiquid))
        {
            if (_current.TryGetValue(jobIdOrBaseLiquid!, out var job))
            {
                return job;
            }

            var byBase = _current.Values.FirstOrDefault(j => j.BaseLiquidId == jobIdOrBaseLiquid);
            if (byBase != null)
            {
                return byBase;
            }
        }

        return _current.GetValueOrDefault("researcher") ?? _default["researcher"];
    }

    /// <summary>用内容资源替换职业目录（空则保持默认，兜底不崩）。</summary>
    public static void SetCatalog(IReadOnlyDictionary<string, JobDefinition> catalog)
    {
        if (catalog.Count > 0)
        {
            _current = catalog;
        }
    }
}

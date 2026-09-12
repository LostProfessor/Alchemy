using System.Linq;
using Alchemy.Core.Encounters;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 遭遇表资源（content/encounters/act*.tres）：定义"某一大层的非首领遭遇池 + 该层首领"，
/// 由 <see cref="ContentCatalog.BuildEncounters"/> 扫描后注入逻辑层 <see cref="EncounterFactory"/>。
/// 每大层一个 .tres；改数值/组合 = 改资源，不动代码。
/// </summary>
[GlobalClass]
public partial class EncounterTableResource : Resource
{
    /// <summary>所属大层（1~5）。</summary>
    [Export] public int ActIndex { get; set; } = 1;

    /// <summary>本层非首领遭遇池（战斗房随机抽一个）。</summary>
    [Export] public EncounterEntryResource[] Normal { get; set; } = [];

    /// <summary>本层首领遭遇（打赢它有专属首领遗物池 BossId）。</summary>
    [Export] public EncounterEntryResource Boss { get; set; } = null!;

    /// <summary>映射成逻辑层大层遭遇内容（供 EncounterFactory.SetActs）。</summary>
    public EncounterFactory.ActContent ToActContent()
    {
        var pool = (Normal ?? []).Select(e => e.ToDef()).ToList();
        var boss = Boss?.ToDef() ?? new EncounterFactory.EncounterDef($"boss_act{ActIndex}", []);
        return new EncounterFactory.ActContent(pool, boss);
    }
}

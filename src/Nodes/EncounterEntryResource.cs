using System;
using System.Collections.Generic;
using Alchemy.Core.Encounters;
using Godot;

namespace Alchemy.Nodes;

/// <summary>一张遭遇卡：一组怪物模板 Id 组合；首领卡额外带 BossId（content/encounters/*.tres 用）。</summary>
[GlobalClass]
public partial class EncounterEntryResource : Resource
{
    /// <summary>遭遇 id（如 slime_pair / boss_goblin_king）。</summary>
    [Export] public string Id { get; set; } = string.Empty;

    /// <summary>怪物模板 id，英文逗号分隔（如 "slime, slime"）。
    /// ⚠️ 故意不用 string[]/PackedStringArray/Array[string]：Godot 编辑器重存 .tres 会丢数组字段（踩过多次坑），
    /// 单 string 字段（如 Id/DisplayName）从没丢过，所以用逗号分隔字符串最稳。</summary>
    [Export] public string MonsterIds { get; set; } = string.Empty;

    /// <summary>首领 id（仅首领卡填；对应首领遗物专属池键，如 goblin_king）。</summary>
    [Export] public string BossId { get; set; } = string.Empty;

    public EncounterFactory.EncounterDef ToDef()
    {
        var ids = new List<string>();
        if (!string.IsNullOrWhiteSpace(MonsterIds))
        {
            foreach (var part in MonsterIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                ids.Add(part);
            }
        }

        return new EncounterFactory.EncounterDef(Id, ids, string.IsNullOrEmpty(BossId) ? null : BossId);
    }
}

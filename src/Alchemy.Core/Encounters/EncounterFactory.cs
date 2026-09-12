using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Random;

namespace Alchemy.Core.Encounters;

/// <summary>
/// 遭遇表：每大层的非首领遭遇池（难度随大层递增，靠更强的怪物组合实现）
/// + 每大层首领（BossId 对应专属首领遗物池）。
/// 数值系数留给以后的进阶难度（Ascension），同一进阶难度下只换更强组合。
/// </summary>
public static class EncounterFactory
{
    /// <summary>一场遭遇的数据定义（模板 Id 组合 + 可选 BossId）。</summary>
    public sealed record EncounterDef(string Id, IReadOnlyList<string> MonsterIds, string? BossId = null);

    // ── 每大层的非首领遭遇池（大层 1 最弱 → 大层 5 最强，只存模板 Id）────────
    private static readonly EncounterDef[][] _byAct =
    {
        new EncounterDef[]
        {
            new("slime_pair", new[] { "slime", "slime" }),
            new("wolf", new[] { "wolf" }),
            new("goblin_trio", new[] { "goblin", "goblin", "goblin" }),
        },

        new EncounterDef[]
        {
            new("big_slime", new[] { "big_slime" }),
            new("wolfpack", new[] { "wolf", "wolf" }),
            new("goblin_shaman", new[] { "goblin_shaman" }),
        },

        new EncounterDef[]
        {
            new("troll", new[] { "troll" }),
            new("ogre", new[] { "ogre" }),
            new("cultist_pair", new[] { "cultist", "cultist" }),
        },

        new EncounterDef[]
        {
            new("wight", new[] { "wight" }),
            new("dark_knight", new[] { "dark_knight" }),
            new("banshee", new[] { "banshee" }),
        },

        new EncounterDef[]
        {
            new("demon", new[] { "demon" }),
            new("dragon_whelp", new[] { "dragon_whelp" }),
            new("archmage", new[] { "archmage" }),
        },
    };

    // ── 每大层首领（各自 BossId 对应专属首领遗物池）────────────────
    private static readonly EncounterDef[] _bosses =
    {
        new("boss_goblin_king", new[] { "goblin_king" }, BossId: "goblin_king"),
        new("boss_witch", new[] { "witch_boss" }, BossId: "witch_boss"),
        new("boss_golem", new[] { "golem_boss" }, BossId: "golem_boss"),
        new("boss_lich_king", new[] { "lich_king" }, BossId: "lich_king"),
        new("boss_abyss_lord", new[] { "abyss_lord" }, BossId: "abyss_lord"),
    };

    private static Encounter Build(EncounterDef def)
    {
        var monsters = def.MonsterIds.Select(MonsterTemplates.Get).ToList();
        if (monsters.Count == 0)
        {
            // fail-fast：空遭遇=没怪物=会被判“全灭”白送奖励，必须在数据层就拦住
            throw new InvalidOperationException($"遭遇 {def.Id} 没有怪物组合（MonsterIds 为空？请检查 content/encounters/*.tres）");
        }

        return new Encounter(def.Id, monsters, def.BossId);
    }

    /// <summary>单个大层的遭遇内容：非首领遭遇池 + 该层首领。</summary>
    public sealed record ActContent(IReadOnlyList<EncounterDef> Pool, EncounterDef Boss);

    /// <summary>大层总数（与 RunManager.TotalActs 一致）。</summary>
    public const int TotalActs = 5;

    private static readonly IReadOnlyDictionary<int, ActContent> _defaultActs = BuildDefaultActs();

    private static IReadOnlyDictionary<int, ActContent> _current = _defaultActs;

    private static IReadOnlyDictionary<int, ActContent> BuildDefaultActs()
    {
        var map = new Dictionary<int, ActContent>();
        for (int i = 0; i < _byAct.Length; i++)
        {
            map[i + 1] = new ActContent(_byAct[i], _bosses[i]);
        }

        return map;
    }

    /// <summary>用内容资源（content/encounters/*.tres，由 Godot 侧注入）整体替换遭遇表；空则忽略。</summary>
    public static void SetActs(IReadOnlyDictionary<int, ActContent> acts)
    {
        if (acts == null || acts.Count == 0)
        {
            return;
        }

        _current = acts;
    }

    /// <summary>按大层随机一个非首领遭遇（难度随大层递增）。</summary>
    public static Encounter Random(IRandomSource rng, int actIndex)
    {
        var act = _current[ClampAct(actIndex)];
        return Build(act.Pool[rng.Next(act.Pool.Count)]);
    }

    /// <summary>该大层的首领（每层 1 个，各带 BossId）。</summary>
    public static Encounter BossForAct(int actIndex) => Build(_current[ClampAct(actIndex)].Boss);

    /// <summary>该大层遭遇池中单只怪物的最高生命（用于验证难度递增）。</summary>
    public static int MaxMonsterHp(int actIndex)
    {
        var act = _current[ClampAct(actIndex)];
        return act.Pool.Max(def => def.MonsterIds.Max(MonsterTemplates.Get).MaxHp);
    }

    private static int ClampAct(int actIndex) => Math.Clamp(actIndex, 1, TotalActs);
}

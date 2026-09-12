using System;
using System.Collections.Generic;
using Alchemy.Core.Effects;

namespace Alchemy.Core.Encounters;

/// <summary>
/// 敌人模板目录：默认硬编码占位，可由 .tres 内容资源（EnemyResource）整体替换。
/// EncounterFactory 的组合只引用模板 Id，运行时从这里解析。
/// </summary>
public static class MonsterTemplates
{
    private static readonly Dictionary<string, MonsterTemplate> _default = new()
    {
        // ── 大层 1 ──
        ["slime"] = new("slime", "史莱姆", 12, new[] { Attack(30f, 4) }),
        ["wolf"] = new("wolf", "野狼", 20, new[] { Attack(25f, 8) }),
        ["goblin"] = new("goblin", "哥布林", 10, new[] { Attack(20f, 3) }),

        // ── 大层 2 ──
        ["big_slime"] = new("big_slime", "大史莱姆", 24, new[] { Attack(28f, 7) }),
        ["goblin_shaman"] = new("goblin_shaman", "哥布林萨满", 22,
            new[] { Attack(26f, 9), Cast(26f, EffectId.Vulnerable, 1, targetSelf: false) }),

        // ── 大层 3 ──
        ["troll"] = new("troll", "巨魔", 30, new[] { Attack(32f, 12) }),
        ["ogre"] = new("ogre", "食人魔", 34, new[] { Attack(30f, 13), Defend(30f, 8) }),
        ["cultist"] = new("cultist", "教徒", 20,
            new[] { Attack(24f, 9), Cast(24f, EffectId.Vulnerable, 1, targetSelf: false) }),

        // ── 大层 4 ──
        ["wight"] = new("wight", "尸鬼", 36, new[] { Attack(28f, 14) }),
        ["dark_knight"] = new("dark_knight", "黑骑士", 40, new[] { Attack(30f, 16), Defend(30f, 10) }),
        ["banshee"] = new("banshee", "女妖", 32,
            new[] { Attack(22f, 12), Cast(22f, EffectId.Sluggish, 1, targetSelf: false) }),

        // ── 大层 5 ──
        ["demon"] = new("demon", "恶魔", 46, new[] { Attack(28f, 18) }),
        ["dragon_whelp"] = new("dragon_whelp", "幼龙", 52, new[] { Attack(30f, 20), Defend(30f, 12) }),
        ["archmage"] = new("archmage", "大法师", 44,
            new[] { Attack(24f, 17), Cast(24f, EffectId.Focus, 2, targetSelf: true) }),

        // ── 首领 ──
        ["goblin_king"] = new("goblin_king", "哥布林王", 35,
            new[] { Attack(22f, 8), Defend(22f, 6), Attack(22f, 10) }),
        ["witch_boss"] = new("witch_boss", "女巫首领", 40,
            new[] { Attack(25f, 10), Cast(25f, EffectId.Vulnerable, 1, targetSelf: false) }),
        ["golem_boss"] = new("golem_boss", "石像魔像", 45,
            new[] { Defend(35f, 12), Attack(35f, 14), Attack(35f, 14) }),
        ["lich_king"] = new("lich_king", "亡灵领主", 50,
            new[] { Attack(30f, 16), Cast(30f, EffectId.Sluggish, 1, targetSelf: false), Cast(30f, EffectId.Vulnerable, 1, targetSelf: false) }),
        ["abyss_lord"] = new("abyss_lord", "深渊之主", 60,
            new[] { Attack(28f, 20), Defend(28f, 14), Attack(28f, 22) }),
    };

    private static IReadOnlyDictionary<string, MonsterTemplate> _current = _default;

    public static MonsterTemplate Get(string id) =>
        _current.TryGetValue(id, out var template)
            ? template
            : throw new KeyNotFoundException($"未知敌人模板：{id}");

    /// <summary>用内容资源（Godot 侧 ContentCatalog）整体替换敌人模板目录。</summary>
    public static void SetCatalog(IReadOnlyDictionary<string, MonsterTemplate> catalog) => _current = catalog;

    private static Intention Attack(float interval, int damage) =>
        new($"attack_{damage}_{interval:0.#}", "攻击", interval, IntentionActionType.Attack, Damage: damage);

    private static Intention Defend(float interval, int block) =>
        new($"defend_{block}_{interval:0.#}", "防御", interval, IntentionActionType.Defend, Block: block);

    private static Intention Cast(float interval, EffectId effect, int layers, bool targetSelf) =>
        new($"cast_{effect}_{layers}", "施法", interval, IntentionActionType.ApplyEffect,
            Effect: effect, EffectLayers: layers, TargetSelf: targetSelf);
}

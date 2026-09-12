using System.Collections.Generic;
using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;

namespace Alchemy.Core.GameData;

/// <summary>
/// 占位药材数据（后续可移到 .tres/JSON 资源文件）。
/// 每种稀有度若干种，词条越稀有越强/越罕见。
/// </summary>
public static class Ingredients
{
    private static IngredientCatalog _default = new(new List<Ingredient>
    {
        // ── 常见 ──
        new("glowcap", "萤光菇", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.Heal, 2) }),
        new("moss", "苔藓", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.IronSkin, 1), AffixOp.Add(EffectId.Heal, 1) }),
        new("bitterroot", "苦根", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.Corrode, 2) }),
        new("ash", "灰烬", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.Corrode, 1), AffixOp.Add(EffectId.Vulnerable, 1) }),

        // ── 罕见 ──
        new("snakeberry", "蛇莓", IngredientRarity.Uncommon, new[] { AffixOp.Add(EffectId.VirulentPoison, 2) }),
        new("vine", "绞藤", IngredientRarity.Uncommon, new[] { AffixOp.Add(EffectId.IronSkin, 2), AffixOp.Add(EffectId.Vulnerable, 1) }),
        new("phosphor", "磷粉", IngredientRarity.Uncommon, new[] { AffixOp.Add(EffectId.Hallucinate, 2) }),
        new("muddleweed", "迷惘草", IngredientRarity.Uncommon, new[] { AffixOp.Add(EffectId.Bruise, 1), AffixOp.Add(EffectId.Corrode, 2) }),

        // ── 稀有 ──
        new("ambergris", "龙涎", IngredientRarity.Rare, new[] { AffixOp.Add(EffectId.Immortal, 2) }),
        new("shadowlotus", "影莲", IngredientRarity.Rare, new[] { AffixOp.Add(EffectId.Precise, 2), AffixOp.Add(EffectId.Bruise, 1) }),
        new("cindercrystal", "炽晶", IngredientRarity.Rare, new[] { AffixOp.Add(EffectId.Corrode, 3), AffixOp.Add(EffectId.Hallucinate, 1) }),
        new("gildmoss", "镀金苔", IngredientRarity.Rare, new[] { AffixOp.Add(EffectId.IronSkin, 3), AffixOp.Add(EffectId.Purify, 1) }),

        // ── 传奇 ──
        new("timesand", "时砂", IngredientRarity.Legendary, new[] { AffixOp.Add(EffectId.Focus, 3), AffixOp.Add(EffectId.Sluggish, 2) }),
        new("phoenixfeather", "凤凰羽", IngredientRarity.Legendary, new[] { AffixOp.Add(EffectId.Heal, 3), AffixOp.Add(EffectId.Immortal, 1) }),
        new("reverseflower", "逆鳞花", IngredientRarity.Legendary, new[] { AffixOp.ReversePolarity() }),
        new("voidpetal", "虚空瓣", IngredientRarity.Legendary, new[] { AffixOp.Add(EffectId.Dependency, 2), AffixOp.RemoveLast(1) }),
    });

    /// <summary>当前药材目录（默认硬编码占位，可由 .tres 内容资源替换）。</summary>
    public static IngredientCatalog Default => _default;

    /// <summary>用内容资源（Godot 侧 ContentCatalog）替换药材目录。</summary>
    public static void SetCatalog(IngredientCatalog catalog) => _default = catalog;
}

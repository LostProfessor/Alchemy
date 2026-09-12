using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Random;

namespace Alchemy.Core.GameData;

/// <summary>
/// 药材目录：按稀有度分组的可抽取池，供奖励袋随机生成。
/// </summary>
public sealed class IngredientCatalog
{
    private readonly IReadOnlyDictionary<IngredientRarity, IReadOnlyList<Ingredient>> _byRarity;

    public IngredientCatalog(IEnumerable<Ingredient> ingredients)
    {
        _byRarity = ingredients
            .GroupBy(i => i.Rarity)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Ingredient>)g.ToList());
    }

    public bool HasRarity(IngredientRarity rarity) =>
        _byRarity.TryGetValue(rarity, out var list) && list.Count > 0;

    public IReadOnlyList<Ingredient> OfRarity(IngredientRarity rarity) =>
        _byRarity.TryGetValue(rarity, out var list) ? list : Array.Empty<Ingredient>();

    /// <summary>从指定稀有度随机抽一种药材。</summary>
    public Ingredient Pick(IRandomSource rng, IngredientRarity rarity)
    {
        var list = OfRarity(rarity);
        if (list.Count == 0)
        {
            throw new InvalidOperationException($"目录中没有稀有度 {rarity} 的药材");
        }

        return list[rng.Next(list.Count)];
    }

    public IEnumerable<Ingredient> All => _byRarity.Values.SelectMany(v => v);
}

using System.Collections.Generic;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.Effects;
using Alchemy.Core.GameData;
using Xunit;

namespace Alchemy.Core.Tests.GameData;

public class IngredientCatalogTests
{
    [Fact]
    public void DefaultCatalog_HasAllFourRarities()
    {
        foreach (var rarity in new[] { IngredientRarity.Common, IngredientRarity.Uncommon, IngredientRarity.Rare, IngredientRarity.Legendary })
        {
            Assert.True(Ingredients.Default.HasRarity(rarity), $"缺少稀有度 {rarity}");
        }
    }

    [Fact]
    public void Pick_ReturnsIngredientOfRequestedRarity()
    {
        var rng = new BattleRandom(7);

        for (int i = 0; i < 20; i++)
        {
            var ingredient = Ingredients.Default.Pick(rng, IngredientRarity.Rare);
            Assert.Equal(IngredientRarity.Rare, ingredient.Rarity);
        }
    }

    [Fact]
    public void Pick_EmptyRarity_Throws()
    {
        var rng = new BattleRandom(7);
        var empty = new IngredientCatalog(new List<Ingredient>
        {
            new("a", "甲", IngredientRarity.Common, new[] { AffixOp.Add(EffectId.Heal, 1) }),
        });

        Assert.Throws<System.InvalidOperationException>(() => empty.Pick(rng, IngredientRarity.Legendary));
    }
}

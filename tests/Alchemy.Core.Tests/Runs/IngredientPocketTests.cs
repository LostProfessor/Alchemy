using Alchemy.Core.Runs;
using Xunit;

namespace Alchemy.Core.Tests.Runs;

public class IngredientPocketTests
{
    [Fact]
    public void Add_IncreasesCount()
    {
        var pocket = new IngredientPocket();

        pocket.Add("glowcap", 3);

        Assert.Equal(3, pocket.CountOf("glowcap"));
        Assert.True(pocket.Contains("glowcap"));
    }

    [Fact]
    public void TryConsume_Decrements()
    {
        var pocket = new IngredientPocket();
        pocket.Add("glowcap", 3);

        var consumed = pocket.TryConsume("glowcap", 2);

        Assert.True(consumed);
        Assert.Equal(1, pocket.CountOf("glowcap"));
    }

    [Fact]
    public void TryConsume_RemovesEntryWhenZero()
    {
        var pocket = new IngredientPocket();
        pocket.Add("glowcap", 1);

        pocket.TryConsume("glowcap", 1);

        Assert.False(pocket.Contains("glowcap"));
        Assert.Equal(0, pocket.CountOf("glowcap"));
    }

    [Fact]
    public void TryConsume_WhenInsufficient_ReturnsFalse_NoChange()
    {
        var pocket = new IngredientPocket();
        pocket.Add("glowcap", 1);

        var consumed = pocket.TryConsume("glowcap", 2);

        Assert.False(consumed);
        Assert.Equal(1, pocket.CountOf("glowcap"));
    }

    [Fact]
    public void TryConsume_WhenEmpty_ReturnsFalse()
    {
        var pocket = new IngredientPocket();

        var consumed = pocket.TryConsume("glowcap", 1);

        Assert.False(consumed);
    }

    [Fact]
    public void TotalCount_SumsAll()
    {
        var pocket = new IngredientPocket();
        pocket.Add("glowcap", 2);
        pocket.Add("tar", 3);

        Assert.Equal(5, pocket.TotalCount);
    }
}

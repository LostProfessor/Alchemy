using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.GameData;
using Alchemy.Core.Runs;
using Xunit;

namespace Alchemy.Core.Tests.Combat;

public class BrewingSessionTests
{
    private static (CombatState Combat, BrewingSession Session, IngredientPocket Pocket) NewSession()
    {
        var (combat, player, _) = CombatFixture.Create();
        var pocket = new IngredientPocket();
        var session = new BrewingSession(combat, player, pocket);
        return (combat, session, pocket);
    }

    private static Ingredient Glowcap => Ingredients.Default.All.First(i => i.Id == "glowcap");

    [Fact]
    public void AddIngredient_ConsumesPocket_TakesTime_ThenApplies()
    {
        var (combat, session, pocket) = NewSession();
        pocket.Add("glowcap", 1);
        session.StartBrew(BaseLiquids.Aqua);

        Assert.True(session.TryStartAddIngredient(Glowcap));
        Assert.Equal(0, pocket.CountOf("glowcap"));   // 立即扣药材
        Assert.Equal(0, session.ActivePotion!.Count); // 尚未加入

        combat.AdvanceTime(BrewingTimings.AddIngredientSeconds);

        Assert.Equal(2, session.ActivePotion!.Count); // 2 秒后真正加入：萤光菇 2 层恢复 = 2 格
    }

    [Fact]
    public void AddIngredient_WhilePending_Rejected()
    {
        var (combat, session, pocket) = NewSession();
        pocket.Add("glowcap", 2);
        session.StartBrew(BaseLiquids.Aqua);

        Assert.True(session.TryStartAddIngredient(Glowcap));
        Assert.False(session.TryStartAddIngredient(Glowcap)); // 有动作在进行
        Assert.Equal(1, pocket.CountOf("glowcap"));           // 第二个未扣药材

        combat.AdvanceTime(BrewingTimings.AddIngredientSeconds);
        Assert.True(session.TryStartAddIngredient(Glowcap));  // 完成后可再添
    }

    [Fact]
    public void AddIngredient_NoBrew_Rejected()
    {
        var (_, session, _) = NewSession();

        Assert.False(session.TryStartAddIngredient(Glowcap));
    }

    [Fact]
    public void AddIngredient_WithoutPocketSupply_Rejected()
    {
        var (_, session, _) = NewSession();
        session.StartBrew(BaseLiquids.Aqua);

        Assert.False(session.TryStartAddIngredient(Glowcap)); // 口袋没有该药材
    }

    [Fact]
    public void CompletePotion_TakesTime_ThenDelivers()
    {
        var (combat, session, _) = NewSession();
        session.StartBrew(BaseLiquids.Aqua);

        Potion? delivered = null;
        Assert.True(session.TryStartCompletePotion(p => delivered = p));
        Assert.True(session.IsBrewing);

        combat.AdvanceTime(BrewingTimings.CompletePotionSeconds);

        Assert.NotNull(delivered);
        Assert.False(session.IsBrewing);
    }

    // ── BrewingTimings 可配置（content/settings 的 .tres 经 Configure 注入）──

    [Fact]
    public void BrewingTimings_Configure_OverridesDurations_AndApplies()
    {
        try
        {
            BrewingTimings.Configure(1.5f, 4f);
            Assert.Equal(1.5f, BrewingTimings.AddIngredientSeconds);
            Assert.Equal(4f, BrewingTimings.CompletePotionSeconds);

            // 实际生效：按新值 AdvanceTime(1.5) 即完成加料
            var (combat, session, pocket) = NewSession();
            pocket.Add("glowcap", 1);
            session.StartBrew(BaseLiquids.Aqua);
            Assert.True(session.TryStartAddIngredient(Glowcap));
            combat.AdvanceTime(1.5f);
            Assert.Equal(2, session.ActivePotion!.Count);
        }
        finally
        {
            BrewingTimings.Configure(
                BrewingTimings.DefaultAddIngredientSeconds,
                BrewingTimings.DefaultCompletePotionSeconds); // 复位，避免污染其它测试
        }
    }

    [Fact]
    public void BrewingTimings_Configure_IgnoresNonPositive()
    {
        try
        {
            BrewingTimings.Configure(0f, -1f); // 无效值忽略 → 保持默认
            Assert.Equal(BrewingTimings.DefaultAddIngredientSeconds, BrewingTimings.AddIngredientSeconds);
            Assert.Equal(BrewingTimings.DefaultCompletePotionSeconds, BrewingTimings.CompletePotionSeconds);
        }
        finally
        {
            BrewingTimings.Configure(
                BrewingTimings.DefaultAddIngredientSeconds,
                BrewingTimings.DefaultCompletePotionSeconds);
        }
    }
}

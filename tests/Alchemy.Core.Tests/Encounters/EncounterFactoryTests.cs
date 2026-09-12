using Alchemy.Core.Encounters;
using Xunit;

namespace Alchemy.Core.Tests.Encounters;

public class EncounterFactoryTests
{
    [Fact]
    public void BossForAct_EachAct_HasBossWithBossId()
    {
        for (int act = 1; act <= 5; act++)
        {
            var boss = EncounterFactory.BossForAct(act);
            Assert.NotNull(boss.BossId);
        }
    }

    [Theory]
    [InlineData(1, "goblin_king")]
    [InlineData(2, "witch_boss")]
    [InlineData(3, "golem_boss")]
    [InlineData(4, "lich_king")]
    [InlineData(5, "abyss_lord")]
    public void BossForAct_ReturnsCorrectBoss(int act, string bossId)
    {
        Assert.Equal(bossId, EncounterFactory.BossForAct(act).BossId);
    }

    [Fact]
    public void Difficulty_Escalates_ByAct()
    {
        int previous = 0;
        for (int act = 1; act <= 5; act++)
        {
            int maxHp = EncounterFactory.MaxMonsterHp(act);
            Assert.True(maxHp > previous, $"大层 {act} 的最高怪物生命应高于大层 {act - 1}");
            previous = maxHp;
        }
    }
}

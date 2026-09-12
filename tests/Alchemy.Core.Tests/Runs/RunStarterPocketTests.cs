using System.Linq;
using Alchemy.Core.Runs;
using Xunit;

namespace Alchemy.Core.Tests.Runs;

public class RunStarterPocketTests
{
    [Fact]
    public void StartRun_DefaultJob_GivesAquaStarterPocket()
    {
        var manager = new RunManager(5);
        manager.StartRun();

        Assert.Equal(2, manager.Run.Pocket.CountOf("glowcap"));
        Assert.Equal(2, manager.Run.Pocket.CountOf("moss"));
        Assert.Equal(2, manager.Run.Pocket.CountOf("bitterroot"));
        Assert.Equal(2, manager.Run.Pocket.CountOf("ash"));
        Assert.Equal(8, manager.Run.Pocket.TotalCount);
    }

    [Fact]
    public void StartRun_DifferentJobs_GiveDifferentStarterPockets()
    {
        var aqua = new RunManager(5, jobId: "researcher");
        aqua.StartRun();
        var oil = new RunManager(5, jobId: "elf");
        oil.StartRun();
        var slime = new RunManager(5, jobId: "et");
        slime.StartRun();

        Assert.NotEqual(
            aqua.Run.Pocket.Counts.OrderBy(kv => kv.Key),
            oil.Run.Pocket.Counts.OrderBy(kv => kv.Key));
        Assert.NotEqual(
            oil.Run.Pocket.Counts.OrderBy(kv => kv.Key),
            slime.Run.Pocket.Counts.OrderBy(kv => kv.Key));

        // 浓油职业偏腐蚀：有苦根/灰烬，无萤光菇
        Assert.Equal(3, oil.Run.Pocket.CountOf("bitterroot"));
        Assert.Equal(3, oil.Run.Pocket.CountOf("ash"));
        Assert.Equal(0, oil.Run.Pocket.CountOf("glowcap"));

        // 黏液职业偏状态：有蛇莓/磷粉/迷惘草
        Assert.Equal(2, slime.Run.Pocket.CountOf("snakeberry"));
        Assert.Equal(2, slime.Run.Pocket.CountOf("phosphor"));
        Assert.Equal(2, slime.Run.Pocket.CountOf("muddleweed"));
    }

    [Fact]
    public void StartRun_UnknownJob_FallsBackToAqua()
    {
        var manager = new RunManager(5, jobId: "not_a_job");
        manager.StartRun();

        Assert.Equal(2, manager.Run.Pocket.CountOf("glowcap")); // 兜底 aqua 套
        Assert.True(manager.Run.Relics.Has("aqua_craft"));     // 兜底 aqua 遗物
    }

    [Fact]
    public void StartRun_GivesJobRelic_ForSelectedJob()
    {
        var aqua = new RunManager(5, jobId: "researcher");
        aqua.StartRun();
        Assert.True(aqua.Run.Relics.Has("aqua_craft"));

        var oil = new RunManager(5, jobId: "elf");
        oil.StartRun();
        Assert.True(oil.Run.Relics.Has("oil_craft"));

        var slime = new RunManager(5, jobId: "et");
        slime.StartRun();
        Assert.True(slime.Run.Relics.Has("slime_craft"));
    }
}

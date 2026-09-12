using System;
using System.Collections.Generic;
using System.IO;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.History;
using Alchemy.Core.Runs.Saves;
using Xunit;

namespace Alchemy.Core.Tests.Runs;

/// <summary>整局历史归档：结束一局后留存记录、最新在前、超上限裁剪、损坏不崩。</summary>
public class RunArchiveTests
{
    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), $"alchemy_archive_{Guid.NewGuid():N}.json");

    [Fact]
    public void Append_And_LoadAll_RoundTrips()
    {
        var path = TempPath();
        try
        {
            RunArchive.Append(path, new RunRecord
            {
                TimestampUtc = "2026-09-12T00:00:00Z",
                JobId = "elf",
                Outcome = "Completed",
                ActIndex = 5,
                Currency = 123,
                RelicIds = new List<string> { "oil_craft" },
                History = new List<HistoryEntry> { new() { Order = 0, Message = "进入" } },
            });

            var all = RunArchive.LoadAll(path);
            Assert.Single(all);
            Assert.Equal("elf", all[0].JobId);
            Assert.Equal("Completed", all[0].Outcome);
            Assert.Equal(5, all[0].ActIndex);
            Assert.Equal(123, all[0].Currency);
            Assert.Equal(new[] { "oil_craft" }, all[0].RelicIds);
            Assert.Single(all[0].History);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Append_NewestFirst_KeepsAll()
    {
        var path = TempPath();
        try
        {
            for (int i = 0; i < 8; i++)
            {
                RunArchive.Append(path, new RunRecord { Outcome = "Defeated", ActIndex = i });
            }

            var all = RunArchive.LoadAll(path);
            Assert.Equal(8, all.Count);      // 全部保留（不设上限）
            Assert.Equal(7, all[0].ActIndex); // 最新在最前
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LoadAll_MissingOrCorrupt_ReturnsEmpty()
    {
        Assert.Empty(RunArchive.LoadAll(TempPath())); // 文件不存在

        var path = TempPath();
        try
        {
            File.WriteAllText(path, "{ not json");
            Assert.Empty(RunArchive.LoadAll(path)); // 损坏 → 空，不抛
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void FromRun_MapsRunState()
    {
        var manager = new RunManager(42, startingHp: 40);
        manager.StartRun();
        manager.MoveToNextRoom(); // 产生一条“进入房间”历史
        manager.Run.Currency = 77;

        var record = RunRecord.FromRun(manager, "Completed", 123.5f, "2026-09-12T00:00:00Z");

        Assert.Equal("researcher", record.JobId);
        Assert.Equal("Completed", record.Outcome);
        Assert.Equal(123.5f, record.TotalSeconds);
        Assert.Equal(40, record.MaxHp);
        Assert.Equal(77, record.Currency);
        Assert.NotEmpty(record.RelicIds);  // 职业遗物
        Assert.NotEmpty(record.History);   // MoveToNextRoom 记了进房
    }
}

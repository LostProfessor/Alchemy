using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Combat;
using Alchemy.Core.Map;
using Xunit;

namespace Alchemy.Core.Tests.Map;

public class StandardActMapTests
{
    [Fact]
    public void Map_HasGuaranteedStructure()
    {
        var map = new StandardActMap(new BattleRandom(42));

        // 首行必为普通战斗、中部固定宝箱、首领前固定休息、顶部首领
        Assert.All(map.PointsInRow(1), p => Assert.Equal(MapPointType.Combat, p.Type));
        Assert.All(map.PointsInRow(map.TreasureRow), p => Assert.Equal(MapPointType.Treasure, p.Type));
        Assert.All(map.PointsInRow(map.ContentFloors), p => Assert.Equal(MapPointType.RestSite, p.Type));
        Assert.Equal(MapPointType.Boss, map.Boss.Type);
        Assert.True(map.PointsInRow(map.TreasureRow).Count > 0, "中部宝箱行不应为空");
        Assert.True(map.PointsInRow(map.ContentFloors).Count > 0, "首领前休息行不应为空");
    }

    [Fact]
    public void Map_EveryStartCanReachBoss()
    {
        var map = new StandardActMap(new BattleRandom(7));

        foreach (var point in map.PointsInRow(1))
        {
            Assert.True(CanReachBoss(map, point), $"起点 {point} 无法到达首领");
        }
    }

    [Fact]
    public void Map_CombatPointsHaveEncounters()
    {
        var map = new StandardActMap(new BattleRandom(42));

        foreach (var point in map.AllPoints.Where(p => p.Type is MapPointType.Combat or MapPointType.Elite))
        {
            Assert.NotNull(point.Encounter);
        }
    }

    [Fact]
    public void Map_EliteAndShopNeverAdjacent()
    {
        // 规则：生成时精英/商店不得连续相连（商店-商店、精英-精英、商店-精英都不行，含同排左右与上下连线）。
        // 事件点在进房时按概率分流（如 7% 变商店）不受此静态约束——那属运行时分流，合理。
        for (int seed = 1; seed <= 300; seed++)
        {
            var map = new StandardActMap(new BattleRandom(seed));
            foreach (var p in map.AllPoints.Where(p => p.Type is MapPointType.Elite or MapPointType.Shop))
            {
                foreach (var nb in NeighborsOf(map, p))
                {
                    Assert.False(
                        nb.Type is MapPointType.Elite or MapPointType.Shop,
                        $"seed={seed}: 精英/商店相邻 {p} ↔ {nb}");
                }
            }
        }
    }

    [Fact]
    public void Map_Act1NoEliteBeforeFirstTreasure()
    {
        // 强度规则：第一大层精英不得出现在首个宝箱（TreasureRow）之前
        for (int seed = 1; seed <= 300; seed++)
        {
            var map = new StandardActMap(new BattleRandom(seed), actIndex: 1);
            foreach (var p in map.AllPoints.Where(p => p.Type == MapPointType.Elite))
            {
                Assert.True(p.Coord.Row >= map.TreasureRow, $"seed={seed}: 首个宝箱前出现精英 {p}");
            }
        }
    }

    private static IEnumerable<MapPoint> NeighborsOf(StandardActMap map, MapPoint p)
    {
        foreach (var child in p.Children)
        {
            yield return child;
        }

        foreach (var parent in p.Parents)
        {
            yield return parent;
        }

        foreach (var other in map.AllPoints)
        {
            if (other.Coord.Row == p.Coord.Row && Math.Abs(other.Coord.Col - p.Coord.Col) == 1)
            {
                yield return other;
            }
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(999)]
    public void Map_SameSeed_Deterministic(int seed)
    {
        var a = new StandardActMap(new BattleRandom(seed));
        var b = new StandardActMap(new BattleRandom(seed));

        var seqA = a.AllPoints.Select(p => (p.Coord.Col, p.Coord.Row, p.Type)).OrderBy(x => x.Row).ThenBy(x => x.Col);
        var seqB = b.AllPoints.Select(p => (p.Coord.Col, p.Coord.Row, p.Type)).OrderBy(x => x.Row).ThenBy(x => x.Col);
        Assert.Equal(seqA, seqB);
    }

    private static bool CanReachBoss(StandardActMap map, MapPoint from)
    {
        var visited = new HashSet<MapPoint>();
        var stack = new Stack<MapPoint>();
        stack.Push(from);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == map.Boss)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var child in current.Children)
            {
                stack.Push(child);
            }
        }

        return false;
    }
}

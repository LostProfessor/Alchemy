using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Encounters;
using Alchemy.Core.Random;

namespace Alchemy.Core.Map;

/// <summary>
/// 标准大层地图：照抄杀戮尖塔 2 的算法——
/// 拓扑随机（随机游走 + 防交叉）+ 结构固定（首行战斗、中部宝箱、首领前行休息、首领收尾）
/// + 数量随机但受限 + 摆放随机但受限。
/// </summary>
public sealed class StandardActMap
{
    public const int Width = 7;

    private readonly int _contentFloors; // 内容行数（不含起始行与首领行）
    private readonly int _actIndex;       // 大层序号（用于选遭遇/首领）
    private readonly IRandomSource _rng;
    private readonly MapPoint?[,] _grid;

    /// <summary>内容行数（首行 1 到末行 ContentFloors）。</summary>
    public int ContentFloors => _contentFloors;

    /// <summary>固定宝箱所在行（中部）。</summary>
    public int TreasureRow => _contentFloors / 2;

    public MapPoint Boss { get; }

    public MapPoint Start { get; }

    public StandardActMap(IRandomSource rng, int actIndex = 1, int contentFloors = 8)
    {
        _rng = rng;
        _actIndex = actIndex;
        _contentFloors = contentFloors;
        int rows = contentFloors + 2; // 0=起始行, 1..contentFloors=内容, contentFloors+1=首领行
        _grid = new MapPoint?[Width, rows];
        Boss = new MapPoint(Width / 2, contentFloors + 1);
        Start = new MapPoint(Width / 2, 0);
        GeneratePaths();
        AssignTypes();
        RollEncounters();
    }

    public IEnumerable<MapPoint> AllPoints
    {
        get
        {
            for (int row = 1; row <= _contentFloors; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (_grid[col, row] != null)
                    {
                        yield return _grid[col, row]!;
                    }
                }
            }
        }
    }

    public IReadOnlyList<MapPoint> PointsInRow(int row)
    {
        var list = new List<MapPoint>();
        for (int col = 0; col < Width; col++)
        {
            if (_grid[col, row] != null)
            {
                list.Add(_grid[col, row]!);
            }
        }

        return list;
    }

    private MapPoint GetOrCreate(int col, int row)
    {
        if (_grid[col, row] != null)
        {
            return _grid[col, row]!;
        }

        var point = new MapPoint(col, row);
        _grid[col, row] = point;
        return point;
    }

    // ── 拓扑：随机游走 + 防交叉 ──────────────────────────────

    private void GeneratePaths()
    {
        // 第 1 行随机放 5 个起点，各向上游走
        var starts = new List<MapPoint>();
        for (int i = 0; i < 5; i++)
        {
            var start = GetOrCreate(_rng.Next(Width), 1);
            if (!starts.Contains(start))
            {
                starts.Add(start);
            }
        }

        foreach (var start in starts)
        {
            PathGenerate(start);
        }

        // 起始点 → 第 1 行；末行 → 首领
        foreach (var p in PointsInRow(1))
        {
            Start.AddChild(p);
        }

        foreach (var p in PointsInRow(_contentFloors))
        {
            p.AddChild(Boss);
        }
    }

    private void PathGenerate(MapPoint start)
    {
        var current = start;
        while (current.Coord.Row < _contentFloors)
        {
            var nextCoord = GenerateNextCoord(current);
            var next = GetOrCreate(nextCoord.Col, nextCoord.Row);
            current.AddChild(next);
            current = next;
        }
    }

    private MapCoord GenerateNextCoord(MapPoint current)
    {
        var options = new List<int> { -1, 0, 1 };
        options.Shuffle(_rng);
        foreach (var delta in options)
        {
            int targetCol = Math.Clamp(current.Coord.Col + delta, 0, Width - 1);
            if (!HasInvalidCrossover(current, targetCol))
            {
                return new MapCoord(targetCol, current.Coord.Row + 1);
            }
        }

        return new MapCoord(current.Coord.Col, current.Coord.Row + 1); // 兜底直上
    }

    /// <summary>防交叉：同行的既有节点若已有"反向斜出"的孩子，则穿过会形成交叉。</summary>
    private bool HasInvalidCrossover(MapPoint current, int targetCol)
    {
        int dx = targetCol - current.Coord.Col;
        if (dx == 0)
        {
            return false;
        }

        var existing = _grid[targetCol, current.Coord.Row];
        if (existing == null)
        {
            return false;
        }

        foreach (var child in existing.Children)
        {
            if (child.Coord.Col - existing.Coord.Col == -dx)
            {
                return true;
            }
        }

        return false;
    }

    // ── 类型分配：固定行 + 随机受限摆放 ───────────────────────

    private void AssignTypes()
    {
        SetRowType(1, MapPointType.Combat);            // 首行必为普通战斗
        SetRowType(TreasureRow, MapPointType.Treasure); // 中部固定宝箱
        SetRowType(_contentFloors, MapPointType.RestSite); // 首领前固定休息

        var queue = new Queue<MapPointType>();
        int shops = _rng.Next(2) + 1;   // 1~2
        int events = _rng.Next(2) + 2;  // 2~3
        int elites = _rng.Next(3) + 2;  // 2~3（比原来的 1~2 更多）
        for (int i = 0; i < shops; i++)
        {
            queue.Enqueue(MapPointType.Shop);
        }

        for (int i = 0; i < events; i++)
        {
            queue.Enqueue(MapPointType.Event);
        }

        for (int i = 0; i < elites; i++)
        {
            queue.Enqueue(MapPointType.Elite);
        }

        AssignToRandomPoints(queue);

        // 剩余未分配 → 普通战斗
        foreach (var p in AllPoints.Where(p => p.Type == MapPointType.Unassigned))
        {
            p.Type = MapPointType.Combat;
        }

        Boss.Type = MapPointType.Boss;
    }

    private void SetRowType(int row, MapPointType type)
    {
        foreach (var point in PointsInRow(row))
        {
            point.Type = type;
            point.CanBeModified = false;
        }
    }

    private void AssignToRandomPoints(Queue<MapPointType> queue)
    {
        var candidates = AllPoints.Where(p => p.Type == MapPointType.Unassigned).ToList();
        candidates.Shuffle(_rng);
        foreach (var point in candidates)
        {
            if (queue.Count == 0)
            {
                break;
            }

            var type = queue.Peek();

            // 第一大层：精英不得出现在第一个宝箱（TreasureRow）之前，避免前期强度过大
            if (_actIndex == 1 && type == MapPointType.Elite && point.Coord.Row < TreasureRow)
            {
                continue;
            }

            if (!CanPlaceSpecialAt(point))
            {
                continue; // 特殊节点不扎堆
            }

            point.Type = queue.Dequeue();
            point.CanBeModified = false;
        }
    }

    /// <summary>特殊节点（商店/事件/精英）不与宝箱/商店/休息/精英相邻。</summary>
    private bool CanPlaceSpecialAt(MapPoint point)
    {
        foreach (var neighbor in NeighborsOf(point))
        {
            if (neighbor.Type is MapPointType.Treasure or MapPointType.Shop or MapPointType.RestSite or MapPointType.Elite)
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerable<MapPoint> NeighborsOf(MapPoint point)
    {
        foreach (var child in point.Children)
        {
            yield return child;
        }

        foreach (var parent in point.Parents)
        {
            yield return parent;
        }

        for (int dx = -1; dx <= 1; dx += 2)
        {
            int col = point.Coord.Col + dx;
            if (col >= 0 && col < Width && _grid[col, point.Coord.Row] != null)
            {
                yield return _grid[col, point.Coord.Row]!;
            }
        }
    }

    // ── 遭遇预掷（保证创建房间时不消耗随机、确定性）────────────

    private void RollEncounters()
    {
        // 首领不在内容网格中，按大层单独取
        Boss.Encounter = EncounterFactory.BossForAct(_actIndex);

        foreach (var point in AllPoints)
        {
            if (point.Type is MapPointType.Combat or MapPointType.Elite)
            {
                point.Encounter = EncounterFactory.Random(_rng, _actIndex);
            }
        }
    }
}

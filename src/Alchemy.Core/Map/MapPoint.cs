using System.Collections.Generic;
using Alchemy.Core.Encounters;

namespace Alchemy.Core.Map;

/// <summary>地图坐标（列, 行）。</summary>
public readonly record struct MapCoord(int Col, int Row);

/// <summary>
/// 地图节点：7 列网格中的一个点，含类型、父/子连接与（战斗类）遭遇。
/// 拓扑由 StandardActMap 的随机游走生成。
/// </summary>
public sealed class MapPoint
{
    public MapCoord Coord { get; }

    public MapPointType Type { get; set; } = MapPointType.Unassigned;

    /// <summary>是否可被随机类型分配改写（固定行的节点为 false）。</summary>
    public bool CanBeModified { get; set; } = true;

    public List<MapPoint> Parents { get; } = new();

    public List<MapPoint> Children { get; } = new();

    /// <summary>战斗类节点（Combat/Elite/Boss）在生成时预掷的遭遇。</summary>
    public Encounter? Encounter { get; set; }

    public MapPoint(int col, int row) => Coord = new MapCoord(col, row);

    public void AddChild(MapPoint child)
    {
        Children.Add(child);
        child.Parents.Add(this);
    }

    public override string ToString() => $"{Type}@{Coord.Col},{Coord.Row}";
}

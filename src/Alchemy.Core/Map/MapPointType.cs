namespace Alchemy.Core.Map;

/// <summary>地图节点类型（对应策划案的房间类型 + 首领 + 火堆）。</summary>
public enum MapPointType
{
    /// <summary>尚未分配。</summary>
    Unassigned,

    /// <summary>普通战斗房。</summary>
    Combat,

    /// <summary>精英战斗房（更强遭遇）。</summary>
    Elite,

    /// <summary>事件房。</summary>
    Event,

    /// <summary>宝箱房（中间固定必有）。</summary>
    Treasure,

    /// <summary>火堆休息房（首领前必有）。</summary>
    RestSite,

    /// <summary>商店房。</summary>
    Shop,

    /// <summary>首领。</summary>
    Boss,
}

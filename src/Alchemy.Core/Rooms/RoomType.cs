namespace Alchemy.Core.Rooms;

/// <summary>房间类型。</summary>
public enum RoomType
{
    /// <summary>战斗房（预设战斗房间：怪物类型与组合由资源文件控制）。</summary>
    Combat,

    /// <summary>事件房（抉择事件/商店等特殊玩法，由资源文件控制）。</summary>
    Event,

    /// <summary>宝藏房（遗物/药材/货币随机刷）。</summary>
    Treasure,

    /// <summary>商店房。</summary>
    Shop,

    /// <summary>火堆休息房（首领前必有；睡觉/探索）。</summary>
    RestSite,
}

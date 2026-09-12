namespace Alchemy.Core.Rooms;

/// <summary>火堆休息房：首领前必有；玩家二选一（睡觉 / 探索）。</summary>
public sealed class RestSiteRoom : AbstractRoom
{
    public RestSiteRoom(string id, int floor) : base(id, RoomType.RestSite, floor)
    {
    }
}

/// <summary>火堆的两个选项。</summary>
public enum RestChoice
{
    /// <summary>睡觉：恢复已损失生命的 30%（可被遗物修饰）。</summary>
    Sleep,

    /// <summary>探索：随机获得一个普通遗物 + 一次不含最高稀有度的药材奖励。</summary>
    Explore,
}

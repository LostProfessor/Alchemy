namespace Alchemy.Core.Rooms;

/// <summary>
/// 房间基类：整局中的一个节点。战斗/事件/宝藏/商店各成子类。
/// 具体玩法内容后续由资源文件（.tres）驱动。
/// </summary>
public abstract class AbstractRoom
{
    public string Id { get; }

    public RoomType Type { get; }

    /// <summary>所在大层内的楼层序号。</summary>
    public int Floor { get; }

    public bool Completed { get; private set; }

    protected AbstractRoom(string id, RoomType type, int floor)
    {
        Id = id;
        Type = type;
        Floor = floor;
    }

    public void MarkCompleted() => Completed = true;
}

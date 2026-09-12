using Alchemy.Core.Rooms.Events;

namespace Alchemy.Core.Rooms;

/// <summary>事件房：进入时分配一个随机事件，玩家在 2~3 个选项中做抉择。</summary>
public sealed class EventRoom : AbstractRoom
{
    /// <summary>进入时分配的事件（内容由数据定义）。</summary>
    public EventDefinition? Event { get; set; }

    public EventRoom(string id, int floor) : base(id, RoomType.Event, floor)
    {
    }
}

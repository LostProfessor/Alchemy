using Alchemy.Core.Encounters;

namespace Alchemy.Core.Rooms;

/// <summary>战斗房：携带一场遭遇（怪物组合）。</summary>
public sealed class CombatRoom : AbstractRoom
{
    public Encounter Encounter { get; }

    public CombatRoom(string id, int floor, Encounter encounter)
        : base(id, RoomType.Combat, floor) => Encounter = encounter;
}

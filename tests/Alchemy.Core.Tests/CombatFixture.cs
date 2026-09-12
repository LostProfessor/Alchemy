using Alchemy.Core.Combat;
using Alchemy.Core.Entities;

namespace Alchemy.Core.Tests;

internal static class CombatFixture
{
    public static (CombatState Combat, Creature Player, Creature Enemy) Create(int seed = 12345)
    {
        var combat = new CombatState(seed);
        var player = new Creature("玩家", 30, isPlayer: true);
        var enemy = new Creature("敌人", 30);
        combat.AddAlly(player);
        combat.AddEnemy(enemy);
        return (combat, player, enemy);
    }
}

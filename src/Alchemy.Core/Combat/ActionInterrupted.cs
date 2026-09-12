using Alchemy.Core.Entities;
using Alchemy.Core.Encounters;

namespace Alchemy.Core.Combat;

/// <summary>
/// 打断（破招）：可打断的敌人出手，在"预兆已发、尚未结算"的窗口内受到一次达阈值的伤害 →
/// 出手被取消，敌人进入 <see cref="StaggerSeconds"/> 的瘫痪，结束后直接进入下一个意图。
/// </summary>
public sealed record ActionInterrupted(
    Creature Actor,
    Intention Intention,
    int Damage,
    float StaggerSeconds);

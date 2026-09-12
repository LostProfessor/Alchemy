using Alchemy.Core.Entities;

namespace Alchemy.Core.Combat;

/// <summary>
/// 动作预兆：某个计时动作即将在 <see cref="SecondsUntilExecute"/> 秒后执行。
/// 表现层据此播"抬手/蓄力/预警"等预备动画（敌人出手、将来也可用于玩家动作/药水出炉）。
/// 未来若想让遗物/效果也响应预兆，可在 HookHub 上再加一个监听点，复用同一份上下文。
/// </summary>
public sealed record ActionTelegraph(
    Creature? Actor,
    string ActionId,
    TimedAction Action,
    float SecondsUntilExecute);

namespace Alchemy.Core.Runs;

/// <summary>整局阶段（一局游戏的主循环状态机）。</summary>
public enum RunPhase
{
    /// <summary>尚未开始。</summary>
    NotStarted,

    /// <summary>在地图上选择下一房间。</summary>
    OnMap,

    /// <summary>正在某个房间内（战斗/事件/商店/宝藏）。</summary>
    InRoom,

    /// <summary>战斗胜利后的奖励结算（三选一）。</summary>
    Reward,

    /// <summary>首领战后、进入下一大层前的首领遗物三选一。</summary>
    BossRelicChoice,

    /// <summary>通关。</summary>
    Completed,

    /// <summary>玩家死亡。</summary>
    Defeated,

    /// <summary>放弃游戏。</summary>
    Abandoned,
}

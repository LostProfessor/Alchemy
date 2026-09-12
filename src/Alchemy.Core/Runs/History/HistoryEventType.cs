namespace Alchemy.Core.Runs.History;

/// <summary>经历记录事件类型（存档中留存的玩家经历）。</summary>
public enum HistoryEventType
{
    /// <summary>进入房间。</summary>
    RoomEntered,

    /// <summary>完成房间（含战斗胜负）。</summary>
    RoomCompleted,

    /// <summary>生命值变化（每房起点→终点）。</summary>
    HpChanged,

    /// <summary>消耗药材。</summary>
    IngredientConsumed,

    /// <summary>获得奖励（货币/遗物/药材袋/事件收益等）。</summary>
    RewardObtained,
}

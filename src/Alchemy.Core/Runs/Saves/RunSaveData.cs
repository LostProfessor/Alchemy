using System.Collections.Generic;
using Alchemy.Core.Runs.History;

namespace Alchemy.Core.Runs.Saves;

/// <summary>
/// 存档数据（JSON 序列化）：只存"状态"，模板定义（效果/词条等）不入档。
/// 检查点 = 进入新房间时的地图状态；读档后从该房间重进（战斗中退出→重进该房重打）。
/// </summary>
public sealed class RunSaveData
{
    public int Version { get; set; } = 1;

    /// <summary>整局随机主种子（与 StreamCounter 一起保证读档后随机确定）。</summary>
    public int MasterSeed { get; set; }

    /// <summary>职业 id（researcher/elf/et；旧档缺失时读档按默认解析）。</summary>
    public string? JobId { get; set; }

    public int StreamCounter { get; set; }

    public int ActIndex { get; set; }

    /// <summary>当前大层地图的生成种子（读档时据此重建同一张地图）。</summary>
    public int CurrentMapSeed { get; set; }

    /// <summary>检查点节点坐标（读档后玩家在此地图节点等待重进下一房间）。</summary>
    public int CurrentMapCol { get; set; } = -1;

    public int CurrentMapRow { get; set; } = -1;

    public int PlayerMaxHp { get; set; }

    public int PlayerCurrentHp { get; set; }

    public int Currency { get; set; }

    /// <summary>药材口袋：药材 Id → 数量。</summary>
    public Dictionary<string, int> Pocket { get; set; } = new();

    /// <summary>遗物 Id 列表（读档时用 RelicCatalog.CreateById 重建）。</summary>
    public List<string> RelicIds { get; set; } = new();

    /// <summary>经历记录（房间/生命/药材/奖励）。</summary>
    public List<HistoryEntry> History { get; set; } = new();

    /// <summary>奖励界面阶段快照（Reward/BossRelicChoice）；非奖励阶段为 null（读档回地图检查点）。</summary>
    public SavedRewardState? RewardState { get; set; }
}

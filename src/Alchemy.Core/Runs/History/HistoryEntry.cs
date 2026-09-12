namespace Alchemy.Core.Runs.History;

/// <summary>
/// 一条经历记录（扁平字段，便于 JSON 序列化/存档）。
/// </summary>
public sealed class HistoryEntry
{
    /// <summary>顺序号（时间线）。</summary>
    public int Order { get; set; }

    /// <summary>所在大层。</summary>
    public int Act { get; set; }

    /// <summary>相关房间 Id（无则空）。</summary>
    public string RoomId { get; set; } = string.Empty;

    public HistoryEventType Type { get; set; }

    /// <summary>人类可读描述。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>数值（HP 变化量 / 货币等）。</summary>
    public int Amount { get; set; }

    public int HpBefore { get; set; }

    public int HpAfter { get; set; }

    /// <summary>相关药材/遗物 Id（无则空）。</summary>
    public string ItemId { get; set; } = string.Empty;
}

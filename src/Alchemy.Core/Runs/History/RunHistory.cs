using System.Collections.Generic;

namespace Alchemy.Core.Runs.History;

/// <summary>
/// 整局经历记录：进入/完成房间、每房生命变化、消耗的药材、获得的奖励。
/// 属于存档的一部分，用于复盘与展示。
/// </summary>
public sealed class RunHistory
{
    private readonly List<HistoryEntry> _entries = new();
    private int _nextOrder;

    public IReadOnlyList<HistoryEntry> Entries => _entries;

    public void RecordRoomEntered(int act, string roomId, string message) =>
        Add(HistoryEventType.RoomEntered, act, roomId, message);

    public void RecordRoomCompleted(int act, string roomId, string message) =>
        Add(HistoryEventType.RoomCompleted, act, roomId, message);

    public void RecordHpChange(int act, string roomId, int before, int after) =>
        Add(HistoryEventType.HpChanged, act, roomId, $"生命 {before}→{after}", amount: after - before, before: before, after: after);

    public void RecordIngredientConsumed(int act, string roomId, string ingredientId) =>
        Add(HistoryEventType.IngredientConsumed, act, roomId, $"消耗药材 {ingredientId}", itemId: ingredientId);

    public void RecordReward(int act, string roomId, string message, int amount = 0, string itemId = "") =>
        Add(HistoryEventType.RewardObtained, act, roomId, message, amount: amount, itemId: itemId);

    /// <summary>存档恢复用：清空并重建记录。</summary>
    public void Restore(IEnumerable<HistoryEntry> entries)
    {
        _entries.Clear();
        foreach (var entry in entries)
        {
            _entries.Add(entry);
        }

        _nextOrder = _entries.Count > 0 ? _entries[^1].Order + 1 : 0;
    }

    private void Add(HistoryEventType type, int act, string roomId, string message, int amount = 0, int before = 0, int after = 0, string itemId = "")
    {
        _entries.Add(new HistoryEntry
        {
            Order = _nextOrder++,
            Act = act,
            RoomId = roomId,
            Type = type,
            Message = message,
            Amount = amount,
            HpBefore = before,
            HpAfter = after,
            ItemId = itemId,
        });
    }
}

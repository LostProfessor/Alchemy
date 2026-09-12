using System.Linq;
using Alchemy.Core.Rooms.Events;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 事件资源（content/events/*.tres）：一个事件 = 标题/描述 + 若干选项（抉择），
/// 由 <see cref="ContentCatalog.BuildEvents"/> 扫描后注入逻辑层 <see cref="EventCatalog"/>。
/// 改事件内容 = 加/改 .tres，不动代码。
/// </summary>
[GlobalClass]
public partial class EventResource : Resource
{
    /// <summary>事件 id（如 wounded_hunter）。</summary>
    [Export] public string Id { get; set; } = string.Empty;

    /// <summary>事件标题。</summary>
    [Export] public string Title { get; set; } = string.Empty;

    /// <summary>事件正文描述（UI 里展示给玩家读的剧情/情况说明）。</summary>
    [Export] public string Description { get; set; } = string.Empty;

    /// <summary>可选项（2~3 个抉择）。</summary>
    [Export] public EventChoiceResource[] Choices { get; set; } = [];

    public EventDefinition ToDefinition() => new(
        Id,
        Title,
        (Choices ?? []).Select(c => c.ToChoice()).ToList());
}

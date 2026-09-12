using System.Linq;
using Alchemy.Core.Rooms.Events;
using Godot;

namespace Alchemy.Nodes;

/// <summary>事件的一个选项：按钮文案 + 触发后的动作序列。</summary>
[GlobalClass]
public partial class EventChoiceResource : Resource
{
    /// <summary>选项按钮文案（含结果说明，如"包扎伤口（恢复 8 生命）"）。</summary>
    [Export] public string Label { get; set; } = string.Empty;

    /// <summary>选中后执行的动作序列。</summary>
    [Export] public EventActionResource[] Actions { get; set; } = [];

    public EventChoice ToChoice() => new(
        Label,
        (Actions ?? []).Select(a => a.ToAction()).ToList());
}

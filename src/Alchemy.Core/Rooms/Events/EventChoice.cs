using System.Collections.Generic;

namespace Alchemy.Core.Rooms.Events;

/// <summary>事件的一个选项：按钮文案 + 触发后的动作序列。</summary>
public sealed record EventChoice(string Label, IReadOnlyList<EventAction> Actions);

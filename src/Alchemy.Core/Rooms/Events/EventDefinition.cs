using System.Collections.Generic;

namespace Alchemy.Core.Rooms.Events;

/// <summary>一个事件 = 2~3 个可选选项（策划案：抉择事件）。</summary>
public sealed record EventDefinition(string Id, string Title, IReadOnlyList<EventChoice> Choices);

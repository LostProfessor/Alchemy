using Godot;

namespace Alchemy.Nodes;

/// <summary>职业开局药材的一条：药材 id + 数量（JobResource 用数组）。</summary>
[GlobalClass]
public partial class JobStarterIngredient : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public int Count { get; set; } = 1;
}

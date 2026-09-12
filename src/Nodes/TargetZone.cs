using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 投掷目标接收区（VBoxContainer，可竖排子节点）：出炉药水（type=potion）拖到它上面松手 = 触发 <see cref="PotionDropped"/>。
/// 挂到玩家区（Inspector 拖 TargetZone.cs）；敌人视图（代码生成）也用本类。
/// "目标自己接收 drop"同锅（CauldronZone），是 Godot 拖放最可靠的方式，无需任何坐标换算。
/// </summary>
public partial class TargetZone : VBoxContainer
{
	/// <summary>药水被拖入时触发（具体药水由 CombatNode 的 _finishedPotion 提供）。</summary>
	public event System.Action? PotionDropped;

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		var dict = data.AsGodotDictionary();
		return dict.TryGetValue("type", out var t) && t.AsString() == "potion";
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		PotionDropped?.Invoke();
	}
}

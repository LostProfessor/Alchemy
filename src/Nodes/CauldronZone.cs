using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Combat;
using Alchemy.Core.GameData;
using Alchemy.Core.Runs;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 锅（炼药投放区）：材料卡拖到它上面 = 触发 <see cref="IngredientDropped"/>。
/// 外观按职业选锅图（研究员=烧杯 / 精灵=陶锅 / ET=神秘面板），锅图透明区下垫一块随当前药水颜色变化的 ColorRect。
/// ⚠️ "目标控件自己接收 drop"是 Godot 拖放最可靠的方式，无需任何跨层坐标换算。
/// </summary>
public partial class CauldronZone : PanelContainer
{
	/// <summary>材料被拖入锅时触发。</summary>
	public event System.Action<Ingredient>? IngredientDropped;

	[Export] private ColorRect _potionColor = null!; // 药水颜色（垫在锅图透明区下）
	[Export] private TextureRect _potIcon = null!;   // 职业锅图

	private GameState _game = null!;
	private BrewingSession? _session;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");
		ApplyJobPot();
	}

	/// <summary>CombatNode 建好炼药会话后注入，本控件每帧据此刷新药水颜色。</summary>
	public void SetSession(BrewingSession session) => _session = session;

	/// <summary>按职业/基底选锅图：aqua=烧杯(研究员)、oil=陶锅(精灵)、slime=神秘面板(ET)。</summary>
	private void ApplyJobPot()
	{
		if (_potIcon == null)
		{
			return;
		}

		string baseLiquid = _game.Manager?.BaseLiquidId ?? "aqua";
		string path = baseLiquid switch
		{
			"oil" => "res://Theme/textures/ui/earthen_pot.png",
			"slime" => "res://Theme/textures/ui/mysterious_panel.png",
			_ => "res://Theme/textures/ui/beaker.png",
		};
		_potIcon.Texture = GD.Load<Texture2D>(path);
	}

	public override void _Process(double delta)
	{
		if (_potionColor == null || _session == null)
		{
			return;
		}

		var potion = _session.ActivePotion;
		if (potion == null)
		{
			_potionColor.Color = new Color(0, 0, 0, 0); // 无药水：透明
			return;
		}

		var c = potion.Color; // PotionColor 为 0~255 字节，转 0~1
		_potionColor.Color = new Color(c.R / 255f, c.G / 255f, c.B / 255f, 1f);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		// 只要拖的是材料就允许放在这（能否真正加入由 CombatNode 判断）
		var dict = data.AsGodotDictionary();
		return dict.TryGetValue("type", out var t) && t.AsString() == "ingredient";
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var dict = data.AsGodotDictionary();
		if (dict.TryGetValue("id", out var idVar))
		{
			var ingredient = Ingredients.Default.All.FirstOrDefault(i => i.Id == idVar.AsString());
			if (ingredient != null)
			{
				IngredientDropped?.Invoke(ingredient);
			}
		}
	}
}

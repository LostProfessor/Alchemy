using System.Linq;
using Alchemy.Core.Brewing;
using Alchemy.Core.Effects;
using Alchemy.Core.Relics;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 通用悬浮信息面板：跟随鼠标、出现在鼠标旁。
/// 目前用于药材卡悬停：展示药材名（稀有度着色）+ 描述 + **增减效果条目**（重点）。
/// 外观在预制体 scenes/ui/info_tooltip.tscn 里可视化编辑（[Export] 绑定标题/描述/容器）；
/// 效果条目是动态的，运行时加到 _body（保留 Title/Desc 两个固定节点）。
/// 由场景加载预制体并挂到 CanvasLayer 顶层，单例 Instance 供卡片直接调用。
/// </summary>
public partial class InfoTooltip : PanelContainer
{
	public static InfoTooltip? Instance { get; private set; }

	private const float OffsetX = 16f;
	private const float OffsetY = 18f;

	// 预制体绑定的节点（可在编辑器可视化改外观/布局）
	[Export] private VBoxContainer _body = null!;  // 主容器（效果条目/分隔线动态加到里面）
	[Export] private Label _title = null!;         // 标题：药材名
	[Export] private Label _desc = null!;          // 描述

	public override void _Ready()
	{
		Instance = this;
		MouseFilter = MouseFilterEnum.Ignore; // 不挡鼠标点击
		ZIndex = 1000;
		Visible = false;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public override void _Process(double delta)
	{
		if (!Visible)
		{
			return;
		}

		var viewport = GetViewport();
		var mouse = viewport.GetMousePosition();
		var viewSize = viewport.GetVisibleRect().Size;
		var mySize = GetCombinedMinimumSize();

		// 跟随鼠标；右/下越界时翻到另一侧
		var pos = mouse + new Vector2(OffsetX, OffsetY);
		if (pos.X + mySize.X > viewSize.X)
		{
			pos.X = mouse.X - mySize.X - OffsetX;
		}

		if (pos.Y + mySize.Y > viewSize.Y)
		{
			pos.Y = mouse.Y - mySize.Y - OffsetY;
		}

		GlobalPosition = pos;
	}

	/// <summary>
	/// 展示药材悬浮信息：名字（稀有度色）+ 描述 + 增减效果条目。
	/// </summary>
	/// <summary>清空主容器里除标题/描述外的动态子节点（效果条目/分隔线等）。</summary>
	private void ClearBody()
	{
		foreach (var child in _body.GetChildren().Cast<Node>().ToList())
		{
			if (child != _title && child != _desc)
			{
				_body.RemoveChild(child);
				child.QueueFree();
			}
		}
	}

	public void ShowIngredient(Ingredient ingredient)
	{
		ClearBody();

		_title.Text = ingredient.DisplayName;
		_title.Modulate = RarityColor(ingredient.Rarity);
		_desc.Text = ContentCatalog.GetIngredientDescription(ingredient.Id);
		_desc.Visible = !string.IsNullOrWhiteSpace(_desc.Text);

		// 增减效果（重点）
		if (ingredient.AffixOps.Count > 0)
		{
			_body.AddChild(new HSeparator());
			foreach (var op in ingredient.AffixOps)
			{
				_body.AddChild(EffectLine(op));
			}
		}

		Visible = true;
	}

	/// <summary>
	/// 展示遗物悬浮信息：名称（按稀有度着色）+ 描述。
	/// displayName 传最终要显示的遗物名（.tres 有则用 .tres 名，否则逻辑层遗物自带名）。
	/// </summary>
	public void ShowRelic(string displayName, string description, RelicRarity rarity)
	{
		ClearBody();

		_title.Text = displayName;
		_title.Modulate = RelicRarityColor(rarity);
		_desc.Text = description;
		_desc.Visible = !string.IsNullOrWhiteSpace(description);

		Visible = true;
	}

	public void HideTooltip() => Visible = false;

	/// <summary>
	/// 把词条操作格式化成一行中文（含增/减着色）。
	/// </summary>
	private static Label EffectLine(AffixOp op)
	{
		var (text, color) = op.Type switch
		{
			AffixOpType.AddEffect => (
				$"添加 {EffectName(op.Effect)} ×{op.Amount}",
				PolarityColor(op.Effect)),
			AffixOpType.RemoveEffect => (
				$"移除 {EffectName(op.Effect)} ×{op.Amount}",
				new Color(1f, 0.55f, 0.4f)),
			AffixOpType.RemoveLast => ($"去掉末尾 ×{op.Amount}", Colors.LightGray),
			AffixOpType.ReverseOrder => ("翻转顺序", Colors.LightGray),
			AffixOpType.ReversePolarity => ("反转极性", Colors.LightGray),
			_ => (op.ToString(), Colors.LightGray),
		};
		return new Label
		{
			Text = text,
			Modulate = color,
			CustomMinimumSize = new Vector2(240, 0),
		};
	}

	private static string EffectName(EffectId id) =>
		EffectRegistry.Definitions.TryGetValue(id, out var d) ? d.DisplayName : id.ToString();

	/// <summary>
	/// 按效果极性着色：增益绿 / 减益红。
	/// </summary>
	private static Color PolarityColor(EffectId id) =>
		EffectRegistry.Definitions.TryGetValue(id, out var d)
			? d.Polarity == EffectPolarity.Positive
				? new Color(0.45f, 1f, 0.55f)
				: new Color(1f, 0.45f, 0.45f)
			: Colors.LightGray;

	private static Color RarityColor(IngredientRarity rarity) => rarity switch
	{
		IngredientRarity.Common => Colors.White,
		IngredientRarity.Uncommon => new Color(0.4f, 0.9f, 0.4f),
		IngredientRarity.Rare => new Color(0.4f, 0.6f, 1f),
		IngredientRarity.Legendary => new Color(1f, 0.7f, 0.2f),
		_ => Colors.White,
	};

	private static Color RelicRarityColor(RelicRarity rarity) => rarity switch
	{
		RelicRarity.Common => Colors.White,
		RelicRarity.Uncommon => new Color(0.4f, 0.9f, 0.4f),
		RelicRarity.Rare => new Color(0.4f, 0.6f, 1f),
		RelicRarity.Boss => new Color(1f, 0.7f, 0.2f),
		_ => Colors.White,
	};
}

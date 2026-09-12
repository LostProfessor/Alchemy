using Alchemy.Core.Brewing;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 背包材料卡：显示药材名 + 数量，可被拖拽（拖到锅上加入炼药）。
/// 外观在预制体 scenes/ui/ingredient_card.tscn 里可视化编辑（[Export] 绑定背景/图标/文字）；
/// 悬停时在鼠标旁弹出 InfoTooltip（描述 + 增减效果）。
/// </summary>
public partial class IngredientCard : PanelContainer
{
	public string IngredientId => _ingredient.Id;

	private Ingredient _ingredient = null!;
	private string _name = string.Empty;

	// 预制体里绑定的节点（可在编辑器改外观/布局）
	// 稀有度背景不另加 ColorRect，而是直接改卡片自身样式（panel stylebox，见 Setup）
	[Export] private TextureRect _icon = null!; // 药材图标
	[Export] private Label _label = null!;      // 名字 ×数量

	/// <summary>
	/// 从预制体（scenes/ui/ingredient_card.tscn）实例化一张药材卡并填充内容。
	/// 预制体缺失时返回 null（CombatNode 会跳过，避免崩溃）。
	/// </summary>
	public static IngredientCard? Create(Ingredient ingredient, int count)
	{
		const string scenePath = "res://scenes/ui/ingredient_card.tscn";
		if (!ResourceLoader.Exists(scenePath))
		{
			GD.PushError($"药材卡预制体不存在：{scenePath}");
			return null;
		}

		var card = GD.Load<PackedScene>(scenePath).Instantiate<IngredientCard>();
		card.Setup(ingredient, count);
		return card;
	}

	/// <summary>填充一张卡的内容（预制体节点由 [Export] 绑定）。</summary>
	private void Setup(Ingredient ingredient, int count)
	{
		_ingredient = ingredient;
		_name = ingredient.DisplayName;

		// 稀有度直接作用在卡片自身背景（panel stylebox），不影响图标/文字
		AddThemeStyleboxOverride("panel", CreateRarityStyle(ingredient.Rarity));

		if (_icon != null)
		{
			var icon = ContentCatalog.GetIngredientIcon(ingredient.Id);
			if (icon != null)
			{
				_icon.Texture = icon;
				_icon.Visible = true;
			}
			else
			{
				_icon.Visible = false;
			}
		}

		if (_label != null)
		{
			_label.Text = $"{_name} ×{count}";
		}

		// 悬停：鼠标旁显示描述 + 增减效果
		MouseEntered += () => InfoTooltip.Instance?.ShowIngredient(ingredient);
		MouseExited += () => InfoTooltip.Instance?.HideTooltip();
	}

	public void UpdateCount(int count)
	{
		if (_label != null)
		{
			_label.Text = $"{_name} ×{count}";
		}
	}

	/// <summary>
	/// 稀有度卡片背景：直接加载 Theme/textures/ui/{稀有度}_ingredient_card.png（130x60，1:1 填充）。
	/// TextureMargin 保留四角圆角区（九宫格），卡片尺寸变化时圆角也不变形。
	/// </summary>
	private static StyleBox CreateRarityStyle(IngredientRarity rarity)
	{
		var name = rarity switch
		{
			IngredientRarity.Common => "common",
			IngredientRarity.Uncommon => "uncommon",
			IngredientRarity.Rare => "rare",
			IngredientRarity.Legendary => "legendary",
			_ => "common",
		};

		return new StyleBoxTexture
		{
			Texture = GD.Load<Texture2D>($"res://Theme/textures/ui/{name}_ingredient_card.png"),
			TextureMarginLeft = 6,
			TextureMarginTop = 6,
			TextureMarginRight = 6,
			TextureMarginBottom = 6,
		};
	}

	/// <summary>开始拖拽：返回数据（type=ingredient, id=药材Id），并设一个跟随鼠标的预览。</summary>
	public override Variant _GetDragData(Vector2 atPosition)
	{
		var data = new Godot.Collections.Dictionary
		{
			["type"] = "ingredient",
			["id"] = IngredientId,
		};

		var preview = new Label
		{
			Text = _label?.Text ?? _name,
			Modulate = new Color(1f, 1f, 1f),
		};
		SetDragPreview(preview);
		return data;
	}
}

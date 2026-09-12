using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Relics;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 遗物栏（scenes/ui/relic_bar.tscn）：横向展示当前整局拥有的全部遗物（含开局职业遗物）。
/// 每个图标悬停时在鼠标旁弹 <see cref="InfoTooltip"/>（名称按稀有度着色 + 描述）。
/// 显示名/描述优先取 content/relics/*.tres（按遗物 Id 绑定），没有则回退逻辑层遗物自带名。
/// 放到地图/战斗场景顶栏区域；没有进行中的局时自动保持空。
/// </summary>
public partial class RelicBar : PanelContainer
{
	[Export] private HBoxContainer _row = null!; // 遗物图标行（遗物数量变化时重建）

	private GameState _game = null!;
	private int _lastCount = -1;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");
		CallDeferred(nameof(EnsureTooltip)); // 等本帧挂载完再建 tooltip（直接 AddChild 到 Root 可能撞上挂载期）
	}

	public override void _Process(double delta)
	{
		var mgr = _game.Manager;
		int count = mgr?.Run.Relics.Count ?? 0;
		if (count == _lastCount)
		{
			return; // 数量没变就跳过（图标本身不变，悬停数据静态）
		}

		_lastCount = count;
		Rebuild(mgr?.Run.Relics.Relics);
	}

	private void Rebuild(IReadOnlyList<Relic>? relics)
	{
		foreach (var child in _row.GetChildren())
		{
			child.QueueFree();
		}

		if (relics == null)
		{
			return;
		}

		foreach (var relic in relics)
		{
			_row.AddChild(CreateIcon(relic));
		}
	}

	/// <summary>单个遗物图标：有 .tres 图标用图，否则深色圆角块 + 名称首字占位。</summary>
	private Control CreateIcon(Relic relic)
	{
		var res = ContentCatalog.GetRelicResource(relic.Id);
		string name = string.IsNullOrWhiteSpace(res?.DisplayName) ? relic.DisplayName : res!.DisplayName;
		string desc = ContentCatalog.GetRelicDescription(relic.Id);
		var rarityColor = RarityColor(relic.Rarity);

		// 图标槽：固定 36x36（原 40，逐步减到 36）、深色圆角底 + 稀有度描边
		var slot = new PanelContainer { CustomMinimumSize = new Vector2(36, 36) };
		slot.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.12f, 0.14f, 0.2f, 0.9f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			BorderColor = rarityColor,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomRight = 6,
			CornerRadiusBottomLeft = 6,
		});
		slot.MouseEntered += () => InfoTooltip.Instance?.ShowRelic(name, desc, relic.Rarity);
		slot.MouseExited += () => InfoTooltip.Instance?.HideTooltip();

		var tex = ContentCatalog.GetRelicIcon(relic.Id);
		if (tex != null)
		{
			slot.AddChild(new TextureRect
			{
				Texture = tex,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = MouseFilterEnum.Ignore,
			});
		}
		else
		{
			slot.AddChild(new Label
			{
				Text = string.IsNullOrEmpty(name) ? "?" : name[..1],
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Modulate = rarityColor,
				MouseFilter = MouseFilterEnum.Ignore,
			});
		}

		return slot;
	}

	/// <summary>场景里还没有 InfoTooltip 实例时自己建一个（挂独立高层 CanvasLayer）。</summary>
	private void EnsureTooltip()
	{
		if (InfoTooltip.Instance != null)
		{
			return;
		}

		const string scenePath = "res://scenes/ui/info_tooltip.tscn";
		if (!ResourceLoader.Exists(scenePath))
		{
			return;
		}

		var layer = new CanvasLayer { Layer = 100 };
		GetTree().Root.AddChild(layer);
		layer.AddChild(GD.Load<PackedScene>(scenePath).Instantiate<InfoTooltip>());
	}

	private static Color RarityColor(RelicRarity rarity) => rarity switch
	{
		RelicRarity.Common => new Color(0.75f, 0.78f, 0.85f),
		RelicRarity.Uncommon => new Color(0.4f, 0.9f, 0.4f),
		RelicRarity.Rare => new Color(0.4f, 0.6f, 1f),
		RelicRarity.Boss => new Color(1f, 0.7f, 0.2f),
		_ => Colors.White,
	};
}

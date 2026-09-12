using Alchemy.Core.Relics;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 开局遗物三选一场景（scenes/opening/opening.tscn）：职业遗物已自动获得，
/// 这里从普通遗物池再选 1 件（可跳过）→ 进地图。
/// 无候选（读档继续等）时直接跳地图。
/// </summary>
public partial class OpeningRelicNode : Control
{
	[Export] private VBoxContainer _list = null!; // 候选按钮列表

	private GameState _game = null!;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");
		Render();
	}

	private void Render()
	{
		var mgr = _game.Manager;
		if (mgr == null || mgr.PendingOpeningRelicChoices is not { Count: > 0 })
		{
			CallDeferred(nameof(GoMap)); // 没有开局候选 → 延迟进地图（不能在 _Ready 里直接切场景）
			return;
		}

		_list.AddChild(new Label
		{
			Text = "选择一件开局遗物（职业遗物已自动获得）",
			HorizontalAlignment = HorizontalAlignment.Center,
		});
		_list.AddChild(new HSeparator());

		for (int i = 0; i < mgr.PendingOpeningRelicChoices.Count; i++)
		{
			var relic = mgr.PendingOpeningRelicChoices[i];
			string name = ContentCatalog.GetRelicResource(relic.Id)?.DisplayName ?? relic.DisplayName;
			int index = i;
			var btn = new Button
			{
				Text = $"选择：{name}",
				CustomMinimumSize = new Vector2(420, 0),
			};
			btn.AddThemeColorOverride("font_color", RarityColor(relic.Rarity));
			btn.Pressed += () =>
			{
				mgr.ChooseOpeningRelic(index);
				GoMap();
			};
			_list.AddChild(btn);
		}

		_list.AddChild(new HSeparator());
		var skip = new Button
		{
			Text = "跳过（不拿额外遗物）",
			CustomMinimumSize = new Vector2(420, 0),
		};
		skip.Pressed += () =>
		{
			mgr.SkipOpeningRelic();
			GoMap();
		};
		_list.AddChild(skip);
	}

	private void GoMap() => _game.ChangeScene("res://scenes/map/map.tscn");

	private static Color RarityColor(RelicRarity rarity) => rarity switch
	{
		RelicRarity.Common => Colors.White,
		RelicRarity.Uncommon => new Color(0.4f, 0.9f, 0.4f),
		RelicRarity.Rare => new Color(0.4f, 0.6f, 1f),
		RelicRarity.Boss => new Color(1f, 0.7f, 0.2f),
		_ => Colors.White,
	};
}

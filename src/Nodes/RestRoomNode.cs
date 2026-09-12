using Alchemy.Core.Rooms;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 火堆休息房场景（scenes/room/rest.tscn）：二选一——睡觉（回血）/ 探索（普通遗物 + 药材）。
/// </summary>
public partial class RestRoomNode : RoomNodeBase
{
	protected override void Render()
	{
		ClearContent();
		var mgr = Manager;
		if (mgr?.CurrentRoom is not RestSiteRoom)
		{
			ShowMissing("当前不在休息点");
			return;
		}

		var title = new Label
		{
			Text = L.T("🔥 休息点"),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 34);
		Content.AddChild(title);

		Content.AddChild(new Label
		{
			Text = L.T("首领前的最后一处篝火。"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(0.75f, 0.75f, 0.75f),
		});
		Content.AddChild(new HSeparator());

		var hp = mgr.Player;
		Content.AddChild(new Label
		{
			Text = $"{hp.Name}  {hp.CurrentHp}/{hp.MaxHp}",
			HorizontalAlignment = HorizontalAlignment.Center,
		});

		int heal = RestSiteActions.CalculateSleepHeal(hp); // 基础值（不含遗物加成）
		Content.AddChild(new Label
		{
			Text = L.F("睡一觉约恢复 {0} 点生命（遗物可加成）", heal),
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(0.6f, 0.9f, 0.65f),
		});

		var sleep = new Button
		{
			Text = L.T("睡觉"),
			CustomMinimumSize = new Vector2(420, 0),
		};
		sleep.Pressed += () => Rest(RestChoice.Sleep);
		Content.AddChild(sleep);

		var explore = new Button
		{
			Text = L.T("探索（随机获得 1 件普通遗物 + 药材奖励）"),
			CustomMinimumSize = new Vector2(420, 0),
		};
		explore.Pressed += () => Rest(RestChoice.Explore);
		Content.AddChild(explore);
	}

	private void Rest(RestChoice choice)
	{
		var mgr = Manager;
		if (mgr == null)
		{
			return;
		}

		mgr.PerformRest(choice); // 完成该房（Phase → OnMap）
		BackToMap();
	}
}

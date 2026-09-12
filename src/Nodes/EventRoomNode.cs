using Alchemy.Core.Rooms;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 事件房场景（scenes/room/event.tscn）：展示随机事件（标题 + 2~3 个选项），
/// 点选一个选项 → 逻辑层执行动作并完成该房 → 回地图。
/// </summary>
public partial class EventRoomNode : RoomNodeBase
{
	protected override void Render()
	{
		ClearContent();
		var mgr = Manager;
		if (mgr?.CurrentRoom is not EventRoom room || room.Event == null)
		{
			ShowMissing("当前不在事件房");
			return;
		}

		var ev = room.Event;

		var title = new Label
		{
			Text = L.T(ev.Title),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 34);
		Content.AddChild(title);

		var desc = ContentCatalog.GetEventDescription(ev.Id);
		Content.AddChild(new Label
		{
			Text = string.IsNullOrWhiteSpace(desc)
				? L.T("你遇到了一件奇事，接下来要如何选择？")
				: L.T(desc),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(0.85f, 0.85f, 0.85f),
		});
		Content.AddChild(new HSeparator());

		for (int i = 0; i < ev.Choices.Count; i++)
		{
			var choice = ev.Choices[i];
			var btn = new Button
			{
				Text = L.T(choice.Label),
				CustomMinimumSize = new Vector2(420, 0),
			};
			int index = i;
			btn.Pressed += () => Choose(index);
			Content.AddChild(btn);
		}
	}

	private void Choose(int index)
	{
		var mgr = Manager;
		if (mgr == null)
		{
			return;
		}

		mgr.PerformEventChoice(index); // 应用选项动作并完成该房（Phase → OnMap）
		BackToMap();
	}
}

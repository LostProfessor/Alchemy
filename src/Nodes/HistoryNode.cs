using System;
using System.Linq;
using Alchemy.Core.Runs.Saves;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 历史回顾场景（scenes/history/history.tscn）：列出全部已归档的对局（最新在前），
/// 每条可展开查看完整经历时间线。数据来自 GameState.LoadRunRecords()（user://run_history.json）。
/// 界面文案走 Tr()，接入翻译文件后即可本地化。
/// </summary>
public partial class HistoryNode : Control
{
	private GameState _game = null!;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");

		var content = GetNodeOrNull<VBoxContainer>("Content");
		if (content == null)
		{
			GD.PushError("HistoryNode: 场景缺少 Content(VBoxContainer)");
			return;
		}

		Build(content);
	}

	private void Build(VBoxContainer content)
	{
		var title = new Label
		{
			Text = TranslationServer.Translate("历史回顾"),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 36);
		content.AddChild(title);

		var records = _game.LoadRunRecords();
		content.AddChild(new Label
		{
			Text = string.Format(TranslationServer.Translate("共 {0} 局"), records.Count),
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(0.7f, 0.7f, 0.7f),
		});

		var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		list.AddThemeConstantOverride("separation", 6);
		scroll.AddChild(list);
		content.AddChild(scroll);

		if (records.Count == 0)
		{
			list.AddChild(new Label { Text = TranslationServer.Translate("（暂无历史记录）") });
		}
		else
		{
			for (int i = 0; i < records.Count; i++)
			{
				list.AddChild(BuildRecordRow(records[i], records.Count - i)); // 编号从最早=1 到最新=N
			}
		}

		var actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		var back = new Button { Text = TranslationServer.Translate("返回主菜单"), CustomMinimumSize = new Vector2(220, 0) };
		back.Pressed += () => _game.ChangeScene(GameState.MainMenuScenePath);
		actions.AddChild(back);
		content.AddChild(actions);
	}

	/// <summary>一条记录：摘要行 + 「详情」按钮（点开懒加载时间线）。</summary>
	private static Control BuildRecordRow(RunRecord record, int number)
	{
		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 4);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);

		row.AddChild(new Label
		{
			Text = Summary(record, number),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Modulate = OutcomeColor(record.Outcome),
		});

		var detail = new VBoxContainer { Visible = false };
		detail.AddThemeConstantOverride("separation", 2);

		bool built = false;
		var toggle = new Button { Text = TranslationServer.Translate("详情") };
		toggle.Pressed += () =>
		{
			if (!built)
			{
				BuildTimeline(detail, record);
				built = true;
			}

			detail.Visible = !detail.Visible;
			toggle.Text = TranslationServer.Translate(detail.Visible ? "收起" : "详情");
		};
		row.AddChild(toggle);

		box.AddChild(row);
		box.AddChild(detail);
		return box;
	}

	/// <summary>展开时懒建该局的时间线（避免一次建上千个 Label）。</summary>
	private static void BuildTimeline(VBoxContainer detail, RunRecord record)
	{
		if (record.History.Count == 0)
		{
			detail.AddChild(new Label
			{
				Text = TranslationServer.Translate("（无详细记录）"),
				Modulate = new Color(0.6f, 0.6f, 0.6f),
			});
			return;
		}

		foreach (var entry in record.History.Where(e => !string.IsNullOrWhiteSpace(e.Message)))
		{
			detail.AddChild(new Label
			{
				Text = $"  [{entry.Act}] {entry.Message}",
				Modulate = new Color(0.8f, 0.8f, 0.8f),
			});
		}
	}

	private static string Summary(RunRecord record, int number)
	{
		string job = ContentCatalog.GetJob(record.JobId)?.DisplayName ?? record.JobId;
		return string.Format(
			TranslationServer.Translate("#{0}  {1}  {2}  ·  {3}  ·  第 {4} 层  ·  {5}  ·  货币 {6}"),
			number,
			FormatWhen(record.TimestampUtc),
			job,
			OutcomeText(record.Outcome),
			record.ActIndex,
			FormatDuration(record.TotalSeconds),
			record.Currency);
	}

	private static string OutcomeText(string outcome) => TranslationServer.Translate(outcome switch
	{
		"Completed" => "通关",
		"Defeated" => "倒下",
		_ => "放弃",
	});

	private static Color OutcomeColor(string outcome) => outcome switch
	{
		"Completed" => new Color(1f, 0.8f, 0.3f),
		"Defeated" => new Color(0.9f, 0.45f, 0.45f),
		_ => new Color(0.7f, 0.7f, 0.7f),
	};

	private static string FormatWhen(string iso) =>
		DateTimeOffset.TryParse(iso, out var t) ? t.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : iso;

	private static string FormatDuration(float seconds)
	{
		int total = (int)Math.Max(0f, seconds);
		return $"{total / 60:00}:{total % 60:00}";
	}
}

using System;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 设置场景（scenes/settings/settings.tscn）：从顶栏“设置”按钮进入。
/// 提供：主/音乐/音效音量、帧率上限、语言选择 + 保存当前局 + 返回来源场景。
/// 设置项在代码里生成并挂到场景的 Content(VBoxContainer) 下，无需手动搭 Inspector。
/// 偏好持久化到 user://settings.json（SettingsService），改动即保存并生效。
/// </summary>
public partial class SettingsNode : Control
{
	[Export] private Button _saveBtn = null!;   // 保存当前局
	[Export] private Button _backBtn = null!;   // 返回来源场景（战斗/地图…）
	[Export] private Label _statusLabel = null!; // 操作反馈

	/// <summary>帧率档位：显示名 → 上限（0=不限制）。</summary>
	private static readonly (string Label, int Value)[] FpsOptions =
	{
		("不限制", 0), ("30", 30), ("60", 60), ("120", 120), ("165", 165), ("240", 240),
	};

	/// <summary>语言档位：locale → 显示名（.po 翻译文件以后放 localization/ 下）。</summary>
	private static readonly (string Code, string Name)[] LanguageOptions =
	{
		("zh_CN", "简体中文"), ("en", "English"), ("ja", "日本語"), ("ko", "한국어"),
	};

	private GameState _game = null!;
	private GameSettings _settings = new();
	private ConfirmationDialog? _confirmDialog;

	public override void _Ready()
	{
		_game = GetNode<GameState>("/root/GameState");
		_settings = SettingsService.Load();

		if (_saveBtn != null) _saveBtn.Pressed += SaveGame;
		if (_backBtn != null) _backBtn.Pressed += Back;

		BuildSettingsUi();
	}

	// ── 生成设置项 UI（挂到场景 Content，紧跟标题之后）────────────────

	private void BuildSettingsUi()
	{
		var content = GetNodeOrNull<VBoxContainer>("Content");
		if (content == null)
		{
			GD.PushError("SettingsNode: 场景缺少 Content(VBoxContainer)");
			return;
		}

		// 旧占位提示已不需要
		var hint = content.GetNodeOrNull<Label>("FutureHint");
		if (hint != null)
		{
			hint.Visible = false;
		}

		var section = new VBoxContainer();
		section.AddThemeConstantOverride("separation", 10);

		section.AddChild(SectionTitle("音量（音频总线已就绪，资源到位即生效）"));
		section.AddChild(VolumeRow("主音量", _settings.MasterVolume, v => { _settings.MasterVolume = v; Persist(); }));
		section.AddChild(VolumeRow("音乐", _settings.MusicVolume, v => { _settings.MusicVolume = v; Persist(); }));
		section.AddChild(VolumeRow("音效", _settings.SfxVolume, v => { _settings.SfxVolume = v; Persist(); }));

		section.AddChild(new HSeparator());
		section.AddChild(FpsRow());
		section.AddChild(LanguageRow());
		section.AddChild(new Label
		{
			Text = "（翻译 .po 文件放 localization/ 后即生效；目前界面仍是中文源串）",
			Modulate = new Color(0.6f, 0.6f, 0.6f),
		});

		// 保存并退出到主菜单（带复核确认）
		section.AddChild(new HSeparator());
		var actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		actions.AddThemeConstantOverride("separation", 12);
		var saveQuit = new Button
		{
			Text = "保存并退出到主菜单",
			CustomMinimumSize = new Vector2(220, 0),
		};
		saveQuit.Pressed += ConfirmSaveAndQuit;
		actions.AddChild(saveQuit);
		section.AddChild(actions);

		content.AddChild(section);
		content.MoveChild(section, 1); // 紧跟标题
	}

	/// <summary>保存并退出到主菜单：先弹窗复核确认，确认后才保存+回主菜单（存档保留，可“继续”）。</summary>
	private void ConfirmSaveAndQuit()
	{
		var mgr = _game.Manager;
		if (mgr == null)
		{
			_game.ChangeScene(GameState.MainMenuScenePath); // 没有进行中的局 → 不必保存/确认
			return;
		}

		_confirmDialog ??= BuildConfirmDialog();
		_confirmDialog.DialogText =
			$"保存当前进度并退出到主菜单？\n（第 {mgr.ActIndex} 大层 · 生命 {mgr.Player.CurrentHp}/{mgr.Player.MaxHp}）\n之后可从主菜单「继续」这一局。";
		_confirmDialog.PopupCentered();
	}

	/// <summary>复核确认弹窗（首次使用时创建并常驻本场景）。</summary>
	private ConfirmationDialog BuildConfirmDialog()
	{
		var dialog = new ConfirmationDialog
		{
			Title = "确认",
			OkButtonText = "保存并退出",
			CancelButtonText = "取消",
		};
		dialog.Confirmed += () =>
		{
			_game.SaveAndQuitToMenu();
			_game.ChangeScene(GameState.MainMenuScenePath);
		};
		AddChild(dialog);
		return dialog;
	}

	private static Label SectionTitle(string text)
	{
		var label = new Label { Text = text };
		label.AddThemeFontSizeOverride("font_size", 20);
		return label;
	}

	/// <summary>一行音量滑杆：名字 + 滑杆 + 当前百分比。</summary>
	private HBoxContainer VolumeRow(string title, int value, Action<int> onChanged)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);

		row.AddChild(new Label { Text = title, CustomMinimumSize = new Vector2(90, 0) });

		var slider = new HSlider
		{
			MinValue = 0,
			MaxValue = 100,
			Step = 1,
			Value = value,
			CustomMinimumSize = new Vector2(0, 30),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		row.AddChild(slider);

		var valueLabel = new Label
		{
			Text = $"{value}%",
			CustomMinimumSize = new Vector2(48, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		row.AddChild(valueLabel);

		slider.ValueChanged += v =>
		{
			int percent = Mathf.RoundToInt((float)v);
			valueLabel.Text = $"{percent}%";
			onChanged(percent);
		};

		return row;
	}

	private Control FpsRow()
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		row.AddChild(new Label { Text = "帧率上限", CustomMinimumSize = new Vector2(90, 0) });

		var option = new OptionButton
		{
			CustomMinimumSize = new Vector2(220, 0),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		int selected = 0;
		for (int i = 0; i < FpsOptions.Length; i++)
		{
			option.AddItem(FpsOptions[i].Label);
			if (FpsOptions[i].Value == _settings.MaxFps)
			{
				selected = i;
			}
		}

		option.Select(selected);
		option.ItemSelected += index =>
		{
			int i = (int)index;
			if (i >= 0 && i < FpsOptions.Length)
			{
				_settings.MaxFps = FpsOptions[i].Value;
				Persist();
			}
		};

		row.AddChild(option);
		return row;
	}

	private Control LanguageRow()
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		row.AddChild(new Label { Text = "语言", CustomMinimumSize = new Vector2(90, 0) });

		var option = new OptionButton
		{
			CustomMinimumSize = new Vector2(220, 0),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		int selected = 0;
		for (int i = 0; i < LanguageOptions.Length; i++)
		{
			option.AddItem(LanguageOptions[i].Name);
			if (LanguageOptions[i].Code == _settings.Language)
			{
				selected = i;
			}
		}

		option.Select(selected);
		option.ItemSelected += index =>
		{
			int i = (int)index;
			if (i >= 0 && i < LanguageOptions.Length)
			{
				_settings.Language = LanguageOptions[i].Code;
				Persist();
			}
		};

		row.AddChild(option);
		return row;
	}

	/// <summary>应用 + 落盘设置。</summary>
	private void Persist()
	{
		SettingsService.Apply(_settings);
		SettingsService.Save(_settings);
		if (_statusLabel != null)
		{
			_statusLabel.Text = "设置已保存 ✓";
		}
	}

	/// <summary>保存当前局（无局时提示）。</summary>
	private void SaveGame()
	{
		var mgr = _game.Manager;
		if (mgr == null)
		{
			if (_statusLabel != null) _statusLabel.Text = "当前没有进行中的局";
			return;
		}

		_game.Save();
		if (_statusLabel != null)
		{
			_statusLabel.Text = $"已保存 ✓（第 {mgr.ActIndex} 层；房内保存=读档重打本房）";
		}
	}

	private void Back()
	{
		_game.ChangeScene(_game.ReturnScenePath);
	}
}

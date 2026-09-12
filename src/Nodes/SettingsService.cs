using System.IO;
using System.Text.Json;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 用户设置服务：读/写 user://settings.json 并应用到引擎
/// （音频总线音量 / 帧率上限 / 语言）。与整局存档（user://save.json）完全分开。
/// </summary>
public static class SettingsService
{
	public const string SettingsPath = "user://settings.json";

	/// <summary>音乐总线名（运行时确保存在；以后把音乐流挂到这条总线）。</summary>
	public const string MusicBus = "Music";

	/// <summary>音效总线名（运行时确保存在）。</summary>
	public const string SfxBus = "Sfx";

	private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

	private static string PathAbsolute => ProjectSettings.GlobalizePath(SettingsPath);

	/// <summary>读设置（文件缺失/损坏回默认）。</summary>
	public static GameSettings Load()
	{
		if (File.Exists(PathAbsolute))
		{
			try
			{
				return JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(PathAbsolute), Options) ?? new GameSettings();
			}
			catch
			{
				// 损坏 → 回默认
			}
		}

		return new GameSettings();
	}

	public static void Save(GameSettings settings)
	{
		File.WriteAllText(PathAbsolute, JsonSerializer.Serialize(settings, Options));
	}

	/// <summary>读取并应用（启动时调用一次）。</summary>
	public static GameSettings LoadAndApply()
	{
		var settings = Load();
		Apply(settings);
		return settings;
	}

	/// <summary>把当前设置应用到引擎：确保音频总线 + 套音量/帧率/语言。</summary>
	public static void Apply(GameSettings settings)
	{
		EnsureBuses();
		SetBusVolume("Master", settings.MasterVolume);
		SetBusVolume(MusicBus, settings.MusicVolume);
		SetBusVolume(SfxBus, settings.SfxVolume);

		Engine.MaxFps = Mathf.Max(0, settings.MaxFps);

		// 自定义帧率上限想跑上去需关垂直同步；"不限制"(0) 用默认 vsync（随显示器刷新）。
		if (!OS.HasFeature("headless"))
		{
			DisplayServer.WindowSetVsyncMode(
				settings.MaxFps > 0
					? DisplayServer.VSyncMode.Disabled
					: DisplayServer.VSyncMode.Enabled);
		}

		TranslationServer.SetLocale(string.IsNullOrWhiteSpace(settings.Language) ? "zh_CN" : settings.Language);
	}

	/// <summary>确保 Master 之外还有 Music / Sfx 总线（项目默认只有 Master）。</summary>
	public static void EnsureBuses()
	{
		if (AudioServer.GetBusIndex(MusicBus) == -1)
		{
			AudioServer.AddBus();
			AudioServer.SetBusName(AudioServer.GetBusCount() - 1, MusicBus);
		}

		if (AudioServer.GetBusIndex(SfxBus) == -1)
		{
			AudioServer.AddBus();
			AudioServer.SetBusName(AudioServer.GetBusCount() - 1, SfxBus);
		}
	}

	/// <summary>把 0~100 音量设到指定总线（0=静音，否则线性换算 dB）。</summary>
	public static void SetBusVolume(string bus, int percent)
	{
		int index = AudioServer.GetBusIndex(bus);
		if (index == -1)
		{
			return;
		}

		float p = Mathf.Clamp(percent, 0, 100);
		AudioServer.SetBusMute(index, p <= 0f);
		AudioServer.SetBusVolumeDb(index, p <= 0f ? -80f : Mathf.LinearToDb(p / 100f));
	}
}

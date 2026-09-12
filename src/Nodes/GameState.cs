using System;
using System.Collections.Generic;
using System.IO;
using Alchemy.Core.Runs;
using Alchemy.Core.Runs.Saves;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// Autoload 单例（Project Settings → Autoload 注册，名字必须是 GameState）。
/// 全剧唯一持有当前整局 <see cref="RunManager"/> 的地方；
/// 场景切换时它不销毁，任何场景都通过 /root/GameState 拿实例。
/// </summary>
public partial class GameState : Node
{
	/// <summary>启动时：从 .tres 内容资源构建逻辑层目录（没有则保持默认硬编码）。</summary>
	public override void _Ready()
	{
		var catalog = ContentCatalog.BuildIngredientCatalog();
		if (catalog != null)
		{
			Alchemy.Core.GameData.Ingredients.SetCatalog(catalog);
		}

		var enemies = ContentCatalog.BuildMonsterTemplates();
		if (enemies != null)
		{
			Alchemy.Core.Encounters.MonsterTemplates.SetCatalog(enemies);
		}

		ContentCatalog.BuildJobs(); // 职业资源（表现层缓存）
		ContentCatalog.BuildRelics(); // 遗物表现资源（名称/描述/图标）
		ContentCatalog.BuildEncounters(); // 每大层遭遇表（注入逻辑层 EncounterFactory）
		ContentCatalog.BuildEvents(); // 事件资源（注入逻辑层 EventCatalog）
		ContentCatalog.BuildBrewingSettings(); // 炼药耗时等平衡数值（注入逻辑层 BrewingTimings）
		SettingsService.LoadAndApply(); // 用户偏好：音量总线/帧率上限/语言
	}

	/// <summary>存档路径（Godot user:// 目录；System.IO 需先 GlobalizePath）。</summary>
	public const string SavePath = "user://save.json";

	/// <summary>整局历史归档路径（结束一局后留存，供回顾）。</summary>
	public const string ArchivePath = "user://run_history.json";

	/// <summary>设置界面“返回”要回去的场景（打开设置前由入口记录；默认地图）。</summary>
	public string ReturnScenePath { get; set; } = "res://scenes/map/map.tscn";

	/// <summary>通关结算场景（整局胜利后进入）。</summary>
	public const string VictoryScenePath = "res://scenes/result/victory.tscn";

	/// <summary>失败结算场景（玩家倒下后进入）。</summary>
	public const string DefeatScenePath = "res://scenes/result/defeat.tscn";

	/// <summary>主菜单场景。</summary>
	public const string MainMenuScenePath = "res://scenes/main_menu/main_menu.tscn";

	/// <summary>设置场景（顶栏与主菜单都可进入）。</summary>
	public const string SettingsScenePath = "res://scenes/settings/settings.tscn";

	/// <summary>历史回顾场景。</summary>
	public const string HistoryScenePath = "res://scenes/history/history.tscn";

	/// <summary>当前进行中的整局；null = 当前没有进行中的局。</summary>
	public RunManager? Manager { get; private set; }

	public bool HasActiveRun => Manager != null;

	/// <summary>System.IO 可直接使用的存档绝对路径。</summary>
	public static string SavePathAbsolute => ProjectSettings.GlobalizePath(SavePath);

	/// <summary>System.IO 可直接使用的归档绝对路径。</summary>
	public static string ArchivePathAbsolute => ProjectSettings.GlobalizePath(ArchivePath);

	public bool HasSave => File.Exists(SavePathAbsolute);

	/// <summary>主菜单解析好的种子暂存于此；职业选择场景用它开新局。</summary>
	public int PendingSeed { get; set; }

	/// <summary>整局已游玩时间（秒，实时累计：有新局且未暂停时累加；新局/结束局归零）。</summary>
	public float RunTotalSeconds { get; private set; }

	public override void _Process(double delta)
	{
		// 只在“有进行中的局且没暂停”时累计，主菜单/暂停不增长
		if (Manager == null || GetTree().Paused)
		{
			return;
		}

		RunTotalSeconds += (float)delta;
	}

	public void StartNewRun(int seed, string jobId = "researcher")
	{
		// 开新局：删掉可能残留的旧存档（避免主菜单“继续”读回上一局）
		if (File.Exists(SavePathAbsolute))
		{
			File.Delete(SavePathAbsolute);
		}

		Manager = new RunManager(seed, jobId: jobId);
		Manager.StartRun();
		RunTotalSeconds = 0;
	}

	/// <summary>读档；成功返回 true，Manager 变为读档后的整局（Phase=OnMap）。</summary>
	public bool TryLoad()
	{
		var data = SaveService.LoadFromFile(SavePathAbsolute);
		if (data == null)
		{
			return false;
		}

		Manager = RunManager.LoadFromSaveData(data);
		return true;
	}

	/// <summary>保存当前整局（检查点 = 最近完成房间的节点）。</summary>
	public void Save()
	{
		if (Manager != null)
		{
			SaveService.SaveToFile(Manager.CreateSaveData(), SavePathAbsolute);
		}
	}

	/// <summary>整局已结束（通关/倒下）→ 切到对应结算场景并返回 true；未结束返回 false（不切换）。</summary>
	public bool GoToResultIfRunOver()
	{
		if (Manager == null)
		{
			return false;
		}

		if (Manager.Phase == RunPhase.Completed)
		{
			ChangeScene(VictoryScenePath);
			return true;
		}

		if (Manager.Phase == RunPhase.Defeated)
		{
			ChangeScene(DefeatScenePath);
			return true;
		}

		return false;
	}


	/// <summary>
	/// 保存当前局并退出到主菜单：保留存档（可从主菜单“继续”恢复），**不归档、不结束本局**。
	/// 与 <see cref="EndRun"/>（结束/放弃 → 归档 + 删档）区分。
	/// </summary>
	public void SaveAndQuitToMenu()
	{
		Save(); // 无局时 Save 内部判空
		Manager = null;
		RunTotalSeconds = 0;
	}

	/// <summary>结束/放弃当前局（回主菜单时调用）：先把本局归档到历史，再删除进行中的存档。</summary>
	public void EndRun()
	{
		ArchiveCurrentRun();
		Manager = null;
		RunTotalSeconds = 0;
	}

	/// <summary>读取历史归档（供“回顾”界面展示）。</summary>
	public List<RunRecord> LoadRunRecords() => RunArchive.LoadAll(ArchivePathAbsolute);

	/// <summary>把当前局归档并删除进行中的存档（Manager 为空则什么都不做）。</summary>
	private void ArchiveCurrentRun()
	{
		var mgr = Manager;
		if (mgr == null)
		{
			return;
		}

		try
		{
			string outcome = mgr.Phase switch
			{
				RunPhase.Completed => "Completed",
				RunPhase.Defeated => "Defeated",
				_ => "Abandoned", // 中途放弃 / 调试回主菜单
			};

			var record = RunRecord.FromRun(mgr, outcome, RunTotalSeconds, DateTimeOffset.UtcNow.ToString("o"));
			RunArchive.Append(ArchivePathAbsolute, record);

			if (File.Exists(SavePathAbsolute))
			{
				File.Delete(SavePathAbsolute); // 整局已结束 → 不再“继续”
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning($"归档/删除存档失败：{ex.Message}");
		}
	}

	public void ChangeScene(string scenePath)
	{
		GetTree().Paused = false; // 防呆：切场景前恢复暂停，避免新场景被冻结
		GetTree().ChangeSceneToFile(scenePath);
	}
}

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

	/// <summary>设置界面“返回”要回去的场景（打开设置前由入口记录；默认地图）。</summary>
	public string ReturnScenePath { get; set; } = "res://scenes/map/map.tscn";

	/// <summary>通关结算场景（整局胜利后进入）。</summary>
	public const string VictoryScenePath = "res://scenes/result/victory.tscn";

	/// <summary>失败结算场景（玩家倒下后进入）。</summary>
	public const string DefeatScenePath = "res://scenes/result/defeat.tscn";

	/// <summary>当前进行中的整局；null = 当前没有进行中的局。</summary>
	public RunManager? Manager { get; private set; }

	public bool HasActiveRun => Manager != null;

	/// <summary>System.IO 可直接使用的存档绝对路径。</summary>
	public static string SavePathAbsolute => ProjectSettings.GlobalizePath(SavePath);

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


	/// <summary>结束/放弃当前局（回主菜单时调用）。</summary>
	public void EndRun()
	{
		Manager = null;
		RunTotalSeconds = 0;
	}

	public void ChangeScene(string scenePath)
	{
		GetTree().Paused = false; // 防呆：切场景前恢复暂停，避免新场景被冻结
		GetTree().ChangeSceneToFile(scenePath);
	}
}

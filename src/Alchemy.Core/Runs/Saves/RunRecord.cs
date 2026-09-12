using System.Collections.Generic;
using System.Linq;
using Alchemy.Core.Runs.History;

namespace Alchemy.Core.Runs.Saves;

/// <summary>
/// 一局结束后的历史归档记录（user://run_history.json 里的元素）：结局 + 汇总 + 完整经历时间线。
/// 与"进行中的存档"分开存——存档删了，这局的过程仍可回顾。
/// </summary>
public sealed class RunRecord
{
	public int Version { get; set; } = 1;

	/// <summary>结束时间（ISO-8601，调用方传入；Core 不依赖时钟）。</summary>
	public string TimestampUtc { get; set; } = string.Empty;

	/// <summary>职业 id（researcher/elf/et…）。</summary>
	public string JobId { get; set; } = string.Empty;

	/// <summary>结局：Completed（通关）/ Defeated（倒下）/ Abandoned（放弃）。</summary>
	public string Outcome { get; set; } = string.Empty;

	/// <summary>结束时所在大层。</summary>
	public int ActIndex { get; set; }

	public int MaxHp { get; set; }

	public int CurrentHp { get; set; }

	public int Currency { get; set; }

	/// <summary>结束时的药材口袋快照。</summary>
	public Dictionary<string, int> Pocket { get; set; } = new();

	/// <summary>结束时的遗物 Id 列表。</summary>
	public List<string> RelicIds { get; set; } = new();

	/// <summary>整局游玩时长（秒）。</summary>
	public float TotalSeconds { get; set; }

	/// <summary>完整经历时间线（进房/生命/药材/奖励）。</summary>
	public List<HistoryEntry> History { get; set; } = new();

	/// <summary>从整局状态生成一条归档记录。</summary>
	public static RunRecord FromRun(RunManager run, string outcome, float totalSeconds, string timestampUtc)
	{
		return new RunRecord
		{
			TimestampUtc = timestampUtc,
			JobId = run.JobId,
			Outcome = outcome,
			ActIndex = run.ActIndex,
			MaxHp = run.Player.MaxHp,
			CurrentHp = run.Player.CurrentHp,
			Currency = run.Run.Currency,
			Pocket = new Dictionary<string, int>(run.Run.Pocket.Counts),
			RelicIds = run.Run.Relics.Relics.Select(r => r.Id).ToList(),
			TotalSeconds = totalSeconds,
			History = run.History.Entries.ToList(),
		};
	}
}

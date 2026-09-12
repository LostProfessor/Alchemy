using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Alchemy.Core.Runs.Saves;

/// <summary>
/// 整局历史归档（user://run_history.json）：一局结束后留存记录供回顾。
/// 最新的一局在最前；最多保留 <see cref="MaxRecords"/> 局，超出丢最旧的。
/// 与进行中的存档（save.json）分开：存档删了，历史仍在。
/// </summary>
public static class RunArchive
{
	/// <summary>最多保留的历史局数。</summary>
	public const int MaxRecords = 50;

	private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

	/// <summary>读取全部归档（文件缺失/损坏 → 空列表）。</summary>
	public static List<RunRecord> LoadAll(string path)
	{
		if (!File.Exists(path))
		{
			return new List<RunRecord>();
		}

		try
		{
			return JsonSerializer.Deserialize<List<RunRecord>>(File.ReadAllText(path), Options) ?? new List<RunRecord>();
		}
		catch
		{
			return new List<RunRecord>(); // 损坏 → 当作空，不让读档崩
		}
	}

	public static void SaveAll(string path, List<RunRecord> records) =>
		File.WriteAllText(path, JsonSerializer.Serialize(records, Options));

	/// <summary>追加一条记录（放最前 = 最新在前），并裁剪到上限。</summary>
	public static void Append(string path, RunRecord record)
	{
		var all = LoadAll(path);
		all.Insert(0, record);
		if (all.Count > MaxRecords)
		{
			all.RemoveRange(MaxRecords, all.Count - MaxRecords);
		}

		SaveAll(path, all);
	}
}

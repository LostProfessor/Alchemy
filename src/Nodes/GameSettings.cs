namespace Alchemy.Nodes;

/// <summary>
/// 用户设置（user://settings.json）：与整局存档分开的本地偏好。
/// 字段都是"默认值兜底"——文件缺失/损坏时回退这些默认。
/// </summary>
public sealed class GameSettings
{
	/// <summary>主音量（0~100）。</summary>
	public int MasterVolume { get; set; } = 80;

	/// <summary>音乐音量（0~100）。</summary>
	public int MusicVolume { get; set; } = 80;

	/// <summary>音效音量（0~100）。</summary>
	public int SfxVolume { get; set; } = 80;

	/// <summary>目标帧率上限（0=不限制；30/60/120/165/240…）。</summary>
	public int MaxFps { get; set; } = 0;

	/// <summary>语言代码（locale：zh_CN/en/ja/ko…，对应 localization/ 下的 .po）。</summary>
	public string Language { get; set; } = "zh_CN";
}

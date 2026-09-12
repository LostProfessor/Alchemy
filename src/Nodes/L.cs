using System.Collections.Generic;
using Alchemy.Core.Rooms;
using Godot;

namespace Alchemy.Nodes;

/// <summary>
/// 本地化辅助（表现层专用）。
///
/// 约定：**中文源串就是 msgid**（见 localization/translations.csv，中文无需单列，
/// project.godot 里 locale/fallback="zh_CN"，未翻译即回落源串）。
///
/// 用法：
/// - 直接赋给 Label/Button 等控件的 text 时，Godot 会自动翻译（atr），无需包装。
/// - **拼接/格式化**出来的字符串必须显式 <c>L.T</c> / <c>L.F</c>，否则整串不是 msgid 里的键。
/// - 数据文案（遗物/药材/效果名等）在拼进一句话时同样用 <c>L.T(名字)</c>。
/// </summary>
public static class L
{
	/// <summary>翻译一条 msgid（找不到则原样返回中文源串）。</summary>
	public static string T(string msgid) => TranslationServer.Translate(msgid);

	/// <summary>翻译带占位符的模板再套参数，例如 L.F("第 {0} 层", act)。</summary>
	public static string F(string msgid, params object[] args) =>
		string.Format(TranslationServer.Translate(msgid), args);

	/// <summary>列表分隔符（中文、／英文 ", "），用于拼接口袋/奖励摘要。</summary>
	public static string Sep => TranslationServer.Translate("、");

	/// <summary>把若干（已翻译的）片段用本地化分隔符连起来。</summary>
	public static string Join(IEnumerable<string> parts) => string.Join(Sep, parts);

	/// <summary>房间类型 → 本地化名称（顶栏/房间标题用；直接 ToString() 会得到英文枚举名）。</summary>
	public static string RoomTypeName(RoomType type) => T(type switch
	{
		RoomType.Combat => "战斗",
		RoomType.Event => "事件",
		RoomType.Treasure => "宝藏",
		RoomType.Shop => "商店",
		_ => "房间",
	});
}

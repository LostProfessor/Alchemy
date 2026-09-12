namespace Alchemy.Core.Brewing;

/// <summary>
/// 词条操作类型。每种药材 = 一串按顺序结算的词条操作（先放入的先结算）。
/// </summary>
public enum AffixOpType
{
	/// <summary>添加效果（含层数）。</summary>
	AddEffect,

	/// <summary>移除效果（含层数）。</summary>
	RemoveEffect,

	/// <summary>清除最后 N 个效果（队列=队尾最近加入；栈=栈顶）。</summary>
	RemoveLast,

	/// <summary>翻转整个效果顺序。</summary>
	ReverseOrder,

	/// <summary>反转所有效果极性（增益↔减益互换，不会抵消）。</summary>
	ReversePolarity,
}

namespace Alchemy.Core.Brewing;

/// <summary>
/// 炼药模式 = 基底类型 = 三种"职业"，拥有完全不同的配方思路。
/// </summary>
public enum BrewingMode
{
	/// <summary>队列（FIFO）：5 格视为队列，超出按进入顺序把最旧的挤出。</summary>
	Queue,

	/// <summary>栈（LIFO）：5 格视为栈，栈满后无法添加，除非先移除腾出空间。</summary>
	Stack,

	/// <summary>反转队列：正常队列，但所有材料作用效果相反（添加↔移除）。</summary>
	ReversedQueue,
}

namespace Alchemy.Core.Random;

/// <summary>
/// 随机源（可注入种子，便于测试确定性与重放）。战斗与整局奖励共用。
/// </summary>
public interface IRandomSource
{
	double NextDouble();

	/// <summary>返回 [0, maxExclusive) 的整数。</summary>
	int Next(int maxExclusive);

	/// <summary>以 probability 概率返回 true（0~1）。</summary>
	bool Chance(double probability);
}

using System;
using Alchemy.Core.Random;

namespace Alchemy.Core.Combat;

/// <summary>
/// 战斗随机数（可注入种子，便于测试确定性）。致幻等概率效果走这里。
/// </summary>
public sealed class BattleRandom : IRandomSource
{
	private readonly System.Random _rng;
	private readonly int _seed;

	/// <summary>本实例的实际种子（无参时随机生成）。</summary>
	public int Seed => _seed;

	public BattleRandom(int? seed = null)
	{
		_seed = seed ?? new System.Random().Next(int.MinValue, int.MaxValue);
		_rng = new System.Random(_seed);
	}
	/// <summary>返回 [0, 1) 的双精度浮点数。</summary>
	public double NextDouble() => _rng.NextDouble();

	/// <summary>返回 [0, maxExclusive) 的整数。</summary>
	public int Next(int maxExclusive) => _rng.Next(maxExclusive);

	/// <summary>以 probability 概率返回 true（0~1）。</summary>
	public bool Chance(double probability) => _rng.NextDouble() < probability;
}

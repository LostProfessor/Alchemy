using System;
using System.Collections.Generic;

namespace Alchemy.Core.Random;

/// <summary>随机扩展：用 IRandomSource 做确定性的洗牌等。</summary>
public static class RandomExtensions
{
	/// <summary>Fisher-Yates 洗牌（原地，使用传入随机源，保证可重放）。</summary>
	public static void Shuffle<T>(this IList<T> list, IRandomSource rng)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}

	/// <summary>随机取一个元素。</summary>
	public static T Pick<T>(this IReadOnlyList<T> list, IRandomSource rng)
	{
		if (list.Count == 0)
		{
			throw new InvalidOperationException("不能从空列表随机取元素");
		}

		return list[rng.Next(list.Count)];
	}
}

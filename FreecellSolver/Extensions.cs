using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreecellSolver
{
	public static class Extensions
	{
		public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
		{
			TValue result;

			if (dict.TryGetValue(key, out result) == true)
				return result;
			else
				return default(TValue);
		}

		public static List<T> Swap<T>(this List<T> list, int index1, int index2)
		{
			T temp = list[index1];
			list[index1] = list[index2];
			list[index2] = temp;

			return list;
		}

		public static double Variance(this IEnumerable<int> values)
		{
			double average = values.Average();
			double variance = values.Sum(i => (i - average) * (i - average));

			return variance;
		}
	}
}

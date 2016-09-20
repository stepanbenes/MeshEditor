using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Common.Extensions
{
	public static class ListExtensions
	{
		public static void SwapSegments<T>(this IList<T> source, int firstIndex, int secondIndex, int length)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			Debug.Assert(firstIndex + length <= secondIndex);
			Debug.Assert(secondIndex + length <= source.Count);

			for (int i = 0; i < length; i++)
			{
				T temp = source[firstIndex + i];
				source[firstIndex + i] = source[secondIndex + i];
				source[secondIndex + i] = temp;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Common
{
	public static class ArrayExtensions
	{
		/// <summary>
		/// Return new array that is sub-array of array.
		/// </summary>
		/// <param name="array">Original array</param>
		/// <param name="index">Start index of segment</param>
		/// <param name="length">Length of segment to copy</param>
		/// <returns>Slice of the array</returns>
		public static T[] CreateSlice<T>(this T[] array, int index, int length)
		{
			T[] result = new T[length];
			Array.Copy(array, index, result, 0, length);
			return result;
		}
	}
}

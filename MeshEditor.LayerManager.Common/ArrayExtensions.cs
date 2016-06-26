using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		/// <summary>
		/// Fastest way to fill an array with a single value
		/// http://stackoverflow.com/questions/5943850/fastest-way-to-fill-an-array-with-a-single-value
		/// 
		/// The fastest method I have found uses Array.Copy with the copy size doubling each time through the loop.
		/// The speed is basically the same whether you fill the array with a single value or an array of values.
		/// In my test with 20,000,000 array items, this function is twice as fast as a for loop.
		/// </summary>
		public static void Fill<T>(this T[] destinationArray, params T[] value)
		{
			if (destinationArray == null)
			{
				throw new ArgumentNullException(nameof(destinationArray));
			}

			if (value.Length >= destinationArray.Length)
			{
				throw new ArgumentException("Length of value array must be less than length of destination");
			}

			// set the initial array value
			Array.Copy(value, destinationArray, value.Length);

			int arrayToFillHalfLength = destinationArray.Length / 2;
			int copyLength;

			for (copyLength = value.Length; copyLength < arrayToFillHalfLength; copyLength <<= 1)
			{
				Array.Copy(destinationArray, 0, destinationArray, copyLength, copyLength);
			}

			Array.Copy(destinationArray, 0, destinationArray, copyLength, destinationArray.Length - copyLength);
		}

		public static void FillRange<T>(this T[] array, T value, int index, int count)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (index + count >= array.Length)
			{
				throw new ArgumentException("Length of array must be less than index + count");
			}

			for (int i = 0; i < count; i++)
			{
				array[index + i] = value;
			}
		}

		/// <summary>
		/// Get min value in array, ignore value passed as argument
		/// </summary>
		/// <param name="ignore">Value to ignore in comparisons</param>
		/// <returns>Min value in array or null if empty or full of ignore values</returns>
		public static T? Min<T>(this T[] array, T ignore) where T : struct, IComparable<T>
		{
			bool minSet = false;
			T min = default(T);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].CompareTo(ignore) != 0)
				{
					if (!minSet)
					{
						min = array[i];
						minSet = true;
					}
					else if (array[i].CompareTo(min) < 0)
					{
						min = array[i];
					}
				}
			}
			return (minSet) ? min : (T?)null;
		}

		/// <summary>
		/// Get max value in array, ignore value passed as argument
		/// </summary>
		/// <param name="ignore">Value to ignore in comparisons</param>
		/// <returns>Max value in array or null if empty or full of ignore values</returns>
		public static T? Max<T>(this T[] array, T ignore) where T : struct, IComparable<T>
		{
			bool maxSet = false;
			T max = default(T);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].CompareTo(ignore) != 0)
				{
					if (!maxSet)
					{
						max = array[i];
						maxSet = true;
					}
					else if (array[i].CompareTo(max) > 0)
					{
						max = array[i];
					}
				}
			}
			return (maxSet) ? max : (T?)null;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Common
{
	/// <summary>
	/// Designed to be used with using static
	/// </summary>
	public static class HelperFunctions
	{
		/// <summary>
		/// Swaps values of two arguments.
		/// </summary>
		/// <param name="a">first argument</param>
		/// <param name="b">second argument</param>
		public static void Swap<T>(ref T a, ref T b)
		{
			T temp = a;
			a = b;
			b = temp;
		}
	}
}

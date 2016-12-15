using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Common.Extensions
{
	public static class DoubleExtensions
	{
		public static double Square(this double value) => value * value;
		public static double SquareRoot(this double value) => Math.Sqrt(value);
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Mathematics
{
	public static class FloatComparisons
	{
		public static readonly double Epsilon = 1e-20;
		public static readonly float EpsilonF = 1e-20f;

		public static bool IsAlmostZero(this float x)
		{
			if (x == 0f)
				return true;
			return Math.Abs(x) < EpsilonF;
		}

		public static bool IsAlmostZero(this double x)
		{
			if (x == 0.0)
				return true;
			return Math.Abs(x) < Epsilon;
		}

		public static bool AlmostEquals(this float a, float b)
		{
			return CompareAlmostEqualF(a, b, EpsilonF);
		}

		public static bool AlmostEquals(this double a, float b)
		{
			return CompareAlmostEqualF((float)a, b, EpsilonF);
		}

		public static bool AlmostEquals(this float a, double b)
		{
			return CompareAlmostEqualF(a, (float)b, EpsilonF);
		}

		public static bool AlmostEquals(this double a, double b)
		{
			return CompareAlmostEqualD(a, b, Epsilon);
		}

		public static bool CompareAlmostEqualF(float x, float y, float epsilon)
		{
			// Based upon implementation:
			// http://floating-point-gui.de/errors/comparison/

			if (x == y)
				return true;

			float diff = Math.Abs(x - y);

			if (x * y == 0)
			{
				return diff < (epsilon * epsilon);
			}
			else
			{
				return diff / (Math.Abs(x) + Math.Abs(y)) < epsilon;
			}
		}

		public static bool CompareAlmostEqualD(double x, double y, double epsilon)
		{
			// Based upon implementation:
			// http://floating-point-gui.de/errors/comparison/

			if (x == y)
				return true;

			double diff = Math.Abs(x - y);

			if (x * y == 0)
			{
				return diff < (epsilon * epsilon);
			}
			else
			{
				return diff / (Math.Abs(x) + Math.Abs(y)) < epsilon;
			}
		}
	}
}

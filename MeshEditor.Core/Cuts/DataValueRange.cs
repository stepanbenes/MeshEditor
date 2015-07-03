using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.Cuts
{
	public class DataValueRange
	{
		public static readonly DataValueRange Zero = new DataValueRange(0.0, 0.0, inversed: false);
		public static readonly DataValueRange Unlimited = new DataValueRange(null, null, inversed: false);

		double? minimum, maximum;
		bool inversed;

		public double? Minimum { get { return minimum; } }
		public double? Maximum { get { return maximum; } }

		public bool Inversed { get { return inversed; } }

		public DataValueRange(double? min, double? max, bool inversed)
		{
			this.minimum = min;
			this.maximum = max;
			this.inversed = inversed;
		}

		public bool Contains(double value)
		{
			if (double.IsNaN(value))
				return true; // if value is not defined, I can not decide, so I am conservative

			if (inversed)
			{
				bool isBelowMin = value <= (minimum ?? double.MinValue);
				bool isAboveMax = value >= (maximum ?? double.MaxValue);
				return isBelowMin || isAboveMax;
			}
			else
			{
				bool isAboveMin = value >= (minimum ?? double.MinValue);
				bool isBelowMax = value <= (maximum ?? double.MaxValue);
				return isAboveMin && isBelowMax;
			}
		}
	}
}

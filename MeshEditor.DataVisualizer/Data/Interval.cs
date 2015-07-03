using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public interface IInterval<T>
	{
		T Min { get; }
		T Max { get; }
		T Length { get; }
		void MergeWith(T value);
		void MergeWith(IEnumerable<T> values);
		T CutValue(T value);
		T GetMaxAbsValue();
	}

	public struct Interval : IInterval<float>
	{
		public static Interval Zero = new Interval();
		public static Interval Indefinite = new Interval(float.NegativeInfinity, float.PositiveInfinity);
		public static Interval InvertedMaxMin = new Interval(float.MaxValue, float.MinValue);

		float min, max;

		public float Min
		{
			get { return min; }
			private set { max = value; }
		}

		public float Max
		{
			get { return max; }
			private set { max = value; }
		}

		public float Length
		{
			get { return max - min; }
		}

		public Interval(float min, float max)
		{
			this.min = min;
			this.max = max;
		}

		public void MergeWith(float value)
		{
			min = Math.Min(min, value);
			max = Math.Max(max, value);
		}

		public void MergeWith(IEnumerable<float> values)
		{
			foreach (float value in values)
				MergeWith(value);
		}

		public float CutValue(float value)
		{
			if (value <= min)
				return min;
			if (value >= max)
				return max;
			return value;
		}

		public float GetMaxAbsValue()
		{
			return Math.Max(Math.Abs(min), Math.Abs(max));
		}

		public override string ToString()
		{
			return string.Format("<{0}, {1}>", min, max);
		}
	}

	public struct IntervalD : IInterval<double>
	{
		public static IntervalD Zero = new IntervalD();
		public static IntervalD Indefinite = new IntervalD(double.NegativeInfinity, double.PositiveInfinity);
		public static IntervalD InvertedMaxMin = new IntervalD(double.MaxValue, double.MinValue);
		public static IntervalD NaN = new IntervalD(double.NaN, double.NaN);

		double min, max;

		public double Min
		{
			get { return min; }
			private set { max = value; }
		}

		public double Max
		{
			get { return max; }
			private set { max = value; }
		}

		public double Length
		{
			get { return max - min; }
		}

		public IntervalD(double min, double max)
		{
			this.min = min;
			this.max = max;
		}

		public void MergeWith(double value)
		{
			min = Math.Min(min, value);
			max = Math.Max(max, value);
		}

		public void MergeWith(IEnumerable<double> values)
		{
			foreach (double value in values)
				MergeWith(value);
		}

		public double CutValue(double value)
		{
			if (value <= min)
				return min;
			if (value >= max)
				return max;
			return value;
		}

		public double GetMaxAbsValue()
		{
			return Math.Max(Math.Abs(min), Math.Abs(max));
		}

		public int GetOrder()
		{
			if (max <= min)
				return 0;
			return (int)Math.Floor(Math.Log10(max - min));
		}

		public override string ToString()
		{
			return string.Format("<{0}, {1}>", min, max);
		}
	}
}

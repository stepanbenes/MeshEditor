using MeshEditor.DataVisualizer.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public class ColorScale
	{

		#region Color Scale Sub-Types

		public enum Types
		{
			Grayscale, // Black to White
			LightSpectrum, // standard color scale
			HeatGradient, // Blue to Red
			SeparatedByZero // Has fixed middle value (equals to 0.0)
		}

		public class ControlPoint : INotifyPropertyChanged
		{
			private bool isFixed;
			private int color;
			private double value;

			public ControlPoint(int color)
			{
				this.color = color;
			}

			public ControlPoint(ControlPoint toClone)
			{
				this.color = toClone.color;
				this.isFixed = toClone.isFixed;
				this.value = toClone.value;
			}

			// TODO: while setting fixed values, check for monotony must be done
			public bool IsFixed
			{
				get { return isFixed; }
				set
				{
					if (isFixed != value)
					{
						isFixed = value;
						OnPropertyChanged(nameof(IsFixed));
					}
				}
			}
			public double Value
			{
				get { return this.value; }
				set
				{
					if (this.value != value)
					{
						this.value = value;
						OnPropertyChanged(nameof(Value));
					}
				}
			}

			/// <summary>
			/// color in RGBA32 format
			/// </summary>
			public int Color
			{
				get { return color; }
				set
				{
					if (color != value)
					{
						color = value;
						OnPropertyChanged(nameof(Color));
					}
				}
			}

			#region INotifyPropertyChanged

			public event PropertyChangedEventHandler PropertyChanged;

			protected void OnPropertyChanged(string propertyName)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}

			#endregion
		}

		#endregion

		#region Static members

		//public static readonly int UndefinedValueColor = 0x003232FD; // blood color for undefined values
		public static readonly int UndefinedValueColor = unchecked((int)0xFE000000); // black color for undefined values; alpha must be set to this value correctly display color on beams (not zero, almost 255, but not exactly to disable iso-areas shader)
		public static readonly int OutOfRangeColor = unchecked((int)0xFE808080); // gray color for value out of minimum and maximum limit in color scale

		#endregion

		#region Fields, Constructor

		double minValue, maxValue;
		ControlPoint[] controlPoints;
		Types type;
		bool updatingControlPointValues;

		public ColorScale(Types type)
		{
			this.type = type;
			this.controlPoints = createControlPoints(type);
			registerControlPointsPropertyChangedEvent();
		}

		public ColorScale(ColorScale toClone)
		{
			this.minValue = toClone.minValue;
			this.maxValue = toClone.maxValue;
			this.type = toClone.type;
			this.controlPoints = toClone.controlPoints.Select(cp => new ControlPoint(cp)).ToArray(); // clone control points
			registerControlPointsPropertyChangedEvent();
		}

		#endregion

		#region Properties

		public ControlPoint[] ControlPoints
		{
			get { return controlPoints; }
		}

		public Types Type
		{
			get { return type; }
			set
			{
				if (type != value)
				{
					type = value;
					controlPoints = createControlPoints(type);
					registerControlPointsPropertyChangedEvent();
					SetMinMaxValue(minValue, maxValue);
				}
			}
		}

		public double MinValue
		{
			get { return minValue; }
		}

		public double MaxValue
		{
			get { return maxValue; }
		}

		#endregion

		#region Public methods

		//public int GetColorForValue(double value) // linear search O(N)
		//{
		//	Debug.Assert(controlPoints.Length >= 2 && value >= minValue && value <= maxValue);
		//	int index;
		//	for (index = 1; index < controlPoints.Length; index++)
		//	{
		//		if (controlPoints[index].Value >= value)
		//			break;
		//	}
		//	Debug.Assert(index < controlPoints.Length);
		//	if (index < controlPoints.Length)
		//	{
		//		double delta = controlPoints[index].Value - controlPoints[index - 1].Value;
		//		Debug.Assert(delta >= 0.0);
		//		if (delta > 0.0)
		//		{
		//			double position = (value - controlPoints[index - 1].Value) / delta;
		//			return interpolateTwoColors(controlPoints[index - 1].Color, controlPoints[index].Color, position);
		//		}
		//	}
		//	return UndefinedValueColor;
		//}

		public int GetColorForValue(double value) // binary search O(log N)
		{
			Debug.Assert(controlPoints.Length >= 2 && value >= minValue && value <= maxValue);

			int leftIndex = 0, rightIndex = controlPoints.Length - 1;

			if (value < controlPoints[leftIndex].Value || value > controlPoints[rightIndex].Value)
				return OutOfRangeColor;

			while (leftIndex < rightIndex - 1) // binary search
			{
				int centerIndex = (rightIndex + leftIndex) >> 1; // divide by two
				if (controlPoints[centerIndex].Value >= value)
					rightIndex = centerIndex;
				else
					leftIndex = centerIndex;
			}

			Debug.Assert(leftIndex == rightIndex - 1);
			if (rightIndex < controlPoints.Length)
			{
				double delta = controlPoints[rightIndex].Value - controlPoints[leftIndex].Value;
				if (Math.Abs(delta) > FloatComparisons.Epsilon)
				{
					double position = (value - controlPoints[leftIndex].Value) / delta;
					return Utilities.Functions.InterpolateTwoColors(controlPoints[leftIndex].Color, controlPoints[rightIndex].Color, position);
				}
				else // delta == 0.0
				{
					return controlPoints[rightIndex].Color; // get one of control points color
				}
			}
			return UndefinedValueColor;
		}

		public void SetMinMaxValue(double minValue, double maxValue)
		{
			Debug.Assert(minValue <= maxValue || double.IsNaN(minValue) || double.IsNaN(maxValue));
			Debug.Assert(controlPoints.Length >= 2);

			this.minValue = minValue;
			this.maxValue = maxValue;

			interpolateValuesInWholeInterval();
		}

		public void AddNewControlPoint(ControlPoint controlPoint)
		{
			controlPoint.PropertyChanged += controlPoint_PropertyChanged;

			// add to array
			controlPoints = controlPoints.Concat(new[] { controlPoint }).ToArray();
			updateControlPointValues();

			throw new NotImplementedException(); // isoareas shader does not support more than 5 control points !
		}

		public void RemoveControlPoint(ControlPoint controlPoint)
		{
			controlPoint.PropertyChanged -= controlPoint_PropertyChanged;
			
			// remove from array
			controlPoints = controlPoints.Where(cp => cp != controlPoint).ToArray();
			updateControlPointValues();

			throw new NotImplementedException();
		}

		#endregion

		#region Private methods

		private static ControlPoint[] createControlPoints(Types type)
		{
			switch (type)
			{
				case Types.Grayscale:
					{
						var controlPoints = new ControlPoint[2];
						// Black to White gradient
						controlPoints[0] = new ControlPoint(Utilities.Functions.ColorToRgba32(0, 0, 0, 255));
						controlPoints[1] = new ControlPoint(Utilities.Functions.ColorToRgba32(255, 255, 255, 255));
						return controlPoints;
					}
				case Types.LightSpectrum:
					{
						var controlPoints = new ControlPoint[5];
						// Standard (linear) color scale
						controlPoints[0] = new ControlPoint(Utilities.Functions.ColorToRgba32(51, 51, 254, 255));
						controlPoints[1] = new ControlPoint(Utilities.Functions.ColorToRgba32(51, 186, 193, 255));
						controlPoints[2] = new ControlPoint(Utilities.Functions.ColorToRgba32(12, 197, 7, 255));
						controlPoints[3] = new ControlPoint(Utilities.Functions.ColorToRgba32(238, 250, 9, 255));
						controlPoints[4] = new ControlPoint(Utilities.Functions.ColorToRgba32(237, 15, 3, 255));
						return controlPoints;
					}
				case Types.HeatGradient:
					{
						var controlPoints = new ControlPoint[2];
						// Blue to Red gradient
						controlPoints[0] = new ControlPoint(Utilities.Functions.ColorToRgba32(0, 0, 255, 255));
						controlPoints[1] = new ControlPoint(Utilities.Functions.ColorToRgba32(255, 0, 0, 255));
						return controlPoints;
					}
				case Types.SeparatedByZero:
					{
						var controlPoints = new ControlPoint[3];
						// Non-linear Separated-by-zero color scale
						controlPoints[0] = new ControlPoint(Utilities.Functions.ColorToRgba32(0, 0, 255, 255)); // blue
						controlPoints[1] = new ControlPoint(Utilities.Functions.ColorToRgba32(255, 255, 255, 255)) { IsFixed = true, Value = 0.0 }; // white, fixed zero
						controlPoints[2] = new ControlPoint(Utilities.Functions.ColorToRgba32(255, 0, 0, 255)); // red
						return controlPoints;
					}
				default:
					throw new NotSupportedException();
			}
		}

		private void registerControlPointsPropertyChangedEvent()
		{
			Debug.Assert(controlPoints != null);
			foreach (ControlPoint cp in controlPoints)
			{
				cp.PropertyChanged += controlPoint_PropertyChanged;
			}
		}

		private void controlPoint_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			ControlPoint controlPoint = sender as ControlPoint;
			if (controlPoint != null)
			{
				Debug.Assert(controlPoints.Length > 0);
				switch (e.PropertyName)
				{
					case "IsFixed":
					case "Value":
						updateControlPointValues();
						break;
					//case "Color":
					//	break;
				}
			}
		}

		private void updateControlPointValues()
		{
			if (!updatingControlPointValues)
			{
				updatingControlPointValues = true;
				interpolateValuesInWholeInterval();
				restoreMonotony();
				updatingControlPointValues = false;
			}
		}

		private void interpolateValuesInInterval(int startIndex, int endIndex)
		{
			int firstFixed = startIndex;
			while (firstFixed < endIndex)
			{
				for (int i = startIndex + 1; i <= endIndex; i++)
				{
					if (i == endIndex || controlPoints[i].IsFixed)
					{
						firstFixed = i;
						break;
					}
				}

				double startValue = (controlPoints[startIndex].IsFixed) ? controlPoints[startIndex].Value : this.minValue;
				double fixedValue = (controlPoints[firstFixed].IsFixed) ? controlPoints[firstFixed].Value : this.maxValue;
				double range = fixedValue - startValue;

				for (int i = startIndex + 1; i < firstFixed; i++)
				{
					double position = (double)(i - startIndex) / (firstFixed - startIndex);
					controlPoints[i].Value = Math.Min(range * position + startValue, fixedValue); // if range is negative, monotony is broken, therefore cut overlapping values by the first fixed value
				}

				startIndex = firstFixed;
			}
		}

		private void interpolateValuesInWholeInterval()
		{
			if (!controlPoints[0].IsFixed)
				controlPoints[0].Value = this.minValue;
			int lastIndex = controlPoints.Length - 1;
			if (!controlPoints[lastIndex].IsFixed)
				controlPoints[lastIndex].Value = this.maxValue;

			interpolateValuesInInterval(0, lastIndex);
		}

		private void restoreMonotony()
		{
			// chop from max to min
			double currentMin = double.MaxValue;
			for (int i = controlPoints.Length - 1; i >= 0; i--)
			{
				if (!controlPoints[i].IsFixed)
					controlPoints[i].Value = Math.Min(controlPoints[i].Value, currentMin);
				currentMin = controlPoints[i].Value;
			}
			// chop from min to max
			double currentMax = double.MinValue;
			for (int i = 0; i < controlPoints.Length; i++)
			{
				if (!controlPoints[i].IsFixed)
					controlPoints[i].Value = Math.Max(controlPoints[i].Value, currentMax);
				currentMax = controlPoints[i].Value;
			}
		}

		#endregion

	}
}

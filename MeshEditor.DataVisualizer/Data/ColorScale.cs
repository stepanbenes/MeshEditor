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

			// TODO: while setting fixed values, check for monotony must be done
			public bool IsFixed
			{
				get { return isFixed; }
				set
				{
					if (isFixed != value)
					{
						isFixed = value;
						OnPropertyChanged("IsFixed");
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
						OnPropertyChanged("Value");
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
						OnPropertyChanged("Color");
					}
				}
			}

			#region INotifyPropertyChanged

			public event PropertyChangedEventHandler PropertyChanged;

			protected void OnPropertyChanged(string propertyName)
			{
				var handler = PropertyChanged;
				if (handler != null)
					handler(this, new PropertyChangedEventArgs(propertyName));
			}

			#endregion
		}

		#endregion

		#region Static members

		//public static readonly int UndefinedValueColor = 0x003232FD; // blood color for undefined values
		public static readonly int UndefinedValueColor = 0x00000000; // black color for undefined values
		public static readonly int OutOfRangeColor = 0x00808080; // gray color for value out of minimum and maximum limit in color scale

		#endregion

		#region Fields, Constructor

		double minValue, maxValue;
		ControlPoint[] controlPoints;
		Types type;
		bool updatingControlPointValues;

		public ColorScale(Types type)
		{
			this.type = type;
			setupControlPoints();
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
					setupControlPoints();
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
				if (Math.Abs(delta) > Common.Epsilon)
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

			throw new NotImplementedException();
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

		private void setupControlPoints()
		{
			switch (type)
			{
				case Types.Grayscale:
					controlPoints = new ControlPoint[2];
					// Black to White gradient
					controlPoints[0] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(0, 0, 0, 255) };
					controlPoints[1] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(255, 255, 255, 255) };
					break;
				case Types.LightSpectrum:
					controlPoints = new ControlPoint[5];
					// Standard (linear) color scale
					controlPoints[0] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(51, 51, 254, 255) };
					controlPoints[1] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(51, 186, 193, 255) };
					controlPoints[2] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(12, 197, 7, 255) };
					controlPoints[3] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(238, 250, 9, 255) };
					controlPoints[4] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(237, 15, 3, 255) };
					break;
				case Types.HeatGradient:
					controlPoints = new ControlPoint[2];
					// Blue to Red gradient
					controlPoints[0] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(0, 0, 255, 255) };
					controlPoints[1] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(255, 0, 0, 255) };
					break;
				case Types.SeparatedByZero:
					controlPoints = new ControlPoint[3];
					// Non-linear Separated-by-zero color scale
					controlPoints[0] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(0, 0, 255, 255) }; // blue
					controlPoints[1] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(255, 255, 255, 255), IsFixed = true, Value = 0.0 }; // white, fixed zero
					controlPoints[2] = new ControlPoint { Color = Utilities.Functions.ColorToRgba32(255, 0, 0, 255) }; // red
					break;
				default:
					throw new NotImplementedException();
			}

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

using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.IO;
using MeshEditor.DataVisualizer.Mathematics;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public class DataAbstract
	{

		#region Fields, Properties, Constructor

		public double MinValue { get; set; }
		public double MaxValue { get; set; }

		//public double SumValue { get; private set; }

		public int ItemCount { get; protected set; }

		public int MinValueEntityNumber { get; protected set; }
		public int MaxValueEntityNumber { get; protected set; }

		//public Vector3 GradientStart { get; set; }
		//public Vector3 GradientEnd { get; set; }

		//public double MinGradientValue { get; set; }
		//public double MaxGradientValue { get; set; }

		//public double[] CornerValues { get; set; }

		//public HyperSurface RegressionSurface { get; set; }

		public Polynomial Approximation { get; set; }

		public float MaxError { get; set; }
		public float AverageError { get; set; }

		public DataAbstract()
		{
			this.MinValue = double.MaxValue;
			this.MaxValue = double.MinValue;
			//this.SumValue = 0.0;
			this.ItemCount = 0;

			this.MinValueEntityNumber = this.MaxValueEntityNumber = -1;
		}

		#endregion

		#region Public methods

		public override string ToString()
		{
			return string.Format("#Items: {0}; Min: {1} @ {2}; Max: {3} @ {4}", ItemCount, MinValue, MinValueEntityNumber, MaxValue, MaxValueEntityNumber);
		}

		public void MergeValue(DataValueComponent valueComponent)
		{
			//SumValue += valueComponent.Value;
			++ItemCount;
			if (valueComponent.Value < MinValue) // compare to minimum
			{
				MinValue = valueComponent.Value;
				MinValueEntityNumber = valueComponent.EntityNumber;
			}
			if (valueComponent.Value > MaxValue) // compare to maximum
			{
				MaxValue = valueComponent.Value;
				MaxValueEntityNumber = valueComponent.EntityNumber;
			}
		}

		#endregion

		#region Virtual methods

		public virtual long GetSizeInBytes()
		{
			long result = sizeof(double) * 2 + sizeof(int) * 3 + sizeof(float) * 2 + IntPtr.Size;
			if (Approximation != null)
				result += Approximation.SizeInBytes;
			return result;
		}

		public virtual double ComputeValueAt(ref Vector4 spacetime)
		{
			return Approximation.ComputeValue(spacetime.X, spacetime.Y, spacetime.Z);
		}

		public virtual bool ContainsTime(double time)
		{
			return true;
		}

		#endregion

	}
}

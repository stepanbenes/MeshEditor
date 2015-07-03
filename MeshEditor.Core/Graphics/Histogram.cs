using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// trida reprezentujici obecny histogram hodnot (meri cetnosti jednotlivych hodnot z predem urceneho intervalu)
	/// </summary>
	public class Histogram
	{
		#region Fields, Constructor

		private float leftBound, rightBound;
		private float resolution;

		private int[] columns;
				
		public Histogram(float leftBound, float rightBound, float resolution)
		{
			this.leftBound = leftBound;
			this.rightBound = rightBound;
			this.resolution = resolution;

			int count = (int)Math.Ceiling(((double)rightBound - (double)leftBound) / (double)resolution);

			this.columns = new int[count];
			for (int i = 0; i < count; i++)
				this.columns[i] = 0;
		}
		
		#endregion

		#region Properties

		public int[] Columns
		{
			get { return columns; }
		}

		public float LeftBound
		{
			get { return leftBound; }
		}
		
		public float RightBound
		{
			get { return rightBound; }
		}
				
		public float Resolution
		{
			get { return resolution; }
		}

		#endregion

		#region Public methods

		public void AddValue(float value)
		{
			int index = 0;
			if (value >= rightBound || float.IsNaN(value))
				index = columns.Length - 1;
			else if (value <= leftBound)
				index = 0;
			else
				index = (int)((value - leftBound) / resolution);
			
			columns[index]++;
		}

		#endregion

	}
}

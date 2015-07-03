using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public abstract class DataValue
	{
		public int EntityNumber { get; private set; }

		public DataValue(int entityNumber)
		{
			this.EntityNumber = entityNumber;
		}
	}

	public class NodeValue : DataValue
	{
		public double[] ValueComponents { get; private set; }

		public double this[int componentIndex]
		{
			get { return ValueComponents[componentIndex]; }
		}

		public NodeValue(int entityNumber, double[] values)
			: base(entityNumber)
		{
			this.ValueComponents = values;
		}

		public override string ToString()
		{
			return string.Format("Node ID: {0}; Values: {1}", EntityNumber, string.Join(",", ValueComponents.Select(v => v.ToString()).ToArray()));
		}
	}

	public class ElementValue : DataValue
	{
		/// <summary>
		/// Values in element's Gauss points
		/// </summary>
		public double[,] ValueComponents { get; private set; } // [Gauss points number, component number]

		public double this[int gaussPointIndex, int componentIndex]
		{
			get { return ValueComponents[gaussPointIndex, componentIndex]; }
		}

		public ElementValue(int entityNumber, double[,] values)
			: base(entityNumber)
		{
			this.ValueComponents = values;
		}

		public override string ToString()
		{
			return string.Format("Element ID: {0}; Gauss points number: {1}", EntityNumber, ValueComponents.GetLength(0));
		}
	}

	public struct DataValueComponent
	{
		private int entityNumber;
		private double value;

		public int EntityNumber { get { return entityNumber; } }
		public double Value { get { return this.value; } }

		public DataValueComponent(int entityNumber, double value)
		{
			this.entityNumber = entityNumber;
			this.value = value;
		}

		public override string ToString()
		{
			return string.Format("Entity: {0}; Value: {1}", EntityNumber, Value);
		}
	}
}

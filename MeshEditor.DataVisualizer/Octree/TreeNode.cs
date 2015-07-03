using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.Data;
using System.Diagnostics;
using MeshEditor.DataVisualizer.Mathematics;
using OpenTK.Graphics.OpenGL;
using MeshEditor.CoreInterface;

namespace MeshEditor.DataVisualizer.Octree
{
	/// <summary>
	/// Base class for internal and external octree nodes.
	/// </summary>
	public abstract class TreeNode
	{

		#region Static members

		//public static readonly int MinLeafEntityCountToExpand = 80;
		//public static readonly int MaxValueCapacity = 100;
		//public static readonly double ExpansionMinValueRangeRatio = 0.1;
		public static readonly int MinLeafEntityCount = 8;
		public static readonly int MaxDepth = 10; // <1,Infinity>
		public static readonly double MinRelativeErrorToExpand = 0.01; // <0,1>

		#endregion

		#region Fields, Properties

		private readonly Vector3 lowerBounds;
		private readonly Vector3 upperBounds;
		private readonly Vector3 center;

		private Dictionary<int, DataAbstract> dataCatalog;

		public Vector3 LowerBounds
		{
			get { return lowerBounds; }
		}

		public Vector3 UpperBounds
		{
			get { return upperBounds; }
		}

		public Vector3 Center
		{
			get { return center; }
		}

		public Dictionary<int, DataAbstract> DataCatalog
		{
			get { return dataCatalog; }
			set { dataCatalog = value; }
		}

		#endregion

		#region Constructor

		public TreeNode(Vector3 lowerBounds, Vector3 upperBounds)
		{
			this.lowerBounds = lowerBounds;
			this.upperBounds = upperBounds;
			this.center = (lowerBounds + upperBounds) * 0.5f;

			dataCatalog = new Dictionary<int, DataAbstract>();
		}

		#endregion

		#region Protected methods

		protected void ComputeApproximation(List<DataValueComponent> dataValueComponents, Dictionary<int, Node> nodeIndexMap, int dataIndex, ApproximationMethod method)
		{
			Debug.Assert(dataValueComponents != null);
			ComputeApproximation(new[] { dataValueComponents }, nodeIndexMap, dataIndex, method);
		}

		protected void ComputeApproximation(List<DataValueComponent>[] dataValueComponentLists, Dictionary<int, Node> nodeIndexMap, int dataIndex, ApproximationMethod method)
		{
			Debug.Assert(dataValueComponentLists != null);

			List<float> x = new List<float>();
			List<float> y = new List<float>();
			List<float> z = new List<float>();
			List<float> w = new List<float>();

			foreach (List<DataValueComponent> dataValueComponentList in dataValueComponentLists)
			{
				if (dataValueComponentList == null)
					continue;
				foreach (DataValueComponent dataValue in dataValueComponentList)
				{
					Vector3 dataPos = nodeIndexMap[dataValue.EntityNumber].Position;
					x.Add(dataPos.X);
					y.Add(dataPos.Y);
					z.Add(dataPos.Z);
					w.Add((float)dataValue.Value);
				}
			}

			//Polynomial form = Approximation.DoLSTI(x, y, z, w);
			Polynomial form = Approximation.DoApproximation(x, y, z, w, method);

			DataAbstract dataAbstract = DataCatalog[dataIndex];

			dataAbstract.Approximation = form;

			// compute absolute error
			float maxError = 0.0f;
			float sumError = 0.0f;
			int index = 0;
			foreach (List<DataValueComponent> dataValueComponentList in dataValueComponentLists)
			{
				if (dataValueComponentList == null)
					continue;
				for (int i = 0; i < dataValueComponentList.Count; i++)
				{
					float apx = form.ComputeValue(x[index], y[index], z[index]);
					float error = w[index] - apx;

					dataValueComponentList[i] = new DataValueComponent(dataValueComponentList[i].EntityNumber, error);

					sumError += Math.Abs(error);
					maxError = Math.Max(maxError, Math.Abs(error));
					++index;
				}
			}
			dataAbstract.AverageError = (index > 0) ? sumError / index : 0f;
			dataAbstract.MaxError = maxError;
		}

		protected void DrawTreeNodeBoundary()
		{
			// DRAW BOUNDARY
			GL.Begin(BeginMode.Lines);
			{
				GL.Vertex3(upperBounds.X, upperBounds.Y, upperBounds.Z);
				GL.Vertex3(upperBounds.X, upperBounds.Y, lowerBounds.Z);

				GL.Vertex3(upperBounds.X, upperBounds.Y, lowerBounds.Z);
				GL.Vertex3(lowerBounds.X, upperBounds.Y, lowerBounds.Z);

				GL.Vertex3(lowerBounds.X, upperBounds.Y, lowerBounds.Z);
				GL.Vertex3(lowerBounds.X, upperBounds.Y, upperBounds.Z);

				GL.Vertex3(lowerBounds.X, upperBounds.Y, upperBounds.Z);
				GL.Vertex3(upperBounds.X, upperBounds.Y, upperBounds.Z);


				GL.Vertex3(upperBounds.X, lowerBounds.Y, upperBounds.Z);
				GL.Vertex3(upperBounds.X, lowerBounds.Y, lowerBounds.Z);

				GL.Vertex3(upperBounds.X, lowerBounds.Y, lowerBounds.Z);
				GL.Vertex3(lowerBounds.X, lowerBounds.Y, lowerBounds.Z);

				GL.Vertex3(lowerBounds.X, lowerBounds.Y, lowerBounds.Z);
				GL.Vertex3(lowerBounds.X, lowerBounds.Y, upperBounds.Z);

				GL.Vertex3(lowerBounds.X, lowerBounds.Y, upperBounds.Z);
				GL.Vertex3(upperBounds.X, lowerBounds.Y, upperBounds.Z);


				GL.Vertex3(upperBounds.X, upperBounds.Y, upperBounds.Z);
				GL.Vertex3(upperBounds.X, lowerBounds.Y, upperBounds.Z);

				GL.Vertex3(upperBounds.X, upperBounds.Y, lowerBounds.Z);
				GL.Vertex3(upperBounds.X, lowerBounds.Y, lowerBounds.Z);

				GL.Vertex3(lowerBounds.X, upperBounds.Y, lowerBounds.Z);
				GL.Vertex3(lowerBounds.X, lowerBounds.Y, lowerBounds.Z);

				GL.Vertex3(lowerBounds.X, upperBounds.Y, upperBounds.Z);
				GL.Vertex3(lowerBounds.X, lowerBounds.Y, upperBounds.Z);
			}
			GL.End();
		}

		#endregion

		#region Abstract and virtual methods to be overloaded in derived classes

		// rename to CreateDataCatalog()?
		public abstract void InsertDataValues(IEnumerable<DataValueComponent> dataValueComponents, int dataIndex, Dictionary<int, Node> nodeIndexMap, int depth, double globalRange, ApproximationMethod method);
		public abstract LeafNode GetLeafOnPosition(ref Vector3 position);
		public abstract DataAbstract GetDataAbstract(ref Vector3 position, int dataIndex);
		public abstract TreeNode GetTreeNodeOnPositionWithData(ref Vector3 position, int dataIndex);

		public virtual void ProcessBelongingNodes(IEnumerable<Node> nodes, Action<TreeNode, IEnumerable<Node>> operation)
		{
			Debug.Assert(nodes != null && operation != null);

			// Process this tree node
			operation(this, nodes);
		}

		public virtual double GetValueApproximationAt(ref Vector4 spacetime, int dataIndex)
		{
			DataAbstract dataAbstract;
			if (!DataCatalog.TryGetValue(dataIndex, out dataAbstract))
				return double.NaN;
			return dataAbstract.ComputeValueAt(ref spacetime);
		}

		public virtual void Draw(DataIndex dataIndex, double globalRange = double.NaN)
		{
			Debug.Assert(DataCatalog.ContainsKey(dataIndex.Index));

			//if (double.IsNaN(globalRange))
			//	globalRange = DataCatalog[dataIndex.Index].MaxValue - DataCatalog[dataIndex.Index].MinValue;

			DrawTreeNodeBoundary();
			
			//DataAbstract dataAbstract;
			//if (DataCatalog.TryGetValue(dataIndex.Index, out dataAbstract))
			//{
			//	// DRAW MAX RELATIVE ERROR
			//	Utilities.Functions.DrawText((dataAbstract.MaxError / globalRange).ToString("G4"), center, System.Drawing.Color.Red);
			//	// DRAW MAX ABSOLUTE ERROR
			//	//Utilities.Functions.DrawText(dataAbstract.MaxAbsoluteError.ToString("G4"), center, System.Drawing.Color.Red);
			//}
		}

		#endregion

	}
}

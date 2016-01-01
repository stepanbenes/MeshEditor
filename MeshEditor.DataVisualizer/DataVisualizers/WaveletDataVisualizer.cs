using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.IO;
using MeshEditor.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.DataVisualizer
{
	public class WaveletDataVisualizer : ExactDataVisualizer
	{

		#region Octree

		private class Octree<TData> where TData : class, IItemWithSignificantPoint
		{
			InternalNode<TData> root;
			Vector3 lowerBounds, upperBounds;

			public Octree(Vector3 lowerBounds, Vector3 upperBounds)
			{
				this.lowerBounds = lowerBounds;
				this.upperBounds = upperBounds;
				root = new InternalNode<TData>();
			}

			public void Insert(TData data)
			{
				Debug.Assert(data != null);
				root.Insert(data, ref lowerBounds, ref upperBounds, depth: 0);
			}

			public TData GetData(Vector3 position) => root.GetData(position, lowerBounds, upperBounds);

			public void DrawBoundary()
			{
				GL.Begin(BeginMode.Lines);
				{
					root.DrawBoundary(lowerBounds, upperBounds);
				}
				GL.End();
			}

			public List<TData> Traverse()
			{
				List<TData> dataCollection = new List<TData>();
				root.ZOrderTraverse(dataCollection);
				// TODO: add Hilbert space-filling curve, that has more local characteristics
				return dataCollection;
			}

			abstract class OctreeNode<T> where T : class, IItemWithSignificantPoint
			{
				public abstract T GetData(Vector3 position, Vector3 lowerBounds, Vector3 upperBounds);

				public virtual void DrawBoundary(Vector3 lowerBounds, Vector3 upperBounds)
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

				public abstract void ZOrderTraverse(ICollection<T> dataCollection);
				public abstract void HilbertCurveTraverse(ICollection<T> dataCollection);
			}

			class InternalNode<T> : OctreeNode<T> where T : class, IItemWithSignificantPoint
			{
				OctreeNode<T>[] children = new OctreeNode<T>[8];

				public override T GetData(Vector3 position, Vector3 lowerBounds, Vector3 upperBounds)
				{
					int childIndex = getChildIndex(position, ref lowerBounds, ref upperBounds);
					OctreeNode<T> child = children[childIndex];
					if (child == null)
						return null;
					return child.GetData(position, lowerBounds, upperBounds);
				}

				public void Insert(T data, ref Vector3 lowerBounds, ref Vector3 upperBounds, int depth)
				{
					int childIndex = getChildIndex(data.GetSignificantPoint(), ref lowerBounds, ref upperBounds);
					OctreeNode<T> child = children[childIndex];
					if (child == null)
					{
						children[childIndex] = new LeafNode<T>(data);
					}
					else
					{
						//if (depth > 9)
						//	return;

						Vector3 childLowerBounds, childUpperBounds;
						getChildBounds(childIndex, ref lowerBounds, ref upperBounds, out childLowerBounds, out childUpperBounds);

						InternalNode<T> internalNode;
						LeafNode<T> leafNode = child as LeafNode<T>;
						if (leafNode != null)
						{
							internalNode = new InternalNode<T>();
							children[childIndex] = internalNode;
							internalNode.children[getChildIndex(leafNode.Data.GetSignificantPoint(), ref childLowerBounds, ref childUpperBounds)] = leafNode;
						}
						else
						{
							internalNode = (InternalNode<T>)child;
						}
						internalNode.Insert(data, ref childLowerBounds, ref childUpperBounds, depth + 1);
					}
				}

				private static int getChildIndex(Vector3 position, ref Vector3 lowerBounds, ref Vector3 upperBounds)
				{
					Vector3 center = Utilities.Functions.GetCenterOfLineSegment(ref upperBounds, ref lowerBounds);
					int childIndex = 0;
					if (position.X > center.X)
						childIndex += 1;
					if (position.Y > center.Y)
						childIndex += 2;
					if (position.Z > center.Z)
						childIndex += 4;
					return childIndex;
				}

				private static void getChildBounds(int childIndex, ref Vector3 parentLowerBounds, ref Vector3 parentUpperBounds, out Vector3 childLowerBounds, out Vector3 childUpperBounds)
				{
					Vector3 center = Utilities.Functions.GetCenterOfLineSegment(ref parentUpperBounds, ref parentLowerBounds);
					if ((childIndex & 1) > 0)
					{
						childLowerBounds.X = center.X;
						childUpperBounds.X = parentUpperBounds.X;
					}
					else
					{
						childLowerBounds.X = parentLowerBounds.X;
						childUpperBounds.X = center.X;
					}

					if ((childIndex & 2) > 0)
					{
						childLowerBounds.Y = center.Y;
						childUpperBounds.Y = parentUpperBounds.Y;
					}
					else
					{
						childLowerBounds.Y = parentLowerBounds.Y;
						childUpperBounds.Y = center.Y;
					}

					if ((childIndex & 4) > 0)
					{
						childLowerBounds.Z = center.Z;
						childUpperBounds.Z = parentUpperBounds.Z;
					}
					else
					{
						childLowerBounds.Z = parentLowerBounds.Z;
						childUpperBounds.Z = center.Z;
					}
				}

				public override void DrawBoundary(Vector3 lowerBounds, Vector3 upperBounds)
				{
					for (int i = 0; i < children.Length; i++)
					{
						if (children[i] != null)
						{
							Vector3 childLowerBounds, childUpperBounds;
							getChildBounds(i, ref lowerBounds, ref upperBounds, out childLowerBounds, out childUpperBounds);
							children[i].DrawBoundary(childLowerBounds, childUpperBounds);
						}
					}
				}

				public override void ZOrderTraverse(ICollection<T> dataCollection)
				{
					for (int i = 0; i < children.Length; i++)
					{
						if (children[i] != null)
						{
							children[i].ZOrderTraverse(dataCollection);
						}
					}
				}

				public override void HilbertCurveTraverse(ICollection<T> dataCollection)
				{
					throw new NotImplementedException();
				}
			}

			class LeafNode<T> : OctreeNode<T> where T : class, IItemWithSignificantPoint
			{
				public T Data { get; }
				public LeafNode(T data)
				{
					Data = data;
				}
				public override T GetData(Vector3 position, Vector3 lowerBounds, Vector3 upperBounds) => Data;
				public override void ZOrderTraverse(ICollection<T> dataCollection) => dataCollection.Add(Data);
				public override void HilbertCurveTraverse(ICollection<T> dataCollection) => dataCollection.Add(Data);
			}
		}

		#endregion

		#region Fields

		Octree<Node> octree;
		Dictionary<Node, int> spaceFillingNodeSequenceIndices;
		Dictionary<int, double[]> data;

		int currentDataIndex;
		double[] currentData;

		#endregion

		#region Overrides

		public override void Initialize(Mesh mesh)
		{
			base.Initialize(mesh);

			// non-uniform dimensions => block/prism
			// octree = new Octree<Node>(mesh.LowerBound, mesh.UpperBound);

			// uniform dimensions => cube
			float maxDim = Math.Max(Math.Max(mesh.UpperBound.X - mesh.LowerBound.X, mesh.UpperBound.Y - mesh.LowerBound.Y), mesh.UpperBound.Z - mesh.LowerBound.Z);

			Vector3 lowerBound = mesh.LowerBound;
			Vector3 upperBound;
			upperBound.X = Math.Max(lowerBound.X + maxDim, mesh.UpperBound.X); // avoid rounding error
			upperBound.Y = Math.Max(lowerBound.Y + maxDim, mesh.UpperBound.Y);
			upperBound.Z = Math.Max(lowerBound.Z + maxDim, mesh.UpperBound.Z);

			octree = new Octree<Node>(lowerBound, upperBound);

			currentDataIndex = -1;
			currentData = null;
		}

		public override void LoadData(IApproximationParameters approximationParameters, string[] filenames, LongOpNotifier longOpNotifier)
		{
			base.LoadData(approximationParameters, filenames, longOpNotifier);

			foreach (Node node in nodeIndexMap.Values)
			{
				octree.Insert(node);
			}

			spaceFillingNodeSequenceIndices = octree.Traverse().Select((node, index) => new KeyValuePair<Node, int>(node, index)).ToDictionary(pair => pair.Key, pair => pair.Value);

			data = new Dictionary<int, double[]>();
			foreach (int dataIndex in nodeValues.Keys)
			{
				double[] dataArray = new double[spaceFillingNodeSequenceIndices.Count];
				foreach (var pair in spaceFillingNodeSequenceIndices)
				{
					double value;
					if (nodeValues[dataIndex].TryGetValue(pair.Key.ID, out value))
					{
						dataArray[pair.Value] = value;
					}
					else
						Debugger.Break();
				}
				data[dataIndex] = FWT(dataArray, GetDataValueRange(dataIndex)); // TODO: dowavelet transform on space-time 2D surface instead of 1D space-filling curve
			}
		}

		public override double GetDataValue(Node node, DataIndex dataIndex)
		{
			int index;
			if (!spaceFillingNodeSequenceIndices.TryGetValue(node, out index))
				return double.NaN;

			if (currentDataIndex != dataIndex.Index)
			{
				currentData = IWT(data[dataIndex.Index], GetDataValueRange(dataIndex.Index));
				currentDataIndex = dataIndex.Index;
			}
			return currentData[index];
		}

		public override ApproximationQuality GetApproximationQuality(LongOpNotifier longOpNotifier)
		{
			throw new NotImplementedException();
		}

		public override void DrawItems(PropertyColorsMode propertyColorsMode)
		{
			base.DrawItems(propertyColorsMode);

			Debug.Assert(octree != null);

			if (Settings.DrawGrid)
			{
				bool lightEnabled = GL.IsEnabled(EnableCap.Lighting);
				if (lightEnabled)
					GL.Disable(EnableCap.Lighting);

				GL.LineWidth(1f);
				GL.Disable(EnableCap.Lighting);
				GL.Color3(1f, 1f, 0f);

				//octree.DrawBoundary();

				if (spaceFillingNodeSequenceIndices != null)
				{
					hilbertPosition = new Vector3(0f, 0f, 0f);
					hilbertViewVector = new Vector3(1f, 0f, 0f);
					hilbertUpVector = new Vector3(0f, 1f, 0f);
					const int level = 3;
					const float cubeSideLength = 1.0f;
					const float stepLength = cubeSideLength / ((1 << level) - 1);
					List<Vector3> hilbertCurve = new List<Vector3>();
					createHilbertCurve(level, stepLength, hilbertCurve);

					GL.Color3(1f, 0f, 0f);
					GL.Begin(BeginMode.LineStrip);
					{
						//foreach (Node node in spaceFillingNodeSequenceIndices.OrderBy(pair => pair.Value).Select(pair => pair.Key))
						//	GL.Vertex3(node.Position.X, node.Position.Y, node.Position.Z);

						foreach (Vector3 point in hilbertCurve)
							GL.Vertex3(point);
					}
					GL.End();
				}

				if (lightEnabled)
					GL.Enable(EnableCap.Lighting);
			}
		}

		#endregion

		#region Wavelet transform

		private const double w0 = 0.5;
		private const double w1 = -0.5;
		private const double s0 = 0.5;
		private const double s1 = 0.5;
		private const int IterationsCount = 3;

		private static double[] FWT(double[] input, IntervalD dataInterval)
		{
			//int newLength = getNearestPowerOfTwo(input.Length);
			int newLength = findClosestNumberDivisibleBy(number: input.Length, divider: 1 << IterationsCount);
			double[] scaledInput = enlarge(input, newLength);
			for (int i = 0; i < input.Length; i++)
			{
				scaledInput[i] = scale(dataInterval.Min, dataInterval.Max, -1, 1, scaledInput[i]);
			}
			int usableLength = scaledInput.Length;
			Debug.Assert((usableLength >> IterationsCount) > 1);
			for (int i = 0; i < IterationsCount; i++)
			{
				FWTiteration(scaledInput, usableLength);
				usableLength >>= 1;
			}
			return scaledInput;
		}

		private static double[] IWT(double[] input, IntervalD dataInterval)
		{
			double[] result = input.ToArray();
			int usableLength = result.Length >> (IterationsCount - 1);
			Debug.Assert(usableLength > 1);
			for (int i = 0; i < IterationsCount; i++)
			{
				IWTiteration(result, usableLength);
				usableLength <<= 1;
			}
			for (int i = 0; i < input.Length; i++)
			{
				result[i] = scale(-1, 1, dataInterval.Min, dataInterval.Max, result[i]); // scale output back
			}
			return result;
		}

		private static void FWTiteration(double[] input, int usableLength)
		{
			Debug.Assert(input != null);
			Debug.Assert(usableLength <= input.Length);
			double[] output = new double[usableLength];
			int h = usableLength >> 1;
			for (int i = 0; i < h; i++)
			{
				int k = (i << 1);
				output[i] = input[k] * s0 + input[k + 1] * s1;
				output[i + h] = input[k] * w0 + input[k + 1] * w1;
			}
			for (int i = 0; i < usableLength; i++)
			{
				input[i] = output[i];
			}
		}

		private static void IWTiteration(double[] input, int usableLength)
		{
			Debug.Assert(input != null);
			Debug.Assert(usableLength <= input.Length);
			double[] output = new double[usableLength];
			int h = usableLength >> 1; // TODO: handle cases when the length of the input is not a power of two
			for (int i = 0; i < h; i++)
			{
				int k = (i << 1);
				output[k] = (input[i] * s0 + input[i + h] * w0) / w0;
				output[k + 1] = (input[i] * s1 + input[i + h] * w1) / s0;
			}
			for (int i = 0; i < usableLength; i++)
			{
				input[i] = output[i];
			}
		}

		private static int findClosestNumberDivisibleBy(int number, int divider)
		{
			return (number / divider + 1) * divider;
		}

		private static double[] enlarge(double[] data, int newSize)
		{
			Debug.Assert(newSize >= data.Length);
			if (newSize == data.Length)
				return data.ToArray();
			double[] result = new double[newSize];
			Array.Copy(data, result, data.Length);
			return result;
		}

		private static int getNearestPowerOfTwo(int number) // NOT USED
		{
			Debug.Assert(number >= 0);
			int n = number - 1;
			n |= n >> 1;
			n |= n >> 2;
			n |= n >> 4;
			n |= n >> 8;
			n |= n >> 16;
			return n + 1;
		}

		private static double[] shrink(double[] data, int newSize) // NOT USED
		{
			Debug.Assert(newSize <= data.Length);
			if (newSize == data.Length)
				return data;
			double[] result = new double[newSize];
			Array.Copy(data, result, newSize);
			return result;
		}

		private static double scale(double fromMin, double fromMax, double toMin, double toMax, double x)
		{
			if (fromMax - fromMin == 0) return 0;
			double value = (toMax - toMin) * (x - fromMin) / (fromMax - fromMin) + toMin;
			if (value > toMax)
			{
				value = toMax;
			}
			if (value < toMin)
			{
				value = toMin;
			}
			return value;
		}

		static Vector3 hilbertPosition, hilbertViewVector, hilbertUpVector;

		private static void createHilbertCurve(int level, float stepLength, List<Vector3> hilbertCurve)
		{
			if (level == 0)
			{
				hilbertCurve.Add(hilbertPosition);
				return;
			}

			// rewrite X to ^ < X F ^ < X F X -F ^ > > X F X &F + > > X F X -F > X - >;
			// interpret F as DrawForward(10);
			// interpret + as Yaw(90);
			// interpret - as Yaw(-90);
			// interpret ^ as Pitch(90);
			// interpret & as Pitch(-90);
			// interpret > as Roll(90);
			// interpret < as Roll(-90);

			Vector3 temp;

			// ^
			temp = hilbertUpVector;
			hilbertUpVector = -hilbertViewVector;
			hilbertViewVector = temp;
			// <
			hilbertUpVector = Vector3.Cross(hilbertUpVector, hilbertViewVector);
			// X
			createHilbertCurve(level - 1, stepLength, hilbertCurve);
			// F
			hilbertPosition += hilbertViewVector * stepLength;
			// ^
			temp = hilbertUpVector;
			hilbertUpVector = -hilbertViewVector;
			hilbertViewVector = temp;
			// <
			hilbertUpVector = Vector3.Cross(hilbertUpVector, hilbertViewVector);
			// X
			createHilbertCurve(level - 1, stepLength, hilbertCurve);
			// F
			hilbertPosition += hilbertViewVector * stepLength;
			// X
			createHilbertCurve(level - 1, stepLength, hilbertCurve);
			// -
			hilbertViewVector = Vector3.Cross(hilbertViewVector, hilbertUpVector);
			// F
			hilbertPosition += hilbertViewVector * stepLength;
			// ^
			temp = hilbertUpVector;
			hilbertUpVector = -hilbertViewVector;
			hilbertViewVector = temp;
			// >
			hilbertUpVector = Vector3.Cross(hilbertViewVector, hilbertUpVector);
			// >
			hilbertUpVector = Vector3.Cross(hilbertViewVector, hilbertUpVector);
			// X
			createHilbertCurve(level - 1, stepLength, hilbertCurve);
			// F
			hilbertPosition += hilbertViewVector * stepLength;
			// X
			createHilbertCurve(level - 1, stepLength, hilbertCurve);
			// &
			temp = hilbertViewVector;
			hilbertViewVector = -hilbertUpVector;
			hilbertUpVector = temp;
			// F
			hilbertPosition += hilbertViewVector * stepLength;
			// +
			hilbertViewVector = Vector3.Cross(hilbertUpVector, hilbertViewVector);
			// >
			hilbertUpVector = Vector3.Cross(hilbertViewVector, hilbertUpVector);
			// >
			hilbertUpVector = Vector3.Cross(hilbertViewVector, hilbertUpVector);
			// X
			createHilbertCurve(level - 1, stepLength, hilbertCurve);
			// F
			hilbertPosition += hilbertViewVector * stepLength;
			// X
			createHilbertCurve(level - 1, stepLength, hilbertCurve);
			// -
			hilbertViewVector = Vector3.Cross(hilbertViewVector, hilbertUpVector);
			// F
			hilbertPosition += hilbertViewVector * stepLength;
			// >
			hilbertUpVector = Vector3.Cross(hilbertViewVector, hilbertUpVector);
			// X
			createHilbertCurve(level - 1, stepLength, hilbertCurve);
			// -
			hilbertViewVector = Vector3.Cross(hilbertViewVector, hilbertUpVector);
			// >
			hilbertUpVector = Vector3.Cross(hilbertViewVector, hilbertUpVector);
		}

		#endregion

	}
}

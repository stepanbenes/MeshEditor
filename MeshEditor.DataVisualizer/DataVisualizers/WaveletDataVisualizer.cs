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
			static int[,] orderingTable =
			{
				{ 0, 1, 3, 2, 6, 7, 5, 4 },
				{ 0, 4, 6, 2, 3, 7, 5, 1 },
				{ 0, 1, 5, 4, 6, 7, 3, 2 },
				{ 5, 1, 0, 4, 6, 2, 3, 7 },
				{ 3, 7, 6, 2, 0, 4, 5, 1 },
				{ 6, 7, 3, 2, 0, 1, 5, 4 },
				{ 5, 1, 3, 7, 6, 2, 0, 4 },
				{ 0, 4, 5, 1, 3, 7, 6, 2 },
				{ 5, 4, 0, 1, 3, 2, 6, 7 },
				{ 5, 4, 6, 7, 3, 2, 0, 1 },
				{ 0, 2, 3, 1, 5, 7, 6, 4 },
				{ 6, 4, 0, 2, 3, 1, 5, 7 },
				{ 5, 7, 3, 1, 0, 2, 6, 4 },
				{ 3, 7, 5, 1, 0, 4, 6, 2 },
				{ 6, 4, 5, 7, 3, 1, 0, 2 },
				{ 0, 2, 6, 4, 5, 7, 3, 1 },
				{ 6, 2, 0, 4, 5, 1, 3, 7 },
				{ 6, 2, 3, 7, 5, 1, 0, 4 },
				{ 3, 2, 0, 1, 5, 4, 6, 7 },
				{ 6, 7, 5, 4, 0, 1, 3, 2 },
				{ 5, 7, 6, 4, 0, 2, 3, 1 },
				{ 3, 2, 6, 7, 5, 4, 0, 1 },
				{ 3, 1, 0, 2, 6, 4, 5, 7 },
				{ 3, 1, 5, 7, 6, 4, 0, 2 },
			};

			static int[,] orientationTable =
			{
				{ 1, 2, 0, 3, 4, 0, 5, 6 },
				{ 0, 7, 1, 8, 5, 1, 4, 9 },
				{ 15, 0, 2, 22, 20, 2, 19, 23 },
				{ 20, 6, 3, 23, 15, 3, 16, 22 },
				{ 22, 13, 4, 12, 11, 4, 1, 20 },
				{ 11, 19, 5, 20, 22, 5, 0, 12 },
				{ 9, 3, 6, 2, 21, 6, 17, 0 },
				{ 10, 1, 7, 11, 12, 7, 13, 14 },
				{ 12, 9, 8, 14, 10, 8, 18, 11 },
				{ 6, 8, 9, 7, 17, 9, 21, 1 },
				{ 7, 15, 10, 16, 13, 10, 12, 17 },
				{ 5, 14, 11, 9, 0, 11, 22, 8 },
				{ 8, 20, 12, 19, 18, 12, 10, 5 },
				{ 18, 4, 13, 5, 8, 13, 7, 19 },
				{ 17, 11, 14, 1, 6, 14, 23, 7 },
				{ 2, 10, 15, 18, 19, 15, 20, 21 },
				{ 19, 17, 16, 21, 2, 16, 3, 18 },
				{ 14, 16, 17, 15, 23, 17, 6, 10 },
				{ 13, 21, 18, 17, 7, 18, 8, 16 },
				{ 16, 5, 19, 4, 3, 19, 2, 13 },
				{ 3, 12, 20, 13, 16, 20, 15, 4 },
				{ 23, 18, 21, 10, 14, 21, 9, 15 },
				{ 4, 23, 22, 6, 1, 22, 11, 3 },
				{ 21, 22, 23, 0, 9, 23, 14, 2 },
			};

			InternalNode<TData> root;

			public Octree(Vector3 lowerBounds, Vector3 upperBounds)
			{
				root = new InternalNode<TData>(lowerBounds, upperBounds);
			}

			public void Insert(TData data)
			{
				Debug.Assert(data != null);
				root.Insert(data, depth: 0);
			}

			public TData GetData(Vector3 position) => root.GetData(position);

			public void DrawBoundary()
			{
				GL.Begin(BeginMode.Lines);
				{
					root.DrawBoundary();
				}
				GL.End();
			}

			public void Draw()
			{
				GL.Begin(BeginMode.LineStrip);
				{
					root.DrawHilbertCurve(parentOrientation: 0);
				}
				GL.End();
			}

			public List<TData> ZOrderCurveTraverse()
			{
				List<TData> dataCollection = new List<TData>();
				root.ZOrderTraverse(dataCollection);
				return dataCollection;
			}

			public List<TData> HilbertCurveTraverse()
			{
				List<TData> dataCollection = new List<TData>();
				root.HilbertCurveTraverse(dataCollection, parentOrientation: 0); // Hilbert space-filling curve has more local characteristics than Z-order curve
				return dataCollection;
			}

			abstract class OctreeNode<T> where T : class, IItemWithSignificantPoint
			{
				protected Vector3 lowerBounds, upperBounds;

				public OctreeNode(Vector3 lowerBounds, Vector3 upperBounds)
				{
					this.lowerBounds = lowerBounds;
					this.upperBounds = upperBounds;
				}

				public abstract T GetData(Vector3 position);

				public virtual void DrawBoundary()
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
				public abstract void HilbertCurveTraverse(ICollection<T> dataCollection, int parentOrientation);
				public abstract void DrawHilbertCurve(int parentOrientation);
			}

			class InternalNode<T> : OctreeNode<T> where T : class, IItemWithSignificantPoint
			{
				OctreeNode<T>[] children = new OctreeNode<T>[8];

				public InternalNode(Vector3 lowerBounds, Vector3 upperBounds)
					: base(lowerBounds, upperBounds)
				{ }

				public override T GetData(Vector3 position)
				{
					int childIndex = getChildIndex(position);
					OctreeNode<T> child = children[childIndex];
					if (child == null)
						return null;
					return child.GetData(position);
				}

				public void Insert(T data, int depth)
				{
					int childIndex = getChildIndex(data.GetSignificantPoint());
					Vector3 childLowerBounds, childUpperBounds;
					getChildBounds(childIndex, out childLowerBounds, out childUpperBounds);
					OctreeNode<T> child = children[childIndex];
					if (child == null)
					{
						children[childIndex] = new LeafNode<T>(data, childLowerBounds, childUpperBounds);
					}
					else
					{
						//if (depth > 9)
						//	return;
						
						InternalNode<T> internalNode;
						LeafNode<T> leafNode = child as LeafNode<T>;
						if (leafNode != null)
						{
							internalNode = new InternalNode<T>(childLowerBounds, childUpperBounds);
							children[childIndex] = internalNode;
							internalNode.children[getChildIndex(leafNode.Data.GetSignificantPoint())] = leafNode;
						}
						else
						{
							internalNode = (InternalNode<T>)child;
						}
						internalNode.Insert(data, depth + 1);
					}
				}

				private int getChildIndex(Vector3 position)
				{
					Vector3 center = Utilities.Functions.GetCenterOfLineSegment(ref lowerBounds, ref upperBounds);
					int childIndex = 0;
					if (position.X > center.X)
						childIndex += 1;
					if (position.Y > center.Y)
						childIndex += 2;
					if (position.Z > center.Z)
						childIndex += 4;
					return childIndex;
				}

				private void getChildBounds(int childIndex, out Vector3 childLowerBounds, out Vector3 childUpperBounds)
				{
					Vector3 center = Utilities.Functions.GetCenterOfLineSegment(ref lowerBounds, ref upperBounds);
					if ((childIndex & 1) > 0)
					{
						childLowerBounds.X = center.X;
						childUpperBounds.X = upperBounds.X;
					}
					else
					{
						childLowerBounds.X = lowerBounds.X;
						childUpperBounds.X = center.X;
					}

					if ((childIndex & 2) > 0)
					{
						childLowerBounds.Y = center.Y;
						childUpperBounds.Y = upperBounds.Y;
					}
					else
					{
						childLowerBounds.Y = lowerBounds.Y;
						childUpperBounds.Y = center.Y;
					}

					if ((childIndex & 4) > 0)
					{
						childLowerBounds.Z = center.Z;
						childUpperBounds.Z = upperBounds.Z;
					}
					else
					{
						childLowerBounds.Z = lowerBounds.Z;
						childUpperBounds.Z = center.Z;
					}
				}

				public override void DrawBoundary()
				{
					for (int i = 0; i < children.Length; i++)
					{
						if (children[i] != null)
						{
							children[i].DrawBoundary();
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

				public override void HilbertCurveTraverse(ICollection<T> dataCollection, int parentOrientation)
				{
					for (int i = 0; i < 8; i++)
					{
						int order = orderingTable[parentOrientation, i];
						if (children[order] != null)
						{
							children[order].HilbertCurveTraverse(dataCollection, orientationTable[parentOrientation, i]);
						}
					}
				}

				public override void DrawHilbertCurve(int parentOrientation)
				{
					for (int i = 0; i < 8; i++)
					{
						int order = orderingTable[parentOrientation, i];
						if (children[order] != null)
						{
							children[order].DrawHilbertCurve(orientationTable[parentOrientation, i]);
						}
					}
				}
			}

			class LeafNode<T> : OctreeNode<T> where T : class, IItemWithSignificantPoint
			{
				public LeafNode(T data, Vector3 lowerBounds, Vector3 upperBounds)
					: base(lowerBounds, upperBounds)
				{
					Data = data;
				}
				public T Data { get; }
				public override T GetData(Vector3 position) => Data;
				public override void ZOrderTraverse(ICollection<T> dataCollection) => dataCollection.Add(Data);
				public override void HilbertCurveTraverse(ICollection<T> dataCollection, int parentOrientation) => dataCollection.Add(Data);

				public override void DrawHilbertCurve(int parentOrientation)
				{
					Vector3 center = Utilities.Functions.GetCenterOfLineSegment(ref lowerBounds, ref upperBounds);
					GL.Vertex3(center);
				}
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
			//octree = new Octree<Node>(mesh.LowerBound, mesh.UpperBound);

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

			createSpaceFillingNodeSequence();

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

		private void createSpaceFillingNodeSequence()
		{
			// Octree-based traversal: Z-order curve
			//spaceFillingNodeSequenceIndices = octree.ZOrderCurveTraverse().Select((node, index) => new KeyValuePair<Node, int>(node, index)).ToDictionary(pair => pair.Key, pair => pair.Value);

			// Octree-based traversal: Hilbert curve
			spaceFillingNodeSequenceIndices = octree.HilbertCurveTraverse().Select((node, index) => new KeyValuePair<Node, int>(node, index)).ToDictionary(pair => pair.Key, pair => pair.Value);

			// shortest neighbor traversal
			//spaceFillingNodeSequenceIndices = new Dictionary<Node, int>();
			//HashSet<Node> restOfNodes = new HashSet<Node>(nodeIndexMap.Values);
			//Node currentNode = null;
			//while (restOfNodes.Count > 0)
			//{
			//	Node closestNode = null;
			//	if (currentNode != null) // non-first iteration
			//	{
			//		float minDistance = float.MaxValue;
			//		foreach (Node neighbor in restOfNodes)
			//		{
			//			float distance = (neighbor.Position - currentNode.Position).Length;
			//			if (distance < minDistance)
			//			{
			//				closestNode = neighbor;
			//				minDistance = distance;
			//			}
			//		}
			//	}
			//	else // first iteration
			//	{
			//		closestNode = nodeIndexMap.Values.First();
			//	}
			//	Debug.Assert(closestNode != null);
			//	restOfNodes.Remove(closestNode);
			//	spaceFillingNodeSequenceIndices[closestNode] = spaceFillingNodeSequenceIndices.Count;
			//	currentNode = closestNode;
			//}

			// original node positions
			//spaceFillingNodeSequenceIndices = nodeIndexMap.Values.Select((node, index) => new KeyValuePair<Node, int>(node, index)).ToDictionary(pair => pair.Key, pair => pair.Value);

			// random node positions
			//List<Node> allNodes = new List<Node>(nodeIndexMap.Values);
			//spaceFillingNodeSequenceIndices = randomizeList(allNodes).Select((node, index) => new KeyValuePair<Node, int>(node, index)).ToDictionary(pair => pair.Key, pair => pair.Value);
		}

		private static List<T> randomizeList<T>(List<T> list)
		{
			List<T> randomizedList = new List<T>();
			Random rnd = new Random();
			while (list.Count > 0)
			{
				int index = rnd.Next(0, list.Count); //pick a random item from the master list
				randomizedList.Add(list[index]); //place it at the end of the randomized list
				list.RemoveAt(index);
			}
			return randomizedList;
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

				GL.Color3(1f, 0f, 0f);
				//octree.Draw();

				if (spaceFillingNodeSequenceIndices != null)
				{
					GL.Begin(BeginMode.LineStrip);
					{
						foreach (Node node in spaceFillingNodeSequenceIndices.OrderBy(pair => pair.Value).Select(pair => pair.Key))
							GL.Vertex3(node.Position.X, node.Position.Y, node.Position.Z);
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
		private const int IterationsCount = 2;

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
			//for (int i = usableLength; i < scaledInput.Length; i++)
			//{
			//	scaledInput[i] = 0.0;
			//}
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

		#endregion

	}
}

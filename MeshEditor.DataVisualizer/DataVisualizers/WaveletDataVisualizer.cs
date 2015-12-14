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
using OpenTK;

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
				// TODO: make a cube
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

			abstract class OctreeNode<T> where T : class, IItemWithSignificantPoint
			{
				public abstract T GetData(Vector3 position, Vector3 lowerBounds, Vector3 upperBounds);
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
			}

			class LeafNode<T> : OctreeNode<T> where T : class, IItemWithSignificantPoint
			{
				public T Data { get; }
				public LeafNode(T data)
				{
					Data = data;
				}
				public override T GetData(Vector3 position, Vector3 lowerBounds, Vector3 upperBounds) => Data;
			}
		}

		#endregion

		#region Fields

		Octree<Node> octree;

		#endregion

		#region Overrides

		public override void Initialize(Mesh mesh)
		{
			base.Initialize(mesh);
			octree = new Octree<Node>(mesh.LowerBound, mesh.UpperBound); // TODO: extend to cube or brick with side 2^n
		}

		public override void LoadData(IApproximationParameters approximationParameters, string[] filenames, LongOpNotifier longOpNotifier)
		{
			base.LoadData(approximationParameters, filenames, longOpNotifier);

			foreach (Node node in nodeIndexMap.Values)
			{
				octree.Insert(node);
			}
		}

		#endregion

		#region Private methods



		#endregion

	}
}

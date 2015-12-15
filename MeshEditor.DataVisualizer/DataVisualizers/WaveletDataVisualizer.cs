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
			}

			class LeafNode<T> : OctreeNode<T> where T : class, IItemWithSignificantPoint
			{
				public T Data { get; }
				public LeafNode(T data)
				{
					Data = data;
				}
				public override T GetData(Vector3 position, Vector3 lowerBounds, Vector3 upperBounds) => Data;
				public override void ZOrderTraverse(ICollection<T> dataCollection)
				{
					dataCollection.Add(Data);
				}
			}
		}

		#endregion

		#region Fields

		Octree<Node> octree;
		IEnumerable<Node> spaceFillingNodeSequence;

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
		}

		public override void LoadData(IApproximationParameters approximationParameters, string[] filenames, LongOpNotifier longOpNotifier)
		{
			base.LoadData(approximationParameters, filenames, longOpNotifier);

			foreach (Node node in nodeIndexMap.Values)
			{
				octree.Insert(node);
			}

			spaceFillingNodeSequence = octree.Traverse();
		}

		public override double GetDataValue(Node node, DataIndex dataIndex)
		{
			throw new NotImplementedException();
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

				octree.DrawBoundary();

				if (spaceFillingNodeSequence != null)
				{
					GL.Color3(1f, 0f, 0f);
					GL.Begin(BeginMode.LineStrip);
					{
						Node firstNode = spaceFillingNodeSequence.First();
						GL.Vertex3(firstNode.Position.X, firstNode.Position.Y, firstNode.Position.Z);
						foreach (Node node in spaceFillingNodeSequence.Skip(1))
						{
							GL.Vertex3(node.Position.X, node.Position.Y, node.Position.Z);
						}
					}
					GL.End();
				}

				if (lightEnabled)
					GL.Enable(EnableCap.Lighting);
			}
		}

		#endregion

		#region Private methods



		#endregion

	}
}

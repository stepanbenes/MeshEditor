using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.DataVisualizer
{
	class HilbertOctree
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

		readonly OctreeNode root;
		readonly int level;

		public HilbertOctree(Vector3 lowerBound, Vector3 upperBound)
		{
			root = new OctreeNode(lowerBound, upperBound);
			level = 5;
			root.Split(level);
		}

		public void Draw()
		{
			GL.Begin(BeginMode.LineStrip);
			{
				root.Draw(level, parentOrientation: 0);
			}
			GL.End();
		}

		#region Octree nodes

		class OctreeNode
		{
			Vector3 lowerBound, upperBound;
			OctreeNode[] children;
			public OctreeNode(Vector3 lowerBound, Vector3 upperBound)
			{
				this.lowerBound = lowerBound;
				this.upperBound = upperBound;
			}

			static Random random = new Random();

			public void Split(int level)
			{
				if (level <= 0)
					return;
				if (children == null)
				{
					children = new OctreeNode[8];
					for (int i = 0; i < 8; i++)
					{
						Vector3 childLowerBounds, childUpperBounds;
						getChildBounds(i, out childLowerBounds, out childUpperBounds);
						children[i] = new OctreeNode(childLowerBounds, childUpperBounds);
						children[i].Split(level - 1);
					}
				}
			}

			private void getChildBounds(int childIndex, out Vector3 childLowerBounds, out Vector3 childUpperBounds)
			{
				Vector3 center = Utilities.Functions.GetCenterOfLineSegment(ref lowerBound, ref upperBound);
				if ((childIndex & 1) > 0)
				{
					childLowerBounds.X = center.X;
					childUpperBounds.X = upperBound.X;
				}
				else
				{
					childLowerBounds.X = lowerBound.X;
					childUpperBounds.X = center.X;
				}

				if ((childIndex & 2) > 0)
				{
					childLowerBounds.Y = center.Y;
					childUpperBounds.Y = upperBound.Y;
				}
				else
				{
					childLowerBounds.Y = lowerBound.Y;
					childUpperBounds.Y = center.Y;
				}

				if ((childIndex & 4) > 0)
				{
					childLowerBounds.Z = center.Z;
					childUpperBounds.Z = upperBound.Z;
				}
				else
				{
					childLowerBounds.Z = lowerBound.Z;
					childUpperBounds.Z = center.Z;
				}
			}

			public void Draw(int level, int parentOrientation)
			{
				if (children != null)
				{
					for (int i = 0; i < 8; i++)
					{
						int order = orderingTable[parentOrientation, i];
						children[order].Draw(level - 1, orientationTable[parentOrientation, i]);
					}
					return;
				}

				Vector3 center = Utilities.Functions.GetCenterOfLineSegment(ref lowerBound, ref upperBound);
				GL.Vertex3(center);

				//GL.Vertex3(upperBound.X, upperBound.Y, upperBound.Z);
				//GL.Vertex3(upperBound.X, upperBound.Y, lowerBound.Z);

				//GL.Vertex3(upperBound.X, upperBound.Y, lowerBound.Z);
				//GL.Vertex3(lowerBound.X, upperBound.Y, lowerBound.Z);

				//GL.Vertex3(lowerBound.X, upperBound.Y, lowerBound.Z);
				//GL.Vertex3(lowerBound.X, upperBound.Y, upperBound.Z);

				//GL.Vertex3(lowerBound.X, upperBound.Y, upperBound.Z);
				//GL.Vertex3(upperBound.X, upperBound.Y, upperBound.Z);


				//GL.Vertex3(upperBound.X, lowerBound.Y, upperBound.Z);
				//GL.Vertex3(upperBound.X, lowerBound.Y, lowerBound.Z);

				//GL.Vertex3(upperBound.X, lowerBound.Y, lowerBound.Z);
				//GL.Vertex3(lowerBound.X, lowerBound.Y, lowerBound.Z);

				//GL.Vertex3(lowerBound.X, lowerBound.Y, lowerBound.Z);
				//GL.Vertex3(lowerBound.X, lowerBound.Y, upperBound.Z);

				//GL.Vertex3(lowerBound.X, lowerBound.Y, upperBound.Z);
				//GL.Vertex3(upperBound.X, lowerBound.Y, upperBound.Z);


				//GL.Vertex3(upperBound.X, upperBound.Y, upperBound.Z);
				//GL.Vertex3(upperBound.X, lowerBound.Y, upperBound.Z);

				//GL.Vertex3(upperBound.X, upperBound.Y, lowerBound.Z);
				//GL.Vertex3(upperBound.X, lowerBound.Y, lowerBound.Z);

				//GL.Vertex3(lowerBound.X, upperBound.Y, lowerBound.Z);
				//GL.Vertex3(lowerBound.X, lowerBound.Y, lowerBound.Z);

				//GL.Vertex3(lowerBound.X, upperBound.Y, upperBound.Z);
				//GL.Vertex3(lowerBound.X, lowerBound.Y, upperBound.Z);
			}
		}

		#endregion

	}
}

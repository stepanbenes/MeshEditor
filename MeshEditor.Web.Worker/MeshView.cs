using System;
using System.Collections.Generic;
using System.Linq;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.Web.Worker
{
	/// <summary>
	/// Geometry of a layer mesh prepared for three.js: triangles (three corners each, not shared, so that cell data can be drawn per cell)
	/// and line segments of the cell edges. Volume cells are drawn by their boundary faces, 2D cells as they are, 1D cells as lines.
	/// Each corner remembers its point, cell and cell point (position in the cell connectivity), so that data of any location can be mapped to it.
	/// </summary>
	sealed class MeshView
	{
		// faces of volume cells by their corner nodes (VTK node ordering)
		private static readonly int[][] TetraFaces = { new[] { 0, 2, 1 }, new[] { 0, 1, 3 }, new[] { 1, 2, 3 }, new[] { 0, 3, 2 } };
		private static readonly int[][] WedgeFaces = { new[] { 0, 1, 2 }, new[] { 3, 5, 4 }, new[] { 0, 3, 4, 1 }, new[] { 1, 4, 5, 2 }, new[] { 2, 5, 3, 0 } };
		private static readonly int[][] HexaFaces = { new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 }, new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 } };

		public float[] Positions { get; private set; }     // xyz per triangle corner
		public int[] CornerPoints { get; private set; }
		public int[] CornerCells { get; private set; }
		public int[] CornerCellPoints { get; private set; }
		public float[] EdgePositions { get; private set; } // xyz per line segment end
		public int[] EdgePoints { get; private set; }
		public int NumberOfPoints { get; private set; }
		public int NumberOfCells { get; private set; }
		public int[] CellOffsets { get; private set; }     // start of each cell in the connectivity
		public int[] CellConnectivity { get; private set; }
		public float[] BoundsMin { get; private set; }
		public float[] BoundsMax { get; private set; }

		public static MeshView Build(GeometryDescription geometry)
		{
			var view = new MeshView();
			int dimension = geometry.NumberOfCoordinateComponents;
			float[] coordinates = geometry.PointCoordinates;
			int numberOfCells = geometry.NumberOfCells;

			// connectivity offsets are the ends of the cells (VTK)
			var starts = new int[numberOfCells];
			for (int c = 1; c < numberOfCells; c++)
				starts[c] = geometry.CellOffsets[c - 1];

			var corners = new List<(int cell, int local)>();     // triangle corners
			var edges = new HashSet<(int, int)>();                // by point indices
			var faces = new Dictionary<FaceKey, (int cell, int[] locals, int count)>();

			for (int c = 0; c < numberOfCells; c++)
			{
				CellType cellType = geometry.CellTypes[c];
				switch (GeometryDescription.GetDimensionOfCellType(cellType))
				{
					case 1:
						addEdge(c, 0, 1);
						break;
					case 2:
						int[] polygon = cornerCount(cellType) == 3 ? new[] { 0, 1, 2 } : new[] { 0, 1, 2, 3 };
						addPolygon(c, polygon);
						break;
					case 3:
						foreach (int[] face in facesOf(cellType))
						{
							var key = FaceKey.Create(face.Select(local => point(c, local)));
							faces[key] = faces.TryGetValue(key, out var existing) ? (existing.cell, existing.locals, existing.count + 1) : (c, face, 1);
						}
						break;
				}
			}
			foreach (var (cell, locals, count) in faces.Values)
			{
				if (count == 1) // boundary face: no other cell shares it
					addPolygon(cell, locals);
			}

			view.NumberOfPoints = geometry.NumberOfPoints;
			view.NumberOfCells = numberOfCells;
			view.CellOffsets = starts;
			view.CellConnectivity = geometry.CellConnectivity;
			view.CornerCells = corners.Select(x => x.cell).ToArray();
			view.CornerCellPoints = corners.Select(x => starts[x.cell] + x.local).ToArray();
			view.CornerPoints = view.CornerCellPoints.Select(i => geometry.CellConnectivity[i]).ToArray();
			view.Positions = new float[corners.Count * 3];
			for (int i = 0; i < view.CornerPoints.Length; i++)
				copyPoint(view.CornerPoints[i], view.Positions, i);
			view.EdgePoints = edges.SelectMany(e => new[] { e.Item1, e.Item2 }).ToArray();
			view.EdgePositions = new float[view.EdgePoints.Length * 3];
			for (int i = 0; i < view.EdgePoints.Length; i++)
				copyPoint(view.EdgePoints[i], view.EdgePositions, i);

			view.BoundsMin = new[] { float.MaxValue, float.MaxValue, float.MaxValue };
			view.BoundsMax = new[] { float.MinValue, float.MinValue, float.MinValue };
			for (int i = 0; i < view.Positions.Length; i++)
			{
				view.BoundsMin[i % 3] = Math.Min(view.BoundsMin[i % 3], view.Positions[i]);
				view.BoundsMax[i % 3] = Math.Max(view.BoundsMax[i % 3], view.Positions[i]);
			}
			for (int i = 0; i < view.EdgePositions.Length; i++)
			{
				view.BoundsMin[i % 3] = Math.Min(view.BoundsMin[i % 3], view.EdgePositions[i]);
				view.BoundsMax[i % 3] = Math.Max(view.BoundsMax[i % 3], view.EdgePositions[i]);
			}
			if (view.BoundsMin[0] > view.BoundsMax[0])
			{
				view.BoundsMin = new float[3];
				view.BoundsMax = new float[3];
			}
			return view;

			int point(int cell, int local) => geometry.CellConnectivity[starts[cell] + local];

			void addEdge(int cell, int localA, int localB)
			{
				int a = point(cell, localA), b = point(cell, localB);
				if (a != b)
					edges.Add(a < b ? (a, b) : (b, a));
			}

			void addPolygon(int cell, int[] locals)
			{
				for (int i = 1; i + 1 < locals.Length; i++) // fan
				{
					corners.Add((cell, locals[0]));
					corners.Add((cell, locals[i]));
					corners.Add((cell, locals[i + 1]));
				}
				for (int i = 0; i < locals.Length; i++)
					addEdge(cell, locals[i], locals[(i + 1) % locals.Length]);
			}

			void copyPoint(int pointIndex, float[] target, int targetIndex)
			{
				for (int k = 0; k < 3; k++)
					target[targetIndex * 3 + k] = k < dimension ? coordinates[pointIndex * dimension + k] : 0f;
			}
		}

		/// <summary>
		/// Value of one data component for each triangle corner.
		/// </summary>
		public float[] MapToCorners(DataLocationType location, Func<int, double> valueAt)
		{
			int[] indices = indicesFor(location, CornerPoints, CornerCells, CornerCellPoints);
			var result = new float[indices.Length];
			for (int i = 0; i < indices.Length; i++)
				result[i] = (float)valueAt(indices[i]);
			return result;
		}

		/// <summary>
		/// Vector per point, from vector data of any location (cell point and cell values are spread to the points of the cells).
		/// </summary>
		public float[] VectorsAtPoints(DataLocationType location, IReadOnlyList<double[]> components)
		{
			var result = new float[NumberOfPoints * 3];
			int count = Math.Min(3, components.Count);
			switch (location)
			{
				case DataLocationType.Points:
					for (int p = 0; p < NumberOfPoints; p++)
						for (int k = 0; k < count; k++)
							result[p * 3 + k] = (float)components[k][p];
					break;
				case DataLocationType.CellPoints:
					for (int i = 0; i < CellConnectivity.Length; i++)
						for (int k = 0; k < count; k++)
							result[CellConnectivity[i] * 3 + k] = (float)components[k][i];
					break;
				case DataLocationType.Cells:
					for (int c = 0; c < NumberOfCells; c++)
					{
						int end = c + 1 < NumberOfCells ? CellOffsets[c + 1] : CellConnectivity.Length;
						for (int i = CellOffsets[c]; i < end; i++)
							for (int k = 0; k < count; k++)
								result[CellConnectivity[i] * 3 + k] = (float)components[k][c];
					}
					break;
			}
			return result;
		}

		/// <summary>
		/// Per-point vectors picked for the triangle corners and the edge ends: xyz per corner, then xyz per edge end.
		/// </summary>
		public (float[] corners, float[] edges) PickVectors(float[] vectorsAtPoints)
		{
			return (pick(CornerPoints), pick(EdgePoints));

			float[] pick(int[] points)
			{
				var result = new float[points.Length * 3];
				for (int i = 0; i < points.Length; i++)
					for (int k = 0; k < 3; k++)
						result[i * 3 + k] = vectorsAtPoints[points[i] * 3 + k];
				return result;
			}
		}

		private static int[] indicesFor(DataLocationType location, int[] points, int[] cells, int[] cellPoints)
		{
			switch (location)
			{
				case DataLocationType.Points: return points;
				case DataLocationType.Cells: return cells;
				case DataLocationType.CellPoints: return cellPoints;
				default: throw new NotSupportedException($"Data location {location} is not supported.");
			}
		}

		private static int cornerCount(CellType cellType)
		{
			switch (cellType)
			{
				case CellType.TriangleLinear:
				case CellType.TriangleQuadratic:
					return 3;
				default:
					return 4;
			}
		}

		private static int[][] facesOf(CellType cellType)
		{
			switch (cellType)
			{
				case CellType.TetraLinear:
				case CellType.TetraQuadratic:
					return TetraFaces;
				case CellType.WedgeLinear:
				case CellType.WedgeQuadratic:
					return WedgeFaces;
				case CellType.HexaLinear:
				case CellType.HexaQuadratic:
					return HexaFaces;
				default:
					throw new NotSupportedException($"Cell type {cellType} is not supported.");
			}
		}

		/// <summary>
		/// Face identified by its sorted corner points (triangle: D is -1).
		/// </summary>
		private readonly record struct FaceKey(int A, int B, int C, int D)
		{
			public static FaceKey Create(IEnumerable<int> points)
			{
				int[] sorted = points.OrderBy(p => p).ToArray();
				return sorted.Length == 3 ? new FaceKey(sorted[0], sorted[1], sorted[2], -1) : new FaceKey(sorted[0], sorted[1], sorted[2], sorted[3]);
			}
		}
	}
}

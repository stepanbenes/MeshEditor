using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using System.IO;

namespace MeshEditor.IO
{
	public class VTKFileFormatSaver : IMeshSaver
	{

		public enum VTKFileFormats
		{
			SimpleASCII,
			SimpleBinary,
			XML
		}

		public enum VTKCellTypes
		{
			Unknown = 0,
			VTK_VERTEX = 1,
			VTK_POLY_VERTEX,
			VTK_LINE,
			VTK_POLY_LINE,
			VTK_TRIANGLE,
			VTK_TRIANGLE_STRIP,
			VTK_POLYGON,
			VTK_PIXEL,
			VTK_QUAD,
			VTK_TETRA,
			VTK_VOXEL,
			VTK_HEXAHEDRON,
			VTK_WEDGE,
			VTK_PYRAMID,
			VTK_QUADRATIC_EDGE = 21,
			VTK_QUADRATIC_TRIANGLE,
			VTK_QUADRATIC_QUAD,
			VTK_QUADRATIC_TETRA,
			VTK_QUADRATIC_HEXAHEDRON
		}

		#region Fields, Constructor

		private readonly VTKFileFormats outputFileFormat;
		private MeshIOEventArgs ioea;
		private TextWriter output;

		public VTKFileFormatSaver()
			: this(VTKFileFormats.SimpleASCII)
		{ }

		public VTKFileFormatSaver(VTKFileFormats outputFileFormat)
		{
			this.outputFileFormat = outputFileFormat;
		}

		#endregion

		#region IMeshSaver Members

		public void SaveMesh(Mesh mesh, string filename, bool saveWithoutCuttedElements, YesNoQuestion cancelled)
		{
			throw new NotImplementedException();
		}

		public void SaveMesh(IMeshFileParser fileParser, string destination, YesNoQuestion cancelled)
		{
			if (outputFileFormat != VTKFileFormats.SimpleASCII)
				throw new NotSupportedException(outputFileFormat.ToString() + " file format is not supported.");

			ioea = new MeshIOEventArgs(0);
			// TODO: implement SimpleBinary and XML formats
			// TODO: report progress

			// === VTKFileFormats.SimpleASCII ===

			using (output = new StreamWriter(destination))
			{
				// header
				output.WriteLine("# vtk DataFile Version 2.0");
				output.WriteLine(Path.GetFileNameWithoutExtension(fileParser.Filename));
				output.WriteLine("ASCII");
				output.WriteLine();

				// unstructured grid - POINTS
				output.WriteLine("DATASET UNSTRUCTURED_GRID");
				output.WriteLine(string.Format("POINTS {0} float", fileParser.NodeCount));

				Dictionary<int, int> nodeIndexMap = new Dictionary<int, int>();
				int index = 0;
				foreach (Node node in fileParser.ReadNodes())
				{
					nodeIndexMap[node.ID] = index++;
					output.WriteLine(string.Format("{0} {1} {2}", node.Position.X.ToString(CultureProvider.EnglishCulture), node.Position.Y.ToString(CultureProvider.EnglishCulture), node.Position.Z.ToString(CultureProvider.EnglishCulture)));
				}

				output.WriteLine();

				List<ElementDraft> elements = new List<ElementDraft>(fileParser.ElementCount);
				List<VTKCellTypes> cellTypes = new List<VTKCellTypes>(fileParser.ElementCount);
				int size = 0;
				foreach (ElementDraft element in fileParser.ReadElements())
				{
					size += 1 + Element.MapElementTypeToNodeCount(element.Type);
					VTKCellTypes cellType = convertElementTypeToVTKCellType(element.Type);
					cellTypes.Add(cellType);
					elements.Add(element);
				}

				// unstructured grid - CELLS
				output.WriteLine(string.Format("CELLS {0} {1}", elements.Count, size));
				foreach (ElementDraft element in elements)
				{
					output.Write(element.NodeIDs.Length);
					foreach (int nodeID in element.NodeIDs)
					{
						output.Write(' ');
						output.Write(nodeIndexMap[nodeID]);
					}
					output.WriteLine();
				}

				output.WriteLine();

				// unstructured grid - CELL_TYPES
				output.WriteLine(string.Format("CELL_TYPES {0}", cellTypes.Count));
				foreach (VTKCellTypes cellType in cellTypes)
				{
					output.WriteLine((int)cellType);
				}
			}
		}

		#endregion

		#region IProgressNotifier Members

		public event MeshIOEventHandler Step;

		#endregion

		#region Public methods

		#endregion

		#region Private methods

		private VTKCellTypes convertElementTypeToVTKCellType(ElementType elementType)
		{
			switch (elementType)
			{
				case ElementType.BeamLinear:
					return VTKCellTypes.VTK_LINE;
				case ElementType.BeamQuadratic:
					return VTKCellTypes.VTK_QUADRATIC_EDGE;
				case ElementType.TriangleLinear:
					return VTKCellTypes.VTK_TRIANGLE;
				case ElementType.TriangleQuadratic:
					return VTKCellTypes.VTK_QUADRATIC_TRIANGLE;
				case ElementType.QuadLinear:
					return VTKCellTypes.VTK_QUAD;
				case ElementType.QuadQuadratic:
					return VTKCellTypes.VTK_QUADRATIC_QUAD;
				case ElementType.TetrahedronLinear:
					return VTKCellTypes.VTK_TETRA;
				case ElementType.TetrahedronQuadratic:
					return VTKCellTypes.VTK_QUADRATIC_TETRA;
				case ElementType.SquarePyramidLinear:
					return VTKCellTypes.VTK_PYRAMID;
				case ElementType.SquarePyramidQuadratic:
					throw new NotSupportedException("Quadratic pyramid is not supported.");
				case ElementType.TriangularPrismLinear:
					return VTKCellTypes.VTK_WEDGE;
				case ElementType.TriangularPrismQuadratic:
					throw new NotSupportedException("Quadratic wedge is not supported.");
				case ElementType.HexahedronLinear:
					return VTKCellTypes.VTK_HEXAHEDRON;
				case ElementType.HexahedronQuadratic:
					return VTKCellTypes.VTK_QUADRATIC_HEXAHEDRON;
				default:
					throw new NotSupportedException(elementType.ToString() + " is not supported.");
			}
		}

		#endregion

	}
}

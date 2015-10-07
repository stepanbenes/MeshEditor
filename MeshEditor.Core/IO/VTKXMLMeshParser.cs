using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MeshEditor.Data;
using MeshEditor.Utilities;

namespace MeshEditor.IO
{
	/// <summary>
	/// VTK XML mesh definition parser.
	/// Only serial UnstructuredGrid (.vtu) is supported.
	/// </summary>
	public class VTKXmlMeshParser : VTKXmlFileParserBase, IMeshFileParser
	{

		private enum VTKCellType
		{
			Undefined = 0,
			// 0D
			Point = 1,
			// 1D
			LineLinear = 3,
			LineQuadratic = 21,
			// 2D
			TriangleLinear = 5,
			TriangleQuadratic = 22,
			QuadLinear = 9,
			QuadQuadratic = 23,
			// 3D
			TetraLinear = 10,
			TetraQuadratic = 24,
			WedgeLinear = 13,
			WedgeQuadratic = 26,
			HexaLinear = 12,
			HexaQuadratic = 25,
		}

		#region Fields, constructor

		private bool nodesProcessed, elementsProcessed;
		private int numberOfPoints, numberOfCells;

		public VTKXmlMeshParser(string filename)
		: base(filename)
		{ }

		#endregion

		#region IMeshFileParser members

		public int NodeCount
		{
			get
			{
				EnsureInputIsInitialized();
				return numberOfPoints;
			}
		}

		public int ElementCount
		{
			get
			{
				EnsureInputIsInitialized();
				return numberOfCells;
			}
		}

		public IEnumerable<Node> ReadNodes()
		{
			EnsureInputIsInitialized();

			if (nodesProcessed)
			{
				throw new MeshLoadingException("Points were already processed.", CurrentLineNumber);
			}

			if (!Input.ReadToFollowing("Points"))
			{
				ThrowElementIsMissing("Points");
			}

			if (!Input.ReadToDescendant("DataArray"))
			{
				ThrowElementIsMissing("DataArray");
            }

			int numberOfComponents = 0;
			while (Input.MoveToNextAttribute())
			{
				switch (Input.Name.ToLower())
				{
					case "type":
						//if (input.Value.ToLower() != "float64")
						//{
						//	throw new MeshLoadingException($"Float64 coordinates data type was expected instead of '{input.Value}'.", CurrentLineNumber);
						//}
						// ignore type, parse coordinates as Float32 values
						break;
					case "numberofcomponents":
						numberOfComponents = ParseInt32(Input.Value);
						break;
					case "format":
						if (Input.Value.ToLower() != "ascii")
						{
							throw new MeshLoadingException($"Ascii data array format was expected instead of '{Input.Value}'.", CurrentLineNumber);
						}
						break;
				}
			}

			if (numberOfComponents < 2 || numberOfComponents > 3)
			{
				throw new MeshLoadingException($"Unsupported number of components ({numberOfComponents}).", CurrentLineNumber);
			}

			if (!Input.MoveToElement())
			{
				ThrowElementIsMissing("DataArray");
			}

			float[] coordinates = ParseFloat32AsciiDataArray(); // can't handle 64 precission anyway
			{
				int expectedDataArrayLength = numberOfPoints * numberOfComponents;
				if (coordinates.Length != expectedDataArrayLength)
				{
					throw new MeshLoadingException($"Unexpected length of coordinates data array ({coordinates.Length} instead of {expectedDataArrayLength}).", CurrentLineNumber);
				}
			}

			switch (numberOfComponents)
			{
				case 2:
					for (int i = 0; i < numberOfPoints; i++)
					{
						OpenTK.Vector3 position = new OpenTK.Vector3(coordinates[i * numberOfComponents], coordinates[(i * numberOfComponents) + 1], 0f);
						Node node = new Node(i, position, properties: null);
						yield return node;
					}
					break;
				case 3:
					for (int i = 0; i < numberOfPoints; i++)
					{
						OpenTK.Vector3 position = new OpenTK.Vector3(coordinates[i * numberOfComponents], coordinates[(i * numberOfComponents) + 1], coordinates[(i * numberOfComponents) + 2]);
						Node node = new Node(i, position, properties: null);
						yield return node;
					}
					break;
				default:
					throw new NotSupportedException();
			}

			nodesProcessed = true;
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			EnsureInputIsInitialized();

			if (elementsProcessed)
			{
				throw new MeshLoadingException("Cells were already processed.", CurrentLineNumber);
			}

			if (!Input.ReadToFollowing("Cells"))
			{
				ThrowElementIsMissing("Cells");
			}

			// connectivity array
			if (!Input.ReadToDescendant("DataArray"))
			{
				ThrowElementIsMissing("DataArray");
			}

			while (Input.MoveToNextAttribute())
			{
				switch (Input.Name.ToLower())
				{
					case "type":
						if (Input.Value.ToLower() != "int32")
						{
							throw new MeshLoadingException($"Int32 connectivity data type was expected instead of '{Input.Value}'.", CurrentLineNumber);
						}
						break;
					case "name":
						if (Input.Value.ToLower() != "connectivity")
						{
							throw new MeshLoadingException($"Connectivity data array was expected instead of '{Input.Value}'.", CurrentLineNumber);
						}
						break;
					case "format":
						if (Input.Value.ToLower() != "ascii")
						{
							throw new MeshLoadingException($"Ascii data array format was expected instead of '{Input.Value}'.", CurrentLineNumber);
						}
						break;
				}
			}

			if (!Input.MoveToElement())
			{
				ThrowElementIsMissing("DataArray");
			}

			int[] connectivity = ParseInt32AsciiDataArray();

			// offsets array
			if (!Input.ReadToNextSibling("DataArray"))
			{
				ThrowElementIsMissing("DataArray");
			}

			while (Input.MoveToNextAttribute())
			{
				switch (Input.Name.ToLower())
				{
					case "type":
						if (Input.Value.ToLower() != "int32")
						{
							throw new MeshLoadingException($"Int32 offsets data type was expected instead of '{Input.Value}'.", CurrentLineNumber);
						}
						break;
					case "name":
						if (Input.Value.ToLower() != "offsets")
						{
							throw new MeshLoadingException($"Offsets data array was expected instead of '{Input.Value}'.", CurrentLineNumber);
						}
						break;
					case "format":
						if (Input.Value.ToLower() != "ascii")
						{
							throw new MeshLoadingException($"Ascii data array format was expected instead of '{Input.Value}'.", CurrentLineNumber);
						}
						break;
				}
			}

			if (!Input.MoveToElement())
			{
				ThrowElementIsMissing("DataArray");
			}

			int[] offsets = ParseInt32AsciiDataArray();

			if (offsets.Length != numberOfCells)
			{
				throw new MeshLoadingException($"Unexpected length of offsets data array ({offsets.Length} instead of {numberOfCells}).", CurrentLineNumber);
			}

			// types array
			if (!Input.ReadToNextSibling("DataArray"))
			{
				ThrowElementIsMissing("DataArray");
			}

			while (Input.MoveToNextAttribute())
			{
				switch (Input.Name.ToLower())
				{
					case "type":
						//if (input.Value.ToLower() != "uint8")
						//{
						//	throw new MeshLoadingException($"UInt8 offsets data type was expected instead of '{input.Value}'.", CurrentLineNumber);
						//}
						// ignore type, parse coordinates as Float32 values
						break;
					case "name":
						if (Input.Value.ToLower() != "types")
						{
							throw new MeshLoadingException($"Types data array was expected instead of '{Input.Value}'.", CurrentLineNumber);
						}
						break;
					case "format":
						if (Input.Value.ToLower() != "ascii")
						{
							throw new MeshLoadingException($"Ascii data array format was expected instead of '{Input.Value}'.", CurrentLineNumber);
						}
						break;
				}
			}

			if (!Input.MoveToElement())
			{
				ThrowElementIsMissing("DataArray");
			}

			int[] types = ParseInt32AsciiDataArray();

			if (types.Length != numberOfCells)
			{
				throw new MeshLoadingException($"Unexpected length of types data array ({types.Length} instead of {numberOfCells}).", CurrentLineNumber);
			}

			for (int elementIndex = 0, connectivityIndex = 0; elementIndex < types.Length; elementIndex++)
			{
				int numberOfNodes = offsets[elementIndex] - connectivityIndex;
				ElementType? elementType = MapVTKCellTypeToElementType((VTKCellType)types[elementIndex]);
				if (elementType.HasValue) // ignore unsupported cell types (skip them)
				{
					int[] nodeIds = Functions.GetSliceOfArray(connectivity, connectivityIndex, numberOfNodes);
					Debug.Assert(nodeIds.Length == Element.MapElementTypeToNodeCount(elementType.Value));
					yield return new ElementDraft { ID = elementIndex, Type = elementType.Value, NodeIDs = nodeIds };
				}
				connectivityIndex += numberOfNodes;
			}

			elementsProcessed = true;
		}

		#endregion

		#region Private methods

		private void EnsureInputIsInitialized()
		{
			if (!IsInputInitialized)
			{
				string fileType;
				InitInput(out fileType);
				if (fileType?.ToLower() != "unstructuredgrid")
				{
					throw new MeshLoadingException($"VTK file type '{fileType}' is not supported. Only 'UnstructuredGrid' type is supported.", CurrentLineNumber);
				}
				ReadToPieceElement();
			}
			Debug.Assert(IsInputInitialized);
			Debug.Assert(Input != null);
		}

		private void ReadToPieceElement()
		{
			if (!Input.ReadToDescendant("UnstructuredGrid"))
			{
				ThrowElementIsMissing("UnstructuredGrid");
			}

			if (!Input.ReadToDescendant("Piece"))
			{
				ThrowElementIsMissing("Piece");
			}

			while (Input.MoveToNextAttribute())
			{
				switch (Input.Name.ToLower())
				{
					case "numberofpoints":
						numberOfPoints = ParseInt32(Input.Value);
						break;
					case "numberofcells":
						numberOfCells = ParseInt32(Input.Value);
						break;
				}
			}

			if (!Input.MoveToElement())
			{
				ThrowElementIsMissing("Piece");
			}
		}

		private ElementType? MapVTKCellTypeToElementType(VTKCellType vtkCellType)
		{
			switch (vtkCellType)
			{
				case VTKCellType.LineLinear:
					return ElementType.BeamLinear;
				case VTKCellType.LineQuadratic:
					return ElementType.BeamQuadratic;
				case VTKCellType.TriangleLinear:
					return ElementType.TriangleLinear;
				case VTKCellType.TriangleQuadratic:
					return ElementType.TriangleQuadratic;
				case VTKCellType.QuadLinear:
					return ElementType.QuadLinear;
				case VTKCellType.QuadQuadratic:
					return ElementType.QuadQuadratic;
				case VTKCellType.TetraLinear:
					return ElementType.TetrahedronLinear;
				case VTKCellType.TetraQuadratic:
					return ElementType.TetrahedronQuadratic;
				case VTKCellType.WedgeLinear:
					return ElementType.TriangularPrismLinear;
				case VTKCellType.WedgeQuadratic:
					return ElementType.TriangularPrismQuadratic;
				case VTKCellType.HexaLinear:
					return ElementType.HexahedronLinear;
				case VTKCellType.HexaQuadratic:
					return ElementType.HexahedronQuadratic;
				case VTKCellType.Undefined:
				case VTKCellType.Point:
				default:
					return null;
			}
		}

		#endregion

	}
}

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
	public class VTKXMLMeshParser : IMeshFileParser
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

		#region static members

		public static readonly char[] DataArrayValueDelimiters = new[] { ' ', '\t', '\n', '\r' };

		#endregion

		#region Fields, constructor

		private readonly string filename;

		private StreamReader streamReader;
        private XmlReader input;

		private bool nodesProcessed, elementsProcessed;
		private int numberOfPoints, numberOfCells;

		public VTKXMLMeshParser(string filename)
		{
			this.filename = filename;
		}

		#endregion

		#region IMeshFileParser

		public string Filename => filename;

		public int CurrentLineNumber
		{
			get
			{
				IXmlLineInfo xmlInfo = input as IXmlLineInfo;
				if (xmlInfo == null)
				{
					return -1;
				}
				return xmlInfo.LineNumber;
			}
		}

		public int NodeCount
		{
			get
			{
				if (input == null)
				{
					initInput();
				}
				return numberOfPoints;
			}
		}

		public int ElementCount
		{
			get
			{
				if (input == null)
				{
					initInput();
				}
				return numberOfCells;
			}
		}

		public IEnumerable<Node> ReadNodes()
		{
			if (input == null)
			{
				initInput();
			}

			if (nodesProcessed)
			{
				throw new MeshLoadingException("Points were already processed.", CurrentLineNumber);
			}

			if (!input.ReadToFollowing("Points"))
			{
				throwElementIsMissing("Points");
			}

			if (!input.ReadToDescendant("DataArray"))
			{
				throwElementIsMissing("DataArray");
            }

			int numberOfComponents = 0;
			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						//if (input.Value.ToLower() != "float64")
						//{
						//	throw new MeshLoadingException($"Float64 coordinates data type was expected instead of '{input.Value}'.", CurrentLineNumber);
						//}
						// ignore type, parse coordinates as Float32 values
						break;
					case "numberofcomponents":
						numberOfComponents = parseInt32(input.Value);
						break;
					case "format":
						if (input.Value.ToLower() != "ascii")
						{
							throw new MeshLoadingException($"Ascii data array format was expected instead of '{input.Value}'.", CurrentLineNumber);
						}
						break;
				}
			}

			if (numberOfComponents < 2 || numberOfComponents > 3)
			{
				throw new MeshLoadingException($"Unsupported number of components ({numberOfComponents}).", CurrentLineNumber);
			}

			if (!input.MoveToElement())
			{
				throwElementIsMissing("DataArray");
			}

			float[] coordinates = parseFloat32AsciiDataArray(); // can't handle 64 precission anyway
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
			if (input == null)
			{
				initInput();
			}

			if (elementsProcessed)
			{
				throw new MeshLoadingException("Cells were already processed.", CurrentLineNumber);
			}

			if (!input.ReadToFollowing("Cells"))
			{
				throwElementIsMissing("Cells");
			}

			// connectivity array
			if (!input.ReadToDescendant("DataArray"))
			{
				throwElementIsMissing("DataArray");
			}

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						if (input.Value.ToLower() != "int32")
						{
							throw new MeshLoadingException($"Int32 connectivity data type was expected instead of '{input.Value}'.", CurrentLineNumber);
						}
						break;
					case "name":
						if (input.Value.ToLower() != "connectivity")
						{
							throw new MeshLoadingException($"Connectivity data array was expected instead of '{input.Value}'.", CurrentLineNumber);
						}
						break;
					case "format":
						if (input.Value.ToLower() != "ascii")
						{
							throw new MeshLoadingException($"Ascii data array format was expected instead of '{input.Value}'.", CurrentLineNumber);
						}
						break;
				}
			}

			if (!input.MoveToElement())
			{
				throwElementIsMissing("DataArray");
			}

			int[] connectivity = parseInt32AsciiDataArray();

			// offsets array
			if (!input.ReadToNextSibling("DataArray"))
			{
				throwElementIsMissing("DataArray");
			}

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						if (input.Value.ToLower() != "int32")
						{
							throw new MeshLoadingException($"Int32 offsets data type was expected instead of '{input.Value}'.", CurrentLineNumber);
						}
						break;
					case "name":
						if (input.Value.ToLower() != "offsets")
						{
							throw new MeshLoadingException($"Offsets data array was expected instead of '{input.Value}'.", CurrentLineNumber);
						}
						break;
					case "format":
						if (input.Value.ToLower() != "ascii")
						{
							throw new MeshLoadingException($"Ascii data array format was expected instead of '{input.Value}'.", CurrentLineNumber);
						}
						break;
				}
			}

			if (!input.MoveToElement())
			{
				throwElementIsMissing("DataArray");
			}

			int[] offsets = parseInt32AsciiDataArray();

			if (offsets.Length != numberOfCells)
			{
				throw new MeshLoadingException($"Unexpected length of offsets data array ({offsets.Length} instead of {numberOfCells}).", CurrentLineNumber);
			}

			// types array
			if (!input.ReadToNextSibling("DataArray"))
			{
				throwElementIsMissing("DataArray");
			}

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						//if (input.Value.ToLower() != "uint8")
						//{
						//	throw new MeshLoadingException($"UInt8 offsets data type was expected instead of '{input.Value}'.", CurrentLineNumber);
						//}
						// ignore type, parse coordinates as Float32 values
						break;
					case "name":
						if (input.Value.ToLower() != "types")
						{
							throw new MeshLoadingException($"Types data array was expected instead of '{input.Value}'.", CurrentLineNumber);
						}
						break;
					case "format":
						if (input.Value.ToLower() != "ascii")
						{
							throw new MeshLoadingException($"Ascii data array format was expected instead of '{input.Value}'.", CurrentLineNumber);
						}
						break;
				}
			}

			if (!input.MoveToElement())
			{
				throwElementIsMissing("DataArray");
			}

			int[] types = parseInt32AsciiDataArray();

			if (types.Length != numberOfCells)
			{
				throw new MeshLoadingException($"Unexpected length of types data array ({types.Length} instead of {numberOfCells}).", CurrentLineNumber);
			}

			for (int elementIndex = 0, connectivityIndex = 0; elementIndex < types.Length; elementIndex++)
			{
				int numberOfNodes = offsets[elementIndex] - connectivityIndex;
				ElementType? elementType = mapVTKCellTypeToElementType((VTKCellType)types[elementIndex]);
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

		#region IDisposable Support

		public void Dispose()
		{
			if (streamReader != null)
			{
				streamReader.Dispose();
				streamReader = null;
			}
			if (input != null)
			{
				((IDisposable)input).Dispose();
				input = null;
			}
		}

		#endregion

		#region Private methods

		private void initInput()
		{
			if (!File.Exists(filename))
			{
				throw new MeshLoadingException($"Mesh file can't be found. ({filename})");
			}

			streamReader = new StreamReader(filename);
            input = XmlReader.Create(streamReader);

			validateVTKFileType();

			if (!input.ReadToDescendant("UnstructuredGrid"))
			{
				throwElementIsMissing("UnstructuredGrid");
			}

			if (!input.ReadToDescendant("Piece"))
			{
				throwElementIsMissing("Piece");
			}

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "numberofpoints":
						numberOfPoints = parseInt32(input.Value);
						break;
					case "numberofcells":
						numberOfCells = parseInt32(input.Value);
						break;
				}
			}

			if (!input.MoveToElement())
			{
				throwElementIsMissing("Piece");
			}
		}

		private void validateVTKFileType()
		{
			if (!input.ReadToDescendant("VTKFile"))
			{
				throwElementIsMissing("VTKFile");
			}

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						if (input.Value.ToLower() != "unstructuredgrid")
						{
							throw new MeshLoadingException($"Type '{input.Value}' is not supported. Only 'UnstructuredGrid' is supported.", CurrentLineNumber);
						}
						break;
				}
			}

			if (!input.MoveToElement())
			{
				throwElementIsMissing("VTKFile");
			}
		}

		private void throwElementIsMissing(string elementName)
		{
			throw new MeshLoadingException($"{elementName} element was not found.");
		}

		private double[] parseFloat64AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			// TODO: for binary format use: input.ReadElementContentAsBase64(...)
			string[] parts = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			double[] result = new double[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = parseFloat64(parts[i]);
			}
			return result;
		}

		private float[] parseFloat32AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			string[] parts = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			float[] result = new float[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = parseFloat32(parts[i]);
			}
			return result;
		}

		private int[] parseInt32AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			string[] parts = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			int[] result = new int[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = parseInt32(parts[i]);
			}
			return result;
		}

		private byte[] parseUInt8AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			string[] parts = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			byte[] result = new byte[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = parseUInt8(parts[i]);
			}
			return result;
		}

		private int parseInt32(string text)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new MeshLoadingException($"32bit integer expected instead of '{text}'", CurrentLineNumber);
			}
			return result;
		}

		private byte parseUInt8(string text)
		{
			byte result;
			if (!byte.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new MeshLoadingException($"Unsigned 8bit integer expected instead of '{text}'", CurrentLineNumber);
			}
			return result;
		}

		private double parseFloat64(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new MeshLoadingException($"Floating-point number expected instead of '{text}'", CurrentLineNumber);
			}
			return result;
		}

		private float parseFloat32(string text)
		{
			float result;
			if (!float.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new MeshLoadingException($"Floating-point number expected instead of '{text}'", CurrentLineNumber);
			}
			return result;
		}

		private ElementType? mapVTKCellTypeToElementType(VTKCellType vtkCellType)
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

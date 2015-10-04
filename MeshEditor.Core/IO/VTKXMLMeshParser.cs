using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MeshEditor.Data;

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

		private static readonly char[] dataArrayValueDelimiters = new[] { ' ', '\t', '\n', '\r' };

		#endregion

		#region Fields, constructor

		private string filename;
		private int currentLineNumber;

		private XmlReader input;

		private bool nodesProcessed, elementsProcessed;
		private int nodeCount, elementCount;

		public VTKXMLMeshParser(string filename)
		{
			this.filename = filename;
			currentLineNumber = -1;
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
				return nodeCount;
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
				return elementCount;
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

			input.ReadToFollowing("Points");
			input.ReadToDescendant("DataArray");
			string attribute;
			//attribute = input.GetAttribute("type");
			//if (attribute != "Float64")
			//{
			//	throw new MeshLoadingException($"Float64 point coordinate type was expected instead of '{attribute}'.", CurrentLineNumber);
			//}
			attribute = input.GetAttribute("NumberOfComponents");
			int numberOfComponents = parseInt32(attribute);
			if (numberOfComponents < 2 || numberOfComponents > 3)
			{
				throw new MeshLoadingException($"Unsupported number of components '{attribute}'.", CurrentLineNumber);
			}
			attribute = input.GetAttribute("format");
			if (attribute != "ascii")
			{
				throw new MeshLoadingException($"Ascii data array format was expected instead of '{attribute}'.", CurrentLineNumber);
			}

			float[] coordinates = parseFloat32AsciiDataArray(); // can't handle 64 precission anyway
			{
				int expectedDataArrayLength = nodeCount * numberOfComponents;
				if (coordinates.Length != expectedDataArrayLength)
				{
					throw new MeshLoadingException($"Unexpected length of coordinates data array ({coordinates.Length} instead of {expectedDataArrayLength}).", CurrentLineNumber);
				}
			}

			switch (numberOfComponents)
			{
				case 2:
					for (int i = 0; i < nodeCount; i++)
					{
						OpenTK.Vector3 position = new OpenTK.Vector3(coordinates[i * numberOfComponents], coordinates[(i * numberOfComponents) + 1], 0f);
						Node node = new Node(i, position, properties: null);
						yield return node;
					}
					break;
				case 3:
					for (int i = 0; i < nodeCount; i++)
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

			input.ReadToFollowing("Cells");

			// connectivity array
			input.ReadToDescendant("DataArray");
			string attribute = input.GetAttribute("type");
			if (attribute != "Int32")
			{
				throw new MeshLoadingException($"Int32 connectivity data type was expected instead of '{attribute}'.", CurrentLineNumber);
			}
			attribute = input.GetAttribute("Name");
			if (attribute != "connectivity")
			{
				throw new MeshLoadingException($"Connectivity data array was expected instead of '{attribute}'.", CurrentLineNumber);
			}
			attribute = input.GetAttribute("format");
			if (attribute != "ascii")
			{
				throw new MeshLoadingException($"Ascii data array format was expected instead of '{attribute}'.", CurrentLineNumber);
			}

			int[] connectivity = parseInt32AsciiDataArray();

			// offsets array
			input.ReadToNextSibling("DataArray");
			attribute = input.GetAttribute("type");
			if (attribute != "Int32")
			{
				throw new MeshLoadingException($"Int32 offset data type was expected instead of '{attribute}'.", CurrentLineNumber);
			}
			attribute = input.GetAttribute("Name");
			if (attribute != "offsets")
			{
				throw new MeshLoadingException($"Offsets data array was expected instead of '{attribute}'.", CurrentLineNumber);
			}
			attribute = input.GetAttribute("format");
			if (attribute != "ascii")
			{
				throw new MeshLoadingException($"Ascii data array format was expected instead of '{attribute}'.", CurrentLineNumber);
			}

			int[] offsets = parseInt32AsciiDataArray();
			if (offsets.Length != elementCount)
			{
				throw new MeshLoadingException($"Unexpected length of offsets data array ({offsets.Length} instead of {elementCount}).", CurrentLineNumber);
			}

			// types array
			input.ReadToNextSibling("DataArray");
			attribute = input.GetAttribute("type");
			if (attribute != "UInt8")
			{
				throw new MeshLoadingException($"UInt8 data type was expected instead of '{attribute}'.", CurrentLineNumber);
			}
			attribute = input.GetAttribute("Name");
			if (attribute != "types")
			{
				throw new MeshLoadingException($"Offsets data array was expected instead of '{attribute}'.", CurrentLineNumber);
			}
			attribute = input.GetAttribute("format");
			if (attribute != "ascii")
			{
				throw new MeshLoadingException($"Ascii data array format was expected instead of '{attribute}'.", CurrentLineNumber);
			}

			byte[] types = parseUInt8AsciiDataArray();
			if (types.Length != elementCount)
			{
				throw new MeshLoadingException($"Unexpected length of types data array ({types.Length} instead of {elementCount}).", CurrentLineNumber);
			}

			for (int elementIndex = 0, connectivityIndex = 0; elementIndex < types.Length; elementIndex++)
			{
				ElementType elementType = mapVTKCellTypeToElementType((VTKCellType)types[elementIndex]);
				int nodeCount = offsets[elementIndex] - connectivityIndex;
                int[] nodeIds = getSliceOfArray(connectivity, connectivityIndex, nodeCount);

				Debug.Assert(nodeIds.Length == Element.MapElementTypeToNodeCount(elementType));

				yield return new ElementDraft { ID = elementIndex, Type = elementType, NodeIDs = nodeIds };

				connectivityIndex += nodeCount;
			}

			elementsProcessed = true;
		}

		#endregion

		#region IDisposable Support

		public void Dispose()
		{
			disposeInput();
		}

		private void disposeInput()
		{
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

			input = XmlReader.Create(new StreamReader(filename));

			validateVTKFileType();

			bool elementFound = input.ReadToDescendant("UnstructuredGrid");
			if (!elementFound)
			{
				throw new MeshLoadingException("UnstructuredGrid element was not found.");
			}

			elementFound = input.ReadToDescendant("Piece");
			if (!elementFound)
			{
				throw new MeshLoadingException("Piece element was not found.");
			}

			nodeCount = parseInt32(input.GetAttribute("NumberOfPoints"));
			elementCount = parseInt32(input.GetAttribute("NumberOfCells"));
		}

		private void validateVTKFileType()
		{
			bool hasRootElement = input.ReadToDescendant("VTKFile");
			if (!hasRootElement)
			{
				throw new MeshLoadingException("Root element (VTKFile) was not found.");
			}
			var type = input.GetAttribute("type");
			if (type != "UnstructuredGrid")
			{
				throw new MeshLoadingException($"Type '{type}' is not supported. Only 'UnstructuredGrid' is supported.", CurrentLineNumber);
			}
		}

		private double[] parseFloat64AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			// TODO: for binary format use: input.ReadElementContentAsBase64(...)
			string[] parts = content.Split(dataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
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
			string[] parts = content.Split(dataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
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
			string[] parts = content.Split(dataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
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
			string[] parts = content.Split(dataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
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

		private T[] getSliceOfArray<T>(T[] array, int index, int length)
		{
			T[] result = new T[length];
			Array.Copy(array, index, result, 0, length); 
			return result;
		}

		private ElementType mapVTKCellTypeToElementType(VTKCellType vtkCellType)
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
					throw new NotSupportedException();
			}
		}

		#endregion

	}
}

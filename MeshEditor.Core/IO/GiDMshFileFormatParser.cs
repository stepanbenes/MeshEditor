using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;
using System.IO;
using System.Diagnostics;
using OpenTK;
using System.Globalization;

namespace MeshEditor.IO
{
	public class GiDMshFileFormatParser : IMeshFileParser
	{

		#region Static members

		public const string COMMENT_PATTERN = "#";
		public const string MESH_PATTERN = "MESH";
		public const string DIMENSION_PATTERN = "dimension";
		public const string ELEMTYPE_PATTERN = "Elemtype";
		public const string NNODE_PATTERN = "Nnode";
		public const string COORDINATES_PATTERN = "coordinates";
		public const string ELEMENTS_PATTERN = "elements";
		public const string END_PATTERN = "end";

		public static readonly Dictionary<ElementType, string> NamesOfElementTypes;

		static GiDMshFileFormatParser()
		{
			NamesOfElementTypes = new Dictionary<ElementType, string>();
			NamesOfElementTypes[ElementType.BeamLinear] = NamesOfElementTypes[ElementType.BeamQuadratic] = "Linear";
			NamesOfElementTypes[ElementType.TriangleLinear] = NamesOfElementTypes[ElementType.TriangleQuadratic] = "Triangle";
			NamesOfElementTypes[ElementType.QuadLinear] = NamesOfElementTypes[ElementType.QuadQuadratic] = "Quadrilateral";
			NamesOfElementTypes[ElementType.TetrahedronLinear] = NamesOfElementTypes[ElementType.TetrahedronQuadratic] = "Tetrahedra";
			NamesOfElementTypes[ElementType.HexahedronLinear] = NamesOfElementTypes[ElementType.HexahedronQuadratic] = "Hexahedra";
			NamesOfElementTypes[ElementType.TriangularPrismLinear] = NamesOfElementTypes[ElementType.TriangularPrismQuadratic] = "Prism";
		}

		#endregion

		#region Fields, constructor

		private string filename;
		private bool fileProcessed;
		private int currentLineNumber;

		private List<Node> nodes;
		private List<ElementDraft> elements;

		public GiDMshFileFormatParser(string filename)
		{
			this.filename = filename;
			this.currentLineNumber = -1;

			this.nodes = new List<Node>();
			this.elements = new List<ElementDraft>();
		}

		#endregion

		#region IMeshFileParser Members

		public string Filename
		{
			get { return filename; }
		}

		public int NodeCount
		{
			get
			{
				if (!fileProcessed)
					processFile();
				return nodes.Count;
			}
		}

		public IEnumerable<Node> ReadNodes()
		{
			if (!fileProcessed)
				processFile();
			return nodes;
		}

		public int ElementCount
		{
			get
			{
				if (!fileProcessed)
					processFile();
				return elements.Count;
			}
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			if (!fileProcessed)
				processFile();
			return elements;
		}

		public int CurrentLineNumber
		{
			get { return currentLineNumber; }
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			// Nothing to do
		}

		#endregion

		#region Private methods

		private enum ParserMode
		{
			Init,
			Coordinates,
			Elements
		}

		private void processFile()
		{
			if (fileProcessed)
				return;

			char[] whiteSpaceSeparators = { ' ', '\t' };
			currentLineNumber = 0;
			ParserMode parserMode = ParserMode.Init;
			using (TextReader reader = new StreamReader(filename))
			{
				string line;
				string[] parts;
				string meshName;
				int dimension = 0 /*it is not used now*/, nnode = 0;
				ElementType elementType = 0;
				while ((line = reader.ReadLine()) != null)
				{
					++currentLineNumber;
					line = line.Trim();
					if (line.StartsWith(COMMENT_PATTERN))
						continue;
					parts = line.Split(whiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length == 0)
						continue;

					if (parts[0].Equals(MESH_PATTERN, StringComparison.InvariantCultureIgnoreCase)) // MESH
					{
						parserMode = ParserMode.Init;
						int index = 1;
						Debug.Assert(parts.Length >= 7);
						if (!parts[index].Equals(DIMENSION_PATTERN, StringComparison.InvariantCultureIgnoreCase)) // if dimension keyword is not following, current part is mesh_name
							meshName = parts[index++]; // optional mesh name
						Debug.Assert(parts[index].Equals(DIMENSION_PATTERN, StringComparison.InvariantCultureIgnoreCase));
						++index;
						dimension = parseInteger(parts[index++]); // dimension
						Debug.Assert(parts[index].Equals(ELEMTYPE_PATTERN, StringComparison.InvariantCultureIgnoreCase));
						++index;
						string elementTypeName = parts[index++]; // element type
						Debug.Assert(parts[index].Equals(NNODE_PATTERN, StringComparison.InvariantCultureIgnoreCase));
						++index;
						nnode = parseInteger(parts[index++]); // number of nodes in one element of type elementType

						// determine element type
						elementType = convertNameToElementType(elementTypeName, nnode, dimension);
					}
					else if (parts[0].Equals(COORDINATES_PATTERN, StringComparison.InvariantCultureIgnoreCase)) // COORDINATES
					{
						parserMode = ParserMode.Coordinates; // prepare to read node coordinates
					}
					else if (parts[0].Equals(ELEMENTS_PATTERN, StringComparison.InvariantCultureIgnoreCase)) // ELEMENTS
					{
						parserMode = ParserMode.Elements; // prepare to read elements
					}
					else
					{
						switch (parserMode)
						{
							case ParserMode.Coordinates:
								{
									if (parts[0].Equals(END_PATTERN, StringComparison.InvariantCultureIgnoreCase))
									{
										Debug.Assert(parts.Length > 1 && parts[1].Equals(COORDINATES_PATTERN, StringComparison.InvariantCultureIgnoreCase));
										parserMode = ParserMode.Init;
										break;
									}
									Debug.Assert(parts.Length >= 3);
									int id;
									Vector3 position;
									id = parseInteger(parts[0]);
									position.X = (float)parseDouble(parts[1]); // WARNING: possible loss of precision
									position.Y = (float)parseDouble(parts[2]); // WARNING: possible loss of precision
									if (parts.Length >= 4)
										position.Z = (float)parseDouble(parts[3]); // WARNING: possible loss of precision
									else
										position.Z = 0f;
									nodes.Add(new Node(id, position, null));
								}
								break;
							case ParserMode.Elements:
								{
									if (parts[0].Equals(END_PATTERN, StringComparison.InvariantCultureIgnoreCase))
									{
										Debug.Assert(parts.Length > 1 && parts[1].Equals(ELEMENTS_PATTERN, StringComparison.InvariantCultureIgnoreCase));
										parserMode = ParserMode.Init;
										break;
									}
									Debug.Assert(parts.Length >= nnode + 1);
									ElementDraft ed = new ElementDraft();
									ed.Type = elementType;
									ed.ID = parseInteger(parts[0]);
									int[] nodeIDs = new int[nnode];
									for (int i = 0; i < nnode; i++)
										nodeIDs[i] = parseInteger(parts[i + 1]);
									ed.NodeIDs = nodeIDs;
									if (parts.Length > nnode + 1)
										ed.Property = new Property(parseInteger(parts[nnode + 1])); // read optional material number
									elements.Add(ed);
								}
								break;
						}
					}
				}
			}
			// file is now parsed
			fileProcessed = true;
		}

		private ElementType convertNameToElementType(string name, int nodeNumber, int dimension/*not used now*/)
		{
			// use nodeNumber to differentiate approximation type
			foreach (KeyValuePair<ElementType, string> pair in NamesOfElementTypes)
			{
				if (pair.Value.Equals(name, StringComparison.InvariantCultureIgnoreCase) && nodeNumber == Element.MapElementTypeToNodeCount(pair.Key))
					return pair.Key;
			}
			throw new FileParserException(string.Format("{0} element type with {1} nodes is not supported.", name, nodeNumber), filename, currentLineNumber);
		}

		private int parseInteger(string text)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
				throw new FileParserException("Integer expected instead of \"" + text + "\"", filename, currentLineNumber);
			return result;
		}

		private double parseDouble(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
				throw new FileParserException("Floating-point number expected instead of \"" + text + "\"", filename, currentLineNumber);
			return result;
		}

		#endregion

	}
}

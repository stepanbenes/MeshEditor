using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MeshEditor.Data;
using OpenTK;
using System.Globalization;

namespace MeshEditor.IO
{
	/// <summary>
	/// trida obsahujici funkce pro cteni a parsovani vstupniho souboru v OBJ formatu
	/// </summary>
	public class OBJFileFormatParser : IMeshFileParser
	{
		#region Fields

		private string filename;
		private TextReader input;
		private bool contentLoaded;
		private int lineNumber;

		public const string COMMENT_PATTERN = "#";
		public const string VERTEX_PATTERN = "v";
		public const string FACE_PATTERN = "f";
		public const char FACE_INPUT_SEPARATOR = '/';
		public const int NODE_START_INDEX = 1;
		public const int ELEMENT_START_INDEX = 1;

		private List<Node> nodes;
		private List<ElementDraft> elements;

		#endregion

		#region Constructor, Destructor

		/// <summary>
		/// Parametric constructor
		/// </summary>
		/// <param name="filename">filepath containing mesh</param>
		public OBJFileFormatParser(string filename)
		{
			this.filename = filename;
			this.input = null;
			this.contentLoaded = false;
			this.lineNumber = -1;

			this.nodes = new List<Node>();
			this.elements = new List<ElementDraft>();
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~OBJFileFormatParser()
		{
			Dispose(false);
		}

		#endregion

		#region IMeshFileParser Members

		public string Filename
		{
			get { return this.filename; }
		}

		public int NodeCount
		{
			get
			{
				if (!contentLoaded)
					initInputAndReadAllContent();
				return nodes.Count;
			}
		}

		public int ElementCount
		{
			get
			{
				if (!contentLoaded)
					initInputAndReadAllContent();
				return elements.Count;
			}
		}

		public IEnumerable<Node> ReadNodes()
		{
			if (!contentLoaded)
				initInputAndReadAllContent();
			return nodes;
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			if (!contentLoaded)
				initInputAndReadAllContent();
			return elements;
		}

		public int CurrentLineNumber
		{
			get { return this.lineNumber; }
		}

		#endregion

		#region Private methods

		private void initInputAndReadAllContent()
		{
			if (filename == null || !File.Exists(filename))
				throw new FileParserException("Mesh file can't be found.", filename);
			input = File.OpenText(filename);
			lineNumber = 0;
			readAllContent();
		}

		private void readAllContent()
		{
			string line;
			while (getNextLineWithValue(out line))
			{
				string[] parts = line.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
				if (parts[0].ToLower() == VERTEX_PATTERN)
					nodes.Add(parseNode(nodes.Count + NODE_START_INDEX, parts[1]));
				if (parts[0].ToLower() == FACE_PATTERN)
					elements.Add(parseElement(elements.Count + ELEMENT_START_INDEX,parts[1]));
			}
			contentLoaded = true;
		}

		private bool getNextLineWithValue(out string line)
		{
			do
			{
				line = input.ReadLine();
				lineNumber++;
				if (line == null) // konec souboru, vratim false, protoze zadnej dalsi radek neni
					return false;
				line = line.Trim();
			} while (line == string.Empty || line.ToLower().StartsWith(COMMENT_PATTERN));
			return true;
		}

		private int parseInteger(string text)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
				throw new FileParserException("Integer expected instead of \"" + text + "\"", filename, lineNumber);
			return result;
		}

		private double parseDouble(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
				throw new FileParserException("Floating-point number expected instead of \"" + text + "\"", filename, lineNumber);
			return result;
		}

		private Node parseNode(int id, string line)
		{
			Vector3 position = Vector3.Zero;
			PropertyEntityPair[] properties = null;

			string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			try
			{
				position.X = (float)parseDouble(parts[0]); /**/ // casting !!!
				position.Y = (float)parseDouble(parts[1]); /**/ // casting !!!
				position.Z = (float)parseDouble(parts[2]); /**/ // casting !!!
			}
			catch (FileParserException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new FileParserException("Wrong file format", filename, lineNumber, ex);
			}
			return new Node(id, position, properties);
		}

		private ElementDraft parseElement(int id, string line)
		{
			ElementDraft e = new ElementDraft();
			string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			try
			{
				e.ID = id;
				switch (parts.Length)
				{
					case 3:
						e.Type = ElementType.TriangleLinear;
						break;
					case 4:
						e.Type = ElementType.QuadLinear;
						break;
					default:
						throw new FileParserException("This element type is not supported.", filename, lineNumber);
				}
				int nodeCountOfElement = Element.MapElementTypeToNodeCount(e.Type);
				e.NodeIDs = new int[nodeCountOfElement];
				for (int i = 0; i < nodeCountOfElement; i++)
				{
					string vertexID = parts[i].Split(new char[] { FACE_INPUT_SEPARATOR }, 2)[0];
					e.NodeIDs[i] = parseInteger(vertexID);
				}
				e.Property = Property.Zero;
			}
			catch (FileParserException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new FileParserException("Wrong file format", filename, lineNumber, ex);
			}
			return e;
		}

		#endregion

		#region Disposing

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			//if (disposing)
			//{
			//    // Free other state (managed objects).
			//}

			if (input != null)
			{
				input.Close();
				input = null;
				nodes = null;
				elements = null;
			}
		}

		#endregion

	}
}

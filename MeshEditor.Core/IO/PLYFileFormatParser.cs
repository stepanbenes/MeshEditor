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
	/// trida obsahujici funkce pro cteni a parsovani vstupniho souboru v PLY formatu
	/// </summary>
	public class PLYFileFormatParser : IMeshFileParser
	{
		#region Fields

		private string filename;
		private TextReader input;
		private int nodeCount, elementCount;
		private int lineNumber;
		private bool headerWasRead;
		private bool nodesWasRead;

		public const string COMMENT_PATTERN = "comment";
		public const string END_OF_HEADER_PATTERN = "end_header";
		public const string VERTEX_COUNT_PATTERN = "element vertex";
		public const string FACE_COUNT_PATTERN = "element face";
		public const string FORMAT_PATTERN = "format";
		public const string ASCII_FORMAT_PATTERN = "ascii";
		public const string BINARY_LITTLE_ENDIAN_FORMAT_PATTERN = "binary_little_endian";
		public const string BINARY_BIG_ENDIAN_FORMAT_PATTERN = "binary_big_endian";

		#endregion

		#region Constructor, Destructor

		/// <summary>
		/// Parametric constructor
		/// </summary>
		/// <param name="filename">filepath containing mesh</param>
		public PLYFileFormatParser(string filename)
		{
			this.filename = filename;
			this.input = null;
			this.nodeCount = this.elementCount = -1;
			this.lineNumber = -1;
			this.headerWasRead = false;
			this.nodesWasRead = false;
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~PLYFileFormatParser()
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
				if (!headerWasRead)
					initInputAndReadHeader();
				return nodeCount;
			}
		}

		public int ElementCount
		{
			get
			{
				if (!headerWasRead)
					initInputAndReadHeader();
				return elementCount;
			}
		}
		
		public IEnumerable<Node> ReadNodes()
		{
			if (!headerWasRead)
				initInputAndReadHeader();
			string line;
			for (int i = 0; i < nodeCount; i++)
			{
				line = getNextLineWithValue();
				yield return parseNode(i, line);
			}
			this.nodesWasRead = true;
		}
		
		public IEnumerable<ElementDraft> ReadElements()
		{
			if(!nodesWasRead)
				throw new MeshLoadingException("All nodes must be processed first before loading elements.", lineNumber);
			string line;
			for (int i = 0; i < elementCount; i++)
			{
				line = getNextLineWithValue();
				yield return parseElement(i, line);
			}
		}

		public int CurrentLineNumber
		{
			get { return this.lineNumber; }
		}

		#endregion

		#region Private methods

		private void initInputAndReadHeader()
		{
			if (filename == null || !File.Exists(filename))
				throw new MeshLoadingException("Mesh file can't be found." + "(" + filename + ")");
			input = File.OpenText(filename);
			lineNumber = 0;
			readHeader();
		}

		private void readHeader()
		{
			bool nodeCountLoaded = false;
			bool elementCountLoaded = false;
			string line;
			do
			{
				line = getNextLineWithValue();
				if (line.ToLower().StartsWith(FORMAT_PATTERN)) // zkontoluj format (ASCII / Binary (Little / Big endian))
					checkFormat(line.Substring(FORMAT_PATTERN.Length).Trim());
				else if (line.ToLower().StartsWith(VERTEX_COUNT_PATTERN)) // nacti pocet uzlu
				{
					this.nodeCount = parseInteger(line.Substring(VERTEX_COUNT_PATTERN.Length).Trim());
					nodeCountLoaded = true;
				}
				else if (line.ToLower().StartsWith(FACE_COUNT_PATTERN)) // nacti pocet prvku (v tomhle pripade trojuhelniku)
				{
					this.elementCount = parseInteger(line.Substring(FACE_COUNT_PATTERN.Length).Trim());
					elementCountLoaded = true;
				}
			} while (!line.ToLower().StartsWith(END_OF_HEADER_PATTERN));

			if(!nodeCountLoaded || !elementCountLoaded)
				throw new MeshLoadingException("Mesh file is not complete (wrong header).", lineNumber);
			
			headerWasRead = true;
		}

		private void checkFormat(string formatline)
		{
			string[] parts = formatline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if(parts[0].ToLower() != ASCII_FORMAT_PATTERN)
				throw new MeshLoadingException("This format of PLY file is not supported. It understands ascii format only.", lineNumber);
		}

		private string getNextLineWithValue()
		{
			string line;
			do
			{
				line = input.ReadLine();
				lineNumber++;
				if (line == null)
					throw new MeshLoadingException("Mesh file is not complete.", lineNumber);
				line = line.Trim();
			} while (line == string.Empty || line.ToLower().StartsWith(COMMENT_PATTERN));
			return line;
		}

		private int parseInteger(string text)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
				throw new MeshLoadingException("Integer expected instead of \"" + text + "\"", lineNumber);
			return result;
		}

		private double parseDouble(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
				throw new MeshLoadingException("Floating-point number expected instead of \"" + text + "\"", lineNumber);
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
			catch (MeshLoadingException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new MeshLoadingException("Wrong file format", lineNumber, ex);
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
				switch (parseInteger(parts[0]))
				{
					case 3:
						e.Type = ElementType.TriangleLinear;
						break;
					case 4:
						e.Type = ElementType.QuadLinear;
						break;
					default:
						throw new MeshLoadingException("This element type is not supported.", lineNumber);
				}
				int nodeCountOfElement = Element.MapElementTypeToNodeCount(e.Type);
				e.NodeIDs = new int[nodeCountOfElement];
				for (int i = 0; i < nodeCountOfElement; i++)
					e.NodeIDs[i] = parseInteger(parts[i + 1]);
				e.Property = Property.Zero;
			}
			catch (MeshLoadingException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new MeshLoadingException("Wrong file format", lineNumber, ex);
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
			}
		}

		#endregion
	}
}

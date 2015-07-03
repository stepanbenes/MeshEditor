using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;
using System.IO;
using MeshEditor.Construction;
using OpenTK;
using System.Globalization;

namespace MeshEditor.IO
{
	/// <summary>
	/// trida obsahujici funkce pro cteni, parsovani standardniho vstupniho souboru
	/// </summary>
	public class DefaultFileFormatParser : IDefaultFileFormatParser
	{

		#region Fields

		private string filename;
		private TextReader input;
		private int nodeCount, elementCount;
		private int faceCount, edgeCount;
		private int lineNumber;
		private string currentLine;
		private bool nodesWereProcessed, elementsWereProcessed, facesWereProcessed;
		private bool edgeCountLoaded;

		public const string COMMENT_PATTERN = "#";
		public const string FACES_PATERN = "faces";
		public const string EDGES_PATERN = "edges";
		public const string PROPERTY_COMMENT_PATTERN = "Property";
		public const string PROPERTY_DESCRIPTION_FILE_PATTERN = "PropertyDescriptionFile";
		public const string BEGIN_SECTION_PATTERN = "begsec_";
		public const string END_SECTION_PATTERN = "endsec_";

		#endregion

		#region Constructor, Destructor

		/// <summary>
		/// Parametric constructor
		/// </summary>
		/// <param name="filename">filepath containing mesh</param>
		public DefaultFileFormatParser(string filename)
		{
			this.filename = filename;
			this.input = null;
			this.nodeCount = this.elementCount = 0;
			this.faceCount = this.edgeCount = 0;
			this.lineNumber = -1;
			this.currentLine = null;
			this.nodesWereProcessed = false;
			this.elementsWereProcessed = false;
			this.facesWereProcessed = false;
			this.edgeCountLoaded = false;
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~DefaultFileFormatParser()
		{
			Dispose(false);
		}

		#endregion

		#region IDefaultFileParser Members

		public string Filename
		{
			get { return this.filename; }
		}

		public int NodeCount
		{
			get
			{
				if (input == null)
				{
					initInput();
					readToNodeCount();
				}
				return nodeCount;
			}
		}
		
		public IEnumerable<Node> ReadNodes()
		{
			if (nodesWereProcessed)
				yield break;

			if (input == null)
			{
				initInput();
				readToNodeCount();
			}
			for (int i = 0; i < nodeCount; i++)
			{
				readToNextLineWithValue("Node missing");
				yield return parseNode(currentLine);
			}
			nodesWereProcessed = true;
			yield break;
		}

		public int ElementCount
		{
			get
			{
				if (!nodesWereProcessed)
					throw new MeshLoadingException("All nodes must be processed before loading elements.", lineNumber);
				return elementCount;
			}
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			if (elementsWereProcessed)
				yield break;

			if (!nodesWereProcessed)
				throw new MeshLoadingException("All nodes must be processed before loading elements.", lineNumber);

			readToNextLineWithValue("Element count number missing");
			// load and set element count
			elementCount = parseInteger(currentLine);

			for (int i = 0; i < elementCount; i++)
			{
				readToNextLineWithValue("Element missing");
				yield return parseElement(currentLine);
			}
			elementsWereProcessed = true;
		}

		// -------------------------------------------------

		public int FaceCount
		{
			get { return faceCount; }
		}

		public IEnumerable<FaceDraft> ReadFaces()
		{
			if (facesWereProcessed)
				yield break;

			if (!elementsWereProcessed)
				throw new MeshLoadingException("All nodes and elements must be processed before loading faces.", lineNumber);


			while (true)
			{
				if (!readToNextLineWithValueIfPossible()) // konec souboru
				{
					facesWereProcessed = true;
					yield break;
				}

				// load and set face count or edge count
				string[] parts = currentLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts[0] == FACES_PATERN) // faces
				{
					faceCount = parseInteger(parts[1]);
					break;
				}
				else if (parts[0] == EDGES_PATERN) // rovnou edges
				{
					edgeCount = parseInteger(parts[1]);
					edgeCountLoaded = true;
					faceCount = 0;
					break;
				}
				else // jinak pokracuj ve cteni
				{
					if (LineWasSkipped != null)
						LineWasSkipped(this, EventArgs.Empty);
				}
			}
			
			for (int i = 0; i < faceCount; i++)
			{
				readToNextLineWithValue("Face missing");
				yield return parseFace(currentLine);
			}
			facesWereProcessed = true;
		}
		
		public int EdgeCount
		{
			get { return edgeCount; }
		}

		public IEnumerable<EdgeDraft> ReadEdges()
		{
			if (!facesWereProcessed)
				throw new MeshLoadingException("All nodes and elements (and faces) must be processed before loading edges.", lineNumber);

			if (!edgeCountLoaded)
			{
				while (true)
				{
					if (!readToNextLineWithValueIfPossible())
						yield break;
					// load and set edge count
					string[] parts = currentLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts[0] == EDGES_PATERN)
					{
						edgeCount = parseInteger(parts[1]);
						edgeCountLoaded = true;
						break;
					}
					else // jinak pokracuj ve cteni
					{
						if (LineWasSkipped != null)
							LineWasSkipped(this, EventArgs.Empty);
					}
				}
			}
			// ---------------------------------------------
			for (int i = 0; i < edgeCount; i++)
			{
				readToNextLineWithValue("Edge missing");
				yield return parseEdge(currentLine);
			}
		}

		// -------------------------------------------------

		public int CurrentLineNumber
		{
			get { return this.lineNumber; }
		}

		public string CurrentLine
		{
			get { return this.currentLine; }
		}

		public event EventHandler LineWasSkipped;

		public void ReadToEnd()
		{
			while (true)
			{
				currentLine = input.ReadLine();
				lineNumber++;
				if (currentLine == null)
					break;
				if (LineWasSkipped != null)
					LineWasSkipped(this, EventArgs.Empty);
			}
		}

		public string ReadNextLine()
		{
			currentLine = input.ReadLine();
			if (currentLine != null)
				lineNumber++;
			return currentLine;
		}

		#endregion

		#region Private methods

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

		private void initInput()
		{
			if (filename == null || !File.Exists(filename))
				throw new MeshLoadingException("Mesh file can't be found." + "(" + filename + ")");
			input = File.OpenText(filename);
			lineNumber = 0;
		}

		private void readToNodeCount()
		{
			readToNextLineWithValue("Node count number missing");
			// read and set node count
			nodeCount = parseInteger(currentLine);
		}

		private Node parseNode(string line)
		{
			int id;
			Vector3 position = Vector3.Zero;
			PropertyEntityPair[] properties = null;
			
			string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			
			try
			{
				id = parseInteger(parts[0]);
				position.X = (float)parseDouble(parts[1]); /**/ // casting !!!
				position.Y = (float)parseDouble(parts[2]); /**/ // casting !!!
				position.Z = (float)parseDouble(parts[3]); /**/ // casting !!!
				int propertyCount = parseInteger(parts[4]);
				if (propertyCount > 0)
				{
					properties = new PropertyEntityPair[propertyCount];
					for (int i = 0; i < propertyCount; i++)
					{
						PropertyEntityPair pair = new PropertyEntityPair(
							new Property(parseInteger(parts[6 + i * 2])),
							(EntityType)parseInteger(parts[5 + i * 2])
							);
						properties[i] = pair;
					}
				}
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

		private ElementDraft parseElement(string line)
		{
			ElementDraft e = new ElementDraft();
			string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			try
			{
				e.ID = parseInteger(parts[0]);
				e.Type = (ElementType)parseInteger(parts[1]);
				int nodeCountOfElement = Element.MapElementTypeToNodeCount(e.Type);

				e.NodeIDs = new int[nodeCountOfElement];
				for (int i = 0; i < nodeCountOfElement; i++)
					e.NodeIDs[i] = parseInteger(parts[i + 2]);
				if (parts.Length > nodeCountOfElement + 2)
				{
					e.Property = new Property(parseInteger(parts[nodeCountOfElement + 2]));

					// -------------------------------------------------------------------------
					// nacitani dodatecnych vlastnosti prvku
					if (parts.Length > nodeCountOfElement + 3)
					{
						// load edge properties ::::::::::::::::::::::::::::::::::::::::::
						int edgePropertyCount = Element.MapElementTypeToEdgeCount(e.Type);
						e.EdgeProperties = new int[edgePropertyCount];
						bool hasNonZeroProperty = false;
						for (int i = 0; i < edgePropertyCount; i++)
						{
							int property = parseInteger(parts[nodeCountOfElement + 3 + i]);
							if (property != 0)
								hasNonZeroProperty = true;
							e.EdgeProperties[i] = property;
						}
						if (!hasNonZeroProperty) // if all properties are 0, set whole array to null
							e.EdgeProperties = null;
						
						if (parts.Length > nodeCountOfElement + 3 + edgePropertyCount)
						{
							// load face properties ::::::::::::::::::::::::::::::::::::::::::
							int facePropertyCount = Element.MapElementTypeToFaceCount(e.Type);
							e.FaceProperties = new int[facePropertyCount];
							hasNonZeroProperty = false;
							for (int i = 0; i < facePropertyCount; i++)
							{
								int property = parseInteger(parts[nodeCountOfElement + 3 + edgePropertyCount + i]);
								if (property != 0)
									hasNonZeroProperty = true;
								e.FaceProperties[i] = property;
							}
							if (!hasNonZeroProperty) // iff all properties are 0, set whole array to null
								e.FaceProperties = null;
						}
					}
					// -------------------------------------------------------------------------
				}
				else
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

		private FaceDraft parseFace(string line)
		{
			FaceDraft fd = new FaceDraft();
			string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			try
			{
				int nodeCount = parseInteger(parts[0]);
				fd.NodeIDs = new int[nodeCount];
				for (int i = 0; i < nodeCount; i++)
					fd.NodeIDs[i] = parseInteger(parts[i + 1]);
				fd.Property = new Property(parseInteger(parts[nodeCount + 1]));
			}
			catch (MeshLoadingException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new MeshLoadingException("Wrong file format", lineNumber, ex);
			}
			return fd;
		}

		private EdgeDraft parseEdge(string line)
		{
			EdgeDraft ed = new EdgeDraft();
			string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			try
			{
				ed.Node1ID = parseInteger(parts[0]);
				ed.Node2ID = parseInteger(parts[1]);
				ed.Property = new Property(parseInteger(parts[2]));
			}
			catch (MeshLoadingException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new MeshLoadingException("Wrong file format", lineNumber, ex);
			}
			return ed;
		}

		private void readToNextLineWithValue(string errorReason)
		{
			while(true)
			{
				currentLine = input.ReadLine();
				lineNumber++;
				if (currentLine == null)
					throw new MeshLoadingException("Mesh file is not complete (" + errorReason + ").", lineNumber);
				currentLine = currentLine.Trim();
				if (currentLine != string.Empty && !currentLine.StartsWith(COMMENT_PATTERN))
					break;
				if (LineWasSkipped != null)
					LineWasSkipped(this, EventArgs.Empty);
			}
		}

		private bool readToNextLineWithValueIfPossible()
		{
			while (true)
			{
				currentLine = input.ReadLine();
				lineNumber++;
				if (currentLine == null)
					return false;
				currentLine = currentLine.Trim();
				if (currentLine != string.Empty && !currentLine.StartsWith(COMMENT_PATTERN))
					break;
				if (LineWasSkipped != null)
					LineWasSkipped(this, EventArgs.Empty);
			}
			return true;
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

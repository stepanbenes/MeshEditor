using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;
using System.IO;
using MeshEditor.CoreInterface;
using MeshEditor.Construction;
using OpenTK;

using Utils = MeshEditor.Utilities.Functions;

namespace MeshEditor.IO
{
	/// <summary>
	/// trida poskytujici funkce pro zapis site do souboru ve standardnim formatu
	/// </summary>
	public class DefaultFileFormatMeshSaver : IMeshSaver
	{
		
		#region Fields, contructor

		private TextWriter output;
		IDefaultFileFormatParser sourceFileParser;
		//private int lineNumber;

		private Mesh mesh;
		private Dictionary<int, Node> nodeMap;
		private Dictionary<int, Element> elementMap;

		private int itemIndex;
		private int itemsToWrite;
		private MeshIOEventArgs ioea;

		public DefaultFileFormatMeshSaver()
		{
			sourceFileParser = null;
			this.output = null;
			//this.lineNumber = -1;
			this.nodeMap = null;
			this.elementMap = null;
			this.mesh = null;
		}

		#endregion

		#region IMeshSaver Members

		public void SaveMesh(Mesh mesh, string filename, bool saveWithoutHiddenElements, YesNoQuestion cancelled)
		{
			itemIndex = itemsToWrite = 0;
			ioea = new MeshIOEventArgs(0);
			this.mesh = mesh;
			string destinationFile = Path.GetTempFileName();
			initOutput(destinationFile);

			writePropertyCommandsFile(mesh.Statistics, filename);

			bool completed;
			try
			{
				if (mesh.LoadedFromDefaultFileFormat && File.Exists(mesh.Filename))
				{
					this.sourceFileParser = new DefaultFileFormatParser(mesh.Filename);
					completed = saveMeshHavingSource(mesh, saveWithoutHiddenElements, cancelled);
				}
				else
				{
					this.sourceFileParser = null;
					completed = saveMeshWithoutSource(mesh, saveWithoutHiddenElements, cancelled);
				}
				// ---------------------------------------------------------------------------

			}
			finally
			{
				if (sourceFileParser != null)
				{
					sourceFileParser.Dispose();
					sourceFileParser = null;
				}
				if (output != null)
				{
					output.Close();
					output = null;
				}
				this.mesh = null;
				this.nodeMap = null;
				this.elementMap = null;
				//if (!done)
				//	File.Delete(destinationFile);
			}
			if (completed)
			{
				replaceFile(destinationFile, filename);
				mesh.Filename = filename;
				mesh.LoadedFromDefaultFileFormat = true;
				mesh.UnsavedChanges = false;
			}
		}

		public void SaveMesh(IMeshFileParser fileParser, string destination, YesNoQuestion cancelled)
		{
			throw new NotImplementedException();
		}

		private void writePropertyCommandsFile(MeshStatistics meshStatistics, string meshFilename)
		{
			if (meshStatistics.PropertyCommands.Count == 0)
			{
				meshStatistics.PropertyCommandsFile = null;
				return;
			}

			if (string.IsNullOrEmpty(meshStatistics.PropertyCommandsFile))
			{
				meshStatistics.PropertyCommandsFile = Path.ChangeExtension(Path.GetFileName(meshFilename), Scene.PropertyDescriptionFileExtension); // set relative path with changed extension
			}

			string destination;
			bool rooted = Path.IsPathRooted(meshStatistics.PropertyCommandsFile);
			if (rooted) // if path is relative
			{
				destination = meshStatistics.PropertyCommandsFile;
			}
			else
			{
				destination = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(meshFilename), meshStatistics.PropertyCommandsFile)); // make absolute path
			}
			
			using (TextWriter writer = new StreamWriter(destination))
			{
				string meshPathToWrite = rooted ? meshFilename : Path.GetFileName(meshFilename); // write absolute or relative according to property command file path
				writer.WriteLine(DefaultFileFormatParser.COMMENT_PATTERN + " PREPROCESSOR COMMANDS FILE (linked to mesh \"" + meshPathToWrite + "\")");

				// write property comments
				SortedDictionary<Property, string> comments = new SortedDictionary<Property, string>();
				foreach (PropertyEntityPair pair in mesh.Statistics.PropertyCommands.Keys) // seradit nejdriv
				{
					string comment;
					if (mesh.Statistics.PropertyComments.TryGetValue(pair.Property, out comment))
						comments[pair.Property] = comment;
				}

				writer.WriteLine();
				foreach (KeyValuePair<Property, string> pair in comments)
				{
					writePropertyComment(writer, pair.Key, pair.Value);
				}

				SortedDictionary<PreprocessorSections, List<PropertyCommand>> sections = getCommandSections(meshStatistics);

				foreach (PreprocessorSections section in sections.Keys)
				{
					writer.WriteLine();
					writer.WriteLine(DefaultFileFormatParser.BEGIN_SECTION_PATTERN + section.ToString());
					foreach (PropertyCommand command in sections[section])
					{
						//// write comment if exists, in either case add new line
						//string comment;
						//if (meshStatistics.PropertyComments.TryGetValue(pair.Property, out comment))
						//	writePropertyComment(writer, pair.Property, comment);
						// write command
						writer.WriteLine(command.ToString());
					}
					writer.WriteLine(DefaultFileFormatParser.END_SECTION_PATTERN + section.ToString());
				}
			}
		}

		private SortedDictionary<PreprocessorSections, List<PropertyCommand>> getCommandSections(MeshStatistics meshStatistics)
		{
			SortedDictionary<PreprocessorSections, List<PropertyCommand>> result = new SortedDictionary<PreprocessorSections, List<PropertyCommand>>();

			foreach (PropertyEntityPair pair in meshStatistics.PropertyCommands.Keys)
			{
				foreach (PropertyCommand command in meshStatistics.PropertyCommands[pair])
				{
					PreprocessorSections section = convertPropertyTargetToPreprocessorSection(pair.EntityType, command.Type);
					List<PropertyCommand> commands;
					if (!result.TryGetValue(section, out commands))
						commands = result[section] = new List<PropertyCommand>();
					commands.Add(command);
				}
			}

			return result;
		}

		private PreprocessorSections convertPropertyTargetToPreprocessorSection(EntityType entity, PropertyCommand.CommandType commandType)
		{
			PropertyCommand.CommandType[] nodeSectionCommands = { PropertyCommand.CommandType.ndofn, PropertyCommand.CommandType.bocon, PropertyCommand.CommandType.dof_coupl, PropertyCommand.CommandType.nod_tfunc, PropertyCommand.CommandType.nod_crsec, PropertyCommand.CommandType.nod_spring, PropertyCommand.CommandType.nod_lcs, PropertyCommand.CommandType.nod_load, PropertyCommand.CommandType.nod_tdload, PropertyCommand.CommandType.nod_inicond, PropertyCommand.CommandType.nod_temper };
			PropertyCommand.CommandType[] elementSectionCommands = { PropertyCommand.CommandType.el_type, PropertyCommand.CommandType.el_mat, PropertyCommand.CommandType.el_crsec, PropertyCommand.CommandType.el_lcs, PropertyCommand.CommandType.el_load, PropertyCommand.CommandType.edge_load, PropertyCommand.CommandType.surf_load, PropertyCommand.CommandType.volume_load, PropertyCommand.CommandType.el_tfunc };

			if (Utils.ArrayContains(nodeSectionCommands, commandType)) // Node sections
			{
				switch (entity)
				{
					case EntityType.Vertex:
						return PreprocessorSections.nodvertpr;
					case EntityType.Edge:
						return PreprocessorSections.nodedgpr;
					case EntityType.Surface: // I suppose that SURFACE is equal to PATCH is equal to SHELL
					case EntityType.Patch:
					case EntityType.Shell:
						return PreprocessorSections.nodsurfpr;
					case EntityType.Region:
						return PreprocessorSections.nodvolpr;
					default:
						return PreprocessorSections.Unknown;
				}
			}
			else if (Utils.ArrayContains(elementSectionCommands, commandType)) // Element sections
			{
				switch (entity)
				{
					case EntityType.Vertex:
						return PreprocessorSections.Unknown; /**/
					case EntityType.Edge:
						return PreprocessorSections.eledgpr;
					case EntityType.Surface: // I suppose that SURFACE is equal to PATCH is equal to SHELL
					case EntityType.Patch:
					case EntityType.Shell:
						return PreprocessorSections.elsurfpr;
					case EntityType.Region:
						return PreprocessorSections.elvolpr;
					default:
						return PreprocessorSections.Unknown;
				}
			}
			else
			{
				return PreprocessorSections.Unknown;
			}
		}

		private bool saveMeshHavingSource(Mesh mesh, bool saveWithoutHiddenElements, YesNoQuestion cancelled)
		{
			generateMaps(mesh, saveWithoutHiddenElements);
			FacesAndEdgesToWrite facesAndEdgesToWrite = new FacesAndEdgesToWrite(mesh, this.nodeMap);
			itemsToWrite = nodeMap.Count + elementMap.Count + facesAndEdgesToWrite.AllItemsCount;

			// ----------------------------
			writePropertyComments();
			writePropertyCommandFilePath();
			// ----------------------------
			writeNodeCount(nodeMap.Count);

			foreach (Node n in sourceFileParser.ReadNodes())
			{
				if (writeNode(n, sourceFileParser.CurrentLine))
					itemIndex++;
				if (Step != null) // informuj o postupu
				{
					int percent = (int)((float)itemIndex / (float)itemsToWrite * 100f);
					if (ioea.PercentDone != percent)
					{
						if (cancelled != null && cancelled())
							return false;
						ioea.PercentDone = percent;
						Step(this, ioea);
					}
				}
			}

		
			//----------------------------
			//int elementCount = (Scene.SaveMeshInCuttedForm) ? sourceFileParser.ElementCount - mesh.CuttedElements.Count : sourceFileParser.ElementCount;
			writeElementCount(elementMap.Count);
			foreach (ElementDraft e in sourceFileParser.ReadElements())
			{
				if (writeElement(e, sourceFileParser.CurrentLine))
					itemIndex++;
				if (Step != null) // informuj o postupu
				{
					int percent = (int)((float)itemIndex / (float)itemsToWrite * 100f);
					if (ioea.PercentDone != percent)
					{
						if (cancelled != null && cancelled())
							return false;
						ioea.PercentDone = percent;
						Step(this, ioea);
					}
				}
			}
			// ---------------------------
			
			//while(true) // opakovat dokud mam co cist nebo nenarazim na faces nebo edges
			//{
			//    string restOfFileLine = sourceFileParser.ReadNextLine();
			//    if (restOfFileLine == null)
			//        break;
			//    restOfFileLine = restOfFileLine.Trim();
			//    if (restOfFileLine.StartsWith(DefaultFileFormatParser.FACES_PATERN) || restOfFileLine.StartsWith(DefaultFileFormatParser.EDGES_PATERN))
			//        break;
			//    output.WriteLine(restOfFileLine);
			//}


			// zapsat zbytek, co byl v puvodnim souboru az do vyskytu faces nebo edges
			//output.WriteLine();
			sourceFileParser.LineWasSkipped += delegate
			{
				output.WriteLine(sourceFileParser.CurrentLine);
				//Console.WriteLine(sourceFileParser.CurrentLine);
			};

			foreach (FaceDraft fd in sourceFileParser.ReadFaces())
				;
			writeFaces(facesAndEdgesToWrite, cancelled);
			// ---------------------------
			foreach (EdgeDraft ed in sourceFileParser.ReadEdges())
				;
			writeEdges(facesAndEdgesToWrite, cancelled);
			//output.WriteLine();

			// prepsat zbytek souboru
			sourceFileParser.ReadToEnd();

			
			// ---------------------------
			/**/ // tady v tom dole je nejaka chyba, obcas t pri cteni facu zahlasi: Integer expected
			// zapsat zbytek, co byl v puvodnim souboru
			//sourceFileParser.LineWasSkipped += sourceFileParser_LineWasSkipped;
			//if (sourceFileParser.FaceCount == 0)
			//    sourceFileParser_LineWasSkipped(null, null);
			//while (sourceFileParser.ReadFaces().GetEnumerator().MoveNext())
			//    ;
			//if (sourceFileParser.EdgeCount == 0)
			//    sourceFileParser_LineWasSkipped(null, null);
			//while (sourceFileParser.ReadEdges().GetEnumerator().MoveNext())
			//    ;
			//sourceFileParser.ReadToEnd();

			return true;
		}

		private bool saveMeshWithoutSource(Mesh mesh, bool saveWithoutHiddenElements, YesNoQuestion cancelled)
		{
			generateMaps(mesh, saveWithoutHiddenElements);
			FacesAndEdgesToWrite facesAndEdgesToWrite = new FacesAndEdgesToWrite(mesh, this.nodeMap);
			itemsToWrite = nodeMap.Count + elementMap.Count + facesAndEdgesToWrite.AllItemsCount;

			//----------------------------
			writePropertyComments();
			writePropertyCommandFilePath();
			writeNodeCount(nodeMap.Count);
			// ---------------------------

			Node[] nodeArray = new Node[nodeMap.Count];
			nodeMap.Values.CopyTo(nodeArray, 0);
			Array.Sort<Node>(nodeArray);

			foreach (Node n in nodeArray)
			{
				writeNode(n);
				itemIndex++;
				if (Step != null) // informuj o postupu
				{
					int percent = (int)((float)itemIndex / (float)itemsToWrite * 100f);
					if (ioea.PercentDone != percent)
					{
						if (cancelled != null && cancelled())
							return false;
						ioea.PercentDone = percent;
						Step(this, ioea);
					}
				}
			}
			nodeArray = null;

			//----------------------------

			Element[] elementArray = new Element[elementMap.Count];
			elementMap.Values.CopyTo(elementArray, 0);
			Array.Sort<Element>(elementArray);

			writeElementCount(elementMap.Count);
			foreach (Element e in elementArray)
			{
				writeElement(e);
				itemIndex++;
				if (Step != null) // informuj o postupu
				{
					int percent = (int)((float)itemIndex / (float)itemsToWrite * 100f);
					if (ioea.PercentDone != percent)
					{
						if (cancelled != null && cancelled())
							return false;
						ioea.PercentDone = percent;
						Step(this, ioea);
					}
				}
			}


			// ---------------------------
			writeFaces(facesAndEdgesToWrite, cancelled);
			writeEdges(facesAndEdgesToWrite, cancelled);
			// ---------------------------

			return true;
		}

		#endregion

		#region Private methods

		private void writePropertyComments()
		{
			if (mesh.Statistics.PropertyComments.Count == 0)
				return;

			SortedDictionary<Property, string> comments = new SortedDictionary<Property, string>();
			foreach (KeyValuePair<Property, string> pair in mesh.Statistics.PropertyComments) // seradit nejdriv
				comments.Add(pair.Key, pair.Value);

			output.WriteLine();

			foreach (KeyValuePair<Property, string> pair in comments)
			{
				writePropertyComment(output, pair.Key, pair.Value);
			}

			output.WriteLine();
		}

		private static void writePropertyComment(TextWriter writer, Property property, string comment)
		{
			writer.Write(DefaultFileFormatParser.COMMENT_PATTERN + " ");
			writer.Write(DefaultFileFormatParser.PROPERTY_COMMENT_PATTERN);
			writer.Write(" " + property + ": ");
			writer.WriteLine(comment);
		}

		private void writePropertyCommandFilePath()
		{
			if (mesh.Statistics.PropertyCommands.Count == 0)
				return;
			
			// write Property commands file path
			if (!string.IsNullOrEmpty(mesh.Statistics.PropertyCommandsFile))
			{
				string filename = mesh.Statistics.PropertyCommandsFile;
				if (Utilities.Functions.CheckIfFileIsInSameDirectory(mesh.Statistics.PropertyCommandsFile, Path.GetDirectoryName(mesh.Filename))) // if same directories
					filename = Path.GetFileName(mesh.Statistics.PropertyCommandsFile); // make relative path

				output.Write(DefaultFileFormatParser.COMMENT_PATTERN + " ");
				output.Write(DefaultFileFormatParser.PROPERTY_DESCRIPTION_FILE_PATTERN + ": ");
				output.WriteLine(filename);
				output.WriteLine();
			}
		}

		private void writeNodeCount(int nodeCount)
		{
			output.WriteLine(nodeCount);
		}

		private bool writeNode(Node node, string line)
		{
			Property propertyToWrite = Property.Zero;
			Node orig;
			if (nodeMap.TryGetValue(node.ID, out orig))
				propertyToWrite = orig.Property;
			else
			{
				//output.WriteLine(line); // uzel v siti neni, opisu cely radek a koncim
				return false;
			}
			// ---------------------------------------
			string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			StringBuilder text = new StringBuilder();
			text.Append(parts[0]);
			text.Append(" ");
			text.Append(parts[1]);
			text.Append(" ");
			text.Append(parts[2]);
			text.Append(" ");
			text.Append(parts[3]);
			text.Append(" ");

			//if (propertyToWrite.IsZero)
			//    text.Append("0"); // zadna vlastnost
			//else
			//{
			//    text.Append("1 1 "); // jedna vlastnost, typ - uzel
			//    text.Append(propertyToWrite.ToString());
			//}
			text.Append(orig.PropertyListInDefaultFormat());
			// ---------------------------------------
			output.WriteLine(text.ToString());
			return true;
		}

		private void writeElementCount(int elementCount)
		{
			output.WriteLine(elementCount);
		}

		private bool writeElement(ElementDraft element, string line)
		{
			Property propertyToWrite = Property.Zero;
			Element orig;
			if (elementMap.TryGetValue(element.ID, out orig))
				propertyToWrite = orig.Property;
			else
			{
				//output.WriteLine(line); // prvek v siti neni, tak koncim
				return false;
			}
			// ---------------------------------------
			string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			StringBuilder text = new StringBuilder();

			text.Append(parts[0]); // cislo prvku
			text.Append(" ");
			text.Append(parts[1]); // typ prvku
			text.Append(" ");
			
			// zapsat uzly
			int nodeCountOfElement = Element.MapElementTypeToNodeCount(element.Type);
			for (int i = 0; i < nodeCountOfElement; i++)
			{
				text.Append(parts[i + 2]);
				text.Append(" ");
			}
			// zapsat property
			text.Append(propertyToWrite.ToString());
			// opsat zbytek ze vstupniho souboru
			for (int i = 3 + nodeCountOfElement; i < parts.Length; i++)
			{
				text.Append(" ");
				text.Append(parts[i]);
			}
			// ---------------------------------------
			output.WriteLine(text.ToString());
			return true;
		}

		private void writeNode(Node node)
		{
			// 1     0.0  0.0  0.0      5       1  1
			StringBuilder text = new StringBuilder();
			AppendDescriptionOfNode(node, text, mesh);
			output.WriteLine(text.ToString());
		}

		private void writeElement(Element element)
		{
			//1       5           1 2 5 4      12
			StringBuilder text = new StringBuilder();
			AppendDesriptionOfElement(element, text);
			output.WriteLine(text.ToString());
		}

		private void writeFaces(FacesAndEdgesToWrite facesAndEdgesToWrite, YesNoQuestion cancelled)
		{
			if (facesAndEdgesToWrite.FaceCount == 0)
				return;

			output.WriteLine(DefaultFileFormatParser.FACES_PATERN + " " + facesAndEdgesToWrite.FaceCount);
			foreach (string itemDescription in facesAndEdgesToWrite.GetDescriptionsOfFaces())
			{
				output.WriteLine(itemDescription);
				// -------------------------------------------
				itemIndex++;
				if (Step != null) // informuj o postupu
				{
					int percent = (int)((float)itemIndex / (float)itemsToWrite * 100f);
					if (ioea.PercentDone != percent)
					{
						if (cancelled != null && cancelled())
							return;
						ioea.PercentDone = percent;
						Step(this, ioea);
					}
				}
			}
		}

		private void writeEdges(FacesAndEdgesToWrite facesAndEdgesToWrite, YesNoQuestion cancelled)
		{
			if (facesAndEdgesToWrite.EdgeCount == 0)
				return;

			output.WriteLine(DefaultFileFormatParser.EDGES_PATERN + " " + facesAndEdgesToWrite.EdgeCount);
			foreach (string itemDescription in facesAndEdgesToWrite.GetDescriptionOfEdges())
			{
				output.WriteLine(itemDescription);
				// -----------------------------------
				itemIndex++;
				if (Step != null) // informuj o postupu
				{
					int percent = (int)((float)itemIndex / (float)itemsToWrite * 100f);
					if (ioea.PercentDone != percent)
					{
						if (cancelled != null && cancelled())
							return;
						ioea.PercentDone = percent;
						Step(this, ioea);
					}
				}
			}
		}

		// ======================================================================

		private void generateMaps(Mesh mesh, bool saveWithoutHiddenElements)
		{
			this.nodeMap = new Dictionary<int, Node>();
			this.elementMap = new Dictionary<int, Element>();

			foreach (Element e in mesh.Elements)
			{
				// pokud se uklada uriznuta sit, tak zkontrolovatm, jestli tento prvek neni v seznamu uriznutych...
				if (saveWithoutHiddenElements && mesh.HiddenElements.Contains(e))
					continue;
				elementMap[e.ID] = e;
				foreach (Node n in e.IterateThroughAllNodesIncludingEdgeMiddleNodes())
					nodeMap[n.ID] = n;
			}
		}

		private void replaceFile(string src, string dest)
		{
			if (src != dest)
			{
				if (File.Exists(dest))
					File.Delete(dest);
				File.Move(src, dest);
			}
		}

		private void initOutput(string filename)
		{
			output = new StreamWriter(filename);
			//lineNumber = 0;
		}

		#endregion

		#region Public Static Methods

		public static void AppendDescriptionOfNode(Node node, StringBuilder text, Mesh mesh)
		{
			Vector3 transformedPosition = (node.Position / mesh.ResizeFactor) + mesh.PositionOffset;
			text.Append(node.ID); text.Append(" ");
			text.Append(transformedPosition.X.ToString(CultureProvider.EnglishCulture)); text.Append(" ");
			text.Append(transformedPosition.Y.ToString(CultureProvider.EnglishCulture)); text.Append(" ");
			text.Append(transformedPosition.Z.ToString(CultureProvider.EnglishCulture)); text.Append(" ");
			//if (!node.Property.IsZero)
			//    text.Append("1 1 " + node.Property);
			//else
			//    text.Append("0");
			text.Append(node.PropertyListInDefaultFormat());
		}

		public static void AppendDesriptionOfElement(Element element, StringBuilder text)
		{
			text.Append(element.ID);
			text.Append(" ");
			text.Append((int)element.ElementType);
			text.Append(" ");

			//int nodeCountOfElement = Element.MapElementTypeToNodeCount(element.Type);
			foreach (Node n in element.IterateThroughAllNodesIncludingEdgeMiddleNodes())
				text.Append(n.ID.ToString() + " ");
			text.Append(element.Property.ToString());
			// ---------------------------------------
		}

		public static void AppendDescriptionOfFace(Element2D face, StringBuilder text, bool completeInfo)
		{
			//pair.Key.ToString() + " " + pair.Value
			if (completeInfo)
			{
				text.Append(face.NodeCount);
				text.Append(" ");
			}
			foreach (Node n in face.IterateThroughAllNodes())
			{
				text.Append(n.ID.ToString());
				text.Append(" ");
			}
			if (completeInfo)
				text.Append(face.Property.ToString());
		}

		public static void AppendDescriptionOfEdge(WingedEdge edge, StringBuilder text, bool completeInfo)
		{
			text.Append(edge.BeginNode.ID.ToString());
			text.Append(" ");
			text.Append(edge.EndNode.ID.ToString());
			if (completeInfo)
			{
				text.Append(" ");
				text.Append(edge.Property.ToString());
			}
		}

		#endregion

		#region IProgressNotifier Members

		public event MeshIOEventHandler Step;

		#endregion

		#region FacesAndEdgesToWrite class

		/// <summary>
		/// trida pro sber, uchovani a poskytnuti seznamu ploch a hran, jez maji byt zapsany do vystupniho souboru
		/// </summary>
		private class FacesAndEdgesToWrite
		{
			private List<KeyValuePair<TriangleMark, Property>> triangles;
			private List<KeyValuePair<QuadMark, Property>> quads;
			private List<KeyValuePair<EdgeMark, Property>> edges;

			public int FaceCount { get { return triangles.Count + quads.Count; } }
			public int EdgeCount { get { return edges.Count; } }
			public int AllItemsCount { get { return FaceCount + EdgeCount; } }

			public FacesAndEdgesToWrite(Mesh mesh, Dictionary<int, Node> nodeMap)
			{
				triangles = new List<KeyValuePair<TriangleMark, Property>>();
				quads = new List<KeyValuePair<QuadMark, Property>>();
				edges = new List<KeyValuePair<EdgeMark, Property>>();
				// -----------------------------------------------------------
				addAllFacesOnSurfaceWithProperty(mesh);
				addAllEdgesOnSurfaceWithProperty(mesh);
				addHiddenFaces(mesh.HiddenItemsProperties, nodeMap);
				addHiddenEdge(mesh.HiddenItemsProperties, nodeMap);
			}

			private void addHiddenFaces(HiddenItemsProperties hiddenItemsProperties, Dictionary<int, Node> nodeMap)
			{
				foreach (KeyValuePair<TriangleMark, Property> pair in hiddenItemsProperties.TriangleProperties)
					if (nodeMap.ContainsKey(pair.Key.Node1ID) && nodeMap.ContainsKey(pair.Key.Node2ID) && nodeMap.ContainsKey(pair.Key.Node3ID))
						triangles.Add(pair);
				foreach (KeyValuePair<QuadMark, Property> pair in hiddenItemsProperties.QuadProperties)
					if (nodeMap.ContainsKey(pair.Key.Node1ID) && nodeMap.ContainsKey(pair.Key.Node2ID) && nodeMap.ContainsKey(pair.Key.Node3ID) && nodeMap.ContainsKey(pair.Key.Node4ID))
						quads.Add(pair);
			}

			private void addHiddenEdge(HiddenItemsProperties hiddenItemsProperties, Dictionary<int, Node> nodeMap)
			{
				foreach (KeyValuePair<EdgeMark, Property> pair in hiddenItemsProperties.EdgeProperties)
					if (nodeMap.ContainsKey(pair.Key.Node1ID) && nodeMap.ContainsKey(pair.Key.Node2ID))
						edges.Add(pair);
			}

			private void addAllFacesOnSurfaceWithProperty(Mesh mesh)
			{
				foreach (Element2D face in mesh.Faces)
				{
					if (!face.Property.IsZero && face is IFaceOfElement3D)
					{
						Triangle t = face as Triangle;
						if (t != null)
							triangles.Add(new KeyValuePair<TriangleMark, Property>(new TriangleMark(t.Node1.ID, t.Node2.ID, t.Node3.ID), face.Property));
						Quadrilateral q = face as Quadrilateral;
						if (q != null)
							quads.Add(new KeyValuePair<QuadMark, Property>(new QuadMark(q.Node1.ID, q.Node2.ID, q.Node3.ID, q.Node4.ID), face.Property));
					}
				}
			}

			private void addAllEdgesOnSurfaceWithProperty(Mesh mesh)
			{
				foreach (WingedEdge edge in mesh.Edges)
				{
					if (!edge.Property.IsZero)
						edges.Add(new KeyValuePair<EdgeMark, Property>(new EdgeMark(edge.BeginNode.ID, edge.EndNode.ID), edge.Property));
				}
			}

			public IEnumerable<string> GetDescriptionsOfFaces()
			{
				foreach (KeyValuePair<TriangleMark, Property> pair in triangles)
					yield return pair.Key.ToString() + " " + pair.Value;
				foreach (KeyValuePair<QuadMark, Property> pair in quads)
					yield return pair.Key.ToString() + " " + pair.Value;
			}

			public IEnumerable<string> GetDescriptionOfEdges()
			{
				foreach (KeyValuePair<EdgeMark, Property> pair in edges)
					yield return pair.Key.ToString() + " " + pair.Value;
			}
		}

		#endregion

	}
}

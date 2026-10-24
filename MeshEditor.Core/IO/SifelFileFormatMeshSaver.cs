using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MeshEditor.Construction;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using OpenTK.Mathematics;
using MeshEditor.Common.GeometryMarkers;
using Utils = MeshEditor.Utilities.Functions;

namespace MeshEditor.IO
{
	/// <summary>
	/// trida poskytujici funkce pro zapis site do souboru ve standardnim formatu
	/// </summary>
	public class SifelFileFormatMeshSaver : IMeshSaver
	{

		#region Fields, contructor

		private TextWriter output;
		private ISifelFileFormatParser sourceFileParser;

		private Mesh mesh;
		private Dictionary<int, Node> nodeMap;
		private Dictionary<int, Element> elementMap;

		private int itemIndex;
		private int itemsToWrite;
		private MeshIOEventArgs ioea;

		public SifelFileFormatMeshSaver()
		{
			this.sourceFileParser = null;
			this.output = null;
			this.nodeMap = null;
			this.elementMap = null;
			this.mesh = null;
		}

		#endregion

		#region IMeshSaver Members

		public void SaveMesh(Mesh mesh, string filename, bool saveWithoutHiddenElements, YesNoQuestion cancelled)
		{
			itemIndex = itemsToWrite = 0;
			ioea = new MeshIOEventArgs(0, "Saving mesh", null);
			this.mesh = mesh;
			string destinationFile = Path.GetTempFileName();
			initOutput(destinationFile);

			writePropertyCommandsFile(mesh.Statistics, filename);

			bool completed;
			try
			{
				if (mesh.LoadedFromSifelFileFormat && File.Exists(mesh.SourceFilename))
				{
					sourceFileParser = new SifelFileFormatParser(mesh.SourceFilename);
					completed = saveMesh(mesh, saveWithoutHiddenElements, cancelled, rewriteNodeCoordinatesFromSource: true);
				}
				else
				{
					sourceFileParser = null;
					completed = saveMesh(mesh, saveWithoutHiddenElements, cancelled, rewriteNodeCoordinatesFromSource: false);
				}
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
				mesh.SourceFilename = filename;
				mesh.LoadedFromSifelFileFormat = true;
				mesh.UnsavedChanges = false;
			}
		}

		public void SaveMesh(IMeshFileParser fileParser, string destination, YesNoQuestion cancelled)
		{
			throw new NotSupportedException();
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
				writer.WriteLine(SifelFileFormatParser.COMMENT_PATTERN + " PREPROCESSOR COMMANDS FILE (linked to mesh \"" + meshPathToWrite + "\")");

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
					writer.WriteLine(SifelFileFormatParser.BEGIN_SECTION_PATTERN + section.ToString());
					foreach (PropertyCommand command in sections[section])
					{
						//// write comment if exists, in either case add new line
						//string comment;
						//if (meshStatistics.PropertyComments.TryGetValue(pair.Property, out comment))
						//	writePropertyComment(writer, pair.Property, comment);
						// write command
						writer.WriteLine(command.ToString());
					}
					writer.WriteLine(SifelFileFormatParser.END_SECTION_PATTERN + section.ToString());
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

			if (nodeSectionCommands.Contains(commandType)) // Node sections
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
			else if (elementSectionCommands.Contains(commandType)) // Element sections
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

		private bool saveMesh(Mesh mesh, bool saveWithoutHiddenElements, YesNoQuestion cancelled, bool rewriteNodeCoordinatesFromSource)
		{
			generateMaps(mesh, saveWithoutHiddenElements);
			FacesAndEdgesToWrite facesAndEdgesToWrite = new FacesAndEdgesToWrite(mesh, this.nodeMap);
			itemsToWrite = nodeMap.Count + elementMap.Count + facesAndEdgesToWrite.AllItemsCount;

			//----------------------------
			writePropertyComments();
			writePropertyCommandFilePath();
			writeNodeCount(nodeMap.Count);
			// ---------------------------

			IEnumerable<Node> nodeSequence;
			if (rewriteNodeCoordinatesFromSource)
			{
				nodeSequence = sourceFileParser.ReadNodes();
			}
			else
			{
				Node[] nodeArray = new Node[nodeMap.Count];
				nodeMap.Values.CopyTo(nodeArray, 0);
				Array.Sort<Node>(nodeArray);
				nodeSequence = nodeArray;
			}

			foreach (Node n in nodeSequence)
			{
				if (rewriteNodeCoordinatesFromSource)
				{
					if (writeNode(n, sourceFileParser.CurrentLine))
						itemIndex++;
				}
				else
				{
					writeNode(n);
					itemIndex++;
				}

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
			nodeSequence = null;
			output.WriteLine();
			//----------------------------

			EdgeFacePropertySet edgeFacePropertySet = getCurrentEdgeFacePropertySet();

			Element[] elementArray = new Element[elementMap.Count];
			elementMap.Values.CopyTo(elementArray, 0);
			Array.Sort<Element>(elementArray);

			writeElementCount(elementMap.Count);
			foreach (Element e in elementArray)
			{
				writeElement(e, edgeFacePropertySet);
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

			return true;
		}

		private EdgeFacePropertySet getCurrentEdgeFacePropertySet()
		{
			EdgeFacePropertySet edgeFacePropertySet = new EdgeFacePropertySet(mesh.HiddenItemsProperties); // copy properties of hidden edges and faces
			foreach (WingedEdge edge in mesh.Edges)
			{
				if (!edge.Property.IsZero)
					edgeFacePropertySet.AddEdgeProperty(edge);
			}
			foreach (Element2D face in mesh.Faces)
			{
				if (face is IFaceOfElement3D && !face.Property.IsZero)
					edgeFacePropertySet.AddFaceProperty(face);
			}
			return edgeFacePropertySet;
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
			writer.Write(SifelFileFormatParser.COMMENT_PATTERN + " ");
			writer.Write(SifelFileFormatParser.PROPERTY_COMMENT_PATTERN);
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
				if (Utilities.Functions.CheckIfFileIsInSameDirectory(mesh.Statistics.PropertyCommandsFile, Path.GetDirectoryName(mesh.SourceFilename))) // if same directories
					filename = Path.GetFileName(mesh.Statistics.PropertyCommandsFile); // make relative path

				output.Write(SifelFileFormatParser.COMMENT_PATTERN + " ");
				output.Write(SifelFileFormatParser.PROPERTY_DESCRIPTION_FILE_PATTERN + ": ");
				output.WriteLine(filename);
				output.WriteLine();
			}
		}

		private void writeNodeCount(int nodeCount)
		{
			output.WriteLine(nodeCount);
		}

		private void writeElementCount(int elementCount)
		{
			output.WriteLine(elementCount);
		}

		private void writeNode(Node node)
		{
			// 1     0.0  0.0  0.0      5       1  1
			StringBuilder text = new StringBuilder();
			AppendDescriptionOfNode(node, text, mesh);
			output.WriteLine(text.ToString());
		}

		private bool writeNode(Node node, string line)
		{
			Property propertyToWrite = Property.Zero;
			Node original;
			if (!nodeMap.TryGetValue(node.ID, out original))
			{
				return false;
			}
			// ---------------------------------------
			string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			Debug.Assert(parts.Length >= 4);
			StringBuilder text = new StringBuilder();
			text.Append(parts[0]); // id
			text.Append(" ");
			text.Append(parts[1]); // x coordinate
			text.Append(" ");
			text.Append(parts[2]); // y coordinate
			text.Append(" ");
			text.Append(parts[3]); // z coordinate
			text.Append(" ");

			text.Append(original.PropertyListInDefaultFormat());
			// ---------------------------------------
			output.WriteLine(text.ToString());
			return true;
		}

		private void writeElement(Element element, EdgeFacePropertySet edgeFacePropertySet)
		{
			//1       5           1 2 5 4      12
			StringBuilder text = new StringBuilder();
			AppendDescriptionOfElement(element, text);
			appendDescriptionOfEdgesAndFaces(element, text, edgeFacePropertySet);
			output.WriteLine(text.ToString());
		}

		private void appendDescriptionOfEdgesAndFaces(Element element, StringBuilder text, EdgeFacePropertySet edgeFacePropertySet)
		{
			int[] nodeIDs = element.IterateThroughAllNodesIncludingEdgeMiddleNodes().Select(node => node.ID).ToArray();

			foreach (EdgeMark edgeMark in Element.GetSequenceOfEdges(element.ElementType, nodeIDs)) // write edge properties
			{
				Property property;
				if (edgeFacePropertySet.EdgeProperties.TryGetValue(edgeMark, out property))
				{
					text.Append(" ");
					text.Append(property.ToString());
				}
				else
				{
					text.Append(" 0");
				}
			}

			// save property of all element faces (including single face of 3D element)
			foreach (object faceMark in Element.GetSequenceOfFaces(element.ElementType, nodeIDs)) // write face properties
			{
				Property property = Property.Zero;
				bool found = false;
				if (faceMark is TriangleMark triangleMark)
				{
					found = edgeFacePropertySet.TriangleProperties.TryGetValue(triangleMark, out property);
				}
				else if (faceMark is QuadMark quadMark)
				{
					if (quadMark.IsCollapsedToTriangle(out var collapsedTriangleMark))
					{
						found = edgeFacePropertySet.TriangleProperties.TryGetValue(collapsedTriangleMark, out property);
					}
					else
					{
						found = edgeFacePropertySet.QuadProperties.TryGetValue(quadMark, out property);
					}
				}

				if (found)
				{
					text.Append(" ");
					text.Append(property.ToString());
				}
				else
				{
					text.Append(" 0");
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
		}

		#endregion

		#region Public Static Methods

		public static void AppendDescriptionOfNode(Node node, StringBuilder text, Mesh mesh)
		{
			text.Append(node.ID);
			text.Append(" ");

			Vector3 transformedPosition = (node.Position / mesh.ResizeFactor) + mesh.PositionOffset;
			text.Append(transformedPosition.X.ToString(CultureProvider.EnglishCulture)); text.Append(" ");
			text.Append(transformedPosition.Y.ToString(CultureProvider.EnglishCulture)); text.Append(" ");
			text.Append(transformedPosition.Z.ToString(CultureProvider.EnglishCulture)); text.Append(" ");

			text.Append(node.PropertyListInDefaultFormat());
		}

		public static void AppendDescriptionOfElement(Element element, StringBuilder text)
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

			private void addHiddenFaces(EdgeFacePropertySet hiddenItemsProperties, Dictionary<int, Node> nodeMap)
			{
				foreach (KeyValuePair<TriangleMark, Property> pair in hiddenItemsProperties.TriangleProperties)
					if (nodeMap.ContainsKey(pair.Key.Node1ID) && nodeMap.ContainsKey(pair.Key.Node2ID) && nodeMap.ContainsKey(pair.Key.Node3ID))
						triangles.Add(pair);
				foreach (KeyValuePair<QuadMark, Property> pair in hiddenItemsProperties.QuadProperties)
					if (nodeMap.ContainsKey(pair.Key.Node1ID) && nodeMap.ContainsKey(pair.Key.Node2ID) && nodeMap.ContainsKey(pair.Key.Node3ID) && nodeMap.ContainsKey(pair.Key.Node4ID))
						quads.Add(pair);
			}

			private void addHiddenEdge(EdgeFacePropertySet hiddenItemsProperties, Dictionary<int, Node> nodeMap)
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

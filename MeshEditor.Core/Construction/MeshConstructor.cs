using System;
using System.Collections.Generic;
using System.Text;

using MeshEditor.Data;
using MeshEditor.IO;

using MeshEditor.Utilities;
using OpenTK;

using MeshEditor.Graphics;
using MeshEditor.CoreInterface;
using System.Diagnostics;
using MeshEditor.Cuts;
using System.IO;
using System.Linq;

namespace MeshEditor.Construction
{
	/// <summary>
	/// Contains methods for loading mesh from file
	/// </summary>
	public class MeshConstructor : IMeshCreator
	{

		#region Fields, Constructor

		private Dictionary<int, Node> nodes;

		private Dictionary<TriangleMark, Triangle> triangleFaces;
		private Dictionary<QuadMark, Quadrilateral> quadFaces;
		private Dictionary<EdgeMark, WingedEdge> edgeMarks;
		private EdgeFacePropertySet hiddenItemsProperties;

		private List<Beam> oneDimensionalElements;
		private Dictionary<Element2D, Node[]> additionalQuadraticNodes;

		private Dictionary<EdgeMark, Property[]> edgeProperties;

		private const float NODE_WORK_RATIO = 0.21f;
		private const float ELEMENT_WORK_RATIO = 0.73f;

		public MeshConstructor()
		{
			this.nodes = new Dictionary<int, Node>();

			this.triangleFaces = new Dictionary<TriangleMark, Triangle>();
			this.quadFaces = new Dictionary<QuadMark, Quadrilateral>();
			this.edgeMarks = new Dictionary<EdgeMark, WingedEdge>();
			this.hiddenItemsProperties = null;

			this.oneDimensionalElements = new List<Beam>();
			this.additionalQuadraticNodes = new Dictionary<Element2D, Node[]>();

			this.edgeProperties = new Dictionary<EdgeMark, Property[]>();
		}

		#endregion

		#region Load and Create Mesh (IMeshCreator Members)

		public event MeshIOEventHandler Step;

		/// <summary>
		/// Loads new mesh using object of IMeshFileParser and returns it. 
		/// </summary>
		/// <param name="meshFileParser">object that iterates through nodes and elements in file and returns them</param>
		/// <returns>new mesh</returns>
		public Mesh CreateMesh(IMeshFileParser meshFileParser, YesNoQuestion cancelled)
		{
			MeshIOEventArgs ioea = new MeshIOEventArgs(0);
			Mesh mesh = null;
			try
			{
				Vector3 lowerBound = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
				Vector3 upperBound = new Vector3(float.MinValue, float.MinValue, float.MinValue);
				MeshStatistics statistics = new MeshStatistics();
				// ====================================================================================
				IDefaultFileFormatParser advancedFileParser = meshFileParser as IDefaultFileFormatParser;
				if (advancedFileParser != null) // zaregistrovat metodu pro nacitani vlastnosti
				{
					advancedFileParser.LineWasSkipped += delegate
					{
						loadPropertyComment(advancedFileParser.CurrentLine, statistics);
					};
				}
				// ====================================================================================
				try
				{
					// nacti uzly
					int processedNodes = 0;
					foreach (Node n in meshFileParser.ReadNodes())
					{
						processNode(n, statistics);

						updateBounds(n.Position, ref lowerBound, ref upperBound);
						processedNodes++;

						if (Step != null) // informuj o postupu
						{
							int percent = (int)((float)processedNodes / (float)meshFileParser.NodeCount * NODE_WORK_RATIO * 100f);
							if (percent != ioea.PercentDone)
							{
								if (cancelled != null && cancelled())
									return null;
								ioea.PercentDone = percent;
								Step(this, ioea);
							}
						}
					}
				}
				catch (ArgumentException ex)
				{
					throw new MeshConstructingException("There are two nodes with same index.", ex);
				}

				Vector3 meshPositionOffset;
				float meshResizeFactor;
				normalizeMesh(ref lowerBound, ref upperBound, out meshPositionOffset, out meshResizeFactor);

				bool loadedFromDefaultFileFormat = meshFileParser is DefaultFileFormatParser;

				// vytvor kostru nove meshe
				mesh = new Mesh(meshFileParser.Filename, loadedFromDefaultFileFormat, meshPositionOffset, meshResizeFactor);

				this.hiddenItemsProperties = mesh.HiddenItemsProperties;
				mesh.Statistics = statistics;

				// nacti prvky
				//HashSet<Property> elementProperties = new HashSet<Property>();
				//HashSet<ElementType> elementTypes = new HashSet<ElementType>();
				int processedElements = 0;
				foreach (ElementDraft draft in meshFileParser.ReadElements())
				{
					// -----------------------------------------------
					mesh.Statistics.AddProperty(draft.Property, EntityType.Region);
					mesh.Statistics.AddElementType(draft.Type);
					// -----------------------------------------------

					Element newElement = createElementFrom(draft);

					if (draft.EdgeProperties != null)
					{
						processEdgePropertiesOfElement(draft, mesh.Statistics);
					}

					if (draft.FaceProperties != null)
					{
						processFacePropertiesOfElement(draft, mesh.Statistics);
					}

					processElement(newElement); // vytvor facy daneho prvku, postupne zjisti ktere jsou povrchove
					mesh.PushElement(newElement); // vloz prvek do site
					processedElements++;

					if (Step != null) // informuj o postupu
					{
						int percent = (int)((NODE_WORK_RATIO + (float)processedElements / (float)meshFileParser.ElementCount * ELEMENT_WORK_RATIO) * 100f);
						if (percent != ioea.PercentDone)
						{
							if (cancelled != null && cancelled())
								return null;
							ioea.PercentDone = percent;
							Step(this, ioea);
						}
					}
				}

				// ==================================================================
				// nacti property ploch a hran, pokud to lze
				if (advancedFileParser != null) // pokud se jedna o format souboru obsahujici facy a hrany, tak...
				{
					/**/ // jeste tu chybi referovat o postupu v teto funkci
					loadFaceAndEdgeProperties(advancedFileParser, mesh.Statistics);
					// load rest of file (parse comments)
					advancedFileParser.ReadToEnd();
				}
				// ==================================================================
				// load property command file
				if (!string.IsNullOrEmpty(mesh.Statistics.PropertyCommandsFile))
				{
					loadPropertyCommandsFile(mesh.Statistics, Path.GetDirectoryName(mesh.Filename));
				}
				// ==================================================================

				// vytvor povrchovou reprezentaci
				Histogram edgeAnglesHistogram = new Histogram(0f, 180f, 1f);
				createSurfaceRepresentation(mesh, iterateThroughAllFaces(), ref edgeAnglesHistogram, ioea, cancelled);

				// ==================================================================

				mesh.TotalNodeCount = nodes.Count;
				mesh.InitializeMesh(edgeAnglesHistogram);

				if (cancelled != null && cancelled())
					return null;

				//Console.WriteLine(hiddenItemsProperties);

				// vrat mesh
				return mesh;
			}
			catch (FileParserException)
			{
				throw;
			}
			catch (Exception ex) // nastala nejaka chyba, zmenim jeji typ, pridam cislo radku a pridam tuto chybu jako vnitrni vyjimku
			{
				throw new MeshConstructingException(ex.Message + Environment.NewLine + Environment.NewLine + "Filename: " + meshFileParser.Filename + Environment.NewLine + "Line number: " + meshFileParser.CurrentLineNumber, ex.InnerException);
			}
			finally
			{
				nodes = null;
				triangleFaces = null;
				quadFaces = null;
				edgeMarks = null;
				hiddenItemsProperties = null;
				oneDimensionalElements = null;
				additionalQuadraticNodes = null;
			}
		}

		private void loadPropertyCommandsFile(MeshStatistics meshStatistics, string meshFileFolder)
		{
			string commandsFile = meshStatistics.PropertyCommandsFile;
			if (!Path.IsPathRooted(commandsFile)) // if its relative path
			{
				commandsFile = Path.GetFullPath(Path.Combine(meshFileFolder, commandsFile)); // look for file in folder with mesh file
			}

			Debug.Assert(File.Exists(commandsFile));
			if (!File.Exists(commandsFile)) // do nothing if file does not exists
				return;

			using (TextReader reader = new StreamReader(commandsFile))
			{
				int lineNumber = 0;
				try
				{
					string line;
					PreprocessorSections currentSection = PreprocessorSections.Unknown;
					while ((line = reader.ReadLine()) != null)
					{
						++lineNumber;
						line = line.Trim();
						if (string.IsNullOrEmpty(line))
							continue;

						int indexOfComment = line.IndexOf(DefaultFileFormatParser.COMMENT_PATTERN);
						if (indexOfComment == 0) // if starts with comment skip line
							continue;
						if (indexOfComment > 0) // if comment is appended trim it
							line = line.Substring(0, indexOfComment);

						if (line.StartsWith(DefaultFileFormatParser.BEGIN_SECTION_PATTERN))
						{
							currentSection = convertTextToPreprocessorSection(line);
							continue;
						}
						else if (line.StartsWith(DefaultFileFormatParser.END_SECTION_PATTERN))
						{
							Debug.Assert(convertTextToPreprocessorSection(line) == currentSection);
							continue;
						}

						PropertyCommand command = PropertyCommand.CreateFromString(line);
						Property? property = command.GetPropertyValue();
						if (property.HasValue)
						{
							EntityType targetEntityType = convertPreprocessorSectionToPropertyTarget(currentSection); // deduce entity type from section name
							PropertyEntityPair pair = new PropertyEntityPair(property.Value, targetEntityType);
							List<PropertyCommand> commands;
							if (!meshStatistics.PropertyCommands.TryGetValue(pair, out commands))
								commands = meshStatistics.PropertyCommands[pair] = new List<PropertyCommand>();
							commands.Add(command);
						}
						else
							throw new ArgumentNullException("property");
					}
				}
				catch (Exception ex)
				{
					throw new MeshConstructingException("Error in property command file at line " + lineNumber + ".", ex);
				}
			}
		}

		private static EntityType convertPreprocessorSectionToPropertyTarget(PreprocessorSections section)
		{
			switch (section)
			{
				case PreprocessorSections.nodvertpr:
					return EntityType.Vertex;
				case PreprocessorSections.nodedgpr:
				case PreprocessorSections.eledgpr:
					return EntityType.Edge;
				case PreprocessorSections.nodsurfpr:
				case PreprocessorSections.elsurfpr:
					return EntityType.Surface; // I suppose that SURFACE is equal to PATCH is equal to SHELL
				case PreprocessorSections.nodvolpr:
				case PreprocessorSections.elvolpr:
					return EntityType.Region;
				//case PreprocessorSections.Unknown:
				//case PreprocessorSections.files:
				//case PreprocessorSections.probdesc:
				//case PreprocessorSections.loadcase:
				//case PreprocessorSections.outdrv:
				//case PreprocessorSections.gfunct:
				default:
					//throw new NotSupportedException();
					return EntityType.Region; /**/ /* default value is considered to be Region */
			}
		}

		private static PreprocessorSections convertTextToPreprocessorSection(string text)
		{
			if (text.StartsWith(DefaultFileFormatParser.BEGIN_SECTION_PATTERN))
				text = text.Substring(DefaultFileFormatParser.BEGIN_SECTION_PATTERN.Length); // cut begin section token
			else if (text.StartsWith(DefaultFileFormatParser.END_SECTION_PATTERN))
				text = text.Substring(DefaultFileFormatParser.END_SECTION_PATTERN.Length); // cut end section token
			else
				throw new ArgumentException("Text does not match the pattern.");

			return (PreprocessorSections)Enum.Parse(typeof(PreprocessorSections), text, /*ignoreCase: */ true);
		}

		//private static bool tryConvertTextToPreprocessorSection(string text, out PreprocessorSections section)
		//{
		//    Debug.Assert(text != null);
		//    if(string.IsNullOrEmpty(text))
		//    {
		//        section = PreprocessorSections.Unknown;
		//        return false;
		//    }
		//    if (text.StartsWith(DefaultFileFormatParser.BEGIN_SECTION_PATTERN))
		//        text = text.Substring(DefaultFileFormatParser.BEGIN_SECTION_PATTERN.Length);
		//    else if (text.StartsWith(DefaultFileFormatParser.END_SECTION_PATTERN))
		//        text = text.Substring(DefaultFileFormatParser.END_SECTION_PATTERN.Length);

		//    section = PreprocessorSections.Unknown;
		//    try
		//    {
		//        section = (PreprocessorSections)Enum.Parse(typeof(PreprocessorSections), text, /*ignoreCase: */ true);
		//    }
		//    catch (ArgumentException)
		//    {
		//        return false;
		//    }
		//    return true;
		//}

		private void loadFaceAndEdgeProperties(IDefaultFileFormatParser advancedFileParser, MeshStatistics statistics)
		{
			// <!>
			// postupne pri nacitani property budu kontrolovat jestli uz neni obsazeno v triangleFaces nebo quadFaces, pokud ano, tak nastavit property, pokud ne, tak ulozit do HiddenItemsProperties
			foreach (FaceDraft fd in advancedFileParser.ReadFaces())
			{
				statistics.AddProperty(fd.Property, EntityType.Surface);

				if (fd.NodeIDs.Length == 3)
				{
					TriangleMark mark = new TriangleMark(fd.NodeIDs[0], fd.NodeIDs[1], fd.NodeIDs[2]);
					Triangle t;
					if (triangleFaces.TryGetValue(mark, out t))
						t.Property = fd.Property;
					else
						hiddenItemsProperties.Add(ref mark, fd.Property);
				}
				else if (fd.NodeIDs.Length == 4)
				{
					QuadMark mark = new QuadMark(fd.NodeIDs[0], fd.NodeIDs[1], fd.NodeIDs[2], fd.NodeIDs[3]);
					Quadrilateral q;
					if (quadFaces.TryGetValue(mark, out q))
						q.Property = fd.Property;
					else
						hiddenItemsProperties.Add(ref mark, fd.Property);
				}
				else
					throw new NotSupportedException("Unsupported face type");
			}


			// nacti property hran a vytvor edgeMarks
			foreach (EdgeDraft ed in advancedFileParser.ReadEdges())
			{
				statistics.AddProperty(ed.Property, EntityType.Edge);

				EdgeMark mark = new EdgeMark(ed.Node1ID, ed.Node2ID);
				hiddenItemsProperties.Add(ref mark, ed.Property);
			}

			//advancedFileParser.ReadToEnd();
		}

		private void loadPropertyComment(string line, MeshStatistics statistics)
		{
			line = line.Trim(); // oriznu prazdne znaky zepredu a zezadu

			if (!string.IsNullOrEmpty(line) && line.StartsWith(DefaultFileFormatParser.COMMENT_PATTERN))
			{
				// pokud komentar obsahuje komentar k vlastnosti tak ho nacist a ulozit tady
				string comment = line.Substring(DefaultFileFormatParser.COMMENT_PATTERN.Length).TrimStart();
				string[] commentParts = comment.Split(new char[] { ' ', '\t', ':' }, 2, StringSplitOptions.RemoveEmptyEntries);

				if (commentParts != null && commentParts.Length > 1) // pokud jde o komentar k cislum vlastnosti
				{
					if (string.Compare(commentParts[0], DefaultFileFormatParser.PROPERTY_COMMENT_PATTERN, true) == 0) // property comment
					{
						string[] suffixParts = commentParts[1].Split(new char[] { ' ', '\t', ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
						int propertyNumber;
						if (suffixParts.Length > 1 && int.TryParse(suffixParts[0], out propertyNumber))
							statistics.PropertyComments[new Property(propertyNumber)] = suffixParts[1];
					}
					else if (string.Compare(commentParts[0], DefaultFileFormatParser.PROPERTY_DESCRIPTION_FILE_PATTERN, true) == 0) // property commands file path
					{
						statistics.PropertyCommandsFile = commentParts[1];
					}
				}
			}
		}

		private static void updateBounds(Vector3 point, ref Vector3 lowerBound, ref Vector3 upperBound)
		{
			if (point.X < lowerBound.X) // X
				lowerBound.X = point.X;
			if (point.X > upperBound.X)
				upperBound.X = point.X;
			if (point.Y < lowerBound.Y) // Y
				lowerBound.Y = point.Y;
			if (point.Y > upperBound.Y)
				upperBound.Y = point.Y;
			if (point.Z < lowerBound.Z) // Z
				lowerBound.Z = point.Z;
			if (point.Z > upperBound.Z)
				upperBound.Z = point.Z;
		}

		/// <summary>
		/// Normalizes positions of nodes in mesh. Translates center of nodes to the origin and normalizes their distance from origin.
		/// </summary>
		/// <param name="lowerBound">lower bound of node positions</param>
		/// <param name="upperBound">upper bound of node positions</param>
		private void normalizeMesh(ref Vector3 lowerBound, ref Vector3 upperBound, out Vector3 offset, out float factor)
		{
			offset = Utilities.Functions.GetCenterOfLineSegment(ref lowerBound, ref upperBound);
			float length = (upperBound - lowerBound).Length;
			factor = (length > 0f) ? (2f * Scene.RADIUS_OF_NORMALIZED_MESH) / length : 1f;
			foreach (Node n in nodes.Values) // prepocitam pozici kazdeho uzlu
				n.Position = (n.Position - offset) * factor;
			lowerBound = (lowerBound - offset) * factor; // jeste prepocitam meze
			upperBound = (upperBound - offset) * factor;
		}

		#endregion

		#region Element processing

		private Element createElementFrom(ElementDraft draft)
		{
			Element newElement;

			//ApproximationType approxType = Element.GetApproximationTypeFrom(draft.Type);
			ElementType elementType = draft.Type;
			Node[] nodesOfElement = new Node[draft.NodeIDs.Length];

			for (int i = 0; i < nodesOfElement.Length; i++)
			{
				Node n;
				if (nodes.TryGetValue(draft.NodeIDs[i], out n))
					nodesOfElement[i] = n;
				else
					throw new MeshConstructingException("Node " + draft.NodeIDs[i] + " in element " + draft.ID + " was not defined.");
			}

			switch (draft.Type)
			{
				case ElementType.BeamLinear:
					newElement = new Beam(draft.ID, elementType, nodesOfElement[0], nodesOfElement[1]);
					break;
				case ElementType.BeamQuadratic:
					newElement = new QuadraticBeam(draft.ID, elementType, nodesOfElement[0], nodesOfElement[1], nodesOfElement[2]);
					break;
				case ElementType.TriangleLinear:
					newElement = new Triangle(draft.ID, elementType, nodesOfElement[0], nodesOfElement[1], nodesOfElement[2]);
					break;
				case ElementType.TriangleQuadratic:
					newElement = new Triangle(draft.ID, elementType, nodesOfElement[0], nodesOfElement[1], nodesOfElement[2]);
					additionalQuadraticNodes[(Element2D)newElement] = new Node[] { nodesOfElement[3], nodesOfElement[4], nodesOfElement[5] };
					break;
				case ElementType.QuadLinear:
					newElement = new Quadrilateral(draft.ID, elementType, nodesOfElement[0], nodesOfElement[1], nodesOfElement[2], nodesOfElement[3]);
					break;
				case ElementType.QuadQuadratic:
					newElement = new Quadrilateral(draft.ID, elementType, nodesOfElement[0], nodesOfElement[1], nodesOfElement[2], nodesOfElement[3]);
					additionalQuadraticNodes[(Element2D)newElement] = new Node[] { nodesOfElement[4], nodesOfElement[5], nodesOfElement[6], nodesOfElement[7] };
					break;
				case ElementType.TetrahedronLinear:
				case ElementType.TetrahedronQuadratic:
					newElement = new Tetrahedron(draft.ID, elementType, nodesOfElement);
					break;
				case ElementType.SquarePyramidLinear:
				case ElementType.SquarePyramidQuadratic:
					newElement = new Pyramid(draft.ID, elementType, nodesOfElement);
					break;
				case ElementType.TriangularPrismLinear:
				case ElementType.TriangularPrismQuadratic:
					newElement = new Wedge(draft.ID, elementType, nodesOfElement);
					break;
				case ElementType.HexahedronLinear:
				case ElementType.HexahedronQuadratic:
					newElement = new Hexahedron(draft.ID, elementType, nodesOfElement);
					break;
				default:
					throw new ArgumentException("This argument is not supported.", "draft.Type");
			}
			// set property of new element
			newElement.Property = draft.Property;

			return newElement;
		}

		private void processEdgePropertiesOfElement(ElementDraft draft, MeshStatistics statistics)
		{
			Debug.Assert(draft.EdgeProperties != null);

			int index = 0;
			foreach (EdgeMark edgeMark in Element.GetSequenceOfEdges(draft.Type, draft.NodeIDs))
			{
				if (index >= draft.EdgeProperties.Length)
					break;
				Property property = new Property(draft.EdgeProperties[index]);
				if (property != Property.Zero)
				{
					EdgeMark mark = edgeMark;
					// -----------------------------------------------------------
					statistics.AddProperty(property, EntityType.Edge);
					hiddenItemsProperties.Add(ref mark, property);
					// -----------------------------------------------------------
				}
				index++;
			}
		}

		private void processFacePropertiesOfElement(ElementDraft draft, MeshStatistics statistics)
		{
			Debug.Assert(draft.FaceProperties != null);

			int index = 0;
			foreach (object mark in Element.GetSequenceOfFaces(draft.Type, draft.NodeIDs))
			{
				if (index >= draft.FaceProperties.Length)
					break;
				Property property = new Property(draft.FaceProperties[index]);
				if (property != Property.Zero)
				{
					// -----------------------------------------------------------
					statistics.AddProperty(property, EntityType.Surface);
					if (mark is TriangleMark)
					{
						TriangleMark tm = (TriangleMark)mark;
						hiddenItemsProperties.Add(ref tm, property);
					}
					else if (mark is QuadMark)
					{
						QuadMark qm = (QuadMark)mark;
						hiddenItemsProperties.Add(ref qm, property);
					}
					// -----------------------------------------------------------
				}
				index++;
			}
		}

		private void processNode(Node n, MeshStatistics statistics)
		{
			// add to nodes dictionary
			nodes.Add(n.ID, n);

			// process properties
			statistics.AddProperty(n.Property, EntityType.Vertex);
			if (n.Properties != null)
			{
				foreach (PropertyEntityPair pair in n.Properties)
					statistics.AddProperty(pair.Property, pair.EntityType);
			}
		}

		private void processElement(Element e)
		{
			Element3D e3D = e as Element3D;
			if (e3D != null)
			{
				foreach (Element2D f in e3D.GenerateAllFaces(this.additionalQuadraticNodes/*do toho se zapisuje, necte se*/)) // vygeneruju plochy tohoto prvku
					processFace(f);
				return;
			}
			Element2D e2D = e as Element2D;
			if (e2D != null)
			{
				processFace(e2D); // zasadni okamzik - negeneruju novy objekt face ale predam rovnou tento 2D prvek
				return;
			}
			Beam b = e as Beam;
			if (b != null)
			{   // do nothing
				return;
			}
			throw new ArgumentException("Unknown element type");
		}

		private void processFace(Element2D face)
		{
			Triangle t = face as Triangle;
			if (t != null)
			{
				processTriangleFace(t);
				return;
			}
			Quadrilateral q = face as Quadrilateral;
			if (q != null)
			{
				processQuadFace(q);
				return;
			}
			throw new ArgumentException("Unknown face type");
		}

		private void processTriangleFace(Triangle face)
		{
			TriangleMark mark = new TriangleMark(face.Node1.ID, face.Node2.ID, face.Node3.ID);
			Triangle triangle;
			if (triangleFaces.TryGetValue(mark, out triangle))
			{
				bool t1IsFaceOfElement3D = triangle is IFaceOfElement3D;
				bool t2IsFaceOfElement3D = face is IFaceOfElement3D;

				if (t1IsFaceOfElement3D & t2IsFaceOfElement3D) // both are internal faces of neighboring 3D elements => remove both faces
				{
					additionalQuadraticNodes.Remove(triangle);
					triangleFaces.Remove(mark); // je to vnitrni plocha, odstran ji z povrchove reprezentace

					// pokud ma nenulovou vlastnost - tak ji uloz <!>
					if (!triangle.Property.IsZero)
					{
						hiddenItemsProperties.Add(ref mark, triangle.Property);
					}
				}
				else if (t2IsFaceOfElement3D) // first is 2D element, second is face of 3D element
				{
					triangleFaces[mark] = face; // replace 2D element with face of 3D element
					face.AddTwinElement(triangle); // add 2D element as twin element
				}
				else // second is 2D element
				{
					triangle.AddTwinElement(face);
				}
			}
			else
			{
				triangleFaces.Add(mark, face);
				// priradit vlastnost <!>
				if (face is IFaceOfElement3D)
				{
					Property property;
					if (hiddenItemsProperties.TryGetPropertyAndRemove(ref mark, out property))
					{
						face.Property = property;
					}
				}
			}
		}

		private void processQuadFace(Quadrilateral face)
		{
			QuadMark mark = new QuadMark(face.Node1.ID, face.Node2.ID, face.Node3.ID, face.Node4.ID);
			Quadrilateral quad;
			if (quadFaces.TryGetValue(mark, out quad))
			{
				bool q1IsFaceOfElement3D = quad is IFaceOfElement3D;
				bool q2IsFaceOfElement3D = face is IFaceOfElement3D;

				if (q1IsFaceOfElement3D & q2IsFaceOfElement3D) // both are internal faces of neighboring 3D elements => remove both faces
				{
					additionalQuadraticNodes.Remove(quad);
					quadFaces.Remove(mark); // je to vnitrni plocha, odstran ji z povrchove reprezentace

					// plocha je vnitrni, pokud ma nenulovou vlastnost - tak ji uloz <!>
					if (!quad.Property.IsZero)
					{
						hiddenItemsProperties.Add(ref mark, quad.Property);
					}
				}
				else if (q2IsFaceOfElement3D) // first is 2D element, second is face of 3D element
				{
					quadFaces[mark] = face; // replace 2D element with face of 3D element
					face.AddTwinElement(quad); // add 2D element as twin element
				}
				else // second is 2D element
				{
					quad.AddTwinElement(face);
				}
			}
			else
			{
				quadFaces.Add(mark, face);
				// priradit vlastnost <!>
				if (face is IFaceOfElement3D)
				{
					Property property;
					if (hiddenItemsProperties.TryGetPropertyAndRemove(ref mark, out property))
					{
						face.Property = property;
					}
				}
			}
		}

		/// <summary>
		/// Iterates through all faces. It works during construction process only.
		/// </summary>
		/// <returns>Iterator of all created faces</returns>
		private IEnumerable<Element2D> iterateThroughAllFaces()
		{
			foreach (Element2D e in triangleFaces.Values)
				yield return e;
			foreach (Element2D e in quadFaces.Values)
				yield return e;
		}

		#endregion

		#region Creating surface representation

		/// <summary>
		/// Creates collection of winged edges from collections of external faces - triangleFaces and quadFaces
		/// </summary>
		private void createSurfaceRepresentation(Mesh mesh, IEnumerable<Element2D> allFaces, ref Histogram edgeAnglesHistogram, MeshIOEventArgs ioea, YesNoQuestion cancelled)
		{
			Vector3 lowerBound = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 upperBound = new Vector3(float.MinValue, float.MinValue, float.MinValue);

			// pro kazde cislo uzlu, hrana ktera s nim inciduje
			Dictionary<Node, List<WingedEdge>> nodesEdgesIncidence = mesh.NodesEdgesIncidence;

			foreach (WingedEdge newEdge in generateAllDistinctEdgesOfAllFaces(mesh, allFaces, ioea, cancelled))
			{
				List<WingedEdge> edges1, edges2;
				if (!nodesEdgesIncidence.TryGetValue(newEdge.BeginNode, out edges1) || edges1 == null)
					nodesEdgesIncidence[newEdge.BeginNode] = edges1 = new List<WingedEdge>(); /**/ // initial capacity 0 -> 4 -> 8 -> ...
				if (!nodesEdgesIncidence.TryGetValue(newEdge.EndNode, out edges2) || edges2 == null)   // inicializuju druhy uzel
					nodesEdgesIncidence[newEdge.EndNode] = edges2 = new List<WingedEdge>(); /**/ // initial capacity 0 -> 4 -> 8 -> ...

				edges1.Add(newEdge); // pridam tuto hranu do seznamu incidence prvniho uzlu hrany
				edges2.Add(newEdge);   // pridam tuto hranu do seznamu incidence druheho uzlu hrany

				mesh.AddEdge(newEdge); // vloz hranu do site

				newEdge.BeginNeighbors = edges1;
				newEdge.EndNeighbors = edges2;
			}

			if (cancelled != null && cancelled())
				return;

			// naplnit histogram
			foreach (WingedEdge edge in mesh.Edges)
				edgeAnglesHistogram.AddValue(edge.FeatureAngle);

			//    // oriznout seznamy sousedu hran
			//    foreach (Node n in nodesEdgesIncidence.Keys)
			//    {
			//        List<WingedEdge> edgesOfn = nodesEdgesIncidence[n];
			//        if (edgesOfn != null)
			//            edgesOfn.TrimExcess(); // oriznout seznam sousedu hran, aby se setrilo mistem
			//    }

			// spocitat meze obalky site (pro vypocet centra a polomeru site)
			foreach (Node n in mesh.GetNodes(false))
				updateBounds(n.Position, ref lowerBound, ref upperBound);

			// nastavit stred rotace site pro nastroj Orbit a radius viditelne site
			mesh.CenterOfRotation = (nodesEdgesIncidence.Count > 0) ? Utilities.Functions.GetCenterOfLineSegment(ref lowerBound, ref upperBound) : /*Vector3.Zero*/ mesh.PositionOffset * -mesh.ResizeFactor;
			mesh.Radius = (nodesEdgesIncidence.Count > 0) ? (lowerBound - upperBound).Length * 0.5f : 1f;

			mesh.LowerBound = lowerBound;
			mesh.UpperBound = upperBound;
			// --------------------------------------------------------------------
		}

		private IEnumerable<WingedEdge> generateAllDistinctEdgesOfAllFaces(Mesh mesh, IEnumerable<Element2D> allFaces, MeshIOEventArgs ioea, YesNoQuestion cancelled)
		{
			WingedEdge newEdge, original;
			//this.edgeMarks = new Dictionary<EdgeMark, WingedEdge>();
			const float WORK_RATIO = 1f - NODE_WORK_RATIO - ELEMENT_WORK_RATIO;
			int totalFaceCount = triangleFaces.Count + quadFaces.Count;
			int count = 0;
			// projdu vsechny plochy a vratim jejich hrany
			foreach (Element2D face in allFaces)
			{
				if (face is Triangle)
				{
					Triangle t = (Triangle)face;
					newEdge = generateEdgeOf(t, 0);
					if (notAlreadyProcessed(newEdge, out original))
						yield return newEdge;
					t.Edge1 = original;
					newEdge = generateEdgeOf(t, 1);
					if (notAlreadyProcessed(newEdge, out original))
						yield return newEdge;
					t.Edge2 = original;
					newEdge = generateEdgeOf(t, 2);
					if (notAlreadyProcessed(newEdge, out original))
						yield return newEdge;
					t.Edge3 = original;
				}
				else if (face is Quadrilateral)
				{
					Quadrilateral q = (Quadrilateral)face;
					newEdge = generateEdgeOf(q, 0);
					if (notAlreadyProcessed(newEdge, out original))
						yield return newEdge;
					q.Edge1 = original;
					newEdge = generateEdgeOf(q, 1);
					if (notAlreadyProcessed(newEdge, out original))
						yield return newEdge;
					q.Edge2 = original;
					newEdge = generateEdgeOf(q, 2);
					if (notAlreadyProcessed(newEdge, out original))
						yield return newEdge;
					q.Edge3 = original;
					newEdge = generateEdgeOf(q, 3);
					if (notAlreadyProcessed(newEdge, out original))
						yield return newEdge;
					q.Edge4 = original;
				}

				// jeste vlozit face do site
				mesh.AddFace(face);
				count++;

				// -------------------------------
				if (Step != null) // informuj o postupu
				{
					int percent = (int)((NODE_WORK_RATIO + ELEMENT_WORK_RATIO + (float)count / (float)totalFaceCount * WORK_RATIO) * 100f);
					if (percent != ioea.PercentDone && percent <= 100)
					{
						if (cancelled != null && cancelled())
							yield break;
						ioea.PercentDone = percent;
						Step(this, ioea);
					}
				}
			}

			// projdu vsechny ulozene 1D prvky a vratim jejich objekt hrana
			//foreach (Beam beam in oneDimensionalElements)
			//    yield return beam.WingedEdge;
		}

		private WingedEdge generateEdgeOf(Triangle t, int rank)
		{
			if (!t.ApproximationIsQuadratic)
			{
				switch (rank)
				{
					case 0: return new WingedEdge(t.Node1, t.Node2, t);
					case 1: return new WingedEdge(t.Node2, t.Node3, t);
					case 2: return new WingedEdge(t.Node3, t.Node1, t);
					default: throw new ArgumentException();
				}
			}
			else // t.ApproximationType == ApproximationType.Quadratic
			{
				switch (rank)
				{
					case 0: return new QuadraticEdge(t.Node1, t.Node2, additionalQuadraticNodes[t][rank], t);
					case 1: return new QuadraticEdge(t.Node2, t.Node3, additionalQuadraticNodes[t][rank], t);
					case 2: return new QuadraticEdge(t.Node3, t.Node1, additionalQuadraticNodes[t][rank], t);
					default: throw new ArgumentException();
				}
			}
		}

		private WingedEdge generateEdgeOf(Quadrilateral q, int rank)
		{
			if (!q.ApproximationIsQuadratic)
			{
				switch (rank)
				{
					case 0: return new WingedEdge(q.Node1, q.Node2, q);
					case 1: return new WingedEdge(q.Node2, q.Node3, q);
					case 2: return new WingedEdge(q.Node3, q.Node4, q);
					case 3: return new WingedEdge(q.Node4, q.Node1, q);
					default: throw new ArgumentException();
				}
			}
			else
			{
				switch (rank)
				{
					case 0: return new QuadraticEdge(q.Node1, q.Node2, additionalQuadraticNodes[q][rank], q);
					case 1: return new QuadraticEdge(q.Node2, q.Node3, additionalQuadraticNodes[q][rank], q);
					case 2: return new QuadraticEdge(q.Node3, q.Node4, additionalQuadraticNodes[q][rank], q);
					case 3: return new QuadraticEdge(q.Node4, q.Node1, additionalQuadraticNodes[q][rank], q);
					default: throw new ArgumentException();
				}
			}
		}

		private bool notAlreadyProcessed(WingedEdge edge, out WingedEdge original)
		{
			//long mark = ((long)edge.BeginNode.ID << 32) + edge.EndNode.ID; /**/ // vypocitam znacku - je to slozenina indexu obou uzlu hrany
			EdgeMark mark = new EdgeMark(edge.BeginNode.ID, edge.EndNode.ID);

			if (edgeMarks.TryGetValue(mark, out original)) // sesterska hrana jiz byla zpracovana, tuto tedy zahodim, ale jeste predtim ...
			{
				original.Face2 = edge.Face1; // priradim druhou plochu k predchozi hrane
				edgeMarks.Remove(mark); // odstranim tuto hranu, predpokladam, ze uz dalsi stejna nebude, setri to pamet
				return false;
			}
			edgeMarks.Add(mark, edge); // pridej znacku
			original = edge;
			// priradit vlastnost <!>
			Property property;
			if (hiddenItemsProperties.TryGetPropertyAndRemove(ref mark, out property))
				original.Property = property;
			return true;
		}

		#endregion

		#region Signal element support

		public void SignalElement(Mesh mesh, Element element)
		{
			Beam beam = element as Beam;
			if (beam != null)
			{
				if (!mesh.Beams.Contains(beam))
					mesh.PushBeam(beam);
			}
			else
			{
				if (element is Element2D)
				{
					if (!mesh.HiddenElements.Contains(element)) // if 2D element is not hidden, no need to recreate surface representation
						return;
				}
				this.hiddenItemsProperties = new EdgeFacePropertySet(); // add temp object
				List<Element2D> existingFacesOfElement = new List<Element2D>();
				foreach (Element2D face in mesh.Faces)
				{
					if (face != element)
					{
						IFaceOfElement3D faceOfElement = face as IFaceOfElement3D;
						if (faceOfElement == null || faceOfElement.ParentElement != element) // pokud plocha nepatri danemu prvku
							continue;
					}
					processFace(face); // add mark of this face to face-list
					existingFacesOfElement.Add(face);
				}

				foreach (Element2D face in existingFacesOfElement)
				{
					foreach (WingedEdge edge in face.IterateThroughAllEdges())
					{
						EdgeMark mark = new EdgeMark(edge.BeginNode.ID, edge.EndNode.ID);
						edgeMarks[mark] = new WingedEdge(edge.BeginNode, edge.EndNode, face); // create dummy edge for later comparing in createSurfaceRep...
					}
				}

				this.hiddenItemsProperties = mesh.HiddenItemsProperties;
				processElement(element); // add marks of all faces of this element to list - faces already contained in surface are preserved, others will be added
				Histogram edgeAnglesHistogram = mesh.Statistics.EdgeAnglesHistogram;
				createSurfaceRepresentation(mesh, iterateThroughAllFaces(), ref edgeAnglesHistogram, null, null);
			}
			mesh.CreateBuffers();
		}

		#endregion

		#region Cutting

		public void CutMesh(Mesh mesh, HashSet<Element> elementsToShow)
		{
			mesh.SelectedItems = new HashSet<ISelectable>(); // odoznacit polozky
			
			this.hiddenItemsProperties = mesh.HiddenItemsProperties; // nastavit odkaz na skryte polozky s vlastnostmi

			// -------------------------------------------------------------
			// pokud jsou normaly ploch otocene, tak je pred rezanim vratim,
			// na konci rezani vratim puvodni stav
			bool normalVectorsWereInverted = mesh.NormalVectorsAreInverted;
			if (mesh.NormalVectorsAreInverted)
				mesh.InvertAllNormals();
			// -------------------------------------------------------------

			// projit vsechny hrany a ulozit jejich vlastnosti
			foreach (WingedEdge edge in mesh.Edges)
			{
				if (!edge.Property.IsZero)
				{
					hiddenItemsProperties.AddEdgeProperty(edge);
				}
			}

			// remove all twin elements, they will be created in all-elements loop again
			foreach (Element2D element2D in mesh.Elements.OfType<Element2D>())
			{
				element2D.RemoveAllTwinElements();
			}

			// smazat povrchovou reprezentaci a buffery
			mesh.ClearSurface();
			mesh.HiddenElements.Clear();
			
			foreach (Element e in mesh.Elements)
			{
				if (elementsToShow.Contains(e))
				{
					processElement(e);
					// pokud to je kvadraticky 2D prvek, tak ho zpracovat
					if (e.ApproximationIsQuadratic)
					{
						Element2D face = e as Element2D;
						if (face != null)
							processQuadraticNodesOfFace(face);
					}
				}
				else
				{
					mesh.HiddenElements.Add(e);
				}
			}

			// vytvor povrchovou reprezentaci
			Histogram edgeAnglesHistogram = new Histogram(0f, 180f, 1f);
			createSurfaceRepresentation(mesh, iterateThroughAllFaces(), ref edgeAnglesHistogram, null, null);
			// smazat nebo vratit beamy do seznamu beamu
			cutOrRestoreBeams(mesh, elementsToShow);
			// doladit par detailu - pripravit sit pro zobrazeni
			mesh.InitializeMesh(edgeAnglesHistogram);
			// vytvorit buffery
			mesh.CreateBuffers(); // docela to zdrzuje, pomaly !!!

			// -------------------------------------------------------------
			// pokud byly normaly puvodne otocene, tak je ted vratim zpet
			if (normalVectorsWereInverted)
				mesh.InvertAllNormals();
			// -------------------------------------------------------------
		}

		private void processQuadraticNodesOfFace(Element2D face)
		{
			List<Node> middleNodes = new List<Node>(face.IterateThroughAllEdgeMiddleNodes());
			additionalQuadraticNodes[face] = middleNodes.ToArray();
		}

		private void cutOrRestoreBeams(Mesh mesh, HashSet<Element> elementsToShow)
		{
			mesh.Beams.Clear();
			mesh.ClearBeamNodesNotInFaces();

			foreach (Beam b in elementsToShow.OfType<Beam>())
			{
				// pridat do seznamu uzlu a jejich uzly zaradit do povrchove reprezentace
				mesh.PushBeam(b);
			}
		}

		private static bool notAllNodesInDictionary(IEnumerable<Node> test, Dictionary<Node, List<WingedEdge>> nodesEdgesIncidence)
		{
			foreach (Node n in test)
				if (!nodesEdgesIncidence.ContainsKey(n))
					return true;
			return false;
		}

		public static bool allNodesInSet(IEnumerable<Node> test, HashSet<Node> nodes)
		{
			foreach (Node n in test)
				if (!nodes.Contains(n))
					return false;
			return true;
		}

		public static bool someNodesInSet(IEnumerable<Node> test, HashSet<Node> nodes)
		{
			foreach (Node n in test)
				if (nodes.Contains(n))
					return true;
			return false;
		}

		public static bool noNodesInSet(IEnumerable<Node> test, HashSet<Node> nodes)
		{
			foreach (Node n in test)
				if (nodes.Contains(n))
					return false;
			return true;
		}

		private static bool noNodesInSets(IEnumerable<Node> test, HashSet<Node> nodes1, HashSet<Node> nodes2)
		{
			foreach (Node n in test)
				if (nodes1.Contains(n) || nodes2.Contains(n))
					return false;
			return true;
		}

		#endregion

	}
}

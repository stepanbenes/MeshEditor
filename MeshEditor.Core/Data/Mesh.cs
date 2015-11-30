using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using MeshEditor.Graphics;
using MeshEditor.Construction;
using MeshEditor.Utilities;

// alias
using Utils = MeshEditor.Utilities.Functions;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using System.Diagnostics;
using System.Linq;

namespace MeshEditor.Data
{
	/// <summary>
	/// ustredni trida reprezentujici sit konecnych prvku
	/// </summary>
	public class Mesh : IDisposable
	{

		#region Fields, Constructor

		private string filename;
		private bool loadedFromSifelFileFormat;
		private PropertyColorsMode colorMode;
		private int referenceCount;
		
		// --- DATA ----------------------
		private Content content;
		private EdgeFacePropertySet hiddenItemsProperties;

		private MeshStatistics statistics;

		private int totalNodeCount;
		private bool buffersAreReady;

		private bool unsavedChanges;

		private bool hasTwinElements;

		// -------------------------------
		private CrossHatchShader crossHatchShader;
		// -------------------------------
		// -- parametry noveho souradneho systemu site
		private Vector3 positionOffset;
		private float resizeFactor;
		// -------------------------------

		// -------------------------------
		// -- parametry oriznute site; neuriznuta sit ma centerOfRotation = Zero; radius = 1
		private Vector3 centerOfRotation;
		private float radius;
		private Vector3 lowerBound, upperBound;
		// -------------------------------

		// --- CUTTED ELEMENTS ------------------
		private HashSet<Element> hiddenElements;
		// --- SELECTED ITEMS -------------------
		private HashSet<ISelectable> selectedItems;
		// --- CROSS-SECTIONS -------------------
		private List<ILayer> layers;
		// ======================================

		// ==========================================
		public Mesh(string sourceFilename, bool loadedFromSifelFileFormat, Vector3 meshPositionOffset, float meshResizeFactor)
		{
			this.filename = sourceFilename;
			this.loadedFromSifelFileFormat = loadedFromSifelFileFormat;
			this.positionOffset = meshPositionOffset;
			this.resizeFactor = meshResizeFactor;

			this.referenceCount = 0;

			this.selectedItems = new HashSet<ISelectable>();

			this.colorMode = PropertyColorsMode.Elements | PropertyColorsMode.Beams;

			this.totalNodeCount = 0;
			this.content = new Content(parentMesh: this);
			this.hiddenItemsProperties = new EdgeFacePropertySet();

            this.buffersAreReady = false;

			this.centerOfRotation = Vector3.Zero;
			this.radius = 1f;

			this.hiddenElements = new HashSet<Element>();

			this.unsavedChanges = false;

			this.statistics = new MeshStatistics();
			//this.statistics.RecreateBuffersNeeded += delegate { RecreateBuffers(); };
			this.layers = new List<ILayer>();
		}

		#endregion

		#region Public Properties

		public bool UnsavedChanges
		{
			get { return unsavedChanges; }
			set { unsavedChanges = value; }
		}

		public bool LoadedFromSifelFileFormat
		{
			get { return loadedFromSifelFileFormat; }
			set { loadedFromSifelFileFormat = value; }
		}

		public bool BuffersAreReady
		{
			get { return buffersAreReady; }
		}

		public int TotalElementCount
		{
			get { return content.Elements.Count; }
		}

		public int CurrentElementCount
		{
			get { return content.Elements.Count - hiddenElements.Count; }
		}
		
		public int TotalNodeCount
		{
			get { return this.totalNodeCount; }
			set { this.totalNodeCount = value; }
		}

		public int FaceCount
		{
			get { return content.Faces.Count; }
		}

		public int EdgeCount
		{
			get { return content.Edges.Count; }
		}

		

		public int BeamCount
		{
			get { return content.Beams.Count; }
		}

		// -----------------------

		public EdgeFacePropertySet HiddenItemsProperties
		{
			get { return hiddenItemsProperties; }
		}

		public IEnumerable<Element> Elements
		{
			get { return content.Elements; }
		}

		public IEnumerable<Element2D> Faces
		{
			get { return content.Faces; }
		}

		public IEnumerable<WingedEdge> Edges
		{
			get { return content.Edges; }
		}

		public Dictionary<Node, List<WingedEdge>> NodesEdgesIncidence
		{
			get { return content.NodesEdgesIncidence; }
		}

		public List<Beam> Beams
        {
            get { return content.Beams; }
        }

		public string Filename
		{
			get { return filename; }
			set { filename = value; }
		}
		
		public MeshStatistics Statistics
		{
			get { return statistics; }
			set { statistics = value; }
		}

		public float SoftBorderLimit
		{
			get { return statistics.SoftBorderLimit; }
		}

		public float HardBorderLimit
		{
			get { return statistics.HardBorderLimit; }
		}

		public Vector3 PositionOffset
		{
			get { return positionOffset; }
		}

		public float ResizeFactor
		{
			get { return resizeFactor; }
		}

		public HashSet<ISelectable> SelectedItems
		{
			get { return selectedItems; }
			set { selectedItems = value; }
		}

		public HashSet<Element> HiddenElements
		{
			get { return hiddenElements; }
			set { hiddenElements = value; }
		}

		public int ReferenceCount
		{
			get { return referenceCount; }
			set { referenceCount = value; }
		}

		public PropertyColorsMode ColorMode
		{
			get { return this.colorMode; }
			set
			{
				PropertyColorsMode oldMode = this.colorMode;
				this.colorMode = value;
				UpdateColors(this.colorMode, oldMode);
			}
		}

		public Vector3 CenterOfRotation
		{
			get { return centerOfRotation; }
			set { centerOfRotation = value; }
		}

		public float Radius
		{
			get { return radius; }
			set { radius = value; }
		}

		public Vector3 LowerBound
		{
			get { return lowerBound; }
			set { lowerBound = value; }
		}

		public Vector3 UpperBound
		{
			get { return upperBound; }
			set { upperBound = value; }
		}

		public float MinimalElementRadius
		{
			get
			{
				if (!statistics.MinimalElementRadiusWasSetFlag)
					ComputeMinimalElementRadius();
				return statistics.MinimalElementRadius;
			}
		}

		public IList<ILayer> Layers
		{
			get { return layers; }
		}

		public bool HasTwinElements
		{
			get	{ return hasTwinElements; }
		}

		#endregion

		#region Public methods

		public IDataVisualizer GetDataVisualizer()
		{
			return content.DataVisualizer;
		}

		public void SetDataVisualizer(IDataVisualizer dv)
		{
			if (content.DataVisualizer != dv)
			{
				if (content.DataVisualizer != null)
				{
					content.DataVisualizer.Dispose();
					UpdateNodeCoordinates();
				}
				content.DataVisualizer = dv;
				if (content.DataVisualizer != null)
				{
					content.DataVisualizer.Initialize(this);
				}
			}
		}

		public void InitializeMesh(Histogram edgeAnglesHistogram, bool hasTwinElements)
		{
			this.statistics.EdgeAnglesHistogram = edgeAnglesHistogram;
			this.hasTwinElements = hasTwinElements;

			content.TrimExcessMemory();
			// hiddenItemsProperties is dictionary-based, it can't be shrinken
			//computeInitialBorderLimitsFromHistogram();
		}

		public void ClearHiddenElements()
		{
			content.Elements.RemoveAll(delegate(Element e) { return hiddenElements.Contains(e); });
			hiddenElements.Clear();
			hiddenItemsProperties.Clear();
		}

		public bool HasHiddenElements()
		{
			return hiddenElements.Count > 0;
		}

		public int ComputeCurrentNodeCount()
		{
			if (hiddenElements.Count == 0)
				return totalNodeCount;

			// pomerne narocne na vypocet
			HashSet<Node> currentNodes = new HashSet<Node>();
			foreach (Element e in content.Elements)
				if (!hiddenElements.Contains(e))
					currentNodes.UnionWith(e.IterateThroughAllNodes());
			return currentNodes.Count;
		}

		public void ComputeMinimalElementRadius()
		{
			if (statistics.MinimalElementRadiusWasSetFlag)
				return;

			float min = float.MaxValue;

			//foreach (Element e in content.Elements)
			//{
			//    float r = (e.GetSignificantPoint() - e.GetCenter()).LengthSquared;
			//    if (r != 0f && r < min)
			//        min = r;
			//}

			foreach (WingedEdge edge in content.Edges)
			{
				float r = (edge.EndNode.Position - edge.BeginNode.Position).LengthSquared;
				if (r > 0f && r < min)
					min = r;
			}

			min = (float)Math.Sqrt(min);

			statistics.MinimalElementRadius = min;
		}

		/// <summary>
		/// Clears surface representation of mesh, e.g. before cutting. Basic structure of mesh remains (elements, beams)
		/// </summary>
		public void ClearSurface()
		{
			content.ClearSurface();
			content.DeleteBuffers();
			this.buffersAreReady = false;

			// GC.Collect(); /**/
		}

		public void ClearBeamNodesNotInFaces()
		{
			content.BeamNodesNotInFaces.Clear();
		}

		/// <summary>
		/// Create vertex buffer object for whole mesh and index buffer objects for each surface object.
		/// </summary>
		public void CreateBuffers()
		{
			this.buffersAreReady = content.CreateBuffers(this.colorMode, statistics.SoftBorderLimit, statistics.HardBorderLimit);
		}

		//public List<WingedEdge> GetEdgesIncidingWithNode(Node n)
		//{
		//    List<WingedEdge> result;
		//    if (content.NodesEdgesIncidence.TryGetValue(n, out result))
		//        return result;
		//    return null;
		//}

		//public void SetEdgesIncidingWithNode(Node n, List<WingedEdge> edges)
		//{
		//    content.NodesEdgesIncidence[n] = edges;
		//}
		
		public IEnumerable<Element2D> GetFacesIncidingWithNode(Node n)
		{
			HashSet<Element2D> result = new HashSet<Element2D>();
			List<WingedEdge> incidingEdges;
			if (!content.NodesEdgesIncidence.TryGetValue(n, out incidingEdges) || incidingEdges == null)
				return result;
			foreach (WingedEdge e in incidingEdges)
			{
				if (e.Face1 != null)
					result.Add(e.Face1);
				if (e.Face2 != null)
					result.Add(e.Face2);
			}
			return result;
		}
		
		public void AddEdge(WingedEdge edge)
		{
			if (edge == null)
				throw new ArgumentNullException();
			content.Edges.Add(edge);
			QuadraticEdge qe = edge as QuadraticEdge;
			if (qe != null)
				content.EdgeMiddleNodes.Add(qe.MiddleNode);
		}

		public void AddFace(Element2D face)
		{
			if (face == null)
				throw new ArgumentNullException();
			content.Faces.Add(face);
		}

		/// <summary>
		/// Inserts element into mesh
		/// </summary>
		/// <param name="e">element to be inserted</param>
		public void PushElement(Element e)
		{
			Beam b = e as Beam;
			if (b != null)
			{
				PushBeam(b);
			}
			//else
			
			// pridat prvek do seznamu prvku
			content.Elements.Add(e);
		}

		public void PushBeam(Beam b)
		{
			// pridat do seznamu beamu
			content.Beams.Add(b);
			// pridat odkaz na uzly do seznamu uzlu
			if (!content.NodesEdgesIncidence.ContainsKey(b.BeginNode))
				content.BeamNodesNotInFaces.Add(b.BeginNode);
			if (!content.NodesEdgesIncidence.ContainsKey(b.EndNode))
				content.BeamNodesNotInFaces.Add(b.EndNode);
			QuadraticBeam q = b as QuadraticBeam;
			if (q != null)
			{
				content.BeamNodesNotInFaces.Add(q.MiddleNode);
			}
		}

		/// <summary>
		/// Draws surface representation of mesh, 
		/// if there are some 1D elements, it draws them too
		/// </summary>
		public void DrawContent(RenderMode renderMode, Camera camera, bool optimizeForMoving, bool optimizeForSelecting, bool drawNodeNumbers, bool drawElementNumbers, bool drawBeams, bool drawBeamNumbers)
		{
			bool elementPropertyColors = (colorMode & PropertyColorsMode.Elements) != 0;
			bool facePropertyColors = (colorMode & PropertyColorsMode.Faces) != 0;
			bool edgePropertyColors = (colorMode & PropertyColorsMode.Edges) != 0;
			bool nodePropertyColors = (colorMode & PropertyColorsMode.Nodes) != 0;
			bool beamPropertyColors = (colorMode & PropertyColorsMode.Beams) != 0;

			//bool numbersAreOK = !optimizeForMoving && content.VisibleNodesReady;// && (Scene.AlwaysShowNumbers || !optimizeForSelecting);
			bool showElementNumbers = drawElementNumbers & !optimizeForMoving;
			bool showBeamNumbers = drawBeamNumbers & !optimizeForMoving;

			foreach (ILayer layer in layers)
			{
				if (layer.Visible)
				{
					if (layer.UpdateNeeded)
						layer.Update(this, content.DataVisualizer, elementPropertyColors);

					layer.Draw(content.DataVisualizer, renderMode, elementPropertyColors);
				}
			}

			if ((renderMode & RenderMode.Faces) != 0)
			{
				GL.Enable(EnableCap.PolygonOffsetFill);
				GL.PolygonOffset(1f, 1f);

				if (Scene.FaceLighting)
					GL.Enable(EnableCap.Lighting);
				else
					GL.Disable(EnableCap.Lighting);

				bool drawData = content.DataVisualizer != null & !facePropertyColors & !elementPropertyColors;
				bool userCrossHatching = HasTwinElements & (elementPropertyColors | facePropertyColors);

				if (drawData) // iso-areas shader
				{
					content.DataVisualizer.BeginDraw(Scene.FaceLighting);
				}
				else if (userCrossHatching)
				{
					initializeCrossHatchShader(); // multiple property colors hatching shader
					crossHatchShader.Use(statistics.GetElementPropertyColorsPalette());
				}

				content.DrawFaces(selectedItems, facePropertyColors, elementPropertyColors, camera);

				// turn off shaders
				if (drawData)
					content.DataVisualizer.EndDraw();
				else if (userCrossHatching)
					crossHatchShader.Unuse();

				if (showElementNumbers)
					content.DrawVisibleElementNumbers(selectedItems);

				if (!Scene.FaceLighting)
					GL.Enable(EnableCap.Lighting);

				GL.Disable(EnableCap.PolygonOffsetFill);
			}

			// -----------------
			
			if ((renderMode & RenderMode.AllLines) != 0 || (renderMode & RenderMode.BorderLines) != 0)
			{
				GL.ShadeModel(ShadingModel.Flat);
				if (!Scene.EdgeLighting)
					GL.Disable(EnableCap.Lighting);
				GL.LineWidth(Scene.BorderEdgeWidth);
				// -----------------------------------------
				if(Scene.LineSmooth)
				{
					GL.Enable(EnableCap.LineSmooth);
					GL.Enable(EnableCap.Blend);
				}

				content.DrawHardBorderEdges(selectedItems, statistics.HardBorderLimit, edgePropertyColors);
				
				// -----------------------------------------
				if (Scene.LineSmooth)
				{
					GL.Disable(EnableCap.LineSmooth);
					GL.Disable(EnableCap.Blend);
				}
				if (!Scene.EdgeLighting)
					GL.Enable(EnableCap.Lighting);
			}

			if ((renderMode & RenderMode.AllLines) != 0)
			{
				if (!Scene.EdgeLighting)
					GL.Disable(EnableCap.Lighting);
				GL.LineWidth(Scene.OrdinaryEdgeWidth);
				if (Scene.LineSmooth)
				{
					GL.Enable(EnableCap.LineSmooth);
					GL.Enable(EnableCap.Blend);
				}
				GL.Color3(Scene.OrdinaryEdgeColor);

				content.DrawOrdinaryAndSoftEdges(selectedItems, statistics.SoftBorderLimit, statistics.HardBorderLimit, edgePropertyColors);
				
				if (Scene.LineSmooth)
				{
					GL.Disable(EnableCap.LineSmooth);
					GL.Disable(EnableCap.Blend);
				}
				if (!Scene.EdgeLighting)
					GL.Enable(EnableCap.Lighting);
			}

			GL.ShadeModel(Scene.MeshShadingModel);

            // ================================================================
            // draw 1D Elements
			if (drawBeams)
			{
				if (!Scene.EdgeLighting)
					GL.Disable(EnableCap.Lighting);

				bool drawData = content.DataVisualizer != null & !beamPropertyColors;

				// data visualizer begin draw
				if (drawData)
					content.DataVisualizer.BeginDraw(Scene.EdgeLighting);

				content.DrawBeams(selectedItems, beamPropertyColors);

				// data visualizer end draw
				if (drawData)
					content.DataVisualizer.EndDraw();

				if (showBeamNumbers)
					content.DrawVisibleBeamNumbers(selectedItems);

				if (!Scene.EdgeLighting)
					GL.Enable(EnableCap.Lighting);
			}
			// --------------------------------------------------

			// ??? otazka: Je mozne neco jako depth_mask ale ne na cteni, ale na zapis? hodnota se do depth-bufferu zapise, ale neuvazuje se pri depth-testu??

			if ((renderMode & RenderMode.Points) != 0)
			{
				GL.PointSize(Scene.PointSize);
				GL.Disable(EnableCap.Lighting);

				if (Scene.PointSmooth & !(optimizeForMoving || optimizeForSelecting))
				{
					GL.Enable(EnableCap.PointSmooth);
					GL.Enable(EnableCap.Blend);
				}

				if (!optimizeForMoving & content.VisibleNodesReady)
				{
					if (renderMode != RenderMode.Points) // pokud se kresli body, tak nechat zapnuty depth test - kvuli reznym plocham
						GL.Disable(EnableCap.DepthTest);
					content.DrawVisibleNodes(selectedItems, nodePropertyColors, drawNodeNumbers);
					if (renderMode != RenderMode.Points)
						GL.Enable(EnableCap.DepthTest);
				}
				else
				{
					content.DrawNodes(selectedItems, nodePropertyColors, Scene.IncludeEdgeMiddleNodes);
				}
				if (Scene.PointSmooth & !(optimizeForMoving || optimizeForSelecting))
				{
					GL.Disable(EnableCap.Blend);
					GL.Disable(EnableCap.PointSmooth);
				}
				GL.Enable(EnableCap.Lighting);
			}

			if (content.DataVisualizer != null)
			{
				content.DataVisualizer.DrawItems(colorMode);
			}

			// Console.Write("*"); // neprekresluje se sit po kazde akci dvakrat?
		}

		#region Draw Section

		//private float[] intersectionAngles;

		//public void DrawCutPlane(Cuts.CutPlane cutPlane, RenderMode renderMode)
		//{
		//    foreach (Element element in content.Elements)
		//    {
		//        List<Vector3> intersections = new List<Vector3>(element.GetAllIntersectionsOfEdgesWithPlane(cutPlane.PointOnPlane, cutPlane.NormalVector));
		//        if (intersections.Count > 2)
		//        {
		//            Vector3 sectionCenter = Vector3.Zero;
		//            for (int i = 0; i < intersections.Count; i++)
		//            {
		//                sectionCenter += intersections[i];
		//            }
		//            sectionCenter /= (float)intersections.Count;

		//            //float[] intersectionAngles = new float[intersections.Count];
		//            Vector3 firstVector = intersections[0] - sectionCenter;

		//            if (firstVector == Vector3.Zero) // all intersections are same, do not cut element - section plane incides only with one point in element
		//                continue; // TODO: vyresit nulovy vektor nebo blizky nule

		//            firstVector.Normalize();
		//            intersectionAngles = new float[intersections.Count];
		//            intersectionAngles[0] = 0f;
		//            for (int i = 1; i < intersections.Count; i++)
		//            {
		//                Vector3 secondVector = intersections[i] - sectionCenter;
		//                //System.Diagnostics.Debug.Assert(secondVector != Vector3.Zero);
		//                if (secondVector == Vector3.Zero) // TODO: vyresit nulovy vektor nebo blizky nule
		//                    continue;
		//                secondVector.Normalize();
		//                float intersectionAngle = Utilities.Functions.GetAngleInDegreesBetweenUnitVectors_0_360(firstVector, secondVector, cutPlane.NormalVector);
		//                intersectionAngles[i] = intersectionAngle;
		//            }

		//            int[] indices = new int[intersections.Count];
		//            for (int i = 0; i < indices.Length; i++)
		//            {
		//                indices[i] = i;
		//            }

		//            Array.Sort(indices, compareAngles);

		//            // draw section plane

		//            GL.Normal3(cutPlane.NormalVector);
		//            GL.Color3(Color.Green);
		//            GL.Begin(BeginMode.TriangleFan);
		//            GL.Vertex3(sectionCenter);
		//            for (int i = 0; i < intersections.Count; i++)
		//            {
		//                GL.Vertex3(intersections[indices[i]]);
		//            }
		//            GL.Vertex3(intersections[indices[0]]); // last point to close loop
		//            GL.End();

		//            GL.LineWidth(2.0f);
		//            GL.Begin(BeginMode.LineLoop);
		//            GL.Color3(Color.Red);
		//            for (int i = 0; i < intersections.Count; i++)
		//            {
		//                GL.Vertex3(intersections[indices[i]]);
		//            }
		//            GL.End();
		//        }
		//    }
		//}

		//private int compareAngles(int index1, int index2)
		//{
		//    return intersectionAngles[index1].CompareTo(intersectionAngles[index2]);
		//}

		#endregion

		/// <summary>
		/// Draws faces in mesh only
		/// </summary>
		public void DrawFacesOnly()
		{
			content.DrawFacesOnly();
		}

		public void UpdateColors()
		{
			content.UpdateAllColors(this.selectedItems, this.colorMode, statistics.SoftBorderLimit, statistics.HardBorderLimit);
			updateLayerColors();
		}

		public void UpdateColors(PropertyColorsMode newColorMode, PropertyColorsMode oldColorMode)
		{
			HashSet<ISelectable> empty = new HashSet<ISelectable>();
			if ((newColorMode & PropertyColorsMode.Faces) != (oldColorMode & PropertyColorsMode.Faces) || (newColorMode & PropertyColorsMode.Elements) != (oldColorMode & PropertyColorsMode.Elements))
			{
				content.UpdateFaceColors(this.selectedItems, colorMode);
				updateLayerColors();
			}
			if ((newColorMode & PropertyColorsMode.Edges) != (oldColorMode & PropertyColorsMode.Edges))
				content.UpdateEdgeColors(this.selectedItems, colorMode, statistics.SoftBorderLimit, statistics.HardBorderLimit);
			if ((newColorMode & PropertyColorsMode.Nodes) != (oldColorMode & PropertyColorsMode.Nodes))
				content.UpdateNodeColors(this.selectedItems, colorMode);
			if ((newColorMode & PropertyColorsMode.Beams) != (oldColorMode & PropertyColorsMode.Beams))
				content.UpdateBeamColors(this.selectedItems, colorMode);
		}

		public void UpdateFaceColors()
		{
			content.UpdateFaceColors(this.selectedItems, colorMode);
			updateLayerColors();
		}

		public void UpdateEdgeColors()
		{
			content.UpdateEdgeColors(this.selectedItems, colorMode, statistics.SoftBorderLimit, statistics.HardBorderLimit);
		}

		public void UpdateNodeColors()
		{
			content.UpdateNodeColors(this.selectedItems, colorMode);
		}

		public void UpdateBeamColors()
		{
			content.UpdateBeamColors(this.selectedItems, colorMode);
		}

		public void UpdateNodeCoordinates()
		{
			content.UpdateNodeCoordinates();

			foreach (ILayer layer in layers)
				layer.GeometryChanged = true;
		}

		public void Dispose()
		{
			content.DeleteBuffers();
            this.buffersAreReady = false;

			if (content.DataVisualizer != null)
			{
				content.DataVisualizer.Dispose();
				content.DataVisualizer = null;
			}

			if (crossHatchShader != null)
			{
				crossHatchShader.Dispose();
				crossHatchShader = null;
			}
		}

		public void RecreateBuffers()
		{
			// nejdrive vsechno odoznacim
			selectedItems = new HashSet<ISelectable>();

			/**/ // je nutny je mazat a vytvaret cely znova? (asi jo)
            this.buffersAreReady = false;
			this.buffersAreReady = content.CreateBuffers(colorMode, statistics.SoftBorderLimit, statistics.HardBorderLimit);
		}

		public void CreateVisibleNodesList(Rectangle window, Camera camera, bool xRayVision)
		{
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
			GL.LoadIdentity();
			camera.LookAt(); // nastavit kameru
			// ---------------------------------------------------

			content.VisibleNodes = FindVisibleNodes(window, camera, xRayVision, true);

			if (buffersAreReady)
				content.CreateVisibleNodesBuffer(window);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.PopMatrix();
		}

		public void CreateVisibleFacesList(Rectangle area, Camera camera)
		{
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
			GL.LoadIdentity();
			camera.LookAt(); // nastavit kameru
			// ---------------------------------------------------

			int[] viewport;
			double[] modelview;
			double[] projection;

			// posunu Near plochu blize objektu pro vetsi presnost
			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix(); // ulozit matici
			GL.LoadIdentity();

			double computedZ_NEAR_PARAM = ComputeMeshMinVisibleDistance(camera);
			Scene.ExtractViewport(out viewport);
			// nastavit perspektivu
			Utils.GluPerspective(Scene.FOVY_PARAM, (double)viewport[2] / (double)viewport[3], computedZ_NEAR_PARAM, Scene.Z_FAR_PARAM);
			// nactu transformacni matice
			Scene.ExtractMatrices(out viewport, out modelview, out projection);

			Vector2 minBound, maxBound;

			// ===================================================================
			// projit plochy a pouze u tech privracenych vypocitat projekce uzlu

			minBound = new Vector2(float.MaxValue, float.MaxValue);
			maxBound = new Vector2(float.MinValue, float.MinValue);
			Dictionary<Element2D, Vector3> facesCenters = new Dictionary<Element2D, Vector3>();
			Vector3 winPos;

			foreach (Element2D face in content.Faces)
			{
				bool ok = false;
				foreach (Node n in face.IterateThroughAllNodes())
				{
					if (!content.StickyNodes.Contains(n) && content.VisibleNodes.Contains(n))
					{
						ok = true;
						break;
					}
				}
				if (ok)
				{
					Utils.GluProject(face.GetCenter(), modelview, projection, viewport, out winPos); // vypoctu projekci
					if (area.Contains((int)winPos.X, viewport[3] - (int)winPos.Y - 1) && winPos.Z >= 0f && winPos.Z <= 1f) // transformovat oblast ze souradnic, ktere pouziva OpenGL (Y-osa je obracene)
					{
						facesCenters[face] = winPos; // ulozim projekci do slovniku
						updateBounds(ref winPos, ref minBound, ref maxBound); // jeste updatovat meze
					}
				}
			}

			//Rectangle bounds = new Rectangle((int)minBound.X, (int)minBound.Y, (int)Math.Ceiling(maxBound.X) - (int)minBound.X, (int)Math.Ceiling(maxBound.Y) - (int)minBound.Y);
			Rectangle bounds = new Rectangle((int)minBound.X, (int)minBound.Y, ((int)maxBound.X - (int)minBound.X + 1), ((int)maxBound.Y - (int)minBound.Y + 1));

			// ===================================================================
			// vykreslit do depth bufferu plochy site
			GL.Clear(ClearBufferMask.DepthBufferBit);
			GL.PolygonOffset(1f, 1f); // trochu je posunu, abych pak mohl testovat
			GL.Enable(EnableCap.PolygonOffsetFill);
			DrawFacesOnly();
			GL.Disable(EnableCap.PolygonOffsetFill);
			// -------------------------------------------------------------------

			// dynamicky rozhodnout, zda se nactou vsechny pixely v oblasti v jednom kroku, nebo se budou kontrolovat kazdy uzel zvlast
			bool readPixelsInOneStep = decideIfReadPixelsInOneStep(bounds, facesCenters.Count);
			float[] pixelDepths;
			if (readPixelsInOneStep)
			{
				pixelDepths = new float[bounds.Width * bounds.Height];
				//GL.ReadPixels(0, 0, viewport[2], viewport[3], PixelFormat.DepthComponent, PixelType.Float, pixelDepths);
				GL.ReadPixels(bounds.X, bounds.Y, bounds.Width, bounds.Height, PixelFormat.DepthComponent, PixelType.Float, pixelDepths);
			}
			else
			{
				pixelDepths = new float[1];
			}

			// ===================================================================

			Dictionary<Element2D, Vector2> result = new Dictionary<Element2D, Vector2>();
			float depth;

			//foreach (Node node in getNodes(Scene.IncludeEdgeMiddleNodes))
			foreach (KeyValuePair<Element2D, Vector3> pair in facesCenters)
			{
				winPos = pair.Value;

				if (readPixelsInOneStep)
				{
					//depth = pixelDepths[viewport[2] * ((int)winPos.Y) + ((int)winPos.X)];
					//depth = getPixelDepth(pixelDepths, viewport[2] * ((int)winPos.Y) + ((int)winPos.X), rowwidth);
					depth = getPixelDepth(pixelDepths, ref winPos, ref bounds);
				}
				else // nacist v kazdem cyklu znova hloubku pixelu pomoci glReadPixels
				{
					/**/
					// !!!
					GL.ReadPixels((int)winPos.X, (int)winPos.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, pixelDepths);
					//depth = getPixelDepth(pixelDepths, 4, 3);
					depth = pixelDepths[0];
					//for (int i = 1; i < 9; i += 2) // najdi minimum
					//{
					//    if (pixelDepths[i] > depth)
					//        depth = pixelDepths[i];
					//}
				}
				// klicovy test na hloubku, pokud projde, bod je videt
				if (winPos.Z <= depth)
					result[pair.Key] = winPos.Xy;
			}

			// vratit zpet perspektivu...
			GL.PopMatrix();
			// vratit modelovaci matici zpet
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PopMatrix();

			content.FaceCentersPositions = result;
		}

		public double ComputeMeshMinVisibleDistance(Camera camera)
		{
			Vector3 direction = camera.GetDirection();
			float distance = Utils.PointPlaneDistanceSigned(this.centerOfRotation, camera.Eye, direction);
			distance -= this.radius;
			return (distance > Scene.Z_NEAR_PARAM) ? distance : Scene.Z_NEAR_PARAM;
		}

		public double ComputeMeshMaxVisibleDistance(Camera camera)
		{
			Vector3 direction = camera.GetDirection();
			float distance = Utils.PointPlaneDistanceSigned(this.centerOfRotation, camera.Eye, direction);
			distance += this.radius;
			if (distance <= Scene.Z_NEAR_PARAM)
				return Scene.Z_NEAR_PARAM;
			if (distance >= Scene.Z_FAR_PARAM)
				return Scene.Z_FAR_PARAM;
			return distance;
		}

		public HashSet<Node> FindVisibleNodes(Rectangle area, Camera camera, bool xRayVision, bool computeNodeDensity)
		{
			Dictionary<Node, Vector3> screenProjections;
			return findVisibleNodes(area, camera, xRayVision, computeNodeDensity, out screenProjections);
		}

		public Dictionary<Node, Vector3> FindVisibleNodesProjectedPositions(Rectangle area, Camera camera)
		{
			Dictionary<Node, Vector3> screenProjections, result = new Dictionary<Node, Vector3>();
			HashSet<Node> visibleNodes = findVisibleNodes(area, camera, false, false, out screenProjections);
			foreach (Node n in visibleNodes)
				result[n] = screenProjections[n];
			return result;
		}

		public IEnumerable<Node> GetNodes(bool includeMiddleNodes)
		{
			if (includeMiddleNodes && content.EdgeMiddleNodes.Count > 0)
				return content.GetAllExternalNodes();
			if (content.BeamNodesNotInFaces.Count > 0)
				return content.GetSimpleExternalNodes();
			return content.NodesEdgesIncidence.Keys;
		}

		public bool IsNodeClearlyVisible(Node node)
		{
			if (!content.VisibleNodesReady)
				return false;
			return content.VisibleNodes.Contains(node) && !content.StickyNodes.Contains(node);
		}

		#endregion

		#region Private methods

		private void updateLayerColors()
		{
			foreach (ILayer layer in layers)
				layer.ColorsChanged = true;
		}

		private HashSet<Node> findVisibleNodes(Rectangle area, Camera camera, bool xRayVision, bool computeNodeDensity, out Dictionary<Node, Vector3> screenProjections)
		{
			int[] viewport;
			double[] modelview;
			double[] projection;

			// posunu Near plochu blize objektu pro vetsi presnost
			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix(); // ulozit matici
			GL.LoadIdentity();

			double computedZ_NEAR_PARAM = ComputeMeshMinVisibleDistance(camera);
			Scene.ExtractViewport(out viewport);
			// nastavit perspektivu
			Utils.GluPerspective(Scene.FOVY_PARAM, (double)viewport[2] / (double)viewport[3], computedZ_NEAR_PARAM, Scene.Z_FAR_PARAM);
			// nactu transformacni matice
			Scene.ExtractMatrices(out viewport, out modelview, out projection);

			Vector2 minBound, maxBound;
			HashSet<Node> result;

			// ===================================================================
			// projit plochy a pouze u tech privracenych vypocitat projekce uzlu

			minBound = new Vector2(float.MaxValue, float.MaxValue);
			maxBound = new Vector2(float.MinValue, float.MinValue);
			screenProjections = new Dictionary<Node, Vector3>();
			Vector3 winPos;

			foreach (Node node in GetNodes(Scene.IncludeEdgeMiddleNodes))
			{
				Utils.GluProject(node.Position, modelview, projection, viewport, out winPos); // vypoctu projekci
				if (area.Contains((int)winPos.X, viewport[3] - (int)winPos.Y - 1) && winPos.Z >= 0f && winPos.Z <= 1f) // transformovat oblast ze souradnic, ktere pouziva OpenGL (Y-osa je obracene)
				{
					screenProjections[node] = winPos; // ulozim projekci do slovniku
					updateBounds(ref winPos, ref minBound, ref maxBound); // jeste updatovat meze
				}
			}

			//Rectangle bounds = new Rectangle((int)minBound.X, (int)minBound.Y, (int)Math.Ceiling(maxBound.X) - (int)minBound.X, (int)Math.Ceiling(maxBound.Y) - (int)minBound.Y);
			Rectangle bounds = new Rectangle((int)minBound.X, (int)minBound.Y, ((int)maxBound.X - (int)minBound.X + 1), ((int)maxBound.Y - (int)minBound.Y + 1));

			// ------------------------------------------------------------------------------------------------

			// pokud vidim rentgenove, tak nemusim pocitat test hloubky, rovnou vratim uzly, co jsou ve vyrezu
			if (xRayVision)
			{
				GL.PopMatrix(); // vratit ulozenou matici projekce
				result = new HashSet<Node>(screenProjections.Keys);
				if (computeNodeDensity)
					findStickyNodes(screenProjections, result);
				return result;
			}

			// ===================================================================
			// vykreslit do depth bufferu plochy site
			GL.Clear(ClearBufferMask.DepthBufferBit);
			GL.PolygonOffset(1f, 1f); // trochu je posunu, abych pak mohl testovat
			GL.Enable(EnableCap.PolygonOffsetFill);
			DrawFacesOnly();
			GL.Disable(EnableCap.PolygonOffsetFill);
			// -------------------------------------------------------------------

			// dynamicky rozhodnout, zda se nactou vsechny pixely v oblasti v jednom kroku, nebo se budou kontrolovat kazdy uzel zvlast
			bool readPixelsInOneStep = decideIfReadPixelsInOneStep(bounds, screenProjections.Count);
			float[] pixelDepths;
			if (readPixelsInOneStep)
			{
				pixelDepths = new float[bounds.Width * bounds.Height];
				//GL.ReadPixels(0, 0, viewport[2], viewport[3], PixelFormat.DepthComponent, PixelType.Float, pixelDepths);
				GL.ReadPixels(bounds.X, bounds.Y, bounds.Width, bounds.Height, PixelFormat.DepthComponent, PixelType.Float, pixelDepths);
			}
			else
			{
				pixelDepths = new float[1];
			}

			// ===================================================================

			result = new HashSet<Node>();
			float depth;

			//foreach (Node node in getNodes(Scene.IncludeEdgeMiddleNodes))
			foreach (KeyValuePair<Node, Vector3> pair in screenProjections)
			{
				Node node = pair.Key;
				winPos = pair.Value;

				if (readPixelsInOneStep)
				{
					//depth = pixelDepths[viewport[2] * ((int)winPos.Y) + ((int)winPos.X)];
					//depth = getPixelDepth(pixelDepths, viewport[2] * ((int)winPos.Y) + ((int)winPos.X), rowwidth);
					depth = getPixelDepth(pixelDepths, ref winPos, ref bounds);
				}
				else // nacist v kazdem cyklu znova hloubku pixelu pomoci glReadPixels
				{
					/**/
					// !!!
					GL.ReadPixels((int)winPos.X, (int)winPos.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, pixelDepths);
					//depth = getPixelDepth(pixelDepths, 4, 3);
					depth = pixelDepths[0];
					//for (int i = 1; i < 9; i += 2) // najdi minimum
					//{
					//    if (pixelDepths[i] > depth)
					//        depth = pixelDepths[i];
					//}
				}
				// klicovy test na hloubku, pokud projde, bod je videt
				if (winPos.Z <= depth)
					result.Add(node); // je videt, tak ho pridam
			}

			// vratit zpet perspektivu...
			GL.PopMatrix();
			
			// spocitat hustotu uzlu pro zjisteni, zda je mozne kreslit cisla uzlu
			if (computeNodeDensity)
				findStickyNodes(screenProjections, result);

			return result; // vratit vysledny seznam viditelnych uzlu

		}

		private void findStickyNodes(Dictionary<Node, Vector3> screenProjections, HashSet<Node> result)
		{
			content.StickyNodes = new HashSet<Node>();
			foreach (WingedEdge edge in content.Edges)
			{
				QuadraticEdge q = edge as QuadraticEdge;
				decideIfLinkIsShort(screenProjections, result, edge.BeginNode, edge.EndNode, (q != null) ? q.MiddleNode : null);
			}
			foreach (Beam beam in content.Beams)
			{
				QuadraticBeam q = beam as QuadraticBeam;
				decideIfLinkIsShort(screenProjections, result, beam.BeginNode, beam.EndNode, (q != null) ? q.MiddleNode : null);
			}
		}

		private void decideIfLinkIsShort(Dictionary<Node, Vector3> screenProjections, HashSet<Node> visibleNodes, Node n1, Node n2, Node middle)
		{
			if (visibleNodes.Contains(n1) && visibleNodes.Contains(n2))
			{
				if ((screenProjections[n1].Xy - screenProjections[n2].Xy).LengthSquared < Scene.MAX_VISIBLE_NUMBERS_DENSITY/**/)
				{
					content.StickyNodes.Add(n1);
					content.StickyNodes.Add(n2);
					if (middle != null)
						content.StickyNodes.Add(middle);
				}
			}
		}

		private bool decideIfReadPixelsInOneStep(Rectangle bounds, int pointCount)
		{
			// rozhodnout se podle hustoty bodu v oblasti. Pokud je oblast hodne husta, 
			// tak je lepsi nacist blokove celou oblast pomoci glReadPixels, pokud je obladt ridsi,
			// tak volat glReadPixels pouze pro jednotlive uzly
			return ((double)(bounds.Width * bounds.Height) / (double)pointCount) < 100.0;
		}
		
		private float getPixelDepth(float[] pixelDepths, ref Vector3 winPos, ref Rectangle bounds)
		{
			int x = (int)winPos.X - bounds.X;
			int y = (int)winPos.Y - bounds.Y;
			int index = bounds.Width * y + x;

			// projit 5 ruznych bodu a vybrat ten nejbliz (s nejmensi hloubkou)
			float min = pixelDepths[index];
			int test;
			if (x > 0) // vlevo
			{
				if (y > 0) // nahore
				{
					test = index - bounds.Width - 1;
					if (pixelDepths[test] > min)
						min = pixelDepths[test];
				}
				if (y < bounds.Height - 1) // dole
				{
					test = index + bounds.Width - 1;
					if (pixelDepths[test] > min)
						min = pixelDepths[test];
				}
			}
			if (x < bounds.Width - 1) // vpravo
			{
				if (y > 0) // nahore
				{
					test = index - bounds.Width + 1;
					if (pixelDepths[test] > min)
						min = pixelDepths[test];
				}
				if (y < bounds.Height - 1) // dole
				{
					test = index + bounds.Width + 1;
					if (pixelDepths[test] > min)
						min = pixelDepths[test];
				}
			}
			return min;
		}

		private static void updateBounds(ref Vector3 winPos, ref Vector2 minBound, ref Vector2 maxBound)
		{
			if (winPos.X < minBound.X)
				minBound.X = winPos.X;
			if (winPos.Y < minBound.Y)
				minBound.Y = winPos.Y;
			if (winPos.X > maxBound.X)
				maxBound.X = winPos.X;
			if (winPos.Y > maxBound.Y)
				maxBound.Y = winPos.Y;
		}

		private void initializeCrossHatchShader()
		{
			if (crossHatchShader == null)
			{
				crossHatchShader = new CrossHatchShader(Scene.FaceLighting);
				Debug.Assert(crossHatchShader.IsReady);
			}
			crossHatchShader.LightingEnabled = Scene.FaceLighting;
		}

		#endregion

	}
}

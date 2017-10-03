using System;
using System.Collections.Generic;
using System.Drawing;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Utils = MeshEditor.Utilities.Functions;
using MeshEditor.Construction;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace MeshEditor.Data
{
	/// <summary>
	/// scena reprezentuje pohled na sit konecnych prvku. obsahuje odkaz na objekt typu Mesh.
	/// jedna sit muze byt sdilena z vice objektu typu Scene.
	/// </summary>
	public class Scene : IScene, IDisposable
	{

		#region Instance fields & constructor

		private Camera camera;

		private RenderMode renderMode;

		private bool drawAxesFlag;
		private bool drawAxisArrowsFlag;
		private bool drawNodeNumbersFlag;
		private bool drawElementNumbersFlag;
		private bool drawBeamsFlag;
		private bool drawBeamNumbersFlag;

		private Mesh mesh;

		private List<Node> cutPlaneDefinitionNodes;
		private List<CutPlane> cutPlanes;

		private int[] nodeSignal;
		private int? elementSignal;

		private List<Vector3> nodeSignalPositions;
		private Vector3 elementSignalPosition;

		CutInfo lastUsedCutInfo;

		private Element3D tempElement3DAddedToSurfaceRepresentation;
		private Node[] tempNodesAddedToSurfaceRepresentation;

		public Scene()
		{
			this.camera = new Camera();
			this.renderMode = DefaultRenderMode;
			this.drawAxesFlag = true;
			this.drawAxisArrowsFlag = true;
			this.drawNodeNumbersFlag = true;
			this.drawElementNumbersFlag = false;

			this.drawBeamsFlag = true;
			this.drawBeamNumbersFlag = false;

			initializeMeshDependentFields();
		}

		private void initializeMeshDependentFields()
		{
			this.mesh = null;

			this.cutPlaneDefinitionNodes = new List<Node>();
			this.cutPlanes = new List<CutPlane>();

			this.nodeSignal = null;
			this.elementSignal = null;
			this.nodeSignalPositions = null;
			this.elementSignalPosition = Vector3.Zero;

			this.lastUsedCutInfo = null;

			this.tempElement3DAddedToSurfaceRepresentation = null;
		}

		#endregion

		#region Static members

		/// <summary>
		/// delegat odkazujici na funkci, ktera vezme hranu a vrati mnozinu s ni sousedicich hran
		/// </summary>
		private delegate IEnumerable<WingedEdge> NeighborSelection(WingedEdge edge);

		// -------------------------------------------

		public static readonly double FOVY_PARAM;
		public static readonly double Z_NEAR_PARAM;
		public static readonly double Z_FAR_PARAM;

		public static float AxisLength;
		public static float PointSize;
		public static float OrdinaryEdgeWidth;
		public static float BorderEdgeWidth;
		public static float BeamWidth;
		public static float DefaultCameraDistance;

		public static Color NonActiveBackColor;
		public static Color ActiveBackColor;
		public static Color LabelColor;

		public static Color FaceColor;
		public static Color OrdinaryEdgeColor;
		public static Color SoftBorderColor;
		public static Color HardBorderColor;
		public static Color SelectedElementColor;
		public static Color SelectedFaceColor;
		public static Color SelectedEdgeColor;
		public static Color SelectedNodeColor;
		public static Color SelectedElementNumbersColor;
		public static Color ElementNumbersColor;
		public static Color SelectedFaceAndElementColor;
		public static Color BeamColor;
		public static Color SelectedBeamColor;
		public static Color NodesColor;
		public static Color NodeNumbersColor;
		public static bool LineSmooth;
		public static bool PointSmooth;
		public static bool FaceLighting;
		public static bool EdgeLighting;
		public static bool IncludeEdgeMiddleNodes;
		public static int UndoOperationsMaxCount;
		public static float DefaultFirstBorderAngleLimit;
		public static float DefaultSecondBorderAngleLimit;

		public static RenderMode DefaultRenderMode;
		public static string SifelFileFormatExtension, PropertyDescriptionFileExtension;

		public static bool XRayVision;
		private static ShadingModel meshShadingModel;

		public static readonly float WHEEL_ZOOM_FACTOR;
		public static readonly float MAX_ZOOM_DISTANCE;
		public static readonly float RADIUS_OF_NORMALIZED_MESH;
		public static readonly float MAX_VISIBLE_NUMBERS_DENSITY; // pixel^2
		public static readonly int NODE_SELECTION_TOLERANCE_DISTANCE; // pixels
		public static readonly int EDGE_SELECTION_TOLERANCE_DISTANCE; // pixels
		public static readonly float LIMIT_ANGLE_FOR_POINT_INSIDE_FACE_DECISION; // degrees

		// POSTPROCESSING ---------------
		public static ColorScaleLegendPosition ColorScaleLegendPosition;
		public static Color VectorArrowsColor;
		// ------------------------------

		static Scene()
		{
			FOVY_PARAM = 55.0;
			Z_NEAR_PARAM = 0.001; // 0.005
			Z_FAR_PARAM = 50.0; // 50

			RADIUS_OF_NORMALIZED_MESH = 1.0f;
			MAX_VISIBLE_NUMBERS_DENSITY = 200f; // pixel^2

			WHEEL_ZOOM_FACTOR = 0.1f; // (0,1)
			MAX_ZOOM_DISTANCE = 0.2f;
			NODE_SELECTION_TOLERANCE_DISTANCE = 20;
			EDGE_SELECTION_TOLERANCE_DISTANCE = 20;
			LIMIT_ANGLE_FOR_POINT_INSIDE_FACE_DECISION = 10f; /**/

			// ===================================================

			SetDefaultParametres(openGLIsInitialized: false);

			// ------------------------------------------
		}

		public static void SetDefaultParametres(bool openGLIsInitialized)
		{
			ActiveBackColor = Color.FromArgb(229, 224, 222);
			NonActiveBackColor = Color.FromArgb(186, 186, 200);
			LabelColor = Utils.GetContrastColor(ActiveBackColor);

			FaceColor = Color.WhiteSmoke;
			SelectedElementColor = Color.Red;
			//SelectedFaceColor = Color.FromArgb(72, 222, 224);
			SelectedFaceColor = Utils.HslToColor(0.527778f, 1f, 0.5f);
			SelectedEdgeColor = Color.Orange;
			SelectedNodeColor = Color.Magenta;
			SelectedFaceAndElementColor = Color.Yellow;

			SelectedElementNumbersColor = Color.YellowGreen;
			ElementNumbersColor = Color.OrangeRed;

			OrdinaryEdgeColor = Color.Gray;
			SoftBorderColor = Color.Black;
			HardBorderColor = Color.Black;

			BeamColor = Color.Blue;
			SelectedBeamColor = Color.Red;
			NodesColor = Color.Black;
			NodeNumbersColor = Color.DarkBlue;


			DefaultFirstBorderAngleLimit = 1f;
			DefaultSecondBorderAngleLimit = 70f;

			IncludeEdgeMiddleNodes = true;
			LineSmooth = true;
			PointSmooth = true;
			FaceLighting = true;
			EdgeLighting = false;
			AxisLength = 50f;
			PointSize = 8f;
			OrdinaryEdgeWidth = 1f;
			BorderEdgeWidth = 2f;
			BeamWidth = 2f;
			DefaultCameraDistance = 2.2f;
			XRayVision = false;
			//DEPTH_TEST_TOLERANCE_DISTANCE = 0.005f; // musi byt kladne; na tohle cislo radsi nesahej, na jeho vyladeni bylo potreba plno krve, potu a slz
			DefaultRenderMode = RenderMode.FacesLines;

			SifelFileFormatExtension = ".top";
			PropertyDescriptionFileExtension = ".prop";
			UndoOperationsMaxCount = 20;

			MeshShadingModel = ShadingModel.Smooth;

			ColorScaleLegendPosition = ColorScaleLegendPosition.RightTop;
			VectorArrowsColor = Color.FromArgb(159, 100, 164);
		}

		public static void ExtractMatrices(out int[] viewport, out double[] modelview, out double[] projection)
		{
			viewport = new int[4];
			modelview = new double[16]; // mptm Model matrix
			projection = new double[16];    // ptm Projection matrix

			GL.GetInteger(GetPName.Viewport, viewport);
			GL.GetDouble(GetPName.ModelviewMatrix, modelview);
			GL.GetDouble(GetPName.ProjectionMatrix, projection);
		}

		public static void ExtractViewport(out int[] viewport)
		{
			viewport = new int[4];
			GL.GetInteger(GetPName.Viewport, viewport);
		}

		public static Vector3 ProjectWorldCoordToWindowCoords(Vector3 point)
		{
			int[] viewport;
			double[] modelview;
			double[] projection;
			ExtractMatrices(out viewport, out modelview, out projection);

			Vector3 result;
			Utils.GluProject(point, modelview, projection, viewport, out result);
			return result;
		}

		public static float GetPixelDepth(int x, int y, int[] viewport)
		{
			float[] depth = new float[1];
			GL.ReadPixels(x - viewport[0], viewport[3] - y - viewport[1], 1, 1, PixelFormat.DepthComponent, PixelType.Float, depth);
			return depth[0];
		}

		public static ShadingModel MeshShadingModel
		{
			get { return meshShadingModel; }
			set { meshShadingModel = value; }
		}

		#endregion

		#region Properties, access

		public string Title => mesh?.Name;

		public Mesh Mesh => mesh;

		public Camera Camera
		{
			get { return camera; }
			set { camera = value; }
		}

		public bool DrawAxes
		{
			get { return drawAxesFlag; }
			set { drawAxesFlag = value; }
		}

		public bool DrawAxisArrows
		{
			get { return drawAxisArrowsFlag; }
			set { drawAxisArrowsFlag = value; }
		}

		public bool DrawNodeNumbers
		{
			get { return drawNodeNumbersFlag; }
			set { drawNodeNumbersFlag = value; }
		}

		public bool DrawElementNumbers
		{
			get { return drawElementNumbersFlag; }
			set { drawElementNumbersFlag = value; }
		}

		public bool DrawBeamNumbers
		{
			get { return drawBeamNumbersFlag; }
			set { drawBeamNumbersFlag = value; }
		}

		public bool DrawBeams
		{
			get { return drawBeamsFlag; }
			set { drawBeamsFlag = value; }
		}

		public RenderMode RenderMode
		{
			get { return renderMode; }
			set { renderMode = value; }
		}

		public void SetMesh(Mesh newMesh)
		{
			if (newMesh != this.mesh)
			{
				if (this.mesh != null)
				{
					this.mesh.ReferenceCount--;
					if (this.mesh.ReferenceCount <= 0)
						this.mesh.Dispose();
				}
				if (newMesh != null)
				{
					newMesh.ReferenceCount++;
				}

				initializeMeshDependentFields();

				this.mesh = newMesh;
			}
		}

		public List<CutPlane> CutPlanes => cutPlanes;

		public List<Node> CutPlaneDefinitionNodes => cutPlaneDefinitionNodes;

		public int[] NodeSignal
		{
			get { return nodeSignal; }
			set
			{
				setNodeSignal(value);
				if (value == null)
				{
					selectItemsInSet(new HashSet<ISelectable>()); // clear selection
				}
			}
		}

		public int? ElementSignal
		{
			get { return elementSignal; }
			set
			{
				setElementSignal(value);
				if (value == null)
				{
					selectItemsInSet(new HashSet<ISelectable>()); // clear selection
				}
			}
		}

		public List<Vector3> NodeSignalPositions => nodeSignalPositions;

		public Vector3 ElementSignalPosition => elementSignalPosition;

		public CutInfo LastUsedCutInfo => lastUsedCutInfo;

		public bool ContainsMeshWithIdentifier(int meshIdentifier) => mesh?.UniqueIdentifier == meshIdentifier;

		#endregion

		#region Misc - public methods

		public void SetPropertyOfSelectedItems(Property property)
		{
			if (mesh != null)
			{
				setUnsavedChangesFlag();

				Dictionary<Node, HashSet<Property>> nodesEdgeProperties = new Dictionary<Node, HashSet<Property>>();
				Dictionary<Node, HashSet<Property>> nodesSurfaceProperties = new Dictionary<Node, HashSet<Property>>();
				Dictionary<Node, HashSet<Property>> nodesRegionProperties = new Dictionary<Node, HashSet<Property>>();
				HashSet<EntityType> usedEntityTypes = new HashSet<EntityType>();
				foreach (ISelectable item in mesh.SelectedItems)
				{
					item.Property = property;
					EntityType entityType;
					collectAdjacentNodesProperties(item, property, nodesEdgeProperties, nodesSurfaceProperties, nodesRegionProperties, out entityType);
					usedEntityTypes.Add(entityType);
				}

				updateNodeProperties(nodesEdgeProperties, nodesSurfaceProperties, nodesRegionProperties);

				foreach (EntityType entityType in usedEntityTypes)
				{
					mesh.Statistics.AddProperty(property, entityType);
				}
			}
		}

		public void AddPropertyToSelectedNodes(Property property)
		{
			if (mesh == null)
				return;
			setUnsavedChangesFlag();
			// ----------------------------------------------------
			mesh.Statistics.AddProperty(property, EntityType.Vertex);
			foreach (ISelectable item in mesh.SelectedItems)
			{
				Node n = item as Node;
				if (n != null)
					n.Property = property;
			}
		}

		public void RemovePropertyFromSelectedNodes(Property property)
		{
			if (mesh == null)
				return;
			setUnsavedChangesFlag();
			// --------------------------------------------------------
			foreach (ISelectable item in mesh.SelectedItems)
			{
				Node n = item as Node;
				if (n != null)
					n.RemoveVertexProperty(property);
			}
		}

		public SortedDictionary<Property, bool> GetElementPropertiesSorted()
		{
			if (mesh == null)
				return null;
			SortedDictionary<Property, bool> allProperties = new SortedDictionary<Property, bool>();
			foreach (Element e in mesh.Elements)
			{
				if (!mesh.HiddenElements.Contains(e))
					allProperties[e.Property] = true;
			}
			foreach (Element e in mesh.HiddenElements)
			{
				if (!allProperties.ContainsKey(e.Property))
					allProperties[e.Property] = false;
			}
			return allProperties;
		}

		public IScene Copy()
		{
			Scene copy = new Scene();
			copy.camera = new Camera(this.camera);  // naklonuju kameru

			copy.mesh = this.mesh;              // zkopiruju jen odkaz na mesh
			if (this.mesh != null)
				this.mesh.ReferenceCount++;

			//if (this.mesh == null)
			copy.renderMode = DefaultRenderMode;
			//else
			//	copy.renderMode = this.renderMode;
			copy.drawAxesFlag = this.drawAxesFlag;
			copy.drawAxisArrowsFlag = this.drawAxisArrowsFlag;
			// cut planes kopirovat nebudu
			return copy;
		}

		public void RecreateBuffers()
		{
			if (mesh != null)
				mesh.RecreateBuffers();
		}

		public void SetDefaultCameraView()
		{
			if (mesh == null)
			{
				camera.SetView(CameraView.Iso);
				return;
			}

			Vector3 relativeDimensions = (mesh.UpperBound - mesh.LowerBound) / (mesh.Radius * 2f);
			const float negligibleRelativeSize = 0.1f;

			float smallestRelativeDimension = Math.Min(relativeDimensions.X, Math.Min(relativeDimensions.Y, relativeDimensions.Z));
			if (smallestRelativeDimension < negligibleRelativeSize)
			{
				if (relativeDimensions.X < relativeDimensions.Y && relativeDimensions.X < relativeDimensions.Z) // X is smallest
					camera.SetView(CameraView.Right);
				else if (relativeDimensions.Y < relativeDimensions.X && relativeDimensions.Y < relativeDimensions.Z) // Y is smallest
					camera.SetView(CameraView.Top);
				else // Z is smallest
					camera.SetView(CameraView.Front);
			}
			else
				camera.SetView(CameraView.Iso);
		}

		public void ComputeVisibleNodes(Size clientWindow)
		{
			if (mesh == null)
				return;

			bool findVisibleFaces = ((RenderMode & RenderMode.Faces) != 0) && DrawElementNumbers;
			bool beamsRendered = mesh.BeamCount > 0 && DrawBeams;
			bool findVisibleNodes = findVisibleFaces || ((RenderMode & RenderMode.Points) != 0) || beamsRendered;

			if (findVisibleNodes)
			{
				bool xRay = (RenderMode == RenderMode.None && beamsRendered) || RenderMode == RenderMode.Points;
				mesh.CreateVisibleNodesList(new Rectangle(Point.Empty, clientWindow), Camera, xRay, faceDrawer: mesh.DrawFacesOnly);
			}

			if (findVisibleFaces)
			{
				mesh.CreateVisibleFacesList(new Rectangle(Point.Empty, clientWindow), Camera, faceDrawer: mesh.DrawFacesOnly);
			}
		}

		public void Dispose()
		{
			if (this.mesh != null)
			{
				this.mesh.ReferenceCount--;
				if (this.mesh.ReferenceCount <= 0)
					this.mesh.Dispose();
				this.mesh = null;
			}
		}

		#endregion

		#region Misc - private methods

		private void collectAdjacentNodesProperties(ISelectable item, Property property, Dictionary<Node, HashSet<Property>> nodesEdgeProperties, Dictionary<Node, HashSet<Property>> nodesSurfaceProperties, Dictionary<Node, HashSet<Property>> nodesRegionProperties, out EntityType entityType)
		{
			if (item is Node) // its node, do not update
			{
				entityType = EntityType.Vertex;
				return;
			}
			WingedEdge edge = item as WingedEdge;
			if (edge != null)
			{
				entityType = EntityType.Edge;
				foreach (Node node in edge.IterateThroughAllNodes())
				{
					if (mesh.NodesEdgesIncidence.ContainsKey(node))
					{
						inspectNodeAdjacentEntities(node, nodesEdgeProperties, nodesSurfaceProperties, nodesRegionProperties);
					}
				}
				return;
			}
			IFaceOfElement3D face = item as IFaceOfElement3D;
			if (face != null)
			{
				entityType = EntityType.Surface;
				foreach (Node node in ((Element2D)face).IterateThroughAllNodes())
				{
					inspectNodeAdjacentEntities(node, nodesEdgeProperties, nodesSurfaceProperties, nodesRegionProperties);
				}
				return;
			}
			Element element = item as Element;
			if (element != null)
			{
				entityType = EntityType.Region;
				foreach (Node node in element.IterateThroughAllNodes())
				{
					if (mesh.NodesEdgesIncidence.ContainsKey(node))
					{
						inspectNodeAdjacentEntities(node, nodesEdgeProperties, nodesSurfaceProperties, nodesRegionProperties);
					}
				}
				return;
			}

			throw new NotSupportedException(); // unknown item type
		}

		private void addPropertyToMap(Node node, Property property, Dictionary<Node, HashSet<Property>> map)
		{
			Debug.Assert(node != null);
			Debug.Assert(map != null);

			if (property.IsZero)
				return; // ignore

			HashSet<Property> set;
			if (!map.TryGetValue(node, out set))
				map.Add(node, set = new HashSet<Property>());
			set.Add(property);
		}

		private void inspectNodeAdjacentEntities(Node node, Dictionary<Node, HashSet<Property>> nodesEdgeProperties, Dictionary<Node, HashSet<Property>> nodesSurfaceProperties, Dictionary<Node, HashSet<Property>> nodesRegionProperties)
		{
			Debug.Assert(mesh.NodesEdgesIncidence.ContainsKey(node));

			foreach (WingedEdge edge in mesh.NodesEdgesIncidence[node])
			{
				// Edge
				addPropertyToMap(node, edge.Property, nodesEdgeProperties);
				// Face 1
				IFaceOfElement3D faceOfElement = edge.Face1 as IFaceOfElement3D;
				if (faceOfElement != null)
				{
					addPropertyToMap(node, faceOfElement.Property, nodesSurfaceProperties); // face of 3D element
					addPropertyToMap(node, faceOfElement.ParentElement.Property, nodesRegionProperties);
				}
				else if (edge.Face1 != null) // 2D element
				{
					addPropertyToMap(node, edge.Face1.Property, nodesRegionProperties);
				}
				// Face 2
				faceOfElement = edge.Face2 as IFaceOfElement3D;
				if (faceOfElement != null)
				{
					addPropertyToMap(node, faceOfElement.Property, nodesSurfaceProperties); // face of 3D element
					addPropertyToMap(node, faceOfElement.ParentElement.Property, nodesRegionProperties);
				}
				else if (edge.Face2 != null) // 2D element
				{
					addPropertyToMap(node, edge.Face2.Property, nodesRegionProperties);
				}
			}
		}

		private void updateNodeProperties(Dictionary<Node, HashSet<Property>> nodesEdgeProperties, Dictionary<Node, HashSet<Property>> nodesSurfaceProperties, Dictionary<Node, HashSet<Property>> nodesRegionProperties)
		{
			foreach (Element3D element3D in mesh.Elements.OfType<Element3D>()) // walk through all nodes of 3D elements
			{
				if (!element3D.Property.IsZero)
				{
					foreach (Node node in element3D.IterateThroughAllNodesIncludingEdgeMiddleNodes())
					{
						HashSet<Property> set;
						if (nodesRegionProperties.TryGetValue(node, out set))
							set.Add(element3D.Property);
					}
				}
			}

			HashSet<Node> affectedNodes = new HashSet<Node>();
			foreach (Node node in nodesEdgeProperties.Keys)
				affectedNodes.Add(node);
			foreach (Node node in nodesSurfaceProperties.Keys)
				affectedNodes.Add(node);
			foreach (Node node in nodesRegionProperties.Keys)
				affectedNodes.Add(node);

			foreach (Node node in affectedNodes)
			{
				HashSet<Property> edgeProperties, surfaceProperties, regionProperties;
				nodesEdgeProperties.TryGetValue(node, out edgeProperties);
				nodesSurfaceProperties.TryGetValue(node, out surfaceProperties);
				nodesRegionProperties.TryGetValue(node, out regionProperties);
				node.RebuildEdgeSurfaceRegionProperties(edgeProperties, surfaceProperties, regionProperties);
			}
		}

		#endregion

		#region Signal - private methods

		private void setNodeSignal(int[] nodeSignalToSet)
		{
			try
			{
				if (mesh == null)
					return;

				// clear old signal
				if (tempNodesAddedToSurfaceRepresentation != null) // remove new free nodes if added
				{
					foreach (Node tempNode in tempNodesAddedToSurfaceRepresentation)
						mesh.NodesEdgesIncidence.Remove(tempNode);
					tempNodesAddedToSurfaceRepresentation = null;
					mesh.CreateBuffers();
				}

				if (nodeSignalToSet == null)
				{
					return;
				}

				HashSet<ISelectable> toSelect = new HashSet<ISelectable>();
				HashSet<Node> toAddToSurfaceRep = new HashSet<Node>();
				HashSet<int> nodesToSet = new HashSet<int>(nodeSignalToSet);
				foreach (Element element in mesh.Elements)
				{
					foreach (Node node in element.IterateThroughAllNodesIncludingEdgeMiddleNodes())
					{
						if (nodesToSet.Contains(node.ID))
						{
							// ------------------------------------------------
							if (!mesh.NodesEdgesIncidence.ContainsKey(node))
							{
								toAddToSurfaceRep.Add(node);
							}
							toSelect.Add(node);
						}
					}
				}

				// throw if some id does not exist
				if (nodesToSet.Count > toSelect.Count)
				{
					string missingNodes = string.Join(", ", nodesToSet.Except(toSelect.Cast<Node>().Select(n => n.ID)).Select(id => id.ToString()).ToArray());
					Exception nullException = null;
					string textFormat = (nodesToSet.Count - toSelect.Count == 1) ? "Node with ID {0} does not exist!" : "Nodes with IDs {0} do not exist!";
					throw new ArgumentOutOfRangeException(string.Format(textFormat, missingNodes), nullException);
				}

				// ------------------------------------------------
				tempNodesAddedToSurfaceRepresentation = toAddToSurfaceRep.ToArray();
				foreach (Node tempNode in tempNodesAddedToSurfaceRepresentation)
					mesh.NodesEdgesIncidence[tempNode] = null; // add free node
				mesh.CreateBuffers();
				// ------------------------------------------------
				selectItemsInSet(toSelect); // select signalled nodes
				nodeSignalPositions = toSelect.Cast<Node>().Select(n => n.Position).ToList();
				// ------------------------------------------------
			}
			finally
			{
				this.nodeSignal = nodeSignalToSet; // finally save new value to nodeSignal
			}
		}

		private void setElementSignal(int? elementSignalToSet)
		{
			try
			{
				if (mesh == null)
				{
					return; // goto finally
				}

				if (tempElement3DAddedToSurfaceRepresentation != null)
				{
					new MeshConstructor().RemoveSignal(mesh, tempElement3DAddedToSurfaceRepresentation);
					tempElement3DAddedToSurfaceRepresentation = null;
				}

				if (elementSignalToSet == null)
				{
					return; // goto finally
				}

				int elementID = elementSignalToSet.Value;
				Element element = mesh.Elements.FirstOrDefault(e => e.ID == elementID);
				if (element != null)
				{
					elementSignalPosition = element.GetCenter();
					// ------------------------------------------------
					new MeshConstructor().SignalElement(mesh, element); // add element to surface representation
					tempElement3DAddedToSurfaceRepresentation = element as Element3D;
					// ------------------------------------------------
					HashSet<ISelectable> toSelect = new HashSet<ISelectable>();
					toSelect.Add(element);
					selectItemsInSet(toSelect); // select signalled element
				}
				else
				{
					throw new ArgumentOutOfRangeException("Element with ID " + elementID + " does not exist!", innerException: null);
				}
			}
			finally
			{
				this.elementSignal = elementSignalToSet; // finally save new value to elementSignal
			}
		}

		private void selectItemsInSet(HashSet<ISelectable> itemsToSelect)
		{
			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------			
			HashSet<ISelectable> oldSelection = mesh.SelectedItems;
			mesh.SelectedItems = itemsToSelect;
			updateColorBuffers(oldSelection, itemsToSelect);
		}

		#endregion

		#region Drawing - public methods

		public void Draw(bool optimizeForMoving, bool optimizeForSelecting, bool drawDecorations)
		{
			if (mesh != null)
			{
				mesh.DrawContent(this.renderMode, this.camera, optimizeForMoving, optimizeForSelecting, drawNodeNumbersFlag, drawElementNumbersFlag, this.drawBeamsFlag, this.drawBeamNumbersFlag);

				if (drawDecorations)
				{
					var dataVisualizer = mesh.GetDataVisualizer();
					if (dataVisualizer != null)
					{
						dataVisualizer.DrawDecorations(mesh.ColorMode);
					}
				}
			}

			// vykresli osy
			if (drawAxesFlag)
				drawAxes(origin: (mesh != null) ? (mesh.PositionOffset * -mesh.ResizeFactor) : Vector3.Zero);

			if (this.cutPlaneDefinitionNodes.Count > 0)
				drawPlaneDefinitionPoints();

			// draw cut planes
			if (this.cutPlanes.Count > 0)
				drawCutPlanes();

			if (drawAxisArrowsFlag)
				drawAxisArrows();
		}

		public void DrawWithoutMesh(Vector3 origin)
		{
			if (drawAxesFlag)
				drawAxes(origin);

			if (drawAxisArrowsFlag)
				drawAxisArrows();
		}

		#endregion

		#region Drawing  - private methods

		private void drawPlaneDefinitionPoints()
		{
			Vector3[] points = new Vector3[cutPlaneDefinitionNodes.Count];
			for (int i = 0; i < cutPlaneDefinitionNodes.Count; i++)
				points[i] = cutPlaneDefinitionNodes[i].Position;

			GL.Disable(EnableCap.Lighting);
			CutPlane.DrawDefinitionPoints(points);
			GL.Enable(EnableCap.Lighting);
		}

		private void drawCutPlanes()
		{
			// draw planes
			GL.Disable(EnableCap.Lighting);
			GL.DepthMask(false);
			GL.Enable(EnableCap.Blend);

			foreach (CutPlane plane in cutPlanes)
				plane.Draw(camera.Eye);

			GL.Disable(EnableCap.Blend);
			GL.DepthMask(true);
			GL.Enable(EnableCap.Lighting);
		}

		private static void drawAxes(Vector3 origin)
		{
			if (LineSmooth)
			{
				GL.Enable(EnableCap.LineSmooth);
				GL.Enable(EnableCap.Blend);
			}

			GL.LineWidth(1.0f);
			GL.Disable(EnableCap.Lighting);

			{
				GL.PushMatrix();
				GL.Translate(origin);

				// kladne osy
				GL.Begin(BeginMode.Lines);
				GL.Color3(1.0, 0, 0);       // X
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(AxisLength, 0, 0);
				GL.Color3(0, 1.0, 0);       // Y
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(0, AxisLength, 0);
				GL.Color3(0, 0, 1.0);       // Z
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(0, 0, AxisLength);
				GL.End();

				//GL.LineWidth(0.4f);

				GL.Enable(EnableCap.LineStipple);
				GL.LineStipple(2, 52428);
				// zaporne osy
				GL.Begin(BeginMode.Lines);
				GL.Color3(1.0, 0, 0);       // X
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(-AxisLength, 0, 0);
				GL.Color3(0, 1.0, 0);       // Y
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(0, -AxisLength, 0);
				GL.Color3(0, 0, 1.0);       // Z
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(0, 0, -AxisLength);
				GL.End();

				GL.PopMatrix();
			}

			GL.Disable(EnableCap.LineStipple);
			GL.Enable(EnableCap.Lighting);

			if (LineSmooth)
			{
				GL.Disable(EnableCap.LineSmooth);
				GL.Disable(EnableCap.Blend);
			}
		}

		public void drawAxisArrows()
		{
			const float arrowLength = 60f;
			const float distanceFromWindowLeftBorder = 62f;
			const float distanceFromWindowBottomBorder = 75f;
			const float zDistance = 80f;

			int[] viewport = new int[4];
			GL.GetInteger(GetPName.Viewport, viewport);

			if (LineSmooth)
			{
				GL.Enable(EnableCap.LineSmooth);
				GL.Enable(EnableCap.Blend);
			}

			GL.LineWidth(1.5f);
			GL.Disable(EnableCap.Lighting);
			GL.Disable(EnableCap.DepthTest);

			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			{
				GL.LoadIdentity();
				GL.Ortho(0, viewport[2], viewport[3], 0, 0, zDistance * 2);

				GL.MatrixMode(MatrixMode.Modelview);
				GL.PushMatrix();
				{
					GL.LoadIdentity();

					GL.Translate(distanceFromWindowLeftBorder, viewport[3] - distanceFromWindowBottomBorder, -zDistance);
					GL.Scale(1f, -1f, 1f); // flip y-axis
					camera.LookAt();

					Vector3 xAxisEndPoint = new Vector3(arrowLength, 0, 0);
					Vector3 yAxisEndPoint = new Vector3(0, arrowLength, 0);
					Vector3 zAxisEndPoint = new Vector3(0, 0, arrowLength);

					// draw lines
					GL.Begin(BeginMode.Lines);
					{
						GL.Color3(1.0, 0, 0);       // X
						GL.Vertex3(0, 0, 0);
						GL.Vertex3(xAxisEndPoint);

						GL.Color3(0, 1.0, 0);       // Y
						GL.Vertex3(0, 0, 0);
						GL.Vertex3(yAxisEndPoint);

						GL.Color3(0, 0, 1.0);       // Z
						GL.Vertex3(0, 0, 0);
						GL.Vertex3(zAxisEndPoint);
					}
					GL.End();

					// draw labels
					double[] modelview = new double[16];    // mptm Model matrix
					double[] projection = new double[16];   // ptm Projection matrix
					GL.GetDouble(GetPName.ModelviewMatrix, modelview);
					GL.GetDouble(GetPName.ProjectionMatrix, projection);

					Vector3 xLabelPosition, yLabelPosition, zLabelPosition;
					Utils.GluProject(xAxisEndPoint, modelview, projection, viewport, out xLabelPosition);
					Utils.GluProject(yAxisEndPoint, modelview, projection, viewport, out yLabelPosition);
					Utils.GluProject(zAxisEndPoint, modelview, projection, viewport, out zLabelPosition);
					Content.DrawTextLabels(new[] { new KeyValuePair<string, Vector2>("X", xLabelPosition.Xy), new KeyValuePair<string, Vector2>("Y", yLabelPosition.Xy), new KeyValuePair<string, Vector2>("Z", zLabelPosition.Xy) }, viewport[3]);
				}
				GL.PopMatrix();
			}
			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();

			GL.MatrixMode(MatrixMode.Modelview);

			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Lighting);

			if (LineSmooth)
			{
				GL.Disable(EnableCap.LineSmooth);
				GL.Disable(EnableCap.Blend);
			}
		}

		#endregion

		#region Selection - public methods

		public string GetSelectedItemsDescription()
		{
			if (mesh == null || mesh.SelectedItems.Count == 0) // nothing selected
				return string.Empty;

			string description;
			if (mesh.SelectedItems.Count > 1) // selected more than one entity
			{
				description = getSelectionGroupSummary();
			}
			else
			{
				// otherwise show single selected entity description
				ISelectable item = mesh.SelectedItems.FirstOrDefault();

				Node node = item as Node;
				if (node != null)
				{
					description = node.ToStringWithOriginalCoordinates(mesh.ResizeFactor, mesh.PositionOffset);
				}
				else
				{
					description = item.ToString();
				}
			}

			{
				double? minDataValue, maxDataValue;
				getSelectionGroupDataValueRange(out minDataValue, out maxDataValue);
				Debug.Assert(!(minDataValue.HasValue ^ maxDataValue.HasValue));
				if (minDataValue.HasValue)
				{
					if (minDataValue == maxDataValue)
						description += $" | Data value: {minDataValue:G4}";
					else
						description += $" | Data value range: <{minDataValue:G4}, {maxDataValue:G4}>";
				}
			}

			return description;
		}

		public void SelectItems(Rectangle area, SelectMode mode, SelectOperationType opType, bool allVerticesInArea, ItemTypeToSelect itemType)
		{
			if (mesh == null)
				return;

			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
			GL.LoadIdentity();
			camera.LookAt(); // nastavit kameru
							 // ------------------------------------------------

			HashSet<ISelectable> newSelection;

			if (area.Size == Size.Empty)
				newSelection = getPointSelection(area.X, area.Y, mode, itemType);
			else if (area.Width == 0 || area.Height == 0) // neni to ramecek, ma sirku nebo dylku 0, takze nic nedelam
				return;
			else
				newSelection = getItemsInArea(area, itemType, allVerticesInArea);


			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------


			HashSet<ISelectable> selectedItems = new HashSet<ISelectable>(mesh.SelectedItems);

			switch (opType)
			{
				case SelectOperationType.New:
					selectedItems = newSelection;
					break;
				case SelectOperationType.Union:
					selectedItems.UnionWith(newSelection);
					break;
				case SelectOperationType.Intersection:
					selectedItems.IntersectWith(newSelection);
					break;
				case SelectOperationType.Except:
					selectedItems.ExceptWith(newSelection);
					break;
				case SelectOperationType.SymetricDifference:
					selectedItems.SymmetricExceptWith(newSelection);
					break;
				default:
					throw new NotSupportedException("This select operation type is not supported.");
			}

			HashSet<ISelectable> oldSelection = mesh.SelectedItems;
			mesh.SelectedItems = selectedItems;
			updateColorBuffers(oldSelection, selectedItems);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.PopMatrix();
		}

		public void UnselectAllItems()
		{
			if (mesh == null)
				return;

			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------

			unselectAllItems();
		}

		public void SelectAllItems(EditorMode editorMode)
		{
			if (mesh == null)
				return;

			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------

			ItemTypeToSelect itemType = editorModeToItemType(editorMode);

			HashSet<ISelectable> temp = mesh.SelectedItems;
			mesh.SelectedItems = getAllItemsToSelect(itemType, filter: null);
			updateColorBuffers(temp, mesh.SelectedItems);
		}

		public void InvertSelection()
		{
			if (mesh == null)
				return;

			ItemTypeToSelect itemType;
			ISelectable firstItem = mesh.SelectedItems.FirstOrDefault();
			if (firstItem == null) // nic neni vybrano
				return;
			// -------------------------------------------
			if (firstItem is Element3D)
				itemType = ItemTypeToSelect.Element;
			else if (firstItem is Element2D)
				itemType = ItemTypeToSelect.Face;
			else if (firstItem is Node)
				itemType = ItemTypeToSelect.Node;
			else if (firstItem is WingedEdge)
				itemType = ItemTypeToSelect.Edge;
			else if (firstItem is Beam)
				itemType = ItemTypeToSelect.Beam;
			else
				return;
			// -------------------------------------------
			HashSet<ISelectable> oldSelection = mesh.SelectedItems;
			mesh.SelectedItems = new HashSet<ISelectable>();

			switch (itemType)
			{
				case ItemTypeToSelect.Element:
					foreach (Element e in mesh.Elements)
						if (!mesh.HiddenElements.Contains(e) && !oldSelection.Contains(e))
							mesh.SelectedItems.Add(e);
					break;
				case ItemTypeToSelect.Node:
					foreach (Element e in mesh.Elements)
					{
						if (!mesh.HiddenElements.Contains(e))
						{
							foreach (Node n in e.IterateThroughAllNodes())
								if (!oldSelection.Contains(n))
									mesh.SelectedItems.Add(n);
						}
					}
					break;
				case ItemTypeToSelect.Face:
					foreach (Element2D face in mesh.Faces)
						if (!oldSelection.Contains(face))
							mesh.SelectedItems.Add(face);
					break;
				case ItemTypeToSelect.Edge:
					foreach (WingedEdge edge in mesh.Edges)
						if (!oldSelection.Contains(edge))
							mesh.SelectedItems.Add(edge);
					break;
				case ItemTypeToSelect.Beam:
					foreach (Beam beam in mesh.Beams)
						if (!oldSelection.Contains(beam))
							mesh.SelectedItems.Add(beam);
					break;
			}

			updateColorBuffers(oldSelection, mesh.SelectedItems);
		}

		public void SelectItemsIncidingWithFaces()
		{
			if (mesh == null || mesh.SelectedItems.Count == 0)
				return;

			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------

			HashSet<ISelectable> oldSelection = mesh.SelectedItems;
			HashSet<ISelectable> newSelection = new HashSet<ISelectable>();

			switch (SceneFacade.EditorMode)
			{
				case EditorMode.SelectElements:
					foreach (ISelectable item in mesh.SelectedItems)
					{
						IFaceOfElement3D f = item as IFaceOfElement3D;
						if (f != null && f.ParentElement != null)
							newSelection.Add(f.ParentElement);
					}
					break;
				case EditorMode.SelectEdges:
					foreach (ISelectable item in mesh.SelectedItems)
					{
						Element2D f = item as Element2D;
						if (f != null)
						{
							foreach (WingedEdge e in f.IterateThroughAllEdges())
								newSelection.Add(e);
						}
					}
					break;
				case EditorMode.SelectNodes:
					foreach (ISelectable item in mesh.SelectedItems)
					{
						Element2D f = item as Element2D;
						if (f != null)
						{
							foreach (Node n in (Scene.IncludeEdgeMiddleNodes) ? f.IterateThroughAllNodesIncludingEdgeMiddleNodes() : f.IterateThroughAllNodes())
								newSelection.Add(n);
						}
					}
					break;
				default:
					return;
			}
			mesh.SelectedItems = newSelection;
			updateColorBuffers(oldSelection, mesh.SelectedItems);
		}

		public void SelectItemsWithProperty(EditorMode editorMode, Property property, bool addToSelection)
		{
			if (mesh == null)
				return;

			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------

			ItemTypeToSelect itemType = editorModeToItemType(editorMode);

			HashSet<ISelectable> oldSelection = mesh.SelectedItems;
			HashSet<ISelectable> newSelection = getAllItemsToSelect(itemType, filter: item => item.Property == property);
			if (addToSelection)
			{
				newSelection.UnionWith(oldSelection);
			}
			mesh.SelectedItems = newSelection;
			updateColorBuffers(oldSelection, newSelection);
		}

		#endregion

		#region Selection - private methods

		private void unselectAllItems()
		{
			HashSet<ISelectable> oldSelection = mesh.SelectedItems;
			HashSet<ISelectable> newSelection = new HashSet<ISelectable>();
			mesh.SelectedItems = newSelection;
			updateColorBuffers(oldSelection, newSelection);
		}

		private string getSelectionGroupSummary()
		{
			Debug.Assert(mesh.SelectedItems.Count > 1);

			StringBuilder text = new StringBuilder();

			bool nodeOnly = true, edgeOnly = true, faceOnly = true, elementOnly = true;
			HashSet<Property> properties = new HashSet<Property>();
			HashSet<ElementType> elementTypes = new HashSet<ElementType>();

			foreach (var entity in mesh.SelectedItems)
			{
				properties.Add(entity.Property);

				Node node = entity as Node;
				Element element = entity as Element;
				if (node != null)
				{
					edgeOnly = faceOnly = elementOnly = false;
				}
				else if (entity is WingedEdge)
				{
					nodeOnly = faceOnly = elementOnly = false;
				}
				else if (entity is IFaceOfElement3D)
				{
					nodeOnly = edgeOnly = elementOnly = false;
				}
				else if (element != null)
				{
					nodeOnly = edgeOnly = faceOnly = false;
					elementTypes.Add(element.ElementType);
				}
			}

			text.Append(mesh.SelectedItems.Count); // selected items count
			text.Append(' ');

			// check if all items are of the same type
			if (nodeOnly)
			{
				text.Append("nodes");
			}
			else if (edgeOnly)
			{
				text.Append("edges");
			}
			else if (faceOnly)
			{
				text.Append("faces");
			}
			else if (elementOnly)
			{
				text.Append("elements");
				if (elementTypes.Count == 1)
				{
					text.AppendFormat(" | Type: {0}", elementTypes.First());
				}
			}
			else
			{
				text.Append("entities");
			}

			if (properties.Count != 1 || properties.First() != Property.Zero)
			{
				if (properties.Count == 1)
					text.Append(" | Property: ");
				else
					text.Append(" | Properties: ");
				text.Append(string.Join(", ", properties.OrderBy(p => p.Value).Select(p => p.ToString())));
			}

			return text.ToString();
		}

		private void getSelectionGroupDataValueRange(out double? minDataValue, out double? maxDataValue)
		{
			minDataValue = null;
			maxDataValue = null;

			IDataVisualizer dataVisualizer = mesh.GetDataVisualizer();
			if (dataVisualizer == null)
			{
				return;
			}

			foreach (var selectedItem in mesh.SelectedItems)
			{
				Node node = selectedItem as Node;
				if (node != null)
				{
					double dataValue = dataVisualizer.GetDataValue(node);
					updateMinAndMaxDataValues(dataValue, ref minDataValue, ref maxDataValue);
				}
				else
				{
					WingedEdge edge = selectedItem as WingedEdge;
					if (edge != null)
					{
						double dataValue = dataVisualizer.GetDataValue(edge.BeginNode);
						updateMinAndMaxDataValues(dataValue, ref minDataValue, ref maxDataValue);
						dataValue = dataVisualizer.GetDataValue(edge.EndNode);
						updateMinAndMaxDataValues(dataValue, ref minDataValue, ref maxDataValue);
						QuadraticEdge quadEdge = edge as QuadraticEdge;
						if (quadEdge != null)
						{
							dataValue = dataVisualizer.GetDataValue(quadEdge.MiddleNode);
							updateMinAndMaxDataValues(dataValue, ref minDataValue, ref maxDataValue);
						}
					}
					else
					{
						IFaceOfElement3D face = selectedItem as IFaceOfElement3D;
						if (face != null)
						{
							foreach (var elementNode in getElementNodesFor(face))
							{
								double dataValue = dataVisualizer.GetDataValue(elementNode.Key, elementNode.Value);
								updateMinAndMaxDataValues(dataValue, ref minDataValue, ref maxDataValue);
							}
						}
						else
						{
							Element element = selectedItem as Element;
							if (element != null)
							{
								foreach (var elementNode in getElementNodesFor(element))
								{
									double dataValue = dataVisualizer.GetDataValue(elementNode.Key, elementNode.Value);
									updateMinAndMaxDataValues(dataValue, ref minDataValue, ref maxDataValue);
								}
							}
							//else: not supported item type
						}
					}
				}
			}
		}

		private static void updateMinAndMaxDataValues(double dataValue, ref double? minDataValue, ref double? maxDataValue)
		{
			if (!double.IsNaN(dataValue))
			{
				minDataValue = minDataValue.HasValue ? Math.Min(minDataValue.GetValueOrDefault(), dataValue) : dataValue;
				maxDataValue = maxDataValue.HasValue ? Math.Max(maxDataValue.GetValueOrDefault(), dataValue) : dataValue;
			}
		}

		private static IEnumerable<KeyValuePair<Node, Element>> getElementNodesFor(IFaceOfElement3D face)
		{
			Debug.Assert(face is Element2D);
			Element2D element2D = (Element2D)face;
			return element2D.IterateThroughAllNodesIncludingEdgeMiddleNodes().Select(node => new KeyValuePair<Node, Element>(node, face.ParentElement));
		}

		private static IEnumerable<KeyValuePair<Node, Element>> getElementNodesFor(Element element)
		{
			Debug.Assert(!(element is IFaceOfElement3D));
			return element.IterateThroughAllNodesIncludingEdgeMiddleNodes().Select(node => new KeyValuePair<Node, Element>(node, element));
		}

		private HashSet<ISelectable> getPointSelection(int x, int y, SelectMode mode, ItemTypeToSelect itemType)
		{
			HashSet<ISelectable> newSelection;
			// Select single face first
			Element2D faceHit;
			ISelectable itemHit = getSingleEntityOnLocation(x, y, itemType, out faceHit);
			if (itemHit == null && faceHit == null)
				return new HashSet<ISelectable>();

			switch (mode)
			{
				case SelectMode.None:
					newSelection = new HashSet<ISelectable>();
					break;
				case SelectMode.Single:
					newSelection = new HashSet<ISelectable>();
					if (itemHit != null)
					{
						newSelection.Add(itemHit);
						if (itemType == ItemTypeToSelect.Element && faceHit != null && faceHit.HasTwinElements)
						{
							addAllTwinElementsOfFaceToSet(faceHit, newSelection);
						}
					}
					break;
				case SelectMode.NearSurface:
				case SelectMode.ExtendedSurface:
				case SelectMode.Object:
					if (itemHit != null && !itemHit.Property.IsZero && itemTypeToSelectMatchesCurrentPropertyColorsMode(itemType))
					{
						newSelection = getAllItemsToSelect(itemType, filter: itemToSelect => itemToSelect.Property == itemHit.Property);
					}
					else
					{
						ISelectable advancedItemHit = itemHit;
						if (itemType == ItemTypeToSelect.Node)
						{
							advancedItemHit = getSingleEntityOnLocation(x, y, ItemTypeToSelect.Edge, out faceHit);
							Debug.Assert(advancedItemHit == null || advancedItemHit is WingedEdge);
						}
						do
						{
							newSelection = advancedPointSelection(mode, itemType, faceHit, advancedItemHit);
							++mode;
						} while (newSelection.Count == 1 && mode < SelectMode.Object);
					}
					break;
				default:
					throw new NotSupportedException("This select mode is not supported.");
			}

			return newSelection;
		}

		private bool itemTypeToSelectMatchesCurrentPropertyColorsMode(ItemTypeToSelect itemType)
		{
			var colorMode = mesh.ColorMode;
			switch (itemType)
			{
				case ItemTypeToSelect.Node:
					return (colorMode & PropertyColorsMode.Nodes) != 0;
				case ItemTypeToSelect.Edge:
					return (colorMode & PropertyColorsMode.Edges) != 0;
				case ItemTypeToSelect.Face:
					return (colorMode & PropertyColorsMode.Faces) != 0; // WARNING: does not work correctly for 2D elements, because in case of 2D elements faces and elements are considered as equal
				case ItemTypeToSelect.Element:
					return (colorMode & PropertyColorsMode.Elements) != 0;
				case ItemTypeToSelect.Beam:
					return (colorMode & PropertyColorsMode.Beams) != 0;
				default:
					throw new NotSupportedException();
			}
		}

		private HashSet<ISelectable> advancedPointSelection(SelectMode mode, ItemTypeToSelect itemType, Element2D faceHit, ISelectable item)
		{
			float angleLimit = this.mesh.SoftBorderLimit;
			if (mode == SelectMode.ExtendedSurface)
				angleLimit = this.mesh.HardBorderLimit;
			else if (mode == SelectMode.Object)
				angleLimit = float.MaxValue;

			if (itemType == ItemTypeToSelect.Beam)
			{
				HashSet<ISelectable> newSelection = new HashSet<ISelectable>();
				Beam beam = item as Beam;
				if (beam != null)
				{
					newSelection.Add(item);
					foreach (Beam b in getAllBeamsNeighboringWith(beam, angleLimit))
					{
						newSelection.Add(b);
					}
				}
				return newSelection;
			}

			WingedEdge edgeHit = item as WingedEdge;

			if (itemType == ItemTypeToSelect.Face || itemType == ItemTypeToSelect.Element || edgeHit == null || edgeHit.FeatureAngle < this.mesh.HardBorderLimit)
			{
				HashSet<ISelectable> selection = new HashSet<ISelectable>();
				if (faceHit != null)
				{
					selection = transformSelectedFacesInto(itemType, selectWholeSurface(faceHit, angleLimit));
				}
				return selection;
			}

			if (itemType == ItemTypeToSelect.Node || itemType == ItemTypeToSelect.Edge)
			{
				HashSet<ISelectable> selection = new HashSet<ISelectable>();
				foreach (WingedEdge e in getHardBorderEdges(edgeHit, faceHit, mode))
				{
					if (itemType == ItemTypeToSelect.Node)
					{
						selection.Add(e.BeginNode);
						selection.Add(e.EndNode);
						if (Scene.IncludeEdgeMiddleNodes)
						{
							QuadraticEdge q = e as QuadraticEdge;
							if (q != null)
								selection.Add(q.MiddleNode);
						}
					}
					else
						selection.Add(e);
				}
				return selection;
			}

			throw new NotSupportedException();
		}

		private IEnumerable<Beam> getAllBeamsNeighboringWith(Beam startBeam, float angleLimit)
		{
			// create Node-beam incidence map
			ILookup<Node, Beam> beamNodeMap = (from beam in mesh.Beams
											   from node in beam.IterateThroughAllNodes()
											   select new KeyValuePair<Node, Beam>(node, beam)).ToLookup(pair => pair.Key, pair => pair.Value);

			HashSet<Beam> selectionSet = new HashSet<Beam>();
			Stack<Beam> beams = new Stack<Beam>();
			beams.Push(startBeam);
			selectionSet.Add(startBeam);
			while (beams.Count > 0)
			{
				Beam beam = beams.Pop();
				foreach (Beam neighbor in getBeamNeighbors(beam, beamNodeMap, angleLimit))
					if (selectionSet.Add(neighbor))
						beams.Push(neighbor);
			}
			return selectionSet;
		}

		private IEnumerable<Beam> getBeamNeighbors(Beam beam, ILookup<Node, Beam> beamNodeMap, float angleLimit)
		{
			return from node in beam.IterateThroughAllNodes()
				   from neighborBeam in beamNodeMap[node]
				   where angleLimit > getAngleBetweenTwoBeams(beam, neighborBeam, node)
				   select neighborBeam;
		}

		private static float getAngleBetweenTwoBeams(Beam first, Beam second, Node connectingNode)
		{
			Vector3 firstUnitVector, secondUnitVector;

			if (first.BeginNode == connectingNode)
			{
				firstUnitVector = Vector3.Normalize(first.EndNode.Position - first.BeginNode.Position);
			}
			else
			{
				firstUnitVector = Vector3.Normalize(first.BeginNode.Position - first.EndNode.Position);
			}

			if (second.BeginNode == connectingNode)
			{
				secondUnitVector = Vector3.Normalize(second.EndNode.Position - second.BeginNode.Position);
			}
			else
			{
				secondUnitVector = Vector3.Normalize(second.BeginNode.Position - second.EndNode.Position);
			}

			return Utils.GetAngleInDegreesBetweenUnitVectors(firstUnitVector, -secondUnitVector); // invert second vector because the more open angle the better
		}

		private HashSet<WingedEdge> getHardBorderEdges(WingedEdge edgeHit, Element2D faceHit, SelectMode mode)
		{
			NeighborSelection getNeighborsFun;
			switch (mode)
			{
				case SelectMode.NearSurface:
					getNeighborsFun = delegate (WingedEdge e)
					{
						Vector3 unit = Vector3.Normalize(e.EndNode.Position - e.BeginNode.Position);
						HashSet<WingedEdge> neighbors = new HashSet<WingedEdge>();
						WingedEdge neighbor = null;
						int count = 0;
						foreach (WingedEdge n in e.BeginNeighbors)
						{
							if (n != e && n.FeatureAngle >= this.mesh.HardBorderLimit)
							{
								count++;
								Vector3 v = Vector3.Normalize(n.EndNode.Position - n.BeginNode.Position);
								if (n.EndNode != e.BeginNode)
									v = -v;
								float angle = Utils.GetAngleInDegreesBetweenUnitVectors(unit, v);
								if (angle < (this.mesh.HardBorderLimit * 0.9f)) // - 10%
									neighbor = n;
							}
						}
						if (count == 1 && neighbor != null)
							neighbors.Add(neighbor);
						count = 0;
						neighbor = null;
						foreach (WingedEdge n in e.EndNeighbors)
						{
							if (n != e && n.FeatureAngle >= this.mesh.HardBorderLimit)
							{
								count++;
								Vector3 v = Vector3.Normalize(n.EndNode.Position - n.BeginNode.Position);
								if (n.BeginNode != e.EndNode)
									v = -v;
								float angle = Utils.GetAngleInDegreesBetweenUnitVectors(unit, v);
								if (angle < (this.mesh.HardBorderLimit * 0.9f)) // - 10%
									neighbor = n;
							}
						}
						if (count == 1 && neighbor != null)
							neighbors.Add(neighbor);
						return neighbors;
					};
					break;
				case SelectMode.ExtendedSurface:
					if (faceHit == null)
					{
						if (edgeHit.Face1 != null && isFrontFace(edgeHit.Face1)) // vyber privracenou plochu
							faceHit = edgeHit.Face1;
						else if (edgeHit.Face2 != null)
							faceHit = edgeHit.Face2;
						else
						{
							faceHit = edgeHit.Face1;
							if (faceHit == null)
								goto case SelectMode.NearSurface; /**/ // vratit se zpatky
						}
					}
					HashSet<Element2D> surface = selectWholeSurface(faceHit, this.mesh.HardBorderLimit);
					getNeighborsFun = delegate (WingedEdge e)
					{
						Vector3 unit = Vector3.Normalize(e.EndNode.Position - e.BeginNode.Position);
						HashSet<WingedEdge> neighbors = new HashSet<WingedEdge>();
						foreach (WingedEdge n in e.BeginNeighbors)
							if (n != e && n.FeatureAngle >= this.mesh.HardBorderLimit && (surface.Contains(n.Face1) || surface.Contains(n.Face2)))
								neighbors.Add(n);
						foreach (WingedEdge n in e.EndNeighbors)
							if (n != e && n.FeatureAngle >= this.mesh.HardBorderLimit && (surface.Contains(n.Face1) || surface.Contains(n.Face2)))
								neighbors.Add(n);
						return neighbors;
					};
					break;
				case SelectMode.Object:
					getNeighborsFun = delegate (WingedEdge e)
					{
						Vector3 unit = Vector3.Normalize(e.EndNode.Position - e.BeginNode.Position);
						HashSet<WingedEdge> neighbors = new HashSet<WingedEdge>();
						foreach (WingedEdge n in e.BeginNeighbors)
							if (n != e && n.FeatureAngle >= this.mesh.HardBorderLimit)
								neighbors.Add(n);
						foreach (WingedEdge n in e.EndNeighbors)
							if (n != e && n.FeatureAngle >= this.mesh.HardBorderLimit)
								neighbors.Add(n);
						return neighbors;
					};
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode));
			}
			// ------------------------------------
			return getBorderEdges(edgeHit, getNeighborsFun);
		}

		private static HashSet<WingedEdge> getBorderEdges(WingedEdge startEdge, NeighborSelection getNeighborsFun)
		{
			HashSet<WingedEdge> selection = new HashSet<WingedEdge>();
			Stack<WingedEdge> expansion = new Stack<WingedEdge>();
			expansion.Push(startEdge);
			selection.Add(startEdge);
			while (expansion.Count > 0)
			{
				WingedEdge top = expansion.Pop();
				foreach (WingedEdge neighbor in getNeighborsFun(top))
					if (selection.Add(neighbor))
						expansion.Push(neighbor);
			}
			return selection;
		}

		private static HashSet<ISelectable> transformSelectedFacesInto(ItemTypeToSelect itemType, HashSet<Element2D> faces)
		{
			HashSet<ISelectable> items = new HashSet<ISelectable>();
			switch (itemType)
			{
				case ItemTypeToSelect.Face:
					foreach (Element2D face in faces)
					{
						items.Add(face);
						//if (face.HasTwinElements)
						//	addAllTwinElementsOfFaceToSet(face, items);
					}
					break;
				case ItemTypeToSelect.Node:
					foreach (Element2D face in faces)
						foreach (Node n in (Scene.IncludeEdgeMiddleNodes) ? face.IterateThroughAllNodesIncludingEdgeMiddleNodes() : face.IterateThroughAllNodes())
							items.Add(n);
					break;
				case ItemTypeToSelect.Edge:
					foreach (Element2D face in faces)
						foreach (WingedEdge e in face.IterateThroughAllEdges())
							items.Add(e);
					break;
				case ItemTypeToSelect.Element:
					foreach (Element2D face in faces)
					{
						IFaceOfElement3D faceOfElement = face as IFaceOfElement3D;
						if (faceOfElement != null)
							items.Add(faceOfElement.ParentElement);
						else
							items.Add(face); // je to 2D element, ne face

						if (face.HasTwinElements)
							addAllTwinElementsOfFaceToSet(face, items);
					}
					break;
				default:
					throw new NotSupportedException("This type of item is not supported to select.");
			}
			return items;
		}

		private ISelectable getSingleEntityOnLocation(int x, int y, ItemTypeToSelect itemType, out Element2D faceHit)
		{
			Rectangle area;
			int[] viewport = new int[4];
			GL.GetInteger(GetPName.Viewport, viewport);

			Dictionary<Node, Vector3> visibleNodesProjected = null;
			if (itemType == ItemTypeToSelect.Node)
			{
				area = new Rectangle(x - NODE_SELECTION_TOLERANCE_DISTANCE, y - NODE_SELECTION_TOLERANCE_DISTANCE, NODE_SELECTION_TOLERANCE_DISTANCE << 1, NODE_SELECTION_TOLERANCE_DISTANCE << 1);
				visibleNodesProjected = mesh.FindVisibleNodesProjectedPositions(area, this.camera, faceDrawer: mesh.DrawFacesOnly);
			}
			else if (itemType != ItemTypeToSelect.Beam)
			{
				mesh.DrawFacesToDepthBuffer(faceDrawer: mesh.DrawFacesOnly); // tohle tu jen kvuli zjisteni ty hloubky pomoci getPixelDepth nize
			}

			{
				float pixelDepth = GetPixelDepth(x, y, viewport);
				Vector3 hitPoint;
				faceHit = getNearestFace(mesh.Faces, new Vector3(x - viewport[0], viewport[3] - y - viewport[1], pixelDepth), out hitPoint);
			}

			// ------------------------------------
			switch (itemType)
			{
				case ItemTypeToSelect.Face:
					return faceHit;
				case ItemTypeToSelect.Element:
					IFaceOfElement3D faceOfElement = faceHit as IFaceOfElement3D;
					if (faceOfElement != null)
						return faceOfElement.ParentElement;
					return faceHit; // je to 2D element, ne face
				case ItemTypeToSelect.Node:
					return getNearestNode(x, viewport[3] - y, visibleNodesProjected);
				case ItemTypeToSelect.Edge:
					IEnumerable<WingedEdge> edges;
					if (faceHit == null)
						edges = mesh.Edges;
					else
						edges = faceHit.IterateThroughAllEdges();
					return getNearestEdge(x, viewport[3] - y, edges);
				case ItemTypeToSelect.Beam:
					return getNearestBeam(x, viewport[3] - y);
				default:
					throw new NotSupportedException("This type of item is not supported for selection.");
			}
		}

		private ISelectable getNearestBeam(int x, int y)
		{
			// ! V podstate kopie metody getNearestEdge, nepodarilo se mi najit efektivni zpusob, jak sloucit obe metody...

			int[] viewport;
			double[] modelview;
			double[] projection;
			ExtractMatrices(out viewport, out modelview, out projection);
			// ---------------------------------
			Vector2 hitPoint2D = new Vector2(x, y);
			float minDistance = float.MaxValue;
			Beam result = null;
			Vector3 planeNormal = camera.GetDirection();
			Vector3 planePoint = camera.Eye + (planeNormal * (float)Z_NEAR_PARAM);
			foreach (Beam beam in mesh.Beams)
			{
				Vector3 lineA = beam.BeginNode.Position;
				Vector3 lineB = beam.EndNode.Position;

				bool isCompletelyBehind;
				Utils.TrimLineByPlane(ref lineA, ref lineB, planePoint, planeNormal, out isCompletelyBehind); // pokud hrana prochazi obrazovkou, tak ji pred testem oriznout, spatne by se to jinak promitlo

				if (isCompletelyBehind) // pokud je hrana uplne za mnou, tak ji vynecham
					continue;

				Vector3 projA, projB;
				Utils.GluProject(lineA, modelview, projection, viewport, out projA); // promitni hranu do obrazovky
				Utils.GluProject(lineB, modelview, projection, viewport, out projB);
				float dist;
				if (Utils.LineHit(projA.Xy, projB.Xy, hitPoint2D, EDGE_SELECTION_TOLERANCE_DISTANCE, out dist) && dist < minDistance)
				{
					minDistance = dist;
					result = beam;
				}
			}
			return result;
		}

		private ISelectable getNearestEdge(int x, int y, IEnumerable<WingedEdge> edges)
		{
			int[] viewport;
			double[] modelview;
			double[] projection;
			ExtractMatrices(out viewport, out modelview, out projection);
			// ---------------------------------
			Vector2 hitPoint = new Vector2(x, y);
			float minDistance = float.MaxValue;
			WingedEdge result = null;
			Vector3 planeNormal = camera.GetDirection();
			Vector3 planePoint = camera.Eye + (planeNormal * (float)Z_NEAR_PARAM);
			foreach (WingedEdge edge in edges)
			{
				Vector3 lineA = edge.BeginNode.Position;
				Vector3 lineB = edge.EndNode.Position;

				bool isCompletelyBehind;
				Utils.TrimLineByPlane(ref lineA, ref lineB, planePoint, planeNormal, out isCompletelyBehind); // pokud hrana prochazi obrazovkou, tak ji pred testem oriznout, spatne by se to jinak promitlo

				if (isCompletelyBehind) // pokud je hrana uplne za mnou, tak ji vynecham
					continue;

				Vector3 projA, projB;
				Utils.GluProject(lineA, modelview, projection, viewport, out projA); // promitni hranu do obrazovky
				Utils.GluProject(lineB, modelview, projection, viewport, out projB);
				float dist;
				if (Utils.LineHit(projA.Xy, projB.Xy, hitPoint, EDGE_SELECTION_TOLERANCE_DISTANCE, out dist) && dist < minDistance)
				{
					minDistance = dist;
					result = edge;
				}
			}
			return result;
		}

		private Node getNearestNode(int x, int y, Dictionary<Node, Vector3> visibleNodesProjected)
		{
			Vector2 hitPoint = new Vector2(x, y);
			float distanceSQR = float.MaxValue;
			Node result = null;
			foreach (Node node in visibleNodesProjected.Keys)
			{

				float d = (visibleNodesProjected[node].Xy - hitPoint).LengthSquared;
				if (d < distanceSQR)
				{
					distanceSQR = d;
					result = node;
				}
			}
			return result;
		}

		private HashSet<ISelectable> getAllItemsToSelect(ItemTypeToSelect itemType, Predicate<ISelectable> filter)
		{
			HashSet<ISelectable> newSelection = new HashSet<ISelectable>();
			switch (itemType)
			{
				case ItemTypeToSelect.Element:
					foreach (Element e in mesh.Elements)
					{
						if (!mesh.HiddenElements.Contains(e) && !(e is Beam))
						{
							if (filter == null || filter(e))
								newSelection.Add(e);
						}
					}
					break;
				case ItemTypeToSelect.Node:
					foreach (Element e in mesh.Elements)
					{
						if (!mesh.HiddenElements.Contains(e))
						{
							foreach (Node n in e.IterateThroughAllNodesIncludingEdgeMiddleNodes())
							{
								if (filter == null || filter(n))
									newSelection.Add(n);
							}
						}
					}
					break;
				case ItemTypeToSelect.Face:
					foreach (Element2D face in mesh.Faces)
					{
						if (filter == null || filter(face))
							newSelection.Add(face);
					}
					break;
				case ItemTypeToSelect.Edge:
					foreach (WingedEdge edge in mesh.Edges)
					{
						if (filter == null || filter(edge))
							newSelection.Add(edge);
					}
					break;
				case ItemTypeToSelect.Beam:
					foreach (Beam beam in mesh.Beams)
					{
						if (filter == null || filter(beam))
							newSelection.Add(beam);
					}
					break;
			}

			return newSelection;
		}

		private static ItemTypeToSelect editorModeToItemType(EditorMode editorMode)
		{
			ItemTypeToSelect itemType;
			switch (editorMode)
			{
				case EditorMode.SelectElements:
					itemType = ItemTypeToSelect.Element;
					break;
				case EditorMode.SelectNodes:
					itemType = ItemTypeToSelect.Node;
					break;
				case EditorMode.SelectFaces:
					itemType = ItemTypeToSelect.Face;
					break;
				case EditorMode.SelectEdges:
					itemType = ItemTypeToSelect.Edge;
					break;
				case EditorMode.SelectBeams:
					itemType = ItemTypeToSelect.Beam;
					break;
				default:
					itemType = ItemTypeToSelect.Element; // defaultne vybirat prvky
					break;
			}
			return itemType;
		}

		private void updateColorBuffers(HashSet<ISelectable> oldSelection, HashSet<ISelectable> newSelection)
		{
			int changedFacesCount = 0;
			int changedEdgesCount = 0;
			int changedNodesCount = 0;
			int changedBeamsCount = 0;

			foreach (ISelectable item in oldSelection)
			{
				if (item is Element2D || item is Element3D)
					changedFacesCount++;
				else if (item is WingedEdge)
					changedEdgesCount++;
				else if (item is Node)
					changedNodesCount++;
				else if (item is Beam)
					changedBeamsCount++;
			}

			foreach (ISelectable item in newSelection)
			{
				if (item is Element2D || item is Element3D)
					changedFacesCount++;
				else if (item is WingedEdge)
					changedEdgesCount++;
				else if (item is Node)
					changedNodesCount++;
				else if (item is Beam)
					changedBeamsCount++;
			}

			if (changedFacesCount > 0)
			{
				mesh.UpdateFaceColors();
				mesh.UpdateEdgeColors(); // when some element is selected, edge is also drawn as selected (to be visible if wireframe rendering is on)
			}
			else if (changedEdgesCount > 0)
				mesh.UpdateEdgeColors();
			if (changedNodesCount > 0)
				mesh.UpdateNodeColors();
			if (changedBeamsCount > 0)
				mesh.UpdateBeamColors();
		}

		private Element2D getNearestFace(IEnumerable<Element2D> allFaces, Vector3 windowPos, out Vector3 hitPoint)
		{
			int[] viewport;
			double[] modelview;
			double[] projection;
			ExtractMatrices(out viewport, out modelview, out projection);

			// ----------------------------------------------------------

			Utils.GluUnProject(windowPos, modelview, projection, viewport, out hitPoint);

			Element2D frontChoose = null;
			Element2D backChoose = null;
			float angleSum, bestFrontAngleSum = float.MinValue, bestBackAngleSum = float.MinValue;
			foreach (Element2D face in allFaces)
			{
				if (pointIsInsideFace(hitPoint, face, out angleSum))
				{
					if (isFrontFace(face)) // pokud jde o privracenou stranu
					{
						if (angleSum > bestFrontAngleSum)
						{
							frontChoose = face;
							bestFrontAngleSum = angleSum;
						}
					}
					else
					{
						if (angleSum > bestBackAngleSum)
						{
							backChoose = face;
							bestBackAngleSum = angleSum;
						}
					}
				}
			}

			if (backChoose == null)
				return frontChoose;
			else if (frontChoose == null)
				return backChoose;
			else
				return (bestFrontAngleSum >= bestBackAngleSum) ? frontChoose : backChoose;
		}

		private static bool pointIsInsideFace(Vector3 point, Element2D face, out float angleSum)
		{
			List<Vector3> vectors = new List<Vector3>(face.NodeCount);
			foreach (Node n in face.IterateThroughAllNodes())
				vectors.Add(Vector3.Normalize(n.Position - point));
			angleSum = 0f;
			for (int i = 0; i < vectors.Count; i++)
				angleSum += Utils.GetAngleInDegreesBetweenUnitVectors(vectors[i], vectors[(i + 1) % vectors.Count]);
			return angleSum > LIMIT_ANGLE_FOR_POINT_INSIDE_FACE_DECISION; /**/
		}

		private HashSet<ISelectable> getItemsInArea(Rectangle selectionArea, ItemTypeToSelect itemType, bool allVerticesInArea)
		{
			// --------------------------------------
			bool xRay = ((itemType == ItemTypeToSelect.Beam || itemType == ItemTypeToSelect.Node) && (renderMode & RenderMode.Faces) == 0) ? true : Scene.XRayVision;
			HashSet<Node> visibleNodes = mesh.FindVisibleNodes(selectionArea, this.camera, xRay, computeNodeDensity: false, faceDrawer: mesh.DrawFacesOnly);
			// --------------------------------------
			HashSet<ISelectable> result = new HashSet<ISelectable>();
			// --------------------------------------

			if (itemType == ItemTypeToSelect.Node)
			{
				foreach (Node n in visibleNodes)
					result.Add(n);
				return result;
			}
			else if (itemType == ItemTypeToSelect.Beam)
			{
				foreach (Beam b in mesh.Beams)
				{
					int number = getNumberOfVisibleNodesFrom(new Node[] { b.BeginNode, b.EndNode }, visibleNodes);
					if (number == 2 || (!allVerticesInArea && number > 0))
						result.Add(b);
				}
				return result;
			}
			// hrany timto zpusobem nevybiram, nejdriv najdu viditelne plochy, jinak bych totiz vybral i hrany, co maj sice videt jeden uzel, ale jinak jsou skryty

			// ---------------------------------------
			HashSet<Element2D> pickedFaces = new HashSet<Element2D>();

			// najdi pickedfaces
			foreach (Node node in visibleNodes)
			{
				if (Scene.XRayVision)
				{
					pickedFaces.UnionWith(mesh.GetFacesIncidingWithNode(node)); // pridat vsechny
					continue;
				}
				HashSet<Element2D> frontFaces = new HashSet<Element2D>();
				HashSet<Element2D> backFaces = new HashSet<Element2D>();
				Element2D frontOne = null, backOne = null;
				List<WingedEdge> incidingEdges;
				if (!mesh.NodesEdgesIncidence.TryGetValue(node, out incidingEdges) || incidingEdges == null) // neni to normalni uzel, site, ale bud uzel ve stredu hrany nebo jiny uzel (treba uplne mimo - jeden uzel beamu...)
					continue;

				foreach (WingedEdge edge in incidingEdges)
				{
					if (frontOne == null || backOne == null)
						frontOne = backOne = null;
					if (edge.Face1 != null)
					{
						if (isFrontFace(edge.Face1))
						{
							frontFaces.Add(edge.Face1);
							if (frontOne == null) frontOne = edge.Face1;
						}
						else
						{
							backFaces.Add(edge.Face1);
							if (backOne == null) backOne = edge.Face1;
						}
					}
					if (edge.Face2 != null)
					{
						if (isFrontFace(edge.Face2))
						{
							frontFaces.Add(edge.Face2);
							if (frontOne == null) frontOne = edge.Face2;
						}
						else
						{
							backFaces.Add(edge.Face2);
							if (backOne == null) backOne = edge.Face2;
						}
					}
				}

				// =========
				if (frontOne == null)
					pickedFaces.UnionWith(backFaces);
				else if (backOne == null)
					pickedFaces.UnionWith(frontFaces);
				else
				{
					if (frontFaceIsCloserToEye(frontOne, backOne))
						pickedFaces.UnionWith(frontFaces);
					else
						pickedFaces.UnionWith(backFaces);
				}
				// =========
			}
			// ---------------------------------------
			switch (itemType)
			{
				case ItemTypeToSelect.Edge:
					foreach (Element2D face in pickedFaces)
					{
						foreach (WingedEdge edge in face.IterateThroughAllEdges())
						{
							int number = getNumberOfVisibleNodesFrom(new Node[] { edge.BeginNode, edge.EndNode }, visibleNodes);
							if (number == 2 || (!allVerticesInArea && number > 0))
							{
								result.Add(edge);
							}
						}
					}
					break;
				case ItemTypeToSelect.Face:
					foreach (Element2D face in pickedFaces)
					{
						if (!allVerticesInArea)
						{
							result.Add(face);
							//if (face.HasTwinElements)
							//	addAllTwinElementsOfFaceToSet(face, result);
						}
						else
						{
							int number = getNumberOfVisibleNodesFrom(face.IterateThroughAllNodes(), visibleNodes);
							if (number == face.NodeCount)
							{
								result.Add(face);
								//if (face.HasTwinElements)
								//	addAllTwinElementsOfFaceToSet(face, result);
							}
						}
					}
					break;
				case ItemTypeToSelect.Element:
					foreach (Element2D face in pickedFaces)
					{
						Element itemToAdd;
						IFaceOfElement3D faceOfElement = face as IFaceOfElement3D;
						if (faceOfElement != null)
						{
							if (faceOfElement.ParentElement != null)
								itemToAdd = faceOfElement.ParentElement;
							else
								throw new Exception("Parent element of face is null. This should not happen!");
						}
						else
							itemToAdd = face;
						// --------------------------------------
						if (!allVerticesInArea)
						{
							result.Add(itemToAdd);
							if (face.HasTwinElements)
								addAllTwinElementsOfFaceToSet(face, result);
						}
						else
						{
							int number = getNumberOfVisibleNodesFrom(face.IterateThroughAllNodes(), visibleNodes);
							if (number == face.NodeCount)
							{
								result.Add(itemToAdd);
								if (face.HasTwinElements)
									addAllTwinElementsOfFaceToSet(face, result);
							}
						}
					}
					break;
				default:
					throw new NotSupportedException("This type of item is not supported for selection.");
			}
			return result;
		}

		private static void addAllTwinElementsOfFaceToSet(Element2D face, HashSet<ISelectable> selection)
		{
			Debug.Assert(selection != null);
			Debug.Assert(face != null);
			Debug.Assert(face.HasTwinElements);

			foreach (var twinElement in face.GetTwinElements())
			{
				selection.Add(twinElement);
			}
		}

		private bool frontFaceIsCloserToEye(Element2D frontFace, Element2D backFace)
		{
			foreach (Node n in backFace.IterateThroughAllNodes())
			{
				if (!frontFace.ContainsNode_IgnoreMiddleNodes(n))
				{
					if (Vector3.Dot(n.Position - frontFace.GetSignificantPoint(), frontFace.NormalVector) > 0)
						return false;
				}
			}
			return true;
		}

		private bool isFrontFace(Element2D face)
		{
			return (Vector3.Dot(camera.Eye - /*face.GetCenter()*/face.GetSignificantPoint(), face.NormalVector) > 0);
		}

		private int getNumberOfVisibleNodesFrom(IEnumerable<Node> someNodes, HashSet<Node> visibleNodes)
		{
			int number = 0;
			foreach (Node n in someNodes)
				if (visibleNodes.Contains(n))
					number++;
			return number;
		}

		private static HashSet<Element2D> selectWholeSurface(Element2D startFace, float borderAngleLimit)
		{
			HashSet<Element2D> selectionSet = new HashSet<Element2D>();
			Stack<Element2D> faces = new Stack<Element2D>();
			faces.Push(startFace);
			selectionSet.Add(startFace);
			while (faces.Count > 0)
			{
				Element2D face = faces.Pop();
				foreach (Element2D neighbor in face.GetNeighbors(borderAngleLimit))
					if (selectionSet.Add(neighbor))
						faces.Push(neighbor);
			}
			return selectionSet;
		}

		#endregion

		#region Cutting - public methods

		public void PutNextPlaneDefinitionPoint(int x, int y)
		{
			if (mesh == null)
				return;
			Element2D faceHit;
			Node pickedNode = getSingleEntityOnLocation(x, y, ItemTypeToSelect.Node, out faceHit) as Node;
			if (pickedNode == null) // zadnej zasah
				return;
			if (cutPlaneDefinitionNodes.Contains(pickedNode)) // pokud uz jsem ho driv vybral, tak ho odeberu
			{
				cutPlaneDefinitionNodes.Remove(pickedNode);
				return;
			}
			if (cutPlaneDefinitionNodes.Count >= 3) // pokud uz byly 3, tak je smazu, zacnu dalsi varku
				cutPlaneDefinitionNodes.Clear();
			cutPlaneDefinitionNodes.Add(pickedNode);
		}

		public void ClearPlaneDefinitionPoints()
		{
			cutPlaneDefinitionNodes.Clear();
		}

		public void UpdateLastUsedCut()
		{
			if (lastUsedCutInfo != null)
				Cut(lastUsedCutInfo);
		}

		public void Cut(CutInfo cutInfo)
		{
			Debug.Assert(cutInfo != null);
			if (mesh == null)
				return;

			this.lastUsedCutInfo = cutInfo;

			unselectAllItems();

			// -------------------------------
			// remove element signal if exists
			setElementSignal(null);
			// -------------------------------

			bool updateColours = false;
			if (cutInfo.CutTestMethod != null) // By expression
			{
				if (cutInfo.Action == CutInfo.ActionType.Cut) // cut
				{
					Cutter.CutMeshByExpression(mesh, cutInfo);
					//mesh.UpdateFaceColors();
				}
				else // select
				{
					// ------------------------------------------------------------------
					saveStateBeforeSelect();
					// ------------------------------------------------------------------
					Cutter.SelectItemsByExpression(mesh, cutInfo, true);
					updateColours = true;
				}
			}
			else // not by expression
			{
				if (cutInfo.Action == CutInfo.ActionType.Cut) // cut by planes
				{
					Cutter.CutMeshByPlanes(mesh, cutPlanes, cutInfo);
					//mesh.UpdateFaceColors();
				}
				else if (cutInfo.Action == CutInfo.ActionType.ShowHideElements) // show/hide elements
				{
					Cutter.SetVisibility(mesh, cutInfo);
				}
				else // select by planes
				{
					// ------------------------------------------------------------------
					saveStateBeforeSelect();
					// ------------------------------------------------------------------
					Cutter.SelectItemsByCutPlanes(mesh, cutPlanes, cutInfo);
					updateColours = true;
				}
			}
			// ------------------------------------------------
			// update colors
			if (updateColours)
			{
				switch (cutInfo.Action)
				{
					case CutInfo.ActionType.SelectFaces:
					case CutInfo.ActionType.SelectElements:
						mesh.UpdateFaceColors();
						break;
					case CutInfo.ActionType.SelectEdges:
						mesh.UpdateEdgeColors();
						break;
					case CutInfo.ActionType.SelectNodes:
						mesh.UpdateNodeColors();
						break;
					case CutInfo.ActionType.SelectBeams:
						mesh.UpdateBeamColors();
						break;
				}
			}

		}

		public void CreateCutPlaneFromDefinitionPoints()
		{
			if (cutPlaneDefinitionNodes.Count < 2) // musi byt alespon 2 body
				return;
			//throw new Exception("You must specify at least 2 plane-definition points.");
			if (cutPlaneDefinitionNodes.Count == 2)
				cutPlanes.Add(new CutPlane(cutPlaneDefinitionNodes[0].Position, cutPlaneDefinitionNodes[1].Position, mesh.CenterOfRotation, mesh.Radius, mesh.LowerBound, mesh.UpperBound, mesh.MinimalElementRadius, mesh.ResizeFactor, mesh.PositionOffset));
			else
				cutPlanes.Add(new CutPlane(cutPlaneDefinitionNodes[0].Position, cutPlaneDefinitionNodes[1].Position, cutPlaneDefinitionNodes[2].Position, mesh.CenterOfRotation, mesh.Radius, mesh.LowerBound, mesh.UpperBound, mesh.MinimalElementRadius, mesh.ResizeFactor, mesh.PositionOffset));
			cutPlaneDefinitionNodes.Clear(); // smazat definicni body
		}

		public void HideSelectedElements()
		{
			if (mesh == null)
				return;
			// ----------------------------------
			var selectedItems = new HashSet<ISelectable>(mesh.SelectedItems);
			setElementSignal(null); // remove element signal if exists
			mesh.SelectedItems = selectedItems;
			// ----------------------------------
			Cutter.HideSelectedElements(mesh);
		}

		public void RestoreWholeMesh()
		{
			if (mesh == null)
				return;
			// ----------------------------------
			// remove element signal if exists
			setElementSignal(null);
			// ----------------------------------
			Cutter.RestoreAllElements(mesh);
		}

		#endregion

		#region History - private methods

		private void saveStateBeforeSelect()
		{
			if (mesh == null)
				return;
			// no unsaved changes
		}

		private void setUnsavedChangesFlag()
		{
			if (mesh != null)
			{
				mesh.UnsavedChanges = true;
			}
		}

		#endregion

	}
}

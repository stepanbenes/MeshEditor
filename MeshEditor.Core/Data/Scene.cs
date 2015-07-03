using System;
using System.Collections.Generic;
using System.Drawing;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Graphics;
using MeshEditor.UndoRedo;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Wintellect.PowerCollections;
using Utils = MeshEditor.Utilities.Functions;
using MeshEditor.Construction;
using System.Diagnostics;


namespace MeshEditor.Data
{
	/// <summary>
	/// scena reprezentuje pohled na sit konecnych prvku. obsahuje odkaz na objekt typu Mesh.
	/// jedna sit muze byt sdilena z vice objektu typu Scene.
	/// </summary>
	public class Scene : IDisposable
	{

		#region Instance fields & constructor

		private Camera camera;
		private Mesh mesh;

		private RenderMode renderMode;

		private List<Node> cutPlaneDefinitionNodes;
		private List<CutPlane> cutPlanes;

		private bool drawAxesFlag;
		private bool drawNodeNumbersFlag;
		private bool drawElementNumbersFlag;
		private bool drawBeamsFlag;

		private int? nodeSignal;
		private int? elementSignal;

		private Vector3 nodeSignalPosition, elementSignalPosition;

		public Scene()
		{
			this.mesh = null;
			this.camera = new Camera();
			this.renderMode = DefaultRenderMode;
			this.drawAxesFlag = true;
			this.drawNodeNumbersFlag = true;
			this.drawElementNumbersFlag = false;
			this.drawBeamsFlag = true;

			this.cutPlaneDefinitionNodes = new List<Node>();
			this.cutPlanes = new List<CutPlane>();

			this.nodeSignal = this.elementSignal = null;
		}

		#endregion

		#region Static fields & constructor

		public static readonly double FOVY_PARAM;
		public static readonly double Z_NEAR_PARAM;
		public static readonly double Z_FAR_PARAM;

		public static float AxisLength;
		public static float PointSize;
		public static float OrdinaryEdgeWidth;
		public static float BorderEdgeWidth;
		public static float BeamWidth;
		public static float DefaultCameraDistance;
		public static bool AlwaysShowNumbers;

		public static Color NonActiveBackColor;
		public static Color ActiveBackColor;

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
		public static bool EdgeLighting;
		public static bool IncludeEdgeMiddleNodes;
		public static int UndoOperationsMaxCount;
		public static bool SelectFacesOnCut;
		public static float DefaultFirstBorderAngleLimit;
		public static float DefaultSecondBorderAngleLimit;

		public static RenderMode DefaultRenderMode;
		public static string DefaultFileformatExtension, PropertyDescriptionFileExtension;

		public static bool XRayVision;
		private static ShadingModel meshShadingModel;
		private static bool EnableSkipSelectionModeIfNothingNewSelected;

		//public static readonly float DEPTH_TEST_TOLERANCE_DISTANCE;
		public static readonly float WHEEL_ZOOM_FACTOR;
		public static readonly float MAX_ZOOM_DISTANCE;
		public static readonly float LINEAR_ZOOM_DISTANCE;
		public static readonly float CLOSEST_ZOOM_MULTIPLE_OF_NEAR_PARAM;
		public static readonly float RADIUS_OF_NORMALIZED_MESH;
		public static readonly float MAX_VISIBLE_NUMBERS_DENSITY; // pixel^2
		public static readonly int NODE_SELECTION_TOLERANCE_DISTANCE; // pixels
		public static readonly int EDGE_SELECTION_TOLERANCE_DISTANCE; // pixels
		public static readonly float LIMIT_ANGLE_FOR_POINT_INSIDE_FACE_DECISION; // degrees

		

		static Scene()
		{
			FOVY_PARAM = 55.0;
			Z_NEAR_PARAM = 0.001; // 0.005
			Z_FAR_PARAM = 50.0; // 50

			RADIUS_OF_NORMALIZED_MESH = 1.0f;
			MAX_VISIBLE_NUMBERS_DENSITY = 200f; // pixel^2

			WHEEL_ZOOM_FACTOR = 0.1f; // (0,1)
			MAX_ZOOM_DISTANCE = 0.2f;
			LINEAR_ZOOM_DISTANCE = 0.005f;
			CLOSEST_ZOOM_MULTIPLE_OF_NEAR_PARAM = 2.0f;
			NODE_SELECTION_TOLERANCE_DISTANCE = 20;
			EDGE_SELECTION_TOLERANCE_DISTANCE = 20;
			LIMIT_ANGLE_FOR_POINT_INSIDE_FACE_DECISION = 10f; /**/

			// ---------------------------------------------------
			AlwaysShowNumbers = false;
			// ===================================================

			SetDefaultParametres(false);

			// ------------------------------------------
		}

		public static void SetDefaultParametres(bool openGLIsInitialized)
		{
			ActiveBackColor = Color.FromArgb(225, 219, 194);
			NonActiveBackColor = Color.FromArgb(186, 186, 200);

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
			EdgeLighting = false;
			AxisLength = 50f;
			PointSize = 8f;
			OrdinaryEdgeWidth = 1f;
			BorderEdgeWidth = 2f;
			BeamWidth = 2f;
			DefaultCameraDistance = 2.5f;
			XRayVision = false;
			EnableSkipSelectionModeIfNothingNewSelected = false; /**/
			//DEPTH_TEST_TOLERANCE_DISTANCE = 0.005f; // musi byt kladne; na tohle cislo radsi nesahej, na jeho vyladeni bylo potreba plno krve, potu a slz
			DefaultRenderMode = RenderMode.FacesLines;

			DefaultFileformatExtension = ".top";
			PropertyDescriptionFileExtension = ".prop";
			UndoOperationsMaxCount = 20;
			SelectFacesOnCut = true;

			if (openGLIsInitialized)
				MeshShadingModel = ShadingModel.Smooth;
			else
				meshShadingModel = ShadingModel.Smooth;
		}

		public static ShadingModel MeshShadingModel
		{
			get { return meshShadingModel; }
			set
			{
				meshShadingModel = value;
				try
				{
					GL.ShadeModel(meshShadingModel);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Can't change shading model (" + ex.Message + ")");
				}
			}
		}

		#endregion

		#region Properties, access

		public Mesh Mesh
		{
			get { return mesh; }
		}

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

		public bool DrawBeams
		{
			get { return drawBeamsFlag; }
			set { drawBeamsFlag = value; }
		}

		public RenderMode RenderMode
		{
			get { return renderMode; }
			set	{ renderMode = value; }
		}

		public void SetMesh(Mesh newMesh)
		{
			if (newMesh != null && newMesh != this.mesh)
			{
				newMesh.ReferenceCount++;
				if (this.mesh != null)
				{
					this.mesh.ReferenceCount--;
					if (this.mesh.ReferenceCount <= 0)
						this.mesh.Dispose();
				}
			}
			this.mesh = newMesh;
		}

		public List<CutPlane> CutPlanes
		{
			get { return cutPlanes; }
		}

		public List<Node> CurPlaneDefinitionNodes
		{
			get { return cutPlaneDefinitionNodes; }
		}

		public int? NodeSignal
		{
			get { return nodeSignal; }
			set
			{
				setNodeSignal(value);
			}
		}

		public int? ElementSignal
		{
			get { return elementSignal; }
			set
			{
				setElementSignal(value);
			}
		}

		public Vector3 NodeSignalPosition
		{
			get { return nodeSignalPosition; }
		}

		public Vector3 ElementSignalPosition
		{
			get { return elementSignalPosition; }
		}

		#endregion

		#region Misc public methods

		public void SetPropertyOfSelectedItems(Property property)
		{
			if (mesh != null)
			{
				saveStateBeforeSettingProperty(property);
				
				EntityType targetEntity = EntityType.Vertex;

				foreach (ISelectable item in mesh.SelectedItems)
				{
					item.Property = property;
					targetEntity = getPropertyTypeFromSelectableItem(item);
				}

				mesh.Statistics.AddProperty(property, targetEntity);
			}
		}

		private static EntityType getPropertyTypeFromSelectableItem(ISelectable item)
		{
			EntityType type;
			if (item is Node)
				type = EntityType.Vertex;
			else if (item is WingedEdge)
				type = EntityType.Edge;
			else if (item is IFaceOfElement3D)
				type = EntityType.Surface;
			else // Element
				type = EntityType.Region;
			return type;

			//EntityType type = EntityType.Vertex;
			//if (item is Node)
			//    type = EntityType.Vertex;
			//else if (item is WingedEdge)
			//    type = EntityType.Edge;
			//else if (item is IFaceOfElement3D)
			//    type = EntityType.Surface;
			//else // ELement
			//    type = EntityType.Region; /**/
			//return type;
		}

		public void AddPropertyToSelectedNodes(Property property)
		{
			if (mesh == null)
				return;
			// ulozit do historie
			saveStateBeforeAddingPropertyToSelectedNodes(property);
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
			// ulozit do historie
			saveStateBeforeRemovingPropertyFromSelectedNodes(property);
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

		public Scene Copy()
		{
			Scene copy = new Scene();
			copy.camera = this.camera.Clone();	// naklonuju kameru
			
			copy.mesh = this.mesh;				// zkopiruju jen odkaz na mesh
			if (this.mesh != null)
				this.mesh.ReferenceCount++;

			//if (this.mesh == null)
				copy.renderMode = DefaultRenderMode;
			//else
			//	copy.renderMode = this.renderMode;
			copy.drawAxesFlag = this.drawAxesFlag;
			// cut planes kopirovat nebudu
			return copy;
		}

		public void RecreateBuffers()
		{
			if (mesh != null)
				mesh.RecreateBuffers();
		}

		public static void ExtractMatrices(out int[] viewport, out double[] modelview, out double[] projection)
		{
			viewport = new int[4];
			modelview = new double[16];	// mptm modelovací matice
			projection = new double[16];	// ptm projekční matice

			GL.GetInteger(GetPName.Viewport, viewport);
			GL.GetDouble(GetPName.ModelviewMatrix, modelview);
			GL.GetDouble(GetPName.ProjectionMatrix, projection);
		}

		public static void ExtractViewport(out int[] viewport)
		{
			viewport = new int[4];
			GL.GetInteger(GetPName.Viewport, viewport);
		}

		public static void ExtractMatrices(int[] viewport, double[] modelview, double[] projection)
		{
			GL.GetInteger(GetPName.Viewport, viewport);
			GL.GetDouble(GetPName.ModelviewMatrix, modelview);
			GL.GetDouble(GetPName.ProjectionMatrix, projection);
		}

		public static Vector3 ProjectWorldCoordToWindowCoords(Vector3 point)
		{
			int[] viewport = new int[4];
			double[] modelview = new double[16];	// mptm modelovací matice
			double[] projection = new double[16];	// ptm projekční matice

			GL.GetInteger(GetPName.Viewport, viewport);
			GL.GetDouble(GetPName.ModelviewMatrix, modelview);
			GL.GetDouble(GetPName.ProjectionMatrix, projection);

			Vector3 result;
			Utils.GluProject(point, modelview, projection, viewport, out result);
			return result;
		}

		public static Vector3 UnprojectWindowCoordsToWorldCoords(int x, int y, float pixelDepth)
		{
			int[] viewport;
			double[] modelview;
			double[] projection;
			ExtractMatrices(out viewport, out modelview, out projection);
			
			Vector3 windowPos = new Vector3(x - viewport[0], viewport[3] - y - viewport[1], pixelDepth);

			Vector3 position;
			Utils.GluUnProject(windowPos, modelview, projection, viewport, out position);
			return position;
		}

		public Vector3 UnprojectWindowCoordsToWorldCoords(bool redraw, int x, int y, out float pixelDepth)
		{
			int[] viewport;
			double[] modelview;
			double[] projection;
			ExtractMatrices(out viewport, out modelview, out projection);

			if (redraw)
			{
				GL.Clear(ClearBufferMask.DepthBufferBit);
				// vykreslit plochy site jen se nekresli pouze beamy nebo uzly
				if (mesh != null /* && renderMode != RenderMode.None && renderMode != RenderMode.Points*/)
				{
					mesh.DrawFacesOnly();

					//mesh.DrawContent(this.renderMode & ~RenderMode.Points, this.camera, /*optimizeForMoving*/ false, /*optimizeForSelecting*/ false, /*drawNodeNumbersFlag*/ false, /*drawElementNumbersFlag*/ false, /*this.drawBeamsFlag*/ true);

					//GL.Enable(EnableCap.CullFace);
					//GL.CullFace(CullFaceMode.Front);
					////mesh.DrawContent(this.renderMode & ~RenderMode.Points, this.camera, /*optimizeForMoving*/ false, /*optimizeForSelecting*/ false, /*drawNodeNumbersFlag*/ false, /*drawElementNumbersFlag*/ false, /*this.drawBeamsFlag*/ true);
					//mesh.DrawFacesOnly();
					//GL.CullFace(CullFaceMode.Back);
					//GL.Disable(EnableCap.CullFace);
				}
			}

			pixelDepth = getPixelDepth(x, y, viewport);
			Vector3 windowPos = new Vector3(x - viewport[0], viewport[3] - y - viewport[1], pixelDepth);
			
			Vector3 position;
			Utils.GluUnProject(windowPos, modelview, projection, viewport, out position);

			return position;
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

		#region Signal

		private Node tempNodeAddedToSurfaceRepresentation;

		private void setNodeSignal(int? nodeSignalToSet)
		{
			try
			{
				if (mesh == null)
					return;

				// clear old signal
				if (tempNodeAddedToSurfaceRepresentation != null) // remove new free node if was added
				{
					mesh.NodesEdgesIncidence.Remove(tempNodeAddedToSurfaceRepresentation);
					tempNodeAddedToSurfaceRepresentation = null;
					mesh.CreateBuffers();
				}

				if (nodeSignalToSet == null)
				{
					//selectItemsInSet(new Set<ISelectable>()); // clear selection					
					return;
				}

				int nodeID = nodeSignalToSet.Value;

				bool found = false;
				foreach (Element element in mesh.Elements)
				{
					foreach (Node node in element.IterateThroughAllNodesIncludingEdgeMiddleNodes())
					{
						if (node.ID == nodeID)
						{
							nodeSignalPosition = node.Position;							
							// ------------------------------------------------
							if (!mesh.NodesEdgesIncidence.ContainsKey(node))
							{
								tempNodeAddedToSurfaceRepresentation = node;
								mesh.NodesEdgesIncidence[node] = null; // add free node
								mesh.CreateBuffers();
							}
							// ------------------------------------------------
							Set<ISelectable> toSelect = new Set<ISelectable>();
							toSelect.Add(node);
							selectItemsInSet(toSelect); // select signalled node
							// ------------------------------------------------
							found = true;
							break;
						}
					}
					if (found)
						break;
				}
				if (!found)
				{
					//nodeSignal = null;
					Exception nullException = null;
					throw new ArgumentOutOfRangeException("Node with ID " + nodeID + " does not exist!", nullException);
				}
			}
			finally
			{
				this.nodeSignal = nodeSignalToSet; // finally save new value to nodeSignal
			}
		}

		private Element3D tempElementAddedToSurfaceRepresentation;
		private MeshConstructor signalElementConstructor;

		private void setElementSignal(int? elementSignalToSet)
		{
			try
			{
				if (mesh == null)
					return;

				if (tempElementAddedToSurfaceRepresentation != null)
				{
					Debug.Assert(signalElementConstructor != null);
					Set<Element> elementsToRestore = new Set<Element>();
					elementsToRestore.Add(tempElementAddedToSurfaceRepresentation);

					signalElementConstructor.CutMesh(mesh, new Set<Element>(), elementsToRestore, new Set<Node>(), false); // remove signalled element from surface representation

					tempElementAddedToSurfaceRepresentation = null;
					signalElementConstructor = null;
				}

				if (elementSignalToSet == null)
				{
					//selectItemsInSet(new Set<ISelectable>()); // clear selection					
					return;
				}

				int elementID = elementSignalToSet.Value;

				bool found = false;
				foreach (Element element in mesh.Elements)
				{ 
					if (element.ID == elementID)
					{
						elementSignalPosition = element.GetCenter();
						// ------------------------------------------------
						Element3D element3D = element as Element3D;
						if (element3D != null)
						{
							signalElementConstructor = new MeshConstructor();
							tempElementAddedToSurfaceRepresentation = element3D;
							signalElementConstructor.AddSurfaceOfElement3DToMesh(mesh, element3D); // add element to surface representation
						}
						// ------------------------------------------------
						Set<ISelectable> toSelect = new Set<ISelectable>();
						toSelect.Add(element);
						selectItemsInSet(toSelect); // select signalled element
						// ------------------------------------------------
						found = true;
						break;
					}
				}
				if (!found)
				{
					//elementSignal = null;
					Exception nullException = null;
					throw new ArgumentOutOfRangeException("Element with ID " + elementID + " does not exist!", nullException);
				}
			}
			finally
			{
				this.elementSignal = elementSignalToSet; // finally save new value to elementSignal
			}
		}

		private void selectItemsInSet(Set<ISelectable> itemsToSelect)
		{
			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------			
			Set<ISelectable> oldSelection = mesh.SelectedItems;
			mesh.SelectedItems = itemsToSelect;
			updateColorBuffers(oldSelection, itemsToSelect);
		}

		#endregion

		#region Drawing

		public void Draw(bool optimizeForMoving, bool optimizeForSelecting)
		{
			if (mesh != null)
			{
				mesh.DrawContent(this.renderMode, this.camera, optimizeForMoving, optimizeForSelecting, drawNodeNumbersFlag, drawElementNumbersFlag, this.drawBeamsFlag);

				// if cutPlanes.Count != 0 then draw cut planes
				//foreach (CutPlane plane in cutPlanes)
				//	mesh.DrawCutPlane(plane, renderMode);
			}

			// vykresli osy
			if (drawAxesFlag)
				drawAxes();

			if (this.cutPlaneDefinitionNodes.Count > 0)
				drawPlaneDefinitionPoints();

			// draw cut planes
			if (this.cutPlanes.Count > 0)
				drawCutPlanes();
		}

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

		private void drawAxes()
		{
			if (LineSmooth)
			{
				GL.Enable(EnableCap.LineSmooth);
				GL.Enable(EnableCap.Blend);
			}

			GL.LineWidth(1.0f);
			GL.Disable(EnableCap.Lighting);

			if (mesh != null)
			{
				GL.PushMatrix();
				GL.Translate(-mesh.PositionOffset * mesh.ResizeFactor);
			}

			// kladne osy
			GL.Begin(BeginMode.Lines);
			GL.Color3(1.0, 0, 0);		// X
			GL.Vertex3(0, 0, 0);
			GL.Vertex3(AxisLength, 0, 0);
			GL.Color3(0, 0, 1.0);		// Y
			GL.Vertex3(0, 0, 0);
			GL.Vertex3(0, AxisLength, 0);
			GL.Color3(0, 1.0, 0);		// Z
			GL.Vertex3(0, 0, 0);
			GL.Vertex3(0, 0, AxisLength);
			GL.End();

			//GL.LineWidth(0.4f);

			GL.Enable(EnableCap.LineStipple);
			GL.LineStipple(2, 52428);
			// zaporne osy
			GL.Begin(BeginMode.Lines);
			GL.Color3(1.0, 0, 0);		// X
			GL.Vertex3(0, 0, 0);
			GL.Vertex3(-AxisLength, 0, 0);
			GL.Color3(0, 0, 1.0);		// Y
			GL.Vertex3(0, 0, 0);
			GL.Vertex3(0, -AxisLength, 0);
			GL.Color3(0, 1.0, 0);		// Z
			GL.Vertex3(0, 0, 0);
			GL.Vertex3(0, 0, -AxisLength);
			GL.End();

			if (mesh != null)
			{
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

		#endregion

		#region Selecting

		public string GetSelectedItemsDescription()
		{
			if (mesh == null || mesh.SelectedItems.Count == 0) // nic neni vybrano
				return string.Empty;
			else if (mesh.SelectedItems.Count > 1) // je vybrana vic jak jedna polozka
				return mesh.SelectedItems.Count.ToString() + " items selected"; // vypisu jen kolik je vybrano polozek
			// jinak zobrazim popis jedne vybrane polozky
			ISelectable item = getFirstSelectedItem();

			Node node = item as Node;
			if (node != null)
				return node.ToStringWithOriginalCoordinates(mesh.ResizeFactor, mesh.PositionOffset);
			else
				return item.ToString();
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

			Set<ISelectable> newSelection;

			if (area.Size == Size.Empty)
				newSelection = getPointSelection(area.X, area.Y, mode, itemType); /**/
			else if (area.Width == 0 || area.Height == 0) // neni to ramecek, ma sirku nebo dylku 0, takze nic nedelam
				return;
			else
				newSelection = getItemsInArea(area, itemType, allVerticesInArea);


			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------


			Set<ISelectable> selectedItems = mesh.SelectedItems;

			switch (opType)
			{
				case SelectOperationType.New:
					break;
				case SelectOperationType.Union:
					newSelection = selectedItems.Union(newSelection);
					break;
				case SelectOperationType.Intersection:
					newSelection = selectedItems.Intersection(newSelection);
					break;
				case SelectOperationType.Except:
					newSelection = selectedItems.Difference(newSelection);
					break;
				case SelectOperationType.SymetricDifference:
					newSelection = selectedItems.SymmetricDifference(newSelection);
					break;
				default:
					throw new NotSupportedException("This select operation type is not supported.");
			}

			Set<ISelectable> oldSelection = mesh.SelectedItems;
			mesh.SelectedItems = newSelection;
			updateColorBuffers(oldSelection, newSelection);
			
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PopMatrix();
		}

		private Set<ISelectable> getPointSelection(int x, int y, SelectMode mode, ItemTypeToSelect itemType)
		{
			Set<ISelectable> newSelection;
			// Select single face first
			Element2D faceHit;
			ItemTypeToSelect typeToSelect = (mode > SelectMode.Single && itemType == ItemTypeToSelect.Node) ? ItemTypeToSelect.Edge : itemType;
			ISelectable item = getSingleEntityOnLocation(x, y, typeToSelect, out faceHit);
			if (item == null && faceHit == null)
				return new Set<ISelectable>();

            if (itemType == ItemTypeToSelect.Beam)
            {
                newSelection = new Set<ISelectable>();
				if (item != null)
					newSelection.Add(item);
                return newSelection;
            }

			switch (mode)
			{
				case SelectMode.None:
					newSelection = new Set<ISelectable>();
					break;
				case SelectMode.Single:
					newSelection = new Set<ISelectable>();
					if (item != null)
						newSelection.Add(item);
					break;
				case SelectMode.NearSurface:
				case SelectMode.ExtendedSurface:
				case SelectMode.Object:
					do
					{
						newSelection = advancedPointSelection(mode, itemType, faceHit, item);
					} while (EnableSkipSelectionModeIfNothingNewSelected && (itemType == ItemTypeToSelect.Face || itemType == ItemTypeToSelect.Element) && newSelection.Count == 1 && ++mode < SelectMode.Object);
					break;
				default:
					throw new NotSupportedException("This select mode is not supported.");
			}

			return newSelection;
		}

		private Set<ISelectable> advancedPointSelection(SelectMode mode, ItemTypeToSelect itemType, Element2D faceHit, ISelectable item)
		{
			WingedEdge edgeHit = item as WingedEdge;
			Set<ISelectable> selection = new Set<ISelectable>();

			float angleLimit = this.mesh.SoftBorderLimit;
			if (mode == SelectMode.ExtendedSurface)
				angleLimit = this.mesh.HardBorderLimit;
			else if (mode == SelectMode.Object)
				angleLimit = float.MaxValue;

			if (itemType == ItemTypeToSelect.Face || itemType == ItemTypeToSelect.Element || edgeHit == null || edgeHit.FeatureAngle < this.mesh.HardBorderLimit)
			{
				if (faceHit != null)
					selection = transformSelectedFacesInto(itemType, selectWholeSurface(faceHit, angleLimit));
			}
			else if (itemType == ItemTypeToSelect.Node || itemType == ItemTypeToSelect.Edge)
			{
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
			}
			return selection;
		}

		/// <summary>
		/// delegat odkazujici na funkci, ktera vezme hranu a vrati mnozinu s ni sousedicich hran
		/// </summary>
		private delegate IEnumerable<WingedEdge> NeighborSelection(WingedEdge edge);

		private Set<WingedEdge> getHardBorderEdges(WingedEdge edgeHit, Element2D faceHit, SelectMode mode)
		{
			NeighborSelection getNeighborsFun;
			switch (mode)
			{
				case SelectMode.NearSurface:
					getNeighborsFun = delegate(WingedEdge e)
					{
						Vector3 unit = Vector3.Normalize(e.EndNode.Position - e.BeginNode.Position);
						Set<WingedEdge> neighbors = new Set<WingedEdge>();
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
						if(count == 1 && neighbor != null)
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
					Set<Element2D> surface = selectWholeSurface(faceHit, this.mesh.HardBorderLimit);
					getNeighborsFun = delegate(WingedEdge e)
					{
						Vector3 unit = Vector3.Normalize(e.EndNode.Position - e.BeginNode.Position);
						Set<WingedEdge> neighbors = new Set<WingedEdge>();
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
					getNeighborsFun = delegate(WingedEdge e)
					{
						Vector3 unit = Vector3.Normalize(e.EndNode.Position - e.BeginNode.Position);
						Set<WingedEdge> neighbors = new Set<WingedEdge>();
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
					throw new ArgumentOutOfRangeException("mode");
			}
			// ------------------------------------
			return getBorderEdges(edgeHit, getNeighborsFun);
		}

		private static Set<WingedEdge> getBorderEdges(WingedEdge startEdge, NeighborSelection getNeighborsFun)
		{
			Set<WingedEdge> selection = new Set<WingedEdge>();
			Stack<WingedEdge> expansion = new Stack<WingedEdge>();
			expansion.Push(startEdge);
			selection.Add(startEdge);
			while (expansion.Count > 0)
			{
				WingedEdge top = expansion.Pop();
				foreach(WingedEdge neighbor in getNeighborsFun(top))
					if (!selection.Add(neighbor))
						expansion.Push(neighbor);
			}
			return selection;
		}

		private static Set<ISelectable> transformSelectedFacesInto(ItemTypeToSelect itemType, Set<Element2D> faces)
		{
			Set<ISelectable> items = new Set<ISelectable>();
			switch (itemType)
			{
				case ItemTypeToSelect.Face:
					foreach (Element2D face in faces)
						items.Add(face);
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
					}
					break;
				default:
					throw new NotSupportedException("This type of item is not supported to select.");
			}
			return items;
		}

		private ISelectable getSingleEntityOnLocation(int x, int y, ItemTypeToSelect itemType, out Element2D faceHit)
		{
			bool computeFaceHit = (itemType != ItemTypeToSelect.Node && itemType != ItemTypeToSelect.Beam);
			faceHit = null;
			Rectangle area;
			int[] viewport = new int[4];
			GL.GetInteger(GetPName.Viewport, viewport);

			Dictionary<Node, Vector3> visibleNodesProjected = null;
			if (itemType == ItemTypeToSelect.Node)
			{
				area = new Rectangle(x - NODE_SELECTION_TOLERANCE_DISTANCE, y - NODE_SELECTION_TOLERANCE_DISTANCE, NODE_SELECTION_TOLERANCE_DISTANCE << 1, NODE_SELECTION_TOLERANCE_DISTANCE << 1);
				visibleNodesProjected = mesh.FindVisibleNodesProjectedPositions(area, this.camera);
			}
			else if (itemType != ItemTypeToSelect.Beam)
			{
				GL.Clear(ClearBufferMask.DepthBufferBit); // tohle tu jen kvuli zjisteni ty hloubky pomoci getPixelDepth nize
				mesh.DrawFacesOnly();
			}

			if (itemType != ItemTypeToSelect.Node || computeFaceHit)
			{
				float pixelDepth = getPixelDepth(x, y, viewport);
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

		public void UnselectAllItems()
		{
			if (mesh == null)
				return;

			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------

			unselectAllItems();
		}

		private void unselectAllItems()
		{
			Set<ISelectable> oldSelection = mesh.SelectedItems;
			Set<ISelectable> newSelection = new Set<ISelectable>();
			mesh.SelectedItems = newSelection;
			updateColorBuffers(oldSelection, newSelection);
		}

		public void SelectAllItems(EditorMode editorMode)
		{
			if (mesh == null)
				return;

			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------

			ItemTypeToSelect itemType = editorModeToItemType(editorMode);

			Set<ISelectable> temp = mesh.SelectedItems;
			mesh.SelectedItems = new Set<ISelectable>();

			switch (itemType)
			{
				case ItemTypeToSelect.Element:
					foreach (Element e in mesh.Elements)
						if (!mesh.HiddenElements.Contains(e))
							mesh.SelectedItems.Add(e);
					break;
				case ItemTypeToSelect.Node:
					foreach (Element e in mesh.Elements)
					{
						if (!mesh.HiddenElements.Contains(e))
						{
							foreach (Node n in e.IterateThroughAllNodes())
								mesh.SelectedItems.Add(n);
						}
					}
					break;
				case ItemTypeToSelect.Face:
					foreach (Element2D face in mesh.Faces)
						mesh.SelectedItems.Add(face);
					break;
				case ItemTypeToSelect.Edge:
					foreach (WingedEdge edge in mesh.Edges)
						mesh.SelectedItems.Add(edge);
					break;
				case ItemTypeToSelect.Beam:
					foreach (Beam beam in mesh.Beams)
						mesh.SelectedItems.Add(beam);
					break;
			}

			updateColorBuffers(temp, mesh.SelectedItems);
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

		public void InvertSelection()
		{
			if (mesh == null)
				return;

			ItemTypeToSelect itemType;
			ISelectable firstItem = getFirstSelectedItem();
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
			Set<ISelectable> oldSelection = mesh.SelectedItems;
			mesh.SelectedItems = new Set<ISelectable>();

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

			Set<ISelectable> oldSelection = mesh.SelectedItems;
			Set<ISelectable> newSelection = new Set<ISelectable>();
			
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

		private ISelectable getFirstSelectedItem()
		{
			IEnumerator<ISelectable> enumerator = mesh.SelectedItems.GetEnumerator();
			enumerator.MoveNext();
			return enumerator.Current;
		}

		private static float getPixelDepth(int x, int y, int[] viewport)
		{
			float[] depth = new float[1];
			GL.ReadPixels(x - viewport[0], viewport[3] - y - viewport[1], 1, 1, PixelFormat.DepthComponent, PixelType.Float, depth);
			return depth[0];
		}

		private void updateColorBuffers(Set<ISelectable> oldSelection, Set<ISelectable> newSelection)
		{
			int changedFacesCount = 0;
			int chengedEdgesCount = 0;
			int changedNodesCount = 0;
			int changedBeamsCount = 0;

			foreach (ISelectable item in oldSelection)
			{
				if (item is Element2D || item is Element3D)
					changedFacesCount++;
				else if (item is WingedEdge)
					chengedEdgesCount++;
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
					chengedEdgesCount++;
				else if (item is Node)
					changedNodesCount++;
				else if (item is Beam)
					changedBeamsCount++;
			}

			if (changedFacesCount > 0)
				mesh.UpdateFaceColors();
			if (chengedEdgesCount > 0)
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
			foreach(Element2D face in allFaces)
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

		private Set<ISelectable> getItemsInArea(Rectangle selectionArea, ItemTypeToSelect itemType, bool allVerticesInArea)
		{
			// --------------------------------------
			bool xRay = ((itemType == ItemTypeToSelect.Beam || itemType == ItemTypeToSelect.Node) && (renderMode & RenderMode.Faces) == 0) ? true : Scene.XRayVision;
			Set<Node> visibleNodes = mesh.FindVisibleNodes(selectionArea, this.camera, xRay, false);
			// --------------------------------------
			Set<ISelectable> result = new Set<ISelectable>();
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
			Set<Element2D> pickedFaces = new Set<Element2D>();

			// najdi pickedfaces
			foreach(Node node in visibleNodes)
			{
				if (Scene.XRayVision)
				{
					pickedFaces.AddMany(mesh.GetFacesIncidingWithNode(node)); // pridat vsechny
					continue;
				}
				Set<Element2D> frontFaces = new Set<Element2D>();
				Set<Element2D> backFaces = new Set<Element2D>();
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
					pickedFaces.AddMany(backFaces);
				else if (backOne == null)
					pickedFaces.AddMany(frontFaces);
				else
				{
					if (frontFaceIsCloserToEye(frontOne, backOne))
						pickedFaces.AddMany(frontFaces);
					else
						pickedFaces.AddMany(backFaces);
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
								result.Add(edge);
						}
					}
					break;
				case ItemTypeToSelect.Face:
					foreach (Element2D face in pickedFaces)
					{
						if (!allVerticesInArea)
							result.Add(face);
						else
						{
							int number = getNumberOfVisibleNodesFrom(face.IterateThroughAllNodes(), visibleNodes);
							if (number == face.NodeCount)
								result.Add(face);
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
							result.Add(itemToAdd);
						else
						{
							int number = getNumberOfVisibleNodesFrom(face.IterateThroughAllNodes(), visibleNodes);
							if (number == face.NodeCount)
								result.Add(itemToAdd);
						}
					}
					break;
				default:
					throw new NotSupportedException("This type of item is not supported for selection.");
			}
			return result;
		}

		private bool frontFaceIsCloserToEye(Element2D frontFace, Element2D backFace)
		{
			foreach (Node n in backFace.IterateThroughAllNodes())
			{
				if (!frontFace.ContainsNode(n))
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

		private int getNumberOfVisibleNodesFrom(IEnumerable<Node> someNodes, Set<Node> visibleNodes)
		{
			int number = 0;
			foreach (Node n in someNodes)
				if (visibleNodes.Contains(n))
					number++;
			return number;
		}

		private static Set<Element2D> selectWholeSurface(Element2D startFace, float borderAngleLimit)
		{
			Set<Element2D> selectionSet = new Set<Element2D>();
			Stack<Element2D> faces = new Stack<Element2D>();
			faces.Push(startFace);
			selectionSet.Add(startFace);
			while (faces.Count > 0)
			{
				Element2D face = faces.Pop();
				foreach (Element2D neighbor in face.GetNeighbors(borderAngleLimit))
					if (!selectionSet.Add(neighbor))
						faces.Push(neighbor);
			}
			return selectionSet;
		}

		public void SelectItemsWithProperty(EditorMode editorMode, Property property)
		{
			if (mesh == null)
				return;

			// ulozit pred oznacenim do historie --------------------------------------
			saveStateBeforeSelect();
			// ------------------------------------------------------------------------

			ItemTypeToSelect itemType = editorModeToItemType(editorMode);

			Set<ISelectable> oldSelection = mesh.SelectedItems;
			Set<ISelectable> newSelection = new Set<ISelectable>();
			mesh.SelectedItems = newSelection;
			// --------------------------------------------
			switch (itemType)
			{
				case ItemTypeToSelect.Element:
					foreach (Element e in mesh.Elements) // neuriznuty prvky
					{
						if (!mesh.HiddenElements.Contains(e) && !(e is Beam))
						{
							if (e.Property == property)
								newSelection.Add(e);
						}
					}
					break;
				case ItemTypeToSelect.Node:
					foreach (Element e in mesh.Elements) // uzly vsech neuriznutych prvku
					{
						if (!mesh.HiddenElements.Contains(e))
						{
							foreach (Node n in e.IterateThroughAllNodesIncludingEdgeMiddleNodes()) // uzly
								if (n.ContainsProperty(property))
									newSelection.Add(n);
						}
					}
					break;
				case ItemTypeToSelect.Face:
					foreach (Element2D face in mesh.Faces) // plochy na povrchu
						if (face.Property == property)
							newSelection.Add(face);
					break;
				case ItemTypeToSelect.Edge:
					foreach (WingedEdge edge in mesh.Edges) // hrany na povrchu
						if (edge.Property == property)
							newSelection.Add(edge);
					break;
				case ItemTypeToSelect.Beam:
					foreach (Beam b in mesh.Beams)
						if (b.Property == property)
							newSelection.Add(b);
					break;				
			}
			// --------------------------------------------
			updateColorBuffers(oldSelection, newSelection);
		}

		#endregion

		#region Cutting

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

		public void Cut(CutInfo cutInfo)
		{
			if(mesh == null)
				return;

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
					// ------------------------------------------------------------------
					saveStateBeforeHideRestoreElements(Scene.SelectFacesOnCut);
					// ------------------------------------------------------------------
					Cutter.CutMeshByExpression(mesh, cutInfo);
					if (Scene.SelectFacesOnCut)
						mesh.UpdateFaceColors();
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
					// ------------------------------------------------------------------
					saveStateBeforeHideRestoreElements(Scene.SelectFacesOnCut);
					// ------------------------------------------------------------------
					Cutter.CutMeshByPlanes(mesh, cutPlanes, cutInfo);
					if (Scene.SelectFacesOnCut)
						mesh.UpdateFaceColors();
				}
				else if (cutInfo.Action == CutInfo.ActionType.ShowHideElements) // show/hide elements
				{
					// ------------------------------------------------------------------
					saveStateBeforeHideRestoreElements(false);
					// ------------------------------------------------------------------

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

		public void InvertAllNormalsOfMesh()
		{
			if (mesh != null)
			{
				saveStateBeforeInvertNormals();
				mesh.InvertAllNormals();
			}
		}

		public void HideSelectedElements()
		{
			if (mesh == null)
				return;
			// ----------------------------------
			// remove element signal if exists
			setElementSignal(null);
			// ----------------------------------
			saveStateBeforeHideRestoreElements(false);
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
			saveStateBeforeHideRestoreElements(false);
			// ----------------------------------
			Set<Element> toRestore = new Set<Element>(mesh.HiddenElements);
			Cutter.RestoreElements(mesh, toRestore);
		}

		#endregion

		#region History

		private void saveStateBeforeInvertNormals()
		{
			if (mesh == null || mesh.History == null)
				return;
			mesh.History.Do(new InvertNormalsMemento(camera));
		}

		private void saveStateBeforeSelect()
		{
			if (mesh == null || mesh.History == null)
				return;
			SelectionMemento peek = mesh.History.PeekUndo() as SelectionMemento;
			//if (!mesh.History.CanUndo)
			//	mesh.History.Do(new SelectionMemento(new Set<ISelectable>()));
			if (peek != null && peek.SelectedItemsCount == 0 && mesh.SelectedItems.Count == 0)
				return;
			mesh.History.Do(new SelectionMemento(mesh.SelectedItems, camera));
		}

		private void saveStateBeforeSettingProperty(Property property)
		{
			if (mesh == null || mesh.History == null)
				return;
			mesh.History.Do(new SetPropertyMemento(property, mesh.SelectedItems, camera));
		}

		private void saveStateBeforeAddingPropertyToSelectedNodes(Property property)
		{
			if (mesh == null || mesh.History == null)
				return;
			mesh.History.Do(new AddPropertyToSelectedNodesMemento(property, camera));
		}

		private void saveStateBeforeRemovingPropertyFromSelectedNodes(Property property)
		{
			if (mesh == null || mesh.History == null)
				return;
			mesh.History.Do(new RemovePropertyFromSelectedNodesMemento(property, camera));
		}

		private void saveStateBeforeHideRestoreElements(bool selectFacesOnCut)
		{
			if (mesh == null || mesh.History == null)
				return;
			mesh.History.Clear(); /**/
			//mesh.History.BeginCompoundDo();
			//mesh.History.Do(new SelectionMemento(mesh.SelectedItems));
			mesh.History.Do(new HideRestoreElementsMemento(mesh.HiddenElements, selectFacesOnCut, selectFacesOnCut, camera));
			//mesh.History.EndCompoundDo();
		}

		#endregion
		
	}
}

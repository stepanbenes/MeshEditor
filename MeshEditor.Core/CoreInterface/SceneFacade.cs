using System;
using System.IO;
using System.Drawing;
using System.Timers;

using MeshEditor.Data;
using MeshEditor.IO;
using MeshEditor.Construction;
using MeshEditor.Graphics;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Utils = MeshEditor.Utilities.Functions;
using System.Collections.Generic;
using MeshEditor.Cuts;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// Interface between core and GUI
	/// </summary>
	public partial class SceneFacade
	{

		#region Static fields, static constructor

		public static readonly string AppSettingsFilename, UserGuideFileName, PropertyColorsConfigFileName;

		public static readonly int COLOR_BITS;
		public static readonly int DEPTH_BITS;

		public static readonly int CLICK_DISTANCE_TOLERANCE; // pixels
		public static readonly int CAMERA_CHANGED_NOTIFY_INTERVAL; // ms
		public static readonly int MAX_INTERVAL_BETWEEN_CLICKS; // ms
		
		public static Color SELECTION_RECTANGLE_COLOR;
		public static Color ZOOM_RECTANGLE_COLOR;
		public static Color SCREENSHOT_RECTANGLE_COLOR;

		//=========================================================

		private static EditorMode editorMode;
		private static EditorMode editorModeWithoutModificationKeys;

		static SceneFacade()
		{
			DEPTH_BITS = 24;
			COLOR_BITS = 32;

			CLICK_DISTANCE_TOLERANCE = 2;

			CAMERA_CHANGED_NOTIFY_INTERVAL = 1000; // ms
			MAX_INTERVAL_BETWEEN_CLICKS = 400; // ms
			
			SELECTION_RECTANGLE_COLOR = Color.FromArgb(50, Color.Blue);
			ZOOM_RECTANGLE_COLOR = Color.FromArgb(50, Color.Green);
			SCREENSHOT_RECTANGLE_COLOR = Color.FromArgb(50, Color.Yellow);

			editorMode = editorModeWithoutModificationKeys = EditorMode.Orbit;

			AppSettingsFilename = @"appSettings.conf";
			UserGuideFileName = @"userGuide.pdf";
			PropertyColorsConfigFileName = @"propertyColors.xml";
		}

		#endregion

		#region Public Events

		public static event EventHandler EditorModeChanged;
		public static event ShowErrorEventHandler ShowError;

		public event EventHandler MakeCurrentNeeded;
		public event EventHandler RefreshNeeded;
		public event EventHandler InvalidateNeeded;
		public event EventHandler MeshReloaded;
		public event EventHandler SwapBuffersNeeded;
		public event EventHandler<MeshNeedRefreshEventArgs> MeshNeedRefresh;
		public event EventHandler CutPlaneDefinitionPointsChanged;
		public event EventHandler ActionPerformed;
		public event EventHandler ColorModeChanged;
		public event EventHandler RenderModeChanged;
		public event EventHandler<ScreenshotNeededEventArgs> ScreenshotNeeded;

		#endregion

		#region Private Fields

		private IScene scene;

		private Point prevMouseLocation;
		private Point mouseDownLocation;
		private Point mouseUpLocation;

		private PointUnderCursorContext pointUnderCursorContext;

		private bool cameraChangedDirection;

		private bool needToComputeVisibleNodesFlag;
		private bool clickTimerFlag;
		private Timer cameraChangedTimer;

		private int mouseDownCount;
		private int clickCount;
		private DateTime lastClickTime;
		private Timer clickTimer;
		private SelectOperationType selectOperationType;

		private bool controlDown, shiftDown;

		private bool mouseDownFlag;

		private bool drawSelectionRectangleFlag;
		private Size clientWindowSize;

		// ----------------------------------------------------

		#endregion

		#region Private Constructors

		private SceneFacade(IScene scene)
		{
			Debug.Assert(scene != null);
			this.scene = scene;

			this.controlDown = this.shiftDown = false;
			this.clickCount = 0;
			this.mouseDownCount = 0;
			this.selectOperationType = SelectOperationType.New;

			// ------------------------------
			this.clickTimerFlag = false;
			this.clickTimer = new Timer();
			this.clickTimer.Interval = SceneFacade.MAX_INTERVAL_BETWEEN_CLICKS;
			this.clickTimer.Elapsed += delegate
			{
				clickTimer.Stop();
				clickTimerFlag = true;
				mouseDownCount = 0;
				if (InvalidateNeeded != null)
					InvalidateNeeded(null, null);
			};
			// ------------------------------
			this.cameraChangedTimer = new Timer();
			this.cameraChangedTimer.Interval = SceneFacade.CAMERA_CHANGED_NOTIFY_INTERVAL; /**/
			this.cameraChangedTimer.Elapsed += delegate
			{
				needToComputeVisibleNodesFlag = true;
				if (InvalidateNeeded != null)
					InvalidateNeeded(null, null);
			};
			// ------------------------------

			this.lastClickTime = DateTime.MinValue;
			this.mouseDownFlag = false;
			this.drawSelectionRectangleFlag = false;

			this.cameraChangedDirection = true;

			// ================================
			this.pointUnderCursorContext = new PointUnderCursorContext();
		}

		#endregion

		#region Public Properties

		public string MeshFilename
		{
			get { return (scene.Mesh == null) ? null : scene.Mesh.Filename; }
		}

		public bool ContainsMesh
		{
			get { return scene.Mesh != null; }
		}

		public bool ControlDown
		{
			get { return controlDown; }
			set
			{
				if (controlDown == value)
					return;
				controlDown = value;
				if (controlDown && !shiftDown)
					editorModeWithoutModificationKeys = EditorMode;
				setAppropriateEditorMode();
			}
		}

		public bool ShiftDown
		{
			get { return shiftDown; }
			set
			{
				if (shiftDown == value)
					return;
				shiftDown = value;
				if (shiftDown && !controlDown)
					editorModeWithoutModificationKeys = EditorMode;
				setAppropriateEditorMode();
			}
		}

		public int CutPlaneDefinitionPointsCount
		{
			get { return scene.CutPlaneDefinitionNodes.Count; }
		}

		public List<CutPlane> CutPlanes
		{
			get { return scene.CutPlanes; }
		}
		
		#endregion

		#region Public Static Properties

		public static EditorMode EditorMode
		{
			get { return SceneFacade.editorMode; }
			set
			{
				if (SceneFacade.editorMode != value)
				{
					SceneFacade.editorMode = value;
					var handler = EditorModeChanged;
					if (handler != null)
						handler(null, EventArgs.Empty);
				}
			}
		}

		public static string SolutionFileExtension => ".solution.json";

		public static string InputFileFormatFilter => string.Format("All supported files (*{0}, *.msh, *.vtu, *.obj, *.ply, *{1})|*{0};*.msh;*.vtu;*.obj;*.ply;*{1}|SIFEL file format (*{0})|*{0}|GiD mesh file format (*.msh)|*.msh|VTK XML unstructured grid (*.vtu)|*.vtu|OBJ file format (*.obj)|*.obj|PLY file format (*.ply)|*.ply|Solution file format (*{1})|*{1}|All files (*.*)|*.*", AppSettings.Instance.SifelFileformatExtension, SolutionFileExtension);

		public static string OutputFileFormatFilter => string.Format("SIFEL file format (*{0})|*{0}|GiD mesh file format (*.msh)|*.msh|VTK Simple ASCII file format (*.vtk)|*.vtk|All files (*.*)|*.*", AppSettings.Instance.SifelFileformatExtension);

		public static string ImportMeshFileFormatFilter => "All supported files (*.msh, *.vtu)|*.msh;*.vtu|GiD mesh file format (*.msh)|*.msh|VTK XML unstructured grid (*.vtu)|*.vtu|All files (*.*)|*.*";

		public static string ImportDataFileFormatFilter => "All supported files (*.res, *.vtu)|*.res;*.vtu|GiD data file format (*.res)|*.res|VTK XML unstructured grid data (*.vtu)|*.vtu|All files (*.*)|*.*";

		#endregion

		#region Public Static methods

		#region Instance creators

		public static SceneFacade GetEmptyScene()
		{
			return new SceneFacade(new Scene());
		}

		public static SceneFacade GetEmptyMultiScene()
		{
			return new SceneFacade(new MultiScene());
		}

		public static SceneFacade GetCopyOf(SceneFacade sceneToCopy)
		{
			return new SceneFacade(sceneToCopy.scene.Copy());
		}

		#endregion

		public static void InitializeGL()
		{
			//GL.LoadAll();
			//Glu.LoadAll();/**/

			GL.ShadeModel(Scene.MeshShadingModel);                          // enable smooth shading
			GL.ClearColor(Color.Black);                                     // white background
			GL.ClearDepth(1.0);                                             // depth buffer setup
			GL.Enable(EnableCap.DepthTest);                                 // enables depth testing
			GL.DepthFunc(DepthFunction.Lequal);                             // type of depth test
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest); // nice perspective calculations

			GL.Disable(EnableCap.CullFace);
			//GL.CullFace(CullFaceMode.Back);

			// No side is special, use lighting for both sides
			GL.LightModel(LightModelParameter.LightModelTwoSide, 1f);

			//// default materials
			//float[] ambientMat = { 0.4f, 0.4f, 0.4f, 1 };
			//Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_AMBIENT, ambientMat);
			//float[] diffuseMat = { 0.6f, 0.6f, 0.6f, 1 };
			//Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_DIFFUSE, diffuseMat);
			//float[] specularMat = { 1.0f, 1.0f, 1.0f, 1 };
			//Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_SPECULAR, specularMat);
			//float shininess = 64;
			//Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_SHININESS, ref shininess);

			float[] ambient2 = { 0.2f, 0.2f, 0.2f, 1f };
			float[] diffuse2 = { 0.9f, 0.9f, 0.9f, 1f };
			float[] specular2 = { 0.7f, 0.7f, 0.7f, 1f };
			float[] globalAmbient2 = { 0f, 0f, 0f, 1f };

			//Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_AMBIENT, ambient2);
			//Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, diffuse2);
			//Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, specular2);
			//Gl.glLightModelfv(Gl.GL_LIGHT_MODEL_AMBIENT, globalAmbient2);

			GL.Light(LightName.Light0, LightParameter.Ambient, ambient2);
			GL.Light(LightName.Light0, LightParameter.Diffuse, diffuse2);
			GL.Light(LightName.Light0, LightParameter.Specular, specular2);
			GL.LightModel(LightModelParameter.LightModelAmbient, globalAmbient2);


			// enable lights
			GL.Enable(EnableCap.Light0);
			GL.Enable(EnableCap.Lighting);

			// color material
			GL.Enable(EnableCap.ColorMaterial);
			GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);

			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			//GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
			GL.Hint(HintTarget.PointSmoothHint, HintMode.Fastest);
			GL.Hint(HintTarget.LineSmoothHint, HintMode.Fastest);

			// The surface normal, as the name indicates, has to be a normal or unit length vector, otherwise the lighting calculations won't work. If you use glScale anywhere, 
			// surface normals may no longer be correct because the scaling will shorten or lengthen the vector. At startup, write 
			//GL.Enable(EnableCap.Normalize); // I dont use GL.Scale anywhere for now...
			// so OpenGL will check and if necessary renormalise all your surface normals. In Olden Times this could slow your program down significantly, but these days it doesn't matter.

			//int depth;
			//GL.GetInteger(GetPName.DepthBits, out depth);
			//Console.WriteLine("Depth buffer size: " + depth);
			//MessageBox.Show(depth.ToString(), "Buffer size");
		}

		#endregion

		#region Public Instance methods

		public void GetSelectionSummary(out int nodeCount, out int elementCount, out int faceCount, out int edgeCount)
		{
			nodeCount = elementCount = faceCount = edgeCount = 0;
			if (!ContainsMesh)
				return;
			foreach (ISelectable entity in scene.Mesh.SelectedItems)
			{
				if (entity is IFaceOfElement3D)
					faceCount++;
				else if (entity is Element)
					elementCount++;
				else if (entity is Node)
					nodeCount++;
				else if (entity is WingedEdge)
					edgeCount++;
			}
		}

		public string GetDescriptionOfSelectedItems(ItemTypeToSelect entityType, bool showCompleteInfo)
		{
			if (!ContainsMesh)
				return string.Empty;
			// =============================================
			StringBuilder text = new StringBuilder();
			List<Element> elements = new List<Element>();
			List<Node> nodes = new List<Node>();
			foreach (ISelectable entity in scene.Mesh.SelectedItems)
			{
				if (entityType == ItemTypeToSelect.Face && entity is IFaceOfElement3D)
				{
					SifelFileFormatMeshSaver.AppendDescriptionOfFace((Element2D)entity, text, showCompleteInfo);
					text.AppendLine();
				}
				else if (entityType == ItemTypeToSelect.Element && entity is Element)
				{
					elements.Add((Element)entity);
				}
				else if (entityType == ItemTypeToSelect.Node && entity is Node)
				{
					nodes.Add((Node)entity);
				}
				else if (entityType == ItemTypeToSelect.Edge && entity is WingedEdge)
				{
					SifelFileFormatMeshSaver.AppendDescriptionOfEdge((WingedEdge)entity, text, showCompleteInfo);
					text.AppendLine();
				}
			}
			// =============================================================================
			if (entityType == ItemTypeToSelect.Element)
			{
				elements.Sort(); // seradit podle ID
				foreach (Element e in elements)
				{
					if (showCompleteInfo)
						SifelFileFormatMeshSaver.AppendDescriptionOfElement(e, text);
					else
						text.Append(e.ID);
					text.AppendLine();
				}
			}
			else if (entityType == ItemTypeToSelect.Node)
			{
				nodes.Sort(); // seradit podle ID
				foreach (Node n in nodes)
				{
					if (showCompleteInfo)
						SifelFileFormatMeshSaver.AppendDescriptionOfNode(n, text, scene.Mesh);
					else
						text.Append(n.ID);
					text.AppendLine();
				}
			}
			// -------------------------------------------------------
			return text.ToString();
		}

		public void SetPropertyOfSelectedItems(Property property)
		{
			scene.SetPropertyOfSelectedItems(property);
		}

		public void DrawScene(bool isActive, bool swapBuffers)
		{
			if (MakeCurrentNeeded != null)
				MakeCurrentNeeded(this, EventArgs.Empty);
			
			if (clickTimerFlag)
			{
				processPointSelection();
				clickTimerFlag = false;
			}
			if (needToComputeVisibleNodesFlag)
			{
				computeVisibleNodes();
				needToComputeVisibleNodesFlag = false;
			}

			GL.ShadeModel(Scene.MeshShadingModel);

			if (isActive) // vybrat barvu pozadi
				GL.ClearColor(Scene.ActiveBackColor);
			else
				GL.ClearColor(Scene.NonActiveBackColor);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // vymazat buffery
			
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
			
			scene.Camera.LookAt(); // natoc kameru

			scene.Draw(!isActive || cameraChangedTimer.Enabled, !isActive || drawSelectionRectangleFlag); // vykresli scenu, pokud s ni zrovna hejbu, tak ji vykresli rychleji (zjednodusene)

			if (drawSelectionRectangleFlag)
				drawSelectionRectangle();

			if (scene.NodeSignal != null || scene.ElementSignal != null)
				drawSignals();

			if (swapBuffers && SwapBuffersNeeded != null)
				SwapBuffersNeeded(this, EventArgs.Empty);
		}

		public void LostFocusHandler()
		{
			ControlDown = ShiftDown = mouseDownFlag = false;
		}

		public void DisposeScene()
		{
			this.scene.Dispose();
		}

		public void ResizeScene(int width, int height)
		{
			if (MakeCurrentNeeded != null)
				MakeCurrentNeeded(this, EventArgs.Empty);

			this.clientWindowSize.Width = width;
			this.clientWindowSize.Height = height;

			GL.Viewport(0, 0, width, height);

			// set perspective projection
			GL.MatrixMode(MatrixMode.Projection); // Projekční matice
			GL.LoadIdentity(); // Reset projekční matice

			// Perspektivní projekce
			double aspect = (double)this.clientWindowSize.Width / (double)this.clientWindowSize.Height;
			Utils.GluPerspective(Scene.FOVY_PARAM, aspect, Scene.Z_NEAR_PARAM, Scene.Z_FAR_PARAM);
			// --------------------------

			if (InvalidateNeeded != null)
				InvalidateNeeded(this, EventArgs.Empty);
		}

		public void PerformAction(AvailableAction action)
		{
			PerformAction(action, null);
		}

		public void PerformAction(AvailableAction action, object parameter)
		{
			bool thisMeshNeedRefreshInOtherWindows = false;

			switch (action)
			{
				case AvailableAction.CutMesh:
					CutInfo cutInfo = parameter as CutInfo;
					if (scene.Mesh != null && cutInfo != null)
					{
						scene.Cut(cutInfo);
						if (cutInfo.Action == CutInfo.ActionType.Cut || cutInfo.Action == CutInfo.ActionType.ShowHideElements)
							needToComputeVisibleNodesFlag = true;
						thisMeshNeedRefreshInOtherWindows = true;
					}
					break;
				case AvailableAction.UpdateElementsCutByValueLimit:
					if (scene.LastUsedCutInfo != null && scene.LastUsedCutInfo.Action == CutInfo.ActionType.ShowHideElements && scene.LastUsedCutInfo.ValueLimit != null)
					{
						scene.UpdateLastUsedCut();
						needToComputeVisibleNodesFlag = true;
						thisMeshNeedRefreshInOtherWindows = true;
					}
					break;
				case AvailableAction.DeleteSelectedElements:
					if (scene.Mesh != null)
					{
						scene.HideSelectedElements();
						needToComputeVisibleNodesFlag = true;
						thisMeshNeedRefreshInOtherWindows = true;
					}
					break;
				case AvailableAction.RestoreMesh:
					if (scene.Mesh != null)
					{
						scene.RestoreWholeMesh();
						needToComputeVisibleNodesFlag = true;
						thisMeshNeedRefreshInOtherWindows = true;
					}
					break;
				case AvailableAction.LineSmooth:
					Scene.LineSmooth = !Scene.LineSmooth;
					break;
				case AvailableAction.PointSmooth:
					Scene.PointSmooth = !Scene.PointSmooth;
					break;
				case AvailableAction.FaceLighting:
					Scene.FaceLighting = !Scene.FaceLighting;
					break;
				case AvailableAction.EdgeLighting:
					Scene.EdgeLighting = !Scene.EdgeLighting;
					break;
				case AvailableAction.XRayVision:
					Scene.XRayVision = !Scene.XRayVision;
					return; // !!
				case AvailableAction.ChangeRenderMode: // change render mode
					if (scene.RenderMode == RenderMode.Points)
						scene.RenderMode = RenderMode.BorderLines;
					else if (scene.RenderMode == RenderMode.BorderLines)
						scene.RenderMode = RenderMode.AllLines;
					else if (scene.RenderMode == RenderMode.AllLines)
						scene.RenderMode = RenderMode.LinesPoints;
					else if (scene.RenderMode == RenderMode.LinesPoints)
						scene.RenderMode = RenderMode.FacesLines;
					else if (scene.RenderMode == RenderMode.FacesLines)
					{
						scene.RenderMode = RenderMode.FacesLinesPoints;
						computeVisibleNodes();
						//if (scene.Mesh != null && !cameraChangedTimer.Enabled) // pokud jsou zobrazeny body, tak vypocti, ktery jsou videt
						//	scene.Mesh.CreateVisibleNodesList(new Rectangle(Point.Empty, clientWindowSize), scene.Camera);
					}
					else if (scene.RenderMode == RenderMode.FacesLinesPoints)
						scene.RenderMode = RenderMode.FacesBorder;
					else if (scene.RenderMode == RenderMode.FacesBorder)
						scene.RenderMode = RenderMode.Faces;
					else if (scene.RenderMode == RenderMode.Faces)
					{
						scene.RenderMode = RenderMode.Points;
						computeVisibleNodes();
						//if (scene.Mesh != null && !cameraChangedTimer.Enabled) // pokud jsou zobrazeny body, tak vypocti, ktery jsou videt
						//	scene.Mesh.CreateVisibleNodesList(new Rectangle(Point.Empty, clientWindowSize), scene.Camera);
					}
					else // pokud je neco uplne jinyho tak to nastavim na defaultni
						scene.RenderMode = Scene.DefaultRenderMode;

					if (RenderModeChanged != null)
						RenderModeChanged(this, EventArgs.Empty);
					break;
				case AvailableAction.CameraReset:
					//scene.Camera.Reset();
					scene.SetDefaultCameraView();
					needToComputeVisibleNodesFlag = true;
					break;
				case AvailableAction.CameraStandardView:
					scene.Camera.SetView((CameraView)parameter);
					needToComputeVisibleNodesFlag = true;
					break;
				case AvailableAction.UnselectAllItems:
					scene.UnselectAllItems();
					thisMeshNeedRefreshInOtherWindows = true;
					break;
				case AvailableAction.SelectAllItems:
					scene.SelectAllItems(editorMode);
					thisMeshNeedRefreshInOtherWindows = true;
					break;
				case AvailableAction.InvertSelection:
					scene.InvertSelection();
					thisMeshNeedRefreshInOtherWindows = true;
					break;
				case AvailableAction.SelectIncidingItems:
					scene.SelectItemsIncidingWithFaces();
					thisMeshNeedRefreshInOtherWindows = true;
					break;
				case AvailableAction.SelectItemsWithProperty:
				case AvailableAction.SelectItemsWithPropertyAdd:
					if (parameter != null)
					{
						scene.SelectItemsWithProperty(editorMode, (Property)parameter, addToSelection: (action == AvailableAction.SelectItemsWithPropertyAdd));
						thisMeshNeedRefreshInOtherWindows = true;
					}
					break;
				case AvailableAction.Refresh:
					if (scene.Mesh != null)
					{
						scene.Mesh.UpdateColors();
						computeVisibleNodes();
						thisMeshNeedRefreshInOtherWindows = true;
					}
					break;
				case AvailableAction.Redraw:
					thisMeshNeedRefreshInOtherWindows = true;
					break;
				case AvailableAction.UpdateColorBuffers:
					if (scene.Mesh != null)
					{
						scene.Mesh.UpdateColors();
						thisMeshNeedRefreshInOtherWindows = true;
					}
					break;
				case AvailableAction.Storno:
					if (CutPlaneDefinitionPointsCount > 0)
					{
						scene.ClearPlaneDefinitionPoints();
						if (CutPlaneDefinitionPointsChanged != null)
							CutPlaneDefinitionPointsChanged(this, EventArgs.Empty);
					}
					scene.UnselectAllItems();
					thisMeshNeedRefreshInOtherWindows = true;
					break;
				case AvailableAction.ClearPlaneDefinitionPoints:
					scene.ClearPlaneDefinitionPoints();
					if (CutPlaneDefinitionPointsChanged != null)
						CutPlaneDefinitionPointsChanged(this, EventArgs.Empty);
					break;
				case AvailableAction.CreateCutPlane:
					scene.CreateCutPlaneFromDefinitionPoints();
					break;
				case AvailableAction.UpdateVisibleNodes:
					computeVisibleNodes();
					break;
				case AvailableAction.ZoomToFit:
					scene.Camera.ZoomToFit();
					needToComputeVisibleNodesFlag = true;
					break;
				case AvailableAction.RecreateBuffers:
					scene.RecreateBuffers();
					thisMeshNeedRefreshInOtherWindows = true;
					break;
				case AvailableAction.DeleteHiddenItems:
					if (scene.Mesh == null)
						return;
					scene.Mesh.ClearHiddenElements();
					break;
				case AvailableAction.AddPropertyToSelectedNodes:
					scene.AddPropertyToSelectedNodes((Property)parameter);
					break;
				case AvailableAction.RemovePropertyFromSelectedNodes:
					scene.RemovePropertyFromSelectedNodes((Property)parameter);
					break;
				case AvailableAction.SignalNode:
					try
					{
						int[] intArray = parameter as int[];
						if (intArray != null)
						{
							scene.NodeSignal = intArray;
						}
						else
						{
							Debug.Assert(parameter is int);
							scene.NodeSignal = new int[] { (int)parameter };
						}
					}
					catch (ArgumentException ex)
					{
						if (ShowError != null)
							ShowError(this, new ShowErrorEventArgs("Can not signal node", ex.Message));
						scene.NodeSignal = null;
					}
					needToComputeVisibleNodesFlag = true;
					break;
				case AvailableAction.SignalElement:
					try
					{
						scene.ElementSignal = (int)parameter;
					}
					catch (ArgumentException ex)
					{
						if (ShowError != null)
							ShowError(this, new ShowErrorEventArgs("Can not signal element", ex.Message));
						scene.ElementSignal = null;
					}
					needToComputeVisibleNodesFlag = true;
					break;
				case AvailableAction.ClearSignalNode:
					scene.NodeSignal = null;
					needToComputeVisibleNodesFlag = true;
					break;
				case AvailableAction.ClearSignalElement:
					scene.ElementSignal = null;
					needToComputeVisibleNodesFlag = true;
					break;
				case AvailableAction.UpdateNodeCoordinates:
					if (scene.Mesh != null)
					{
						scene.Mesh.UpdateNodeCoordinates();
						thisMeshNeedRefreshInOtherWindows = true;
						needToComputeVisibleNodesFlag = true;
					}
					break;
				default:
					return;
			}

			if (thisMeshNeedRefreshInOtherWindows && MeshNeedRefresh != null && scene.Mesh != null)
				MeshNeedRefresh(this, new MeshNeedRefreshEventArgs(scene.Mesh.Filename));
			else if (RefreshNeeded != null)
				RefreshNeeded(this, EventArgs.Empty);

			if (ActionPerformed != null)
				ActionPerformed(this, EventArgs.Empty);
		}

		public object GetValue(AvailableValue valueName)
		{
			switch (valueName)
			{
				case AvailableValue.MinimalElementRadius:
					if (scene.Mesh != null)
						return scene.Mesh.MinimalElementRadius; // prvni vypocet chvili trva
					return null;
				case AvailableValue.MeshShadingModel:
					return Scene.MeshShadingModel;
				case AvailableValue.MeshStatistics:
					if (scene.Mesh != null)
						return scene.Mesh.Statistics;
					return null;
				case AvailableValue.VBOSupported:
					return RichVBO.IsSupported;
				case AvailableValue.OrdinaryEdgeColor:
					return Scene.OrdinaryEdgeColor;
				case AvailableValue.FaceColor:
					return Scene.FaceColor;
				case AvailableValue.ElementCount:
					if (scene.Mesh != null)
						return scene.Mesh.CurrentElementCount;
					return null;
				case AvailableValue.NodeCount:
					if (scene.Mesh != null)
						return scene.Mesh.ComputeCurrentNodeCount(); // pomerne narocne na vypocet !!!
					return null;
				case AvailableValue.FaceCount:
					if (scene.Mesh != null)
						return scene.Mesh.FaceCount;
					return null;
				case AvailableValue.EdgeCount:
					if (scene.Mesh != null)
						return scene.Mesh.EdgeCount;
					return null;
				
				case AvailableValue.BeamCount:
					if (scene.Mesh != null)
						return scene.Mesh.BeamCount;
					return null;
				case AvailableValue.DrawAxes:
					return scene.DrawAxes;
				case AvailableValue.DrawAxisArrows:
					return scene.DrawAxisArrows;
				case AvailableValue.DrawBeams:
					return scene.DrawBeams;
				case AvailableValue.DrawNodeNumbers:
					return scene.DrawNodeNumbers;
				case AvailableValue.DrawElementNumbers:
					return scene.DrawElementNumbers;
				case AvailableValue.DrawBeamNumbers:
					return scene.DrawBeamNumbers;
				case AvailableValue.SelectedItemsDescription:
					return scene.GetSelectedItemsDescription();
				case AvailableValue.Status:
					if (scene.Mesh != null && mouseDownFlag && !pointUnderCursorContext.MouseDownBackgroundHit && prevMouseLocation == mouseDownLocation && mouseDownCount == 1)
					{
						Vector3 transformedPosition = (pointUnderCursorContext.PointUnderCursor / scene.Mesh.ResizeFactor) + scene.Mesh.PositionOffset;
						return "Point under cursor: " + Utils.GetVector3StringRepresentation(ref transformedPosition);
					}
					return scene.GetSelectedItemsDescription();
				case AvailableValue.MeshElementPropertiesSorted:
					return scene.GetElementPropertiesSorted();
				case AvailableValue.AllMeshPropertiesSorted:
					if (scene.Mesh == null)
						return null;
					return scene.Mesh.Statistics.GetAllPropertiesSorted();
				case AvailableValue.RenderMode:
					return scene.RenderMode;
				case AvailableValue.ColorMode:
					if(scene.Mesh != null)
						return scene.Mesh.ColorMode;
					return null;
				case AvailableValue.DrawQuadraticNodes:
					return Scene.IncludeEdgeMiddleNodes;
				case AvailableValue.MeshHasHiddenElements:
					if (scene.Mesh != null)
						return scene.Mesh.HasHiddenElements();
					return false;
				case AvailableValue.UnsavedChangesInMesh:
					if (scene.Mesh == null)
						return false;
					return scene.Mesh.UnsavedChanges;
				case AvailableValue.NodeSignalIsSet:
					return scene.NodeSignal != null;
				case AvailableValue.ElementSignalIsSet:
					return scene.ElementSignal.HasValue;
				case AvailableValue.DataVisualizer:
					if (scene.Mesh != null)
						return scene.Mesh.GetDataVisualizer();
					return null;
				case AvailableValue.MeshDimensions:
					if (scene.Mesh != null)
						return scene.Mesh.UpperBound - scene.Mesh.LowerBound;
					return Vector3.Zero;
				case AvailableValue.LastUsedCutInfo:
					return scene.LastUsedCutInfo;
				default:
					return null;
			}
		}

		public void SetValue(AvailableValue valueName, object value)
		{
			bool thisMeshNeedRefreshInOtherWindows = false;
			// tady by se nemela provadet zadna akce, jen nastavovat hodnoty a pak prekreslit
			switch (valueName)
			{
				case AvailableValue.OrdinaryEdgeColor:
					Scene.OrdinaryEdgeColor = (Color)value;
					break;
				case AvailableValue.FaceColor:
					Scene.FaceColor = (Color)value;
					break;
				case AvailableValue.DrawAxes:
					scene.DrawAxes = (bool)value;
					break;
				case AvailableValue.DrawAxisArrows:
					scene.DrawAxisArrows = (bool)value;
					break;
				case AvailableValue.DrawBeams:
					scene.DrawBeams = (bool)value;
					break;
				case AvailableValue.DrawQuadraticNodes:
					Scene.IncludeEdgeMiddleNodes = (bool)value;
					computeVisibleNodes();
					thisMeshNeedRefreshInOtherWindows = true;
					break;
				case AvailableValue.DrawNodeNumbers:
					scene.DrawNodeNumbers = (bool)value;
					if(scene.DrawNodeNumbers)
						computeVisibleNodes();
					break;
				case AvailableValue.DrawElementNumbers:
					scene.DrawElementNumbers = (bool)value;
					if (scene.DrawElementNumbers)
						computeVisibleNodes();
					break;
				case AvailableValue.DrawBeamNumbers:
					scene.DrawBeamNumbers = (bool)value;
					if (scene.DrawBeamNumbers)
						computeVisibleNodes();
					break;
				case AvailableValue.RenderMode:
					if (value != null)
					{
						scene.RenderMode = (RenderMode)value;
						computeVisibleNodes();
						if (RenderModeChanged != null)
							RenderModeChanged(this, EventArgs.Empty);
					}
					break;
				case AvailableValue.ColorMode:
					if (scene.Mesh != null && value != null)
					{
						scene.Mesh.ColorMode = (PropertyColorsMode)value;
						if (ColorModeChanged != null)
							ColorModeChanged(this, EventArgs.Empty);
					}
					break;
				case AvailableValue.UnsavedChangesInMesh:
					if (scene.Mesh != null && value != null && value is bool)
						scene.Mesh.UnsavedChanges = (bool)value;
					break;
				case AvailableValue.DataVisualizer:
					if (scene.Mesh != null)
					{
						IDataVisualizer dataVisualizer = value as IDataVisualizer;
						scene.Mesh.SetDataVisualizer(dataVisualizer);
					}
					break;
			}

			if (thisMeshNeedRefreshInOtherWindows && MeshNeedRefresh != null && scene.Mesh != null)
				MeshNeedRefresh(this, new MeshNeedRefreshEventArgs(scene.Mesh.Filename));
			else if (RefreshNeeded != null)
				RefreshNeeded(this, EventArgs.Empty);
		}

		public void MouseDownHandler(Point location)
		{
			if (MakeCurrentNeeded != null)
				MakeCurrentNeeded(this, EventArgs.Empty);

			this.mouseDownCount++;
			this.prevMouseLocation = location;
			this.mouseDownFlag = true;
			this.mouseDownLocation = location;

			if (EditorMode == EditorMode.Pan)
				cameraChangedDirection = true;
			//else if (EditorMode == EditorMode.Orbit)
			pointUnderCursorContext.Compute(scene, mouseDownLocation, true);
		}

		public void MouseUpHandler(Point location, MouseButton button)
		{
			if (!mouseDownFlag) // stisk mysi nebyl v tomto okne, nedelam nic
				return;

			mouseDownFlag = false;
			bool sameLocationAsPreviousClick = (location == mouseUpLocation);
			mouseUpLocation = location;
			bool selectionRectangleWasDrawn = drawSelectionRectangleFlag;
			if (drawSelectionRectangleFlag)
				drawSelectionRectangleFlag = false;

			// ---------------------------------------

			if (button == MouseButton.Left)
			{
				if (!selectionRectangleWasDrawn && pixelDistance(location, mouseDownLocation) <= SceneFacade.CLICK_DISTANCE_TOLERANCE)
				{
					if (sameLocationAsPreviousClick && (DateTime.Now - lastClickTime).TotalMilliseconds < SceneFacade.MAX_INTERVAL_BETWEEN_CLICKS)
						clickCount++;
					else
						clickCount = 1;
					// -------------------
					if (clickTimer.Enabled)
						clickTimer.Stop();
					processMouseClick();
					// -------------------
					lastClickTime = DateTime.Now;
				}
				else
				{
					// -------------------
					processMouseDrag();
					// -------------------
					clickCount = 0;
					lastClickTime = DateTime.MinValue;
				}
			}
			else if (button == MouseButton.Middle)
			{
				//scene.Camera.Reset();
				scene.Camera.ZoomToFit();
				needToComputeVisibleNodesFlag = true;
				if (InvalidateNeeded != null)
					InvalidateNeeded(this, EventArgs.Empty);
			}
		}

		public void MouseMoveHandler(Point location, MouseButton button)
		{
			if (location == prevMouseLocation) // pokud jsem se nepohnul, tak nedelam nic
				return;
			mouseDownCount = 0;
			
			if (!mouseDownFlag) // stisk mysi nebyl v tomto okne, nedelam nic
			{
				prevMouseLocation = location;
				return;
			}
							
			if (button == MouseButton.Left)
			{
				int dX = location.X - prevMouseLocation.X;
				int dY = location.Y - prevMouseLocation.Y;
				switch (EditorMode)
				{
					case EditorMode.SelectFaces:
					case EditorMode.SelectEdges:
					case EditorMode.SelectNodes:
					case EditorMode.SelectElements:
                    case EditorMode.SelectBeams:
					case EditorMode.ZoomWindow:
					case EditorMode.ScreenshotWindow:
						// draw selection rectangle
						if (pixelDistance(mouseDownLocation, location) > SceneFacade.CLICK_DISTANCE_TOLERANCE)
							drawSelectionRectangleFlag = true;
						if (drawSelectionRectangleFlag)
						{
							if (InvalidateNeeded != null)
								InvalidateNeeded(this, EventArgs.Empty);
						}
						break;
					case EditorMode.Orbit:
						strafeCamera(dX, dY); // <--
						break;
					case EditorMode.Pan:
						translateCamera(dX, dY); // <--
						break;
					case EditorMode.LookAround:
						lookAroundCamera(dX, dY); // <--
						break;
				}
			}
			prevMouseLocation = location;
		}

		public void ZoomCamera(Point mouseLocation, int delta)
		{
			if (MakeCurrentNeeded != null)
				MakeCurrentNeeded(this, EventArgs.Empty);
			
			pointUnderCursorContext.Compute(scene, mouseLocation, false);
			Vector3 direction = pointUnderCursorContext.PointUnderCursor - scene.Camera.Eye;
			float distance = direction.Length;

			cameraChangedTimer.Enabled = false;

			Vector3 move;

			//bool useNonLinearZoom = !mouseDownBackgroundHit || scene.Mesh == null || (scene.Camera.Eye - scene.Mesh.CenterOfRotation).Length > scene.Mesh.Radius;

			//if (useNonLinearZoom) // pokud je pod kurzorem sit, tak pouzit NElinearni zoom
			//{
				// !! do not stop in front of mesh, walk through
				// zarazit se, pokud jsem moc blizko objektu
				//if (delta > 0 && (distance - distance * Scene.WHEEL_ZOOM_FACTOR) < (Scene.Z_NEAR_PARAM * Scene.CLOSEST_ZOOM_MULTIPLE_OF_NEAR_PARAM))
				//	return;

				move = direction * Scene.WHEEL_ZOOM_FACTOR;
				float length = move.Length;
				if (length > Scene.MAX_ZOOM_DISTANCE)
					move *= Scene.MAX_ZOOM_DISTANCE / length;
			//}
			//else // pokud pod kurzorem neni sit, ale volny prostor, tak pouzit linearni zoom
			//{
			//    move = Vector3.Normalize(direction) * Scene.LINEAR_ZOOM_DISTANCE;
			//}

			if (delta < 0) // odzoomovani
				move = -move;

			scene.Camera.Move(move);
			cameraChangedDirection = true;

			cameraChangedTimer.Start();
			
			if (RefreshNeeded != null)
				RefreshNeeded(this, EventArgs.Empty);
		}

		public void Initialize()
		{
			if (MakeCurrentNeeded != null)
				MakeCurrentNeeded(this, EventArgs.Empty);

			//scene.Camera.Reset();
			scene.SetDefaultCameraView();

			createBuffers();
			needToComputeVisibleNodesFlag = true;
			if (InvalidateNeeded != null)
				InvalidateNeeded(this, EventArgs.Empty);

			if (EditorModeChanged != null)
				EditorModeChanged(null, EventArgs.Empty);
		}

		public async Task ReloadMeshAsync(IMeshFileParser parser, System.Threading.CancellationToken cancellationToken, Action<string, int> progressReport)
		{
			bool isFirstMesh = !ContainsMesh;
			
			{
				IMeshCreator meshCreator = new MeshConstructor();
				if (progressReport != null)
				{
					meshCreator.Step += (s, e) => progressReport(e.OperationName, e.PercentDone);
				}

				Mesh result;
				using (parser)
				{
					result = await Task.Run(() => meshCreator.CreateMesh(parser, cancelled: () => cancellationToken.IsCancellationRequested));
				}

				PropertyColorsMode? oldColorMode = scene.Mesh?.ColorMode;

				scene.SetMesh(result);

				if (scene.Mesh != null && oldColorMode.HasValue)
				{
					scene.Mesh.ColorMode = oldColorMode.Value;
				}
			}

			createBuffers();

			needToComputeVisibleNodesFlag = true;

			if (isFirstMesh)
			{
				scene.SetDefaultCameraView();
			}

			MeshReloaded?.Invoke(this, EventArgs.Empty);
		}

		public void RemoveMesh()
		{
			scene.SetMesh(null);
			MeshReloaded?.Invoke(this, EventArgs.Empty);
		}

		public void LoadMeshFromFiles(string[] filenames, MeshIOEventHandler progressNotifier, YesNoQuestion cancelled)
		{
			Debug.Assert(filenames != null && filenames.Length > 0);
			IMeshFileParser parser = MeshParserFactory.Create(filenames);

			Mesh result = null;
			using (parser)
			{
#if !DEBUG
				try
				{
#endif
				IMeshCreator meshCreator = new MeshConstructor();
				if (progressNotifier != null)
					meshCreator.Step += progressNotifier;

				result = meshCreator.CreateMesh(parser, cancelled);
#if !DEBUG
				}
				catch (FileParserException ex)
				{
					if (ShowError != null)
						ShowError(this, new ShowErrorEventArgs("Error while loading mesh from file", Utils.BuildErrorMessage(ex)));
				}
				catch (MeshConstructingException ex)
				{
					if (ShowError != null)
						ShowError(this, new ShowErrorEventArgs("Error while constructing mesh", ex.Message));
				}
#endif
			}
			if (result != null && (cancelled == null || !cancelled()))
			{
				scene.SetMesh(result);
			}
		}

		public void SaveMeshToFile(string filename, bool saveWithoutCuttedElements, MeshIOEventHandler progressNotifier, YesNoQuestion cancelled)
		{
//#if !DEBUG
//            try
//            {
//#endif
			if (scene.Mesh != null)
			{
				IMeshSaver meshSaver = MeshSaverFactory.Create(filename);
				if (progressNotifier != null)
					meshSaver.Step += progressNotifier;
				meshSaver.SaveMesh(scene.Mesh, filename, saveWithoutCuttedElements, cancelled);
			}
			//#if !DEBUG
			//            }
			//            catch (MeshSavingException ex)
			//            {
			//                if (ShowError != null)
			//                    ShowError(this, new ShowErrorEventArgs("Error while saving mesh", Utils.BuildErrorMessage(ex)));
			//            }
			//#endif
		}

		public void SetRenderModeAccordingToEditorMode()
		{
			RenderMode mode = scene.RenderMode;
			switch (SceneFacade.EditorMode)
			{
				case EditorMode.SelectElements:
				case EditorMode.SelectFaces:
					mode |= RenderMode.Faces;
					break;
				case EditorMode.SelectNodes:
					mode |= RenderMode.Points;
					break;
				case EditorMode.SelectEdges:
					mode &= ~RenderMode.BorderLines;
					mode |= RenderMode.AllLines;
					break;
				default:
					return; // nic nedelat
			}

			SetValue(AvailableValue.RenderMode, mode);
		}
		
		public bool CheckOpenGLVersion()
		{
			int major, minor;
			Utilities.Functions.GetOpenGLVersion(out major, out minor);
			return major >= 2;
		}

		#endregion

		#region Private stuff

		private void processMouseClick()
		{
			switch (EditorMode)
			{
				case EditorMode.SelectFaces:
				case EditorMode.SelectEdges:
				case EditorMode.SelectNodes:
				case EditorMode.SelectElements:
				case EditorMode.SelectBeams:
					this.selectOperationType = getSelectOperationType();
//					clickTimer.Start(); // spustit timer, po jeho uplynuti se provede akce
					break;
				case EditorMode.PickCuttingPlanePoint:
					if (scene.Mesh != null)
					{
						scene.PutNextPlaneDefinitionPoint(mouseUpLocation.X, mouseUpLocation.Y);
						if (MeshNeedRefresh != null)
							MeshNeedRefresh(this, new MeshNeedRefreshEventArgs(scene.Mesh.Filename));
						if (CutPlaneDefinitionPointsChanged != null)
							CutPlaneDefinitionPointsChanged(this, EventArgs.Empty);
					}
					break;
				case EditorMode.ZoomWindow:
					for (int i = 0; i < 8; i++)
						ZoomCamera(mouseDownLocation, 1);
					break;
			}
			clickTimer.Start(); // spustit timer, po jeho uplynuti se provede akce
		}

		private void createBuffers()
		{
			if (scene.Mesh != null)
				scene.Mesh.CreateBuffers();
		}

		private void setAppropriateEditorMode()
		{
			if (!shiftDown && !controlDown)
			{
				EditorMode = editorModeWithoutModificationKeys;
				pointUnderCursorContext.Compute(scene, prevMouseLocation, true);
				return;
			}

			switch (editorModeWithoutModificationKeys)
			{
				case EditorMode.Orbit:
					if (shiftDown)
						EditorMode = EditorMode.Pan;
					else if (controlDown)
						EditorMode = EditorMode.LookAround;
					break;
				case EditorMode.LookAround:
					if (shiftDown)
						EditorMode = EditorMode.Pan;
					else if (controlDown)
						EditorMode = EditorMode.Orbit;
					break;
				case EditorMode.Pan:
					if (shiftDown)
						EditorMode = EditorMode.Orbit;
					else if (controlDown)
						EditorMode = EditorMode.LookAround;
					break;
			}
		}

		private void processPointSelection()
		{
			if (scene.Mesh == null)
				return;
			switch (EditorMode)
			{
				case EditorMode.SelectFaces:
					pointSelection(ItemTypeToSelect.Face);
					break;
				case EditorMode.SelectEdges:
					pointSelection(ItemTypeToSelect.Edge);
					break;
				case EditorMode.SelectNodes:
					pointSelection(ItemTypeToSelect.Node);
					break;
				case EditorMode.SelectElements:
					pointSelection(ItemTypeToSelect.Element);
					break;
                case EditorMode.SelectBeams:
                    pointSelection(ItemTypeToSelect.Beam);
                    break;
			}
		}

		private void processMouseDrag()
		{
			if (scene.Mesh == null)
			{
				// smazat nakresleny obdelnik vyberu
				if (InvalidateNeeded != null)
					InvalidateNeeded(this, EventArgs.Empty);
				return;
			}
			switch (EditorMode)
			{
				case EditorMode.SelectFaces:
					rectangleSelection(ItemTypeToSelect.Face);
					break;
				case EditorMode.SelectEdges:
					rectangleSelection(ItemTypeToSelect.Edge);
					break;
				case EditorMode.SelectNodes:
					rectangleSelection(ItemTypeToSelect.Node);
					break;
				case EditorMode.SelectElements:
					rectangleSelection(ItemTypeToSelect.Element);
					break;
                case EditorMode.SelectBeams:
                    rectangleSelection(ItemTypeToSelect.Beam);
                    break;
				case EditorMode.ZoomWindow:
					zoomWindow();
					break;
				case EditorMode.ScreenshotWindow:
					{
						var selectionRectangle = getSelectionRectangle();
						RefreshNeeded?.Invoke(this, EventArgs.Empty);
						ScreenshotNeeded?.Invoke(this, new ScreenshotNeededEventArgs(selectionRectangle));
						EditorMode = EditorMode.None;
					}
					break;
			}
		}

		private int pixelDistance(Point p1, Point p2)
		{
			int xDistance = Math.Abs(p1.X - p2.X);
			int yDistance = Math.Abs(p1.Y - p2.Y);
			return Math.Max(xDistance, yDistance);
		}

		private void pointSelection(ItemTypeToSelect itemType)
		{
			int numberOfClicks = (this.clickCount <= 4) ? this.clickCount : 4;
			SelectMode selectMode = (SelectMode)numberOfClicks;
			scene.SelectItems(new Rectangle(this.mouseUpLocation, Size.Empty), selectMode, this.selectOperationType, false, itemType);
			if (MeshNeedRefresh != null) // updatovat ostatni okna s touto meshi
				MeshNeedRefresh(this, new MeshNeedRefreshEventArgs(this.scene.Mesh.Filename));
			if (ActionPerformed != null)
				ActionPerformed(this, EventArgs.Empty);
		}

		private void rectangleSelection(ItemTypeToSelect itemType)
		{
			if (MakeCurrentNeeded != null)
				MakeCurrentNeeded(this, EventArgs.Empty);

			bool allNodesMustBeInAreaToSelectFace = (mouseUpLocation.X > mouseDownLocation.X);

			Rectangle area = getSelectionRectangle();
			SelectOperationType opType = getSelectOperationType();

			scene.SelectItems(area, SelectMode.Single, opType, allNodesMustBeInAreaToSelectFace, itemType);

			if (MeshNeedRefresh != null)
				MeshNeedRefresh(this, new MeshNeedRefreshEventArgs(this.scene.Mesh.Filename));

			if (ActionPerformed != null)
				ActionPerformed(this, EventArgs.Empty);
		}

		private SelectOperationType getSelectOperationType()
		{
			SelectOperationType opType = SelectOperationType.New;
			if (ControlDown && ShiftDown)
			{
				if (clickCount <= 1)
					opType = SelectOperationType.SymetricDifference;
				else
					opType = SelectOperationType.Union; /**/
			}
			else if (ShiftDown)
				opType = SelectOperationType.Union;
			else if (ControlDown)
				opType = SelectOperationType.Except;
			return opType;
		}

		private void computeVisibleNodes()
		{
			cameraChangedTimer.Enabled = false;

			if(scene.Mesh == null)
				return;
		
			bool findVisibleFaces = ((scene.RenderMode & RenderMode.Faces) != 0) && scene.DrawElementNumbers;
			bool beamsRendered = scene.Mesh.BeamCount > 0 && scene.DrawBeams;
			bool findVisibleNodes = findVisibleFaces || ((scene.RenderMode & RenderMode.Points) != 0) || beamsRendered;

			if (findVisibleNodes)
			{
				bool xRay = (scene.RenderMode == RenderMode.None && beamsRendered) || scene.RenderMode == RenderMode.Points;
				scene.Mesh.CreateVisibleNodesList(new Rectangle(Point.Empty, clientWindowSize), scene.Camera, xRay);
			}

			if (findVisibleFaces)
			{
				scene.Mesh.CreateVisibleFacesList(new Rectangle(Point.Empty, clientWindowSize), scene.Camera);
			}
		}

		public void MakeToComputeVisibleNodes()
		{
			this.needToComputeVisibleNodesFlag = true;
			if (InvalidateNeeded != null)
				InvalidateNeeded(this, EventArgs.Empty);
		}

		private void lookAroundCamera(int dX, int dY)
		{
			cameraChangedTimer.Enabled = false;

			float xAngle = -dX * 0.003f;
			float yAngle = -dY * 0.003f;
			scene.Camera.RotateView(xAngle, yAngle);
			cameraChangedDirection = true;

			cameraChangedTimer.Start();
			if (InvalidateNeeded != null)
				InvalidateNeeded(this, EventArgs.Empty);
		}

		private void strafeCamera(int dX, int dY)
		{
			cameraChangedTimer.Enabled = false;
			// ----------------------------------------------
			float xAngle = -dX * 0.005f;
			float yAngle = -dY * 0.005f;
			Vector3 centerOfRotation;
			if (!pointUnderCursorContext.MouseDownBackgroundHit)
				centerOfRotation = pointUnderCursorContext.PointUnderCursor;
			else if (scene.Mesh != null)
				centerOfRotation = scene.Mesh.CenterOfRotation;
			else
				centerOfRotation = Vector3.Zero;
			scene.Camera.Orbit(centerOfRotation, xAngle, yAngle);
			// ----------------------------------------------
			//if (!pointUnderCursorContext.MouseDownBackgroundHit)
			//{
			//	Vector3 translation = pointUnderCursorContext.GetTranslationVector(prevMouseLocation.X + dX, prevMouseLocation.Y + dY);
			//	scene.Camera.Move(translation);
			//}
			cameraChangedDirection = true;

			cameraChangedTimer.Start();
			if (InvalidateNeeded != null)
				InvalidateNeeded(this, EventArgs.Empty);
		}

		private void zoomWindow()
		{
			if (MakeCurrentNeeded != null)
				MakeCurrentNeeded(this, EventArgs.Empty);
			// -----------------------------------------------

			Rectangle area = getSelectionRectangle();

			findClosestPointInArea(ref area);

			//Point center = new Point(area.X + area.Width / 2, area.Y + area.Height / 2);
			//computePointUnderCursor(center, true);

			Camera cam = scene.Camera;
			Vector3 dir = cam.GetDirection();

			float projection = Vector3.Dot(pointUnderCursorContext.PointUnderCursor - cam.Eye, dir);

			float distance = computeDistanceOfCameraFromRectangleSelection(area, projection);

			// do not stop in front of mesh, walk through
			//if (distance < (Scene.Z_NEAR_PARAM * Scene.CLOSEST_ZOOM_MULTIPLE_OF_NEAR_PARAM))
			//	distance = (float)(Scene.Z_NEAR_PARAM * Scene.CLOSEST_ZOOM_MULTIPLE_OF_NEAR_PARAM);

			//distance = projection;

			cam.SetNewEyePosition(pointUnderCursorContext.PointUnderCursor - (dir * distance));

			// -----------------------------------------------
			cameraChangedDirection = true; /**/
			needToComputeVisibleNodesFlag = true;
			if (RefreshNeeded != null)
				RefreshNeeded(this, EventArgs.Empty);
		}

		private void findClosestPointInArea(ref Rectangle area)
		{
			Point leftTop = new Point(area.Left, area.Top);
			Point rightTop = new Point(area.Right, area.Top);
			Point leftBottom = new Point(area.Left, area.Bottom);
			Point rightBottom = new Point(area.Right, area.Bottom);
			Point center = new Point(area.X + area.Width / 2, area.Y + area.Height / 2);
			
			Camera cam = scene.Camera;
			Vector3 dir = cam.GetDirection();

			pointUnderCursorContext.Compute(scene, center, true);

			Vector3 pointUnderCenter = pointUnderCursorContext.PointUnderCursor;
			float distanceOfCenter = (pointUnderCursorContext.PointUnderCursor - cam.Eye).Length; //Vector3.Dot(pointUnderCursor - cam.Eye, dir);
			
			//Vector3 closestPointInArea = pointUnderCenter;
			float distanceOfClosestPoint = (pointUnderCursorContext.MouseDownBackgroundHit) ? float.MaxValue : distanceOfCenter;

			foreach (Point point in new Point[] { leftTop, leftBottom, rightTop, rightBottom })
			{
				pointUnderCursorContext.Compute(scene, point, true);
				float projection = (pointUnderCursorContext.MouseDownBackgroundHit) ? float.MaxValue : (pointUnderCursorContext.PointUnderCursor - cam.Eye).Length; //Vector3.Dot(pointUnderCursor - cam.Eye, dir);
				if (projection < distanceOfClosestPoint)
				{
					distanceOfClosestPoint = projection;
					//closestPointInArea = pointUnderCursor;
				}
			}

			Vector3 course = Vector3.Normalize(pointUnderCenter - cam.Eye);

			// set field to closest
			if (distanceOfClosestPoint < float.MaxValue)
				pointUnderCursorContext.PointUnderCursor = pointUnderCenter - (course * (distanceOfCenter - distanceOfClosestPoint));
			else
				pointUnderCursorContext.PointUnderCursor = pointUnderCenter;
		}

		private float computeDistanceOfCameraFromRectangleSelection(Rectangle area, float distanceToHit)
		{
			int[] viewport;
			Scene.ExtractViewport(out viewport);

			float windowAspect = (float)viewport[2] / (float)viewport[3]; // width : height
			float areaAspect = (float)area.Width / (float)area.Height;

			float a, b;
			if (windowAspect < areaAspect)
			{
				a = (float)viewport[2];
				b = (float)area.Width;
			}
			else
			{
				a = (float)viewport[3];
				b = (float)area.Height;
			}

			return distanceToHit * b / a;
		}

		private void translateCamera(int dX, int dY)
		{
			cameraChangedTimer.Enabled = false;

			if (cameraChangedDirection)
			{
				pointUnderCursorContext.Compute(scene, prevMouseLocation, true);
				cameraChangedDirection = false;
			}

			Vector3 translation = pointUnderCursorContext.GetTranslationVector(prevMouseLocation.X + dX, prevMouseLocation.Y + dY);
			scene.Camera.Move(translation);

			cameraChangedTimer.Start();
			if (InvalidateNeeded != null)
				InvalidateNeeded(this, EventArgs.Empty);
		}

		private void drawSelectionRectangle()
		{
			Rectangle area = getSelectionRectangle();

			Point p1 = area.Location;
			p1.Offset(1, 1);
			Point p2 = area.Location + area.Size;

			// --------------------------------------

			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Ortho(0, clientWindowSize.Width, clientWindowSize.Height, 0, 0, 1);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
			GL.LoadIdentity();

			GL.LineWidth(1.0f);
			GL.Disable(EnableCap.Lighting);
			GL.Enable(EnableCap.Blend);

			// pick color
			if (editorMode == EditorMode.ZoomWindow)
				GL.Color4(SceneFacade.ZOOM_RECTANGLE_COLOR);
			else if (editorMode == EditorMode.ScreenshotWindow)
				GL.Color4(SceneFacade.SCREENSHOT_RECTANGLE_COLOR);
			else
				GL.Color4(SceneFacade.SELECTION_RECTANGLE_COLOR);
			// ----------

			GL.Begin(BeginMode.Quads);
			{
				GL.Vertex2(p1.X, p1.Y);
				GL.Vertex2(p1.X, p2.Y);
				GL.Vertex2(p2.X, p2.Y);
				GL.Vertex2(p2.X, p1.Y);
			}
			GL.End();

			GL.Disable(EnableCap.Blend);

			GL.Begin(BeginMode.LineLoop);
			{
				GL.Vertex2(p1.X, p1.Y);
				GL.Vertex2(p1.X, p2.Y);
				GL.Vertex2(p2.X, p2.Y);
				GL.Vertex2(p2.X, p1.Y);
			}
			GL.End();

			GL.Enable(EnableCap.Lighting);

			GL.PopMatrix();

			GL.MatrixMode(MatrixMode.Projection); // znovu nastavit projekcni matici
			GL.PopMatrix();

			GL.MatrixMode(MatrixMode.Modelview);
		}

		private Rectangle getSelectionRectangle()
		{
			Point p = new Point(Math.Min(mouseDownLocation.X, prevMouseLocation.X), Math.Min(mouseDownLocation.Y, prevMouseLocation.Y));
			Size s = new Size(Math.Abs(prevMouseLocation.X - mouseDownLocation.X), Math.Abs(prevMouseLocation.Y - mouseDownLocation.Y));
			Rectangle area = new Rectangle(p, s);
			return Rectangle.Intersect(new Rectangle(Point.Empty, clientWindowSize), area); // jeste oriznout oknem
		}

		private void drawSignals()
		{
			Vector3 projectedPosition;
			if (scene.NodeSignal != null)
			{
				Vector3[] projectedPositions = scene.NodeSignalPositions.Select(pos => Scene.ProjectWorldCoordToWindowCoords(pos)).Where(proj => proj.Z < 1f).ToArray();
				drawSignal(Scene.SelectedNodeColor, projectedPositions);
			}
			if (scene.ElementSignal != null)
			{
				projectedPosition = Scene.ProjectWorldCoordToWindowCoords(scene.ElementSignalPosition);
				if (projectedPosition.Z < 1f)
					drawSignal(Scene.SelectedElementColor, projectedPosition);
			}
		}

		private void drawSignal(Color color, params Vector3[] windowPositions)
		{
			// --------------------------------------

			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Ortho(0, clientWindowSize.Width, clientWindowSize.Height, 0, 0, 1);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
			GL.LoadIdentity();

			GL.Disable(EnableCap.Lighting);

			// pick color
			GL.Color3(color);
			// ----------

			Debug.Assert(windowPositions != null);

			GL.Enable(EnableCap.LineStipple);
			GL.LineStipple(2, 52428);
			GL.LineWidth(2.0f);

			Vector2[] windowCorners = { new Vector2(), new Vector2(clientWindowSize.Width, 0f), new Vector2(0f, clientWindowSize.Height), new Vector2(clientWindowSize.Width, clientWindowSize.Height) };

			GL.Enable(EnableCap.LineSmooth);
			GL.Enable(EnableCap.Blend);
			GL.Begin(BeginMode.Lines);
			for (int i = 0; i < windowPositions.Length; i++)
			{
				Vector2 point = new Vector2(windowPositions[i].X, clientWindowSize.Height - windowPositions[i].Y);

				foreach (Vector2 corner in windowCorners.OrderBy(c => (c - point).LengthSquared).Take(2)) // take two nearest corners
				{
					GL.Vertex2(corner.X, corner.Y);
					GL.Vertex2(point.X, point.Y);
				}
			}
			GL.End();
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.LineSmooth);

			GL.Disable(EnableCap.LineStipple);

			GL.Enable(EnableCap.Lighting);

			GL.PopMatrix();

			GL.MatrixMode(MatrixMode.Projection); // znovu nastavit projekcni matici
			GL.PopMatrix();

			GL.MatrixMode(MatrixMode.Modelview);
		}

		#endregion

	}
}

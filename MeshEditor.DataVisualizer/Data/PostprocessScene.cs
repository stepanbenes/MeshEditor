using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Data;
using MeshEditor.Graphics;
using OpenTK;

namespace MeshEditor.DataVisualizer.Data
{
	public class PostprocessScene : IScene, IMultiLayerScene, IDisposable
	{
		#region Fields, constructors, dispose

		private Guid? selectedLayer;
		private Scene currentScene;
		private readonly Scene emptyScene;
		private readonly Dictionary<Guid, Scene> layerSceneMap;

		public PostprocessScene()
			: this(new Scene() /*dummy scene*/)
		{ }

		private PostprocessScene(Scene scene)
		{
			Debug.Assert(scene != null);
			emptyScene = scene;
			currentScene = emptyScene;
			layerSceneMap = new Dictionary<Guid, Scene>();
		}

		public void Dispose()
		{
			foreach (var scene in layerSceneMap.Values)
			{
				scene.Dispose();
			}
			emptyScene.Dispose();
			currentScene = null;
			selectedLayer = null;
			layerSceneMap.Clear();
		}

		#endregion

		#region Multi layer scene members

		public Vector3? PositionOffset { get; private set; }
		public float? ResizeFactor { get; private set; }

		public Guid? SelectedLayer
		{
			get { return selectedLayer; }
			set
			{
				if (selectedLayer != value)
				{
					selectedLayer = value;

					if (selectedLayer.HasValue)
					{
						currentScene = getOrCreateSceneFor(selectedLayer.Value);
					}
					else
					{
						currentScene = emptyScene;
					}
				}
			}
		}

		public IReadOnlyCollection<Guid> GetVisibleLayers() => (from pair in layerSceneMap
																where pair.Value.Mesh != null
																select pair.Key).ToArray();

		public void SetMeshForLayer(Guid layerId, Mesh newMesh)
		{
			bool isFirstMesh = newMesh != null && !containsAnyMesh();

			Scene scene = getOrCreateSceneFor(layerId);

			scene.SetMesh(newMesh);

			if (isFirstMesh)
			{
				scene.SetDefaultCameraView();
			}

			if (newMesh != null)
			{
				PositionOffset = newMesh.PositionOffset;
				ResizeFactor = newMesh.ResizeFactor;
			}
			else
			{
				if (!containsAnyMesh())
				{
					PositionOffset = null;
					ResizeFactor = null;
				}
			}
		}

		#endregion

		#region IScene overriden members

		IScene IScene.Copy()
		{
			var result = new PostprocessScene((Scene)emptyScene.Copy())
			{
				PositionOffset = PositionOffset,
				ResizeFactor = ResizeFactor,
			};

			foreach (var pair in layerSceneMap)
			{
				var sceneCopy = (Scene)pair.Value.Copy();
				sceneCopy.Camera = result.emptyScene.Camera;
				result.layerSceneMap.Add(pair.Key, sceneCopy);
			}

			result.SelectedLayer = this.SelectedLayer;

			return result;
		}



		void IScene.Draw(bool optimizeForMoving, bool optimizeForSelecting)
		{
			Debug.Assert(emptyScene.Mesh == null);

			foreach (var layerScene in enumerateAllScenesWithMesh()) // TODO: sort layer scenes: write outlines at the end because of blending
			{
				layerScene.Draw(optimizeForMoving, optimizeForSelecting);
			}

			emptyScene.DrawWithoutMesh(origin: (PositionOffset ?? Vector3.Zero) * -(ResizeFactor ?? 1f));
		}

		void IScene.ComputeVisibleNodes(Size clientWindow)
		{
			Action faceDrawer = null;

			foreach (var mesh in enumerateAllMeshes())
			{
				faceDrawer += mesh.DrawFacesOnly;
			}

			foreach (var layerScene in enumerateAllScenesWithMesh())
			{
				bool findVisibleFaces = ((layerScene.RenderMode & RenderMode.Faces) != 0) && layerScene.DrawElementNumbers;
				bool beamsRendered = layerScene.Mesh.BeamCount > 0 && layerScene.DrawBeams;
				bool findVisibleNodes = findVisibleFaces || ((layerScene.RenderMode & RenderMode.Points) != 0) || beamsRendered;

				if (findVisibleNodes)
				{
					bool xRay = (layerScene.RenderMode == RenderMode.None && beamsRendered) || layerScene.RenderMode == RenderMode.Points;
					layerScene.Mesh.CreateVisibleNodesList(new Rectangle(Point.Empty, clientWindow), layerScene.Camera, xRay, faceDrawer);
				}

				if (findVisibleFaces)
				{
					layerScene.Mesh.CreateVisibleFacesList(new Rectangle(Point.Empty, clientWindow), layerScene.Camera, faceDrawer);
				}
			}
		}

		bool IScene.DrawAxes
		{
			get { return emptyScene.DrawAxes; }
			set { emptyScene.DrawAxes = value; }
		}

		bool IScene.DrawAxisArrows
		{
			get { return emptyScene.DrawAxisArrows; }
			set { emptyScene.DrawAxisArrows = value; }
		}

		#endregion

		#region Private methods

		private Scene getOrCreateSceneFor(Guid layerId)
		{
			Scene scene;
			if (!layerSceneMap.TryGetValue(layerId, out scene))
			{
				scene = layerSceneMap[layerId] = new Scene()
				{
					Camera = emptyScene.Camera,
					DrawAxes = false,
					DrawAxisArrows = false
				};
			}
			return scene;
		}

		private bool containsAnyMesh() => layerSceneMap.Values.Where(scene => scene.Mesh != null).Any();

		private IEnumerable<Scene> enumerateAllScenesWithMesh() => layerSceneMap.Values.Where(scene => scene.Mesh != null);

		private IEnumerable<Mesh> enumerateAllMeshes() => enumerateAllScenesWithMesh().Select(scene => scene.Mesh);

		#endregion

		#region Public proxy properties

		Camera IScene.Camera
		{
			get { return currentScene.Camera; }
			set { currentScene.Camera = value; }
		}

		List<Node> IScene.CutPlaneDefinitionNodes => currentScene.CutPlaneDefinitionNodes;

		List<CutPlane> IScene.CutPlanes => currentScene.CutPlanes;

		bool IScene.DrawBeamNumbers
		{
			get { return currentScene.DrawBeamNumbers; }
			set { currentScene.DrawBeamNumbers = value; }
		}

		bool IScene.DrawBeams
		{
			get { return currentScene.DrawBeams; }
			set { currentScene.DrawBeams = value; }
		}

		bool IScene.DrawElementNumbers
		{
			get { return currentScene.DrawElementNumbers; }
			set { currentScene.DrawElementNumbers = value; }
		}

		bool IScene.DrawNodeNumbers
		{
			get { return currentScene.DrawNodeNumbers; }
			set { currentScene.DrawNodeNumbers = value; }
		}

		int? IScene.ElementSignal
		{
			get { return currentScene.ElementSignal; }
			set { currentScene.ElementSignal = value; }
		}

		Vector3 IScene.ElementSignalPosition => currentScene.ElementSignalPosition;

		CutInfo IScene.LastUsedCutInfo => currentScene.LastUsedCutInfo;

		Mesh IScene.Mesh => currentScene.Mesh;

		int[] IScene.NodeSignal
		{
			get { return currentScene.NodeSignal; }
			set { currentScene.NodeSignal = value; }
		}

		List<Vector3> IScene.NodeSignalPositions => currentScene.NodeSignalPositions;

		RenderMode IScene.RenderMode
		{
			get { return currentScene.RenderMode; }
			set { currentScene.RenderMode = value; }
		}

		#endregion

		#region Public proxy methods

		void IScene.AddPropertyToSelectedNodes(Property property) => currentScene.AddPropertyToSelectedNodes(property);

		void IScene.ClearPlaneDefinitionPoints() => currentScene.ClearPlaneDefinitionPoints();

		void IScene.CreateCutPlaneFromDefinitionPoints() => currentScene.CreateCutPlaneFromDefinitionPoints();

		void IScene.Cut(CutInfo cutInfo) => currentScene.Cut(cutInfo);

		SortedDictionary<Property, bool> IScene.GetElementPropertiesSorted() => currentScene.GetElementPropertiesSorted();

		string IScene.GetSelectedItemsDescription() => currentScene.GetSelectedItemsDescription();

		void IScene.HideSelectedElements() => currentScene.HideSelectedElements();

		void IScene.InvertSelection() => currentScene.InvertSelection();

		void IScene.PutNextPlaneDefinitionPoint(int x, int y) => currentScene.PutNextPlaneDefinitionPoint(x, y);

		void IScene.RecreateBuffers() => currentScene.RecreateBuffers();

		void IScene.RemovePropertyFromSelectedNodes(Property property) => currentScene.RemovePropertyFromSelectedNodes(property);

		void IScene.RestoreWholeMesh() => currentScene.RestoreWholeMesh();

		void IScene.SelectAllItems(EditorMode editorMode) => currentScene.SelectAllItems(editorMode);

		void IScene.SelectItems(Rectangle area, SelectMode mode, SelectOperationType opType, bool allVerticesInArea, ItemTypeToSelect itemType)
			=> currentScene.SelectItems(area, mode, opType, allVerticesInArea, itemType);

		void IScene.SelectItemsIncidingWithFaces() => currentScene.SelectItemsIncidingWithFaces();

		void IScene.SelectItemsWithProperty(EditorMode editorMode, Property property, bool addToSelection) => currentScene.SelectItemsWithProperty(editorMode, property, addToSelection);

		void IScene.SetDefaultCameraView() => currentScene.SetDefaultCameraView();

		void IScene.SetPropertyOfSelectedItems(Property property) => currentScene.SetPropertyOfSelectedItems(property);

		void IScene.UnselectAllItems() => currentScene.UnselectAllItems();

		void IScene.UpdateLastUsedCut() => currentScene.UpdateLastUsedCut();

		void IScene.SetMesh(Mesh newMesh)
		{
			throw new NotSupportedException("Use MultiLayerScene.SetMeshForLayer method instead.");
		}

		#endregion
	}
}

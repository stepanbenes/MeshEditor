using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.Construction;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.IO;
using MeshEditor.Graphics;
using MeshEditor.IO;
using MeshEditor.LayerManager.Data;
using MeshEditor.SolutionManager;
using OpenTK;

namespace MeshEditor.DataVisualizer.Data
{
	public class PostprocessScene : IScene, IMultiLayerScene, IDisposable
	{
		#region Fields, constructors, dispose

		private readonly Scene emptyScene;
		private readonly Dictionary<Guid, Scene> layerSceneMap;
		private readonly HashSet<Guid> visibleLayers;

		private Guid? selectedLayer;
		private Scene currentScene;

		private Vector3? positionOffset;
		private float? resizeFactor;

		public PostprocessScene()
			: this(new Scene() /*empty scene*/)
		{ }

		private PostprocessScene(Scene emptyScene)
		{
			Debug.Assert(emptyScene != null);
			this.emptyScene = emptyScene;
			currentScene = this.emptyScene;
			layerSceneMap = new Dictionary<Guid, Scene>();
			visibleLayers = new HashSet<Guid>();
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

		#region Postprocess members

		public string ProjectName { get; set; }

		public void RemoveMeshFromLayer(Guid layerId)
		{
			IScene scene = getOrCreateSceneFor(layerId);
			setMeshForLayer(scene, null);
		}

		public void RemoveMeshFromAllUncheckedLayers()
		{
			foreach (var layerId in layerSceneMap.Keys)
			{
				if (!visibleLayers.Contains(layerId))
				{
					RemoveMeshFromLayer(layerId);
				}
			}
		}

		public async Task<IDataVisualizerController> UpdateLayerAsync(SolutionHub solutionHub, Guid layerId, string layerName, DataSelection newDataSelection, Action<string, int> progressReport, CancellationToken cancellationToken)
		{
			Debug.Assert(newDataSelection != null);
			IScene scene = getOrCreateSceneFor(layerId);
			var dataVisualizer = scene.Mesh?.GetDataVisualizer() as LayerDataVisualizer;

			if (dataVisualizer == null || newDataSelection.Mesh.Index != dataVisualizer.DataSelection?.Mesh.Index)
			{
				var geometry = await reloadMeshAsync(scene, solutionHub, layerId, layerName, newDataSelection, progressReport, cancellationToken);
				Debug.Assert(scene.Mesh != null);
				dataVisualizer = new LayerDataVisualizer(geometry, dataVisualizer?.Settings);
				scene.Mesh.SetDataVisualizer(dataVisualizer);
			}

			Dictionary<decimal, ComponentDataDescription> scalarComponentsTimeStepMap = null;
			// scalars
			{
				if (dataVisualizer.DataSelection?.ScalarDataIndex != newDataSelection.ScalarDataIndex)
				{
					if (!newDataSelection.ScalarDataIndex.HasValue)
					{
						scalarComponentsTimeStepMap = new Dictionary<decimal, ComponentDataDescription>();
					}
					else
					{
						progressReport?.Invoke($"Loading {newDataSelection.FieldName}/{newDataSelection.ComponentName}", -1);
						var componentList = await solutionHub.LoadDataAsync(layerId, newDataSelection.ScalarDataIndex.Value, cancellationToken);
						scalarComponentsTimeStepMap = componentList.ToDictionary(d => d.TimeStep);
					}
				}
			}

			ILookup<decimal, ComponentDataDescription> vectorComponentsTimeStepMap = null;
			// vectors
			{
				if (dataVisualizer.DataSelection?.VectorDataIndex != newDataSelection.VectorDataIndex)
				{
					if (!newDataSelection.VectorDataIndex.HasValue)
					{
						vectorComponentsTimeStepMap = Enumerable.Empty<ComponentDataDescription>().ToLookup(c => c.TimeStep);
					}
					else
					{
						progressReport?.Invoke($"Loading {newDataSelection.VectorFieldName}", -1);
						var componentList = await solutionHub.LoadDataAsync(layerId, newDataSelection.VectorDataIndex.Value.AllIndices(), cancellationToken);
						vectorComponentsTimeStepMap = componentList.ToLookup(d => d.TimeStep);
					}
				}
			}

			dataVisualizer.UpdateDataSelection(newDataSelection, scalarComponentsTimeStepMap, vectorComponentsTimeStepMap);

			return dataVisualizer;
		}

		#endregion

		#region Multi layer scene members

		Vector3? IMultiLayerScene.PositionOffset => positionOffset;
		float? IMultiLayerScene.ResizeFactor => resizeFactor;

		Guid? IMultiLayerScene.SelectedLayer
		{
			get { return selectedLayer; }
			set
			{
				setSelectedLayer(value);
			}
		}

		ICollection<Guid> IMultiLayerScene.GetVisibleLayers() => visibleLayers;

		#endregion

		#region IScene overriden members

		IScene IScene.Copy()
		{
			var result = new PostprocessScene((Scene)emptyScene.Copy())
			{
				positionOffset = positionOffset,
				resizeFactor = resizeFactor,
				ProjectName = ProjectName
			};

			// copy layerSceneMap
			foreach (var pair in layerSceneMap)
			{
				var sceneCopy = (Scene)pair.Value.Copy();
				sceneCopy.Camera = result.emptyScene.Camera;
				result.layerSceneMap.Add(pair.Key, sceneCopy);
			}

			// copy visibleLayers
			foreach (var visibleLayer in visibleLayers)
			{
				result.visibleLayers.Add(visibleLayer);
			}

			// set selectedLayer and currentScene
			result.setSelectedLayer(this.selectedLayer);

			return result;
		}

		void IScene.Draw(bool optimizeForMoving, bool optimizeForSelecting, bool drawDecorations/*ignored*/)
		{
			Debug.Assert(emptyScene.Mesh == null);

			foreach (var layerScene in enumerateAllScenesWithMeshOrdered())
			{
				layerScene.Draw(optimizeForMoving, optimizeForSelecting, drawDecorations: layerScene == currentScene);
			}

			emptyScene.DrawWithoutMesh(origin: (positionOffset ?? Vector3.Zero) * -(resizeFactor ?? 1f));
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

		string IScene.Title => ProjectName ?? "[Untitled project]"; // ignore layer name
																	//{
																	//	get
																	//	{
																	//		var projectName = ProjectName ?? "[Untitled project]";
																	//		var meshTitle = currentScene.Title;
																	//		if (string.IsNullOrEmpty(meshTitle))
																	//			return projectName;
																	//		return projectName + " - " + meshTitle;
																	//	}
																	//}

		public bool ContainsMeshWithIdentifier(int meshIdentifier) => enumerateAllMeshes().Any(m => m.UniqueIdentifier == meshIdentifier);

		#endregion

		#region Private methods

		private async Task<GeometryDescription> reloadMeshAsync(IScene scene, SolutionHub solutionHub, Guid layerId, string layerName, DataSelection newDataSelection, Action<string, int> progressReport, CancellationToken cancellationToken)
		{
			progressReport?.Invoke("Loading geometry", -1);
			var geometry = await solutionHub.LoadGeometryAsync(layerId, newDataSelection.Mesh.Index, cancellationToken);

			AttributeDescription elementPropertyAttribute = await loadAttributeAsync(AttributeDescription.KnownAttributeNames.ElementProperty, newDataSelection.Mesh, solutionHub, layerId, cancellationToken);

			{
				IMeshCreator meshCreator = new MeshConstructor();
				if (progressReport != null)
				{
					meshCreator.Step += (s, e) => progressReport(e.OperationName, e.PercentDone);
				}

				Mesh createdMesh;
				using (var meshFileParser = new LayerMeshFileParser(layerName, geometry, elementPropertyAttribute))
				{
					createdMesh = await Task.Run(() => meshCreator.CreateMesh(meshFileParser, cancelled: () => cancellationToken.IsCancellationRequested, defaultPositionOffset: positionOffset, defaultResizeFactor: resizeFactor));
					cancellationToken.ThrowIfCancellationRequested();
				}

				Debug.Assert(createdMesh != null);

				setMeshForLayer(scene, createdMesh);
				createdMesh?.CreateBuffers();
			}

			return geometry;
		}

		private static async Task<AttributeDescription> loadAttributeAsync(string attributeName, IMeshFileDescriptor mesh, SolutionHub solutionHub, Guid layerId, CancellationToken cancellationToken)
		{
			int? attributeIndex = mesh.Attributes.FirstOrDefault(a => a.FieldName == attributeName)?.Index;
			if (attributeIndex.HasValue)
			{
				return await solutionHub.LoadAttributeAsync(layerId, attributeIndex.Value, cancellationToken);
			}
			return null;
		}

		private void setMeshForLayer(IScene scene, Mesh newMesh)
		{
			bool isFirstMesh = newMesh != null && !containsAnyMesh();

			scene.SetMesh(newMesh);

			if (isFirstMesh)
			{
				scene.SetDefaultCameraView();
			}

			if (newMesh != null)
			{
				positionOffset = newMesh.PositionOffset;
				resizeFactor = newMesh.ResizeFactor;
			}
			else
			{
				if (!containsAnyMesh())
				{
					positionOffset = null;
					resizeFactor = null;
				}
			}
		}

		private void setSelectedLayer(Guid? value)
		{
			if (selectedLayer != value)
			{
				selectedLayer = value;
				currentScene = (selectedLayer.HasValue) ? getOrCreateSceneFor(selectedLayer.Value) : emptyScene;
				//onCurrentSceneChanged();
			}
		}

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

		/// <summary>
		/// Order layer scenes: write outlines at the end because of blending.
		/// </summary>
		private IEnumerable<Scene> enumerateAllScenesWithMeshOrdered() => enumerateAllScenesWithMesh().OrderBy(scene => scene.RenderMode == RenderMode.AllLines || scene.RenderMode == RenderMode.BorderLines);

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

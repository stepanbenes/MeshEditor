using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Data
{
	public class MultiLayerScene : IScene, IDisposable
	{
		#region Multi layer scene main members

		Guid? selectedLayer;
		Scene currentScene;
		readonly Scene emptyScene;
		readonly Dictionary<Guid, Scene> layerSceneMap;

		public MultiLayerScene()
			: this(new Scene() /*dummy scene*/)
		{ }

		private MultiLayerScene(Scene scene)
		{
			Debug.Assert(scene != null);
			emptyScene = scene;
			currentScene = emptyScene;
			layerSceneMap = new Dictionary<Guid, Scene>();
		}

		public IScene Copy()
		{
			var result = new MultiLayerScene((Scene)emptyScene.Copy())
			{
				PositionOffset = PositionOffset,
				ResizeFactor = ResizeFactor,
			};

			foreach (var pair in layerSceneMap)
			{
				result.layerSceneMap.Add(pair.Key, (Scene)pair.Value.Copy());
			}

			// TODO: set currentScene

			return result;
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
						if (!layerSceneMap.TryGetValue(selectedLayer.Value, out currentScene))
						{
							var newScene = new Scene()
							{
								Camera = emptyScene.Camera,
								DrawAxes = false,
								DrawAxisArrows = false
							};
							currentScene = layerSceneMap[selectedLayer.Value] = newScene;
						}
					}
					else
					{
						currentScene = emptyScene;
					}
				}
			}
		}

		public void SetMesh(Mesh newMesh)
		{
			Debug.Assert(currentScene != emptyScene);
			bool isFirstMesh = newMesh != null && !containsAnyMesh();

			currentScene.SetMesh(newMesh);

			if (isFirstMesh)
			{
				currentScene.SetDefaultCameraView();
			}

			PositionOffset = PositionOffset ?? newMesh?.PositionOffset;
			ResizeFactor = ResizeFactor ?? newMesh?.ResizeFactor;
		}

		public void Draw(bool optimizeForMoving, bool optimizeForSelecting)
		{
			Debug.Assert(emptyScene.Mesh == null);
			
			foreach (var layerScene in enumerateAllScenesWithMesh()) // TODO: sort layer scenes: write outlines at the end because of blending
			{
				layerScene.Draw(optimizeForMoving, optimizeForSelecting);
			}

			emptyScene.DrawWithoutMesh(PositionOffset ?? Vector3.Zero, ResizeFactor ?? 1f);
		}

		public void ComputeVisibleNodes(Size clientWindow)
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

		public bool DrawAxes
		{
			get { return emptyScene.DrawAxes; }
			set { emptyScene.DrawAxes = value; }
		}

		public bool DrawAxisArrows
		{
			get { return emptyScene.DrawAxisArrows; }
			set { emptyScene.DrawAxisArrows = value; }
		}

		public Vector3? PositionOffset { get; private set; }

		public float? ResizeFactor { get; private set; }

		#endregion

		#region Private methods

		private bool containsAnyMesh()
		{
			return layerSceneMap.Values.Where(scene => scene.Mesh != null).Any();
		}

		private IEnumerable<Scene> enumerateAllScenesWithMesh()
		{
			return layerSceneMap.Values.Where(scene => scene.Mesh != null);
		}

		private IEnumerable<Mesh> enumerateAllMeshes()
		{
			return enumerateAllScenesWithMesh().Select(scene => scene.Mesh);
		}

		//private void updateCentersAndRadiaOfAllMeshes()
		//{
		//	Vector3 lowerBound = -Vector3.One, upperBound = Vector3.One;
		//	bool any = false;

		//	foreach (var mesh in enumerateAllMeshes())
		//	{
		//		if (any)
		//		{
		//			Construction.MeshConstructor.UpdateBounds(mesh.LowerBound, ref lowerBound, ref upperBound);
		//			Construction.MeshConstructor.UpdateBounds(mesh.UpperBound, ref lowerBound, ref upperBound);
		//		}
		//		else
		//		{
		//			lowerBound = mesh.LowerBound;
		//			upperBound = mesh.UpperBound;
		//			any = true;
		//		}
		//	}

		//	if (any)
		//	{
		//		Vector3 centerOfRotation = Utilities.Functions.GetCenterOfLineSegment(ref lowerBound, ref upperBound);
		//		float radius = (upperBound - lowerBound).Length * 0.5f;
		//		foreach (var mesh in enumerateAllMeshes())
		//		{
		//			mesh.CenterOfRotation = centerOfRotation;
		//			mesh.Radius = radius;
		//		}
		//	}
		//}

		#endregion

		#region Properties

		public Camera Camera
		{
			get
			{
				return currentScene.Camera;
			}
			set
			{
				currentScene.Camera = value;
			}
		}

		public List<Node> CutPlaneDefinitionNodes
		{
			get
			{
				return currentScene.CutPlaneDefinitionNodes;
			}
		}

		public List<CutPlane> CutPlanes
		{
			get
			{
				return currentScene.CutPlanes;
			}
		}

		public bool DrawBeamNumbers
		{
			get
			{
				return currentScene.DrawBeamNumbers;
			}
			set
			{
				currentScene.DrawBeamNumbers = value;
			}
		}

		public bool DrawBeams
		{
			get
			{
				return currentScene.DrawBeams;
			}
			set
			{
				currentScene.DrawBeams = value;
			}
		}

		public bool DrawElementNumbers
		{
			get
			{
				return currentScene.DrawElementNumbers;
			}
			set
			{
				currentScene.DrawElementNumbers = value;
			}
		}

		public bool DrawNodeNumbers
		{
			get
			{
				return currentScene.DrawNodeNumbers;
			}
			set
			{
				currentScene.DrawNodeNumbers = value;
			}
		}

		public int? ElementSignal
		{
			get
			{
				return currentScene.ElementSignal;
			}
			set
			{
				currentScene.ElementSignal = value;
			}
		}

		public Vector3 ElementSignalPosition
		{
			get
			{
				return currentScene.ElementSignalPosition;
			}
		}

		public CutInfo LastUsedCutInfo
		{
			get
			{
				return currentScene.LastUsedCutInfo;
			}
		}

		public Mesh Mesh
		{
			get
			{
				return currentScene.Mesh;
			}
		}

		public int[] NodeSignal
		{
			get
			{
				return currentScene.NodeSignal;
			}
			set
			{
				currentScene.NodeSignal = value;
			}
		}

		public List<Vector3> NodeSignalPositions
		{
			get
			{
				return currentScene.NodeSignalPositions;
			}
		}

		public RenderMode RenderMode
		{
			get
			{
				return currentScene.RenderMode;
			}
			set
			{
				currentScene.RenderMode = value;
			}
		}

		#endregion

		public void AddPropertyToSelectedNodes(Property property)
		{
			currentScene.AddPropertyToSelectedNodes(property);
		}

		public void ClearPlaneDefinitionPoints()
		{
			currentScene.ClearPlaneDefinitionPoints();
		}

		public void CreateCutPlaneFromDefinitionPoints()
		{
			currentScene.CreateCutPlaneFromDefinitionPoints();
		}

		public void Cut(CutInfo cutInfo)
		{
			currentScene.Cut(cutInfo);
		}

		public SortedDictionary<Property, bool> GetElementPropertiesSorted()
		{
			return currentScene.GetElementPropertiesSorted();
		}

		public string GetSelectedItemsDescription()
		{
			return currentScene.GetSelectedItemsDescription();
		}

		public void HideSelectedElements()
		{
			currentScene.HideSelectedElements();
		}

		public void InvertSelection()
		{
			currentScene.InvertSelection();
		}

		public void PutNextPlaneDefinitionPoint(int x, int y)
		{
			currentScene.PutNextPlaneDefinitionPoint(x, y);
		}

		public void RecreateBuffers()
		{
			currentScene.RecreateBuffers();
		}

		public void RemovePropertyFromSelectedNodes(Property property)
		{
			currentScene.RemovePropertyFromSelectedNodes(property);
		}

		public void RestoreWholeMesh()
		{
			currentScene.RestoreWholeMesh();
		}

		public void SelectAllItems(EditorMode editorMode)
		{
			currentScene.SelectAllItems(editorMode);
		}

		public void SelectItems(Rectangle area, SelectMode mode, SelectOperationType opType, bool allVerticesInArea, ItemTypeToSelect itemType)
		{
			currentScene.SelectItems(area, mode, opType, allVerticesInArea, itemType);
		}

		public void SelectItemsIncidingWithFaces()
		{
			currentScene.SelectItemsIncidingWithFaces();
		}

		public void SelectItemsWithProperty(EditorMode editorMode, Property property, bool addToSelection)
		{
			currentScene.SelectItemsWithProperty(editorMode, property, addToSelection);
		}

		public void SetDefaultCameraView()
		{
			currentScene.SetDefaultCameraView();
		}

		public void SetPropertyOfSelectedItems(Property property)
		{
			currentScene.SetPropertyOfSelectedItems(property);
		}

		public void UnselectAllItems()
		{
			currentScene.UnselectAllItems();
		}

		public void UpdateLastUsedCut()
		{
			currentScene.UpdateLastUsedCut();
		}
	}
}

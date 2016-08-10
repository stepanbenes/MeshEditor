using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Graphics;
using OpenTK;

namespace MeshEditor.Data
{
	public class MultiScene : IScene, IDisposable
	{
		#region Fields, construction, destruction

		Scene currentScene;

		public MultiScene()
			: this(new Scene())
		{ }

		private MultiScene(Scene scene)
		{
			currentScene = scene;
		}

		public IScene Copy()
		{
			return new MultiScene((Scene)currentScene.Copy());
		}

		public void Dispose()
		{
			currentScene.Dispose();
		}

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

		public bool DrawAxes
		{
			get
			{
				return currentScene.DrawAxes;
			}
			set
			{
				currentScene.DrawAxes = value;
			}
		}

		public bool DrawAxisArrows
		{
			get
			{
				return currentScene.DrawAxisArrows;
			}
			set
			{
				currentScene.DrawAxisArrows = value;
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

		public void Draw(bool optimizeForMoving, bool optimizeForSelecting)
		{
			currentScene.Draw(optimizeForMoving, optimizeForSelecting);
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

		public void SetMesh(Mesh newMesh)
		{
			currentScene.SetMesh(newMesh);
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

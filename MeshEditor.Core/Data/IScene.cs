using System;
using System.Collections.Generic;
using System.Drawing;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using MeshEditor.Construction;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace MeshEditor.Data
{
	public interface IScene : IDisposable
	{
		#region Properties, access

		Mesh Mesh { get; }

		Camera Camera { get; set; }

		bool DrawAxes { get; set; }

		bool DrawAxisArrows { get; set; }

		bool DrawNodeNumbers { get; set; }

		bool DrawElementNumbers { get; set; }

		bool DrawBeamNumbers { get; set; }

		bool DrawBeams { get; set; }

		RenderMode RenderMode { get; set; }

		void SetMesh(Mesh newMesh);

		List<CutPlane> CutPlanes { get; }

		List<Node> CutPlaneDefinitionNodes { get; }

		int[] NodeSignal { get; set; }

		int? ElementSignal { get; set; }

		List<Vector3> NodeSignalPositions { get; }

		Vector3 ElementSignalPosition { get; }

		CutInfo LastUsedCutInfo { get; }

		#endregion

		#region Misc - public methods

		void SetPropertyOfSelectedItems(Property property);

		void AddPropertyToSelectedNodes(Property property);

		void RemovePropertyFromSelectedNodes(Property property);

		SortedDictionary<Property, bool> GetElementPropertiesSorted();

		IScene Copy();

		void RecreateBuffers();

		void SetDefaultCameraView();

		void ComputeVisibleNodes(Size clientWindow);

		#endregion

		#region Drawing - public methods

		void Draw(bool optimizeForMoving, bool optimizeForSelecting);

		#endregion

		#region Selection - public methods

		string GetSelectedItemsDescription();

		void SelectItems(Rectangle area, SelectMode mode, SelectOperationType opType, bool allVerticesInArea, ItemTypeToSelect itemType);

		void UnselectAllItems();

		void SelectAllItems(EditorMode editorMode);

		void InvertSelection();

		void SelectItemsIncidingWithFaces();

		void SelectItemsWithProperty(EditorMode editorMode, Property property, bool addToSelection);

		#endregion

		#region Cutting - public methods

		void PutNextPlaneDefinitionPoint(int x, int y);

		void ClearPlaneDefinitionPoints();

		void UpdateLastUsedCut();

		void Cut(CutInfo cutInfo);

		void CreateCutPlaneFromDefinitionPoints();

		void HideSelectedElements();

		void RestoreWholeMesh();

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// vsechny mozne akce podporovane funkci PerformAction tridy SceneFacade
	/// </summary>
	public enum AvailableAction
	{
		Nothing,
		LineSmooth,
		PointSmooth,
		FaceLighting,
		EdgeLighting,
		XRayVision,
		InvertAllNormals,
		ChangeRenderMode,
		CameraReset,
		CameraStandardView,
		UnselectAllItems,
		SelectAllItems,
		InvertSelection,
		SelectIncidingItems,
		SelectItemsWithProperty,
		SelectItemsWithPropertyAdd,
		Refresh,
		UpdateColorBuffers,
		UpdateVisibleNodes,
		CutMesh,
		RestoreMesh,
		CreateCutPlane,
		ClearPlaneDefinitionPoints,
		DeleteSelectedElements,
		Storno,
		ZoomToFit,
		RecreateBuffers,
		DeleteHiddenItems,
		AddPropertyToSelectedNodes,
		RemovePropertyFromSelectedNodes,
		SignalNode,
		SignalElement,
		ClearSignalNode,
		ClearSignalElement,
		UpdateNodeCoordinates,
		Redraw,
		UpdateElementsCutByValueLimit
	}
}

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
		Undo,
		Redo,
		ClearHistory,
		ChangeHistoryCapacity,
		AddPropertyToSelectedNodes,
		RemovePropertyFromSelectedNodes,
		SignalNode,
		SignalElement,
		ClearSignalNode,
		ClearSignalElement
	}
}

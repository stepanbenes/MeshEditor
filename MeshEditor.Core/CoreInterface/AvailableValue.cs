using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// vsechny mozne hodnoty mozne precist ci nastavit funkcemi GetValue resp. SetValue tridy SceneFacade
	/// </summary>
	public enum AvailableValue
	{
		MeshShadingModel, // OpenTK.ShadingModel
		MeshStatistics, // MeshEditor.Data.MeshStatistics
		VBOSupported, // bool
		OrdinaryEdgeColor, // System.Drawing.Color
		FaceColor, // System.Drawing.Color
		ElementCount, // int
		FaceCount, // int
		EdgeCount, // int
		NodeCount, // int
		BeamCount, // int
		DrawAxes, // bool
		DrawAxisArrows, // bool
		DrawNodeNumbers, // bool
		DrawElementNumbers, // bool
		SelectedItemsDescription, // string
		MinimalElementRadius, // int
		MeshElementPropertiesSorted, // Property[] //(Sorted array)
		AllMeshPropertiesSorted, // Property[] //(Sorted array)
		RenderMode, // PropertyColorsMode
		ColorMode, // RenderMode
		Status, // string
		DrawQuadraticNodes, // bool
		MeshHasHiddenElements, // bool
		UnsavedChangesInMesh, // bool
		DrawBeams, // bool
		NodeSignalIsSet, // bool
		ElementSignalIsSet, // bool
		DataVisualizer, // MeshEditor.CoreInterface.IDataVisualizer
		LayerList, // IList<MeshEditor.CoreInterface.ILayer>
		MeshDimensions, // OpenTK.Vector3
		LastUsedCutInfo // Cuts.CutInfo
	}
}

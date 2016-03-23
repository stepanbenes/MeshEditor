using MeshEditor.Data;
using MeshEditor.Graphics;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// Interface that defines data visualization object stored in Mesh class.
	/// Used for postprocessing features.
	/// </summary>
	public interface IDataVisualizer : IDisposable
	{

		/// <summary>
		/// Initializes Data Visualizer using Mesh object.
		/// (e.g. creates octree data structure from element locations in mesh)
		/// </summary>
		/// <param name="mesh">Mesh that is used for initialization</param>
		void Initialize(Mesh mesh);

		/// <summary>
		/// Loads result data stored in file specified by parameter.
		/// </summary>
		/// <param name="approximationParameters">Parameters describing order of approximation, octree creation etc.</param>
		/// <param name="filenames">Path to result files with data to be loaded.</param>
		/// <param name="longOpNotifier">Object that notifies UI about progress in loading data and handles cancellation.</param>
		void LoadData(IApproximationParameters approximationParameters, string[] filenames, LongOpNotifier longOpNotifier);

		/// <summary>
		/// Sequence of already loaded filenames.
		/// </summary>
		IEnumerable<string> LoadedFiles { get; }

		/// <summary>
		/// Completes creation of Data visualizer object.
		/// It is supposed to be called after LoadData() method from the UI thread.
		/// It should set default values of DataVisualizerController settings.
		/// </summary>
		void FinishUp();

		/// <summary>
		/// Returns True if showing of scalars is enabled - that means drawing of data values as colors on mesh surface can be performed.
		/// Returns False if drawing colors on mesh surface is not allowed.
		/// </summary>
		bool DisplayColors { get; }

		/// <summary>
		/// Init drawing of data as colors on mesh entities.
		/// </summary>
		/// <param name="lightingEnabled">Specifies wether to turn on lights in visualized data (specifically in shaders)</param>
		void BeginDraw(bool lightingEnabled);

		/// <summary>
		/// Apply operations needed to finish drawing of data.
		/// </summary>
		void EndDraw();

		/// <summary>
		/// Draws additional decorations of data visualizer (e.g. octree structure, ...).
		/// </summary>
		/// <param name="propertyColorsMode">Flags indicating which entites are drawn with colors according to their properties.</param>
		void DrawItems(PropertyColorsMode propertyColorsMode);

		/// <summary>
		/// Returns color in RGBA32 format that corresponds to data value at specified position.
		/// </summary>
		/// <param name="position">Position of point where color should be displayed</param>
		/// <returns>color in RGBA32 format</returns>
		//int GetDataColor(Vector3 position);

		/// <summary>
		/// Returns color in RGBA32 format that corresponds to data value on specified node.
		/// </summary>
		/// <param name="node">node with value</param>
		/// <param name="element">element containing node <paramref name="node"/></param>
		/// <returns>color in RGBA32 format</returns>
		int GetDataColor(Node node, Element element);

		/// <summary>
		/// Returns current data on specified node.
		/// </summary>
		/// <param name="node">node with value</param>
		/// <returns>current data value</returns>
		double GetDataValue(Node node);

		/// <summary>
		/// Returns color of specified data value.
		/// </summary>
		int GetColorForDataValue(double dataValue);

		/// <summary>
		/// Returns current data on specified node.
		/// </summary>
		/// <param name="node">node with value</param>
		/// <param name="maxError">maximal error of returned value</param>
		/// <returns>current data value</returns>
		double GetDataValue(Node node, out float maxError);

		/// <summary>
		/// Returns maximum data value of current ScalarDataIndex in whole mesh.
		/// </summary>
		double GetMaximumDataValue();

		/// <summary>
		/// Returns minimum data value of current ScalarDataIndex in whole mesh.
		/// </summary>
		double GetMinimumDataValue();

		/// <summary>
		/// Returns ids of nodes (one or more) that have the maximum data value (currently set).
		/// </summary>
		int[] GetEntitiesWithMaximumDataValue();

		/// <summary>
		/// Returns ids of nodes (one or more) that have the minimum data value (currently set).
		/// </summary>
		int[] GetEntitiesWithMinimumDataValue();

		/// <summary>
		/// Returns object describing parameters of currently used approximation method.
		/// </summary>
		/// <param name="longOpNotifier">Object that notifies UI about progress in loading data and handles cancellation.</param>
		ApproximationQuality GetApproximationQuality(LongOpNotifier longOpNotifier);

	}

	public class ApproximationQuality
	{
		//public float MaxAbsoluteError { get; set; }
		public float MaxRelativeError { get; set; }
		public float AverageRelativeError { get; set; }
		//public float CurrentDataMaxAbsoluteError { get; set; }
		public float CurrentDataMaxRelativeError { get; set; }
		public float CurrentDataAverageRelativeError { get; set; }

		public long MemoryConsumption { get; set; }
		public float CompressionRatio { get; set; }

		public override string ToString()
		{
			return string.Format("Max error: {0:G4}% Current data max error: {1:G4}% Avg error: {2:G4}% Current data avg error: {3:G4}% Memory consumption: {4}B Compression ratio: {5:G3}%", MaxRelativeError * 100.0f, CurrentDataMaxRelativeError * 100.0f, AverageRelativeError * 100.0f, CurrentDataAverageRelativeError * 100.0f, MemoryConsumption, CompressionRatio * 100.0f);
		}
	}

	public enum GaussPointsExtrapolationStrategy
	{
		NearestGaussPoint,
		//Variational,
		//Lumped
	}

	public interface IApproximationParameters
	{
		bool LoadInternalEntities { get; }
		bool CompressTime { get; }
		GaussPointsExtrapolationStrategy GPExptrapolationStrategy { get; }
	}

}

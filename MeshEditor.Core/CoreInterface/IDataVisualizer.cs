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
		/// Returns current data on specified element node.
		/// </summary>
		double GetDataValue(Node node, Element element);

		/// <summary>
		/// Returns current data on specified node.
		/// </summary>
		/// <param name="node">node with value</param>
		/// <param name="error">maximal error of returned value</param>
		/// <returns>current data value</returns>
		double GetDataValue(Node node, out double error);

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
		int[] GetIDsOfNodesWithMaximumDataValue();

		/// <summary>
		/// Returns ids of nodes (one or more) that have the minimum data value (currently set).
		/// </summary>
		int[] GetIDsOfNodesWithMinimumDataValue();
	}
}

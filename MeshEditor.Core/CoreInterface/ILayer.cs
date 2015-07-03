using MeshEditor.Data;
using MeshEditor.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// Interface describing group of logically related entities. Each layer can be turn on or off or its display properties can be set (transparency, DrawMode, ...)
	/// Examples of layers: Cross-section, Elements with property #, Elements of specified type, (Macro-elements), Elements of specified dimension (1D, 2D, 3D), ...
	/// </summary>
	public interface ILayer : IDisposable
	{
		/// <summary>
		/// Name of layer to be displayed in UI
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Gets or Sets whether to display this layer
		/// </summary>
		bool Visible { get; set; }

		/// <summary>
		/// Gets or Sets display style of this layer. Wheter to draw faces, edges, points, etc.
		/// </summary>
		RenderMode DisplayStyle { get; set; }

		/// <summary>
		/// Recreates layer geometry and/or repaints layer colors (creates and fills vertex buffer objects with data) according to GeometryChanged and ColorsChanged flags.
		/// </summary>
		/// <param name="mesh">Underlying mesh object</param>
		/// <param name="dataVisualizer">object providing data in mesh nodes</param>
		/// <param name="elementPropertyColors">flag indicating whether to draw property colors of elements in current layer</param>
		void Update(Mesh mesh, IDataVisualizer dataVisualizer, bool elementPropertyColors);

		/// <summary>
		/// Sets flag indicating whether is needed to recreate layer geometry
		/// </summary>
		bool GeometryChanged { set; }

		/// <summary>
		/// Sets flag indicating whether is needed to repaint layer colors
		/// </summary>
		bool ColorsChanged { set; }

		/// <summary>
		/// Gets value of flag indicating, whether is needed to call Update() method that recreates layer geometry and/or colors.
		/// </summary>
		bool UpdateNeeded { get; }

		/// <summary>
		/// Draws layer on the screen
		/// </summary>
		/// <param name="dataVisualizer">data visualizer object used for turning on render shaders (e.g. iso-areas rendering)</param>
		/// <param name="defaultRenderMode">RenderMode that is used for drawing entire mesh. This is used, when the layer's DisplayStyle is set to None.</param>
		/// <param name="elementPropertyColors">flag indicating whether to draw property colors of elements in current layer</param>
		void Draw(IDataVisualizer dataVisualizer, RenderMode defaultDisplayStyle, bool elementPropertyColors);

		/// <summary>
		/// Raises when geometry is updated in the background worker thread to inform UI about the need to redraw scene.
		/// </summary>
		event EventHandler RedrawNeeded;
	}
}

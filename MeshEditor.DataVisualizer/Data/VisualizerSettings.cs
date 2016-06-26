using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public interface IVisualizerSettings
	{
		/// <summary>
		/// Flag that turns on or off data visualization of scalar values as colors on mesh surface.
		/// </summary>
		bool ShowScalars { get; set; }

		/// <summary>
		/// Flag that turns on or off data visualization of vector values as arrows in nodes.
		/// </summary>
		//bool ShowVectors { get; set; }

		/// <summary>
		/// Gets or Sets multiplier factor of vector arrows lengths.
		/// </summary>
		//double VectorLengthFactor { get; set; }

		/// <summary>
		/// Gets or Sets flag indicating whether to invert vector arrows.
		/// </summary>
		//bool MoveEndOfArrowsToNodes { get; set; }

		/// <summary>
		/// Gets or Sets flag indicating whether to draw legend of color scale in the window border.
		/// If DisplayStyle does not contain Scalars flag, this property is ignored.
		/// </summary>
		bool ShowColorScaleLegend { get; set; }

		/// <summary>
		/// Color scale object used to modify displayed range of values and colors assigned to values.
		/// </summary>
		ColorScale ColorScale { get; set; }

		/// <summary>
		/// Show iso areas when drawing colors.
		/// </summary>
		bool DrawIsoAreas { get; set; }

		/// <summary>
		/// Gets or Sets number of iso areas sub-intervals between color scale control points.
		/// </summary>
		int IsoAreasSubIntervalNumber { get; set; }
	}

	internal class VisualizerSettings : IVisualizerSettings
	{
		public VisualizerSettings()
		{
			ColorScale = new ColorScale(ColorScale.Types.LightSpectrum);
			ShowScalars = true;
			ShowColorScaleLegend = true;
			DrawIsoAreas = true;
			IsoAreasSubIntervalNumber = 5;
		}

		public ColorScale ColorScale { get; set; }

		public bool DrawIsoAreas { get; set; }

		public int IsoAreasSubIntervalNumber { get; set; }

		public bool ShowColorScaleLegend { get; set; }

		public bool ShowScalars { get; set; }
	}
}

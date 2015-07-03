using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public interface IVisualizerSettings : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or Sets the way of data visualization (Scalars - , Vectors - visualize through arrows in nodes).
		/// </summary>
		//DataDisplayStyle DisplayStyle { get; set; }

		/// <summary>
		/// Flag that turns on or off data visualization of scalar values as colors on mesh surface.
		/// </summary>
		bool ShowScalars { get; set; }

		/// <summary>
		/// Flag that turns on or off data visualization of vector values as arrows in nodes.
		/// </summary>
		bool ShowVectors { get; set; }

		//bool ShowTensors { get; set; }

		/// <summary>
		/// Switch between rendering exact values on the mesh surface and interpolated avarage values and approximation error.
		/// </summary>
		ScalarDataDisplayMethod DisplayMethod { get; set; }

		/// <summary>
		/// Gets or Sets current data component (scalar) that is currently visualized with colors on mesh surface.
		/// </summary>
		DataIndex ScalarDataIndex { get; set; }

		/// <summary>
		/// Gets or Sets textual description of currently displayed scalar data. (Data value name and component name)
		/// </summary>
		string ScalarDataDescription { get; set; }
		
		/// <summary>
		/// Gets or Sets current vector data type that is currently visualized with arrows in mesh nodes.
		/// </summary>
		DataIndex VectorDataIndex { get; set; }

		/// <summary>
		/// Gets or Sets multiplier factor of vector arrows lengths.
		/// </summary>
		double VectorLengthFactor { get; set; }

		/// <summary>
		/// Gets or Sets flag indicating whether to invert vector arrows.
		/// </summary>
		bool MoveEndOfArrowsToNodes { get; set; }

		/// <summary>
		/// Gets or Sets flag indicating whether to draw legend of color scale in the window border.
		/// If DisplayStyle does not contain Scalars flag, this property is ignored.
		/// </summary>
		bool ShowColorScaleLegend { get; set; }

		/// <summary>
		/// Color scale object used to modify displayed range of values and colors assigned to values.
		/// </summary>
		ColorScale ColorScale { get; }

		/// <summary>
		/// Shows leafs of data structure (usually Octree) that is built upon the mesh as red edges of hexahedra. Also draw numeric values of absolute error and corner values of each leaf.
		/// </summary>
		bool DrawGrid { get; set; }

		/// <summary>
		/// Show iso areas when drawing colors.
		/// </summary>
		bool DrawIsoAreas { get; set; }

		/// <summary>
		/// Gets or Sets number of iso areas sub-intervals between color scale control points.
		/// </summary>
		int IsoAreasSubIntervalNumber { get; set; }

		/// <summary>
		/// Gets deformation scale object used to turn on and off display of deformed model and deformation parameters.
		/// </summary>
		DeformationScale DeformationScale { get; }

		/// <summary>
		/// Suspends raising PropertyChanged event until EndUpdate is called.
		/// </summary>
		void BeginUpdate();

		/// <summary>
		/// Raises cumullative PropertyChanged event with propertyName parameter containing all property names that were changed since BeginUpdate() call.
		/// Resumes normal PropertyChanged event behaviour.
		/// </summary>
		void EndUpdate();

		/// <summary>
		/// Raises PropertyChanged event on VisualizerSettings object.
		/// </summary>
		/// <param name="propertyName">Name of property that was changed</param>
		void OnPropertyChanged(string propertyName);
	}

	public class VisualizerSettings : IVisualizerSettings, INotifyPropertyChanged
	{

		#region Fields, constructor

		bool showScalars, showVectors;
		ScalarDataDisplayMethod displayMethod;
		ColorScale colorScale;
		DeformationScale deformationScale;
		bool drawGrid, drawIsoAreas;
		int isoAreasSubIntervalNumber;
		bool showColorScaleLegend;
		bool moveEndOfArrowsToNodes;

		DataIndex scalarDataIndex;
		DataIndex vectorDataIndex;

		double vectorLengthFactor;

		string scalarDataDescription;

		List<string> batchUpdate;

		public VisualizerSettings()
		{
			colorScale = new ColorScale(ColorScale.Types.LightSpectrum);
			deformationScale = new DeformationScale();
			isoAreasSubIntervalNumber = 3;
			vectorLengthFactor = 1.0f;
			showColorScaleLegend = true;
			deformationScale.PropertyChanged += (s, e) => OnPropertyChanged("DeformationScale");
		}

		#endregion

		#region Properties

		public bool ShowScalars
		{
			get { return showScalars; }
			set
			{
				if (showScalars != value)
				{
					showScalars = value;
					OnPropertyChanged("ShowScalars");
				}
			}
		}

		public bool ShowVectors
		{
			get { return showVectors; }
			set
			{
				if (showVectors != value)
				{
					showVectors = value;
					OnPropertyChanged("ShowVectors");
				}
			}
		}

		public ScalarDataDisplayMethod DisplayMethod
		{
			get { return displayMethod; }
			set
			{
				if (displayMethod != value)
				{
					displayMethod = value;
					OnPropertyChanged("DisplayMethod");
				}
			}
		}

		public DataIndex ScalarDataIndex
		{
			get { return scalarDataIndex; }
			set
			{
				if (scalarDataIndex != value)
				{
					scalarDataIndex = value;
					OnPropertyChanged("ScalarDataIndex");
				}
			}
		}

		public string ScalarDataDescription
		{
			get { return scalarDataDescription; }
			set
			{
				if (scalarDataDescription != value)
				{
					scalarDataDescription = value;
					OnPropertyChanged("ScalarDataDescription");
				}
			}
		}

		public DataIndex VectorDataIndex
		{
			get { return vectorDataIndex; }
			set
			{
				if (vectorDataIndex != value)
				{
					vectorDataIndex = value;
					OnPropertyChanged("VectorDataIndex");
				}
			}
		}

		public double VectorLengthFactor
		{
			get { return vectorLengthFactor; }
			set
			{
				if (vectorLengthFactor != value)
				{
					vectorLengthFactor = value;
					OnPropertyChanged("VectorLengthFactor");
				}
			}
		}

		public bool ShowColorScaleLegend
		{
			get { return showColorScaleLegend; }
			set
			{
				if (showColorScaleLegend != value)
				{
					showColorScaleLegend = value;
					OnPropertyChanged("ShowColorScaleLegend");
				}
			}
		}

		public ColorScale ColorScale
		{
			get { return colorScale; }
		}

		public bool DrawGrid
		{
			get { return drawGrid; }
			set
			{
				if (drawGrid != value)
				{
					drawGrid = value;
					OnPropertyChanged("DrawGrid");
				}
			}
		}

		public bool DrawIsoAreas
		{
			get { return drawIsoAreas; }
			set
			{
				if (drawIsoAreas != value)
				{
					drawIsoAreas = value;
					OnPropertyChanged("DrawIsoAreas");
				}
			}
		}

		public int IsoAreasSubIntervalNumber
		{
			get { return isoAreasSubIntervalNumber; }
			set
			{
				if (isoAreasSubIntervalNumber != value)
				{
					isoAreasSubIntervalNumber = value;
					OnPropertyChanged("IsoAreasSubIntervalNumber");
				}
			}
		}

		public DeformationScale DeformationScale
		{
			get { return deformationScale; }
		}

		public bool MoveEndOfArrowsToNodes
		{
			get { return moveEndOfArrowsToNodes; }
			set
			{
				if (moveEndOfArrowsToNodes != value)
				{
					moveEndOfArrowsToNodes = value;
					OnPropertyChanged("MoveEndOfArrowsToNodes");
				}
			}
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged(string propertyName)
		{
			if (batchUpdate != null)
			{
				batchUpdate.Add(propertyName);
			}
			else
			{
				var handler = PropertyChanged;
				if (handler != null)
					handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

		#region Private methods

		#endregion

		#region Public methods

		public void BeginUpdate()
		{
			if (batchUpdate == null)
				batchUpdate = new List<string>();
		}

		public void EndUpdate()
		{
			if (batchUpdate != null)
			{
				string propertyNames = string.Join(";", batchUpdate.Distinct().ToArray());
				batchUpdate = null;
				OnPropertyChanged(propertyNames); // raise PropertyChanged event with parameter consisting of all property names that were changed since BeginUpdate() call
			}
		}

		#endregion

	}
}

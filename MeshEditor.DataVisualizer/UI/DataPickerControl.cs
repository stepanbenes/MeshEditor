using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MeshEditor.CoreInterface;
using System.Diagnostics;
using MeshEditor.DataVisualizer.Data;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class DataPickerControl : UserControl
	{

		#region Fields, constructor, properties

		IDataVisualizerController dataVisualizer;
		bool initialization;
		LongOpNotifier longOpNotifier;

		public DataPickerControl()
		{
			InitializeComponent();

			dataIndexSetterScalars.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			dataIndexSetterVectors.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			dataIndexSetterDeformations.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		}

		public IDataVisualizerController DataVisualizer
		{
			get { return dataVisualizer; }
			set
			{
				if (dataVisualizer != value)
				{
					dataVisualizer = value;
					setupData();
				}
			}
		}

		public LongOpNotifier LongOpNotifier
		{
			get { return longOpNotifier; }
			set { longOpNotifier = value; }
		}

		#endregion

		#region Private methods

		private void setupData()
		{
			if (dataVisualizer == null)
				return;

			initialization = true;
			try
			{
				setupEmbeddedControls();

				setupColorScale();

				checkBoxShowLegend.Checked = dataVisualizer.Settings.ShowColorScaleLegend;

				checkBoxShowScalarData.Checked = dataVisualizer.Settings.ShowScalars;
				updateEnabledStateOfTabPageControls(tabPageScalars, checkBoxShowScalarData.Checked, checkBoxShowScalarData);
				checkBoxShowVectorData.Checked = dataVisualizer.Settings.ShowVectors;
				updateEnabledStateOfTabPageControls(tabPageVectors, checkBoxShowVectorData.Checked, checkBoxShowVectorData);

				if (dataVisualizer is ExactDataVisualizer)
				{
					comboBoxDisplayMethod.Visible = labelDisplayMethod.Visible = false;
				}
				else
				{
					comboBoxDisplayMethod.Items.AddRange(Enum.GetValues(typeof(ScalarDataDisplayMethod)).Cast<object>().ToArray());
					comboBoxDisplayMethod.SelectedItem = dataVisualizer.Settings.DisplayMethod;
				}

				checkBoxDrawIsoAreas.Checked = dataVisualizer.Settings.DrawIsoAreas;

				comboBoxIsoAreasSubIntervalNumber.Items.AddRange(Enumerable.Range(1, 5).Cast<object>().ToArray());
				comboBoxIsoAreasSubIntervalNumber.SelectedItem = dataVisualizer.Settings.IsoAreasSubIntervalNumber;

				textBoxVectorLengthFactor.Text = dataVisualizer.Settings.VectorLengthFactor.ToString();
				checkBoxMoveEndOfArrowsToNodes.Checked = dataVisualizer.Settings.MoveEndOfArrowsToNodes;

				setupDeformationScale();

				dataVisualizer.Settings.PropertyChanged += dataVisualizerSettings_PropertyChanged;
			}
			finally
			{
				initialization = false;
			}
		}

		private void dataVisualizerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			// propagate changes made outside of this control
			switch (e.PropertyName)
			{
				case "DisplayMethod":
					comboBoxDisplayMethod.SelectedItem = dataVisualizer.Settings.DisplayMethod;
					break;
				case "ScalarDataIndex":
					if (dataIndexSetterScalars.SelectedDataIndex != dataVisualizer.Settings.ScalarDataIndex)
					{
						dataIndexSetterScalars.SelectedDataIndex = dataVisualizer.Settings.ScalarDataIndex;
						dataIndexSetterScalars.SetupData();
					}
					break;
				case "VectorDataIndex":
					if (dataIndexSetterVectors.SelectedDataIndex != dataVisualizer.Settings.VectorDataIndex)
					{
						dataIndexSetterVectors.SelectedDataIndex = dataVisualizer.Settings.VectorDataIndex;
						dataIndexSetterVectors.SetupData();
					}
					break;
				case "DeformationScale":
					if (dataIndexSetterDeformations.SelectedDataIndex != dataVisualizer.Settings.DeformationScale.DeformationDataIndex)
					{
						dataIndexSetterDeformations.SelectedDataIndex = dataVisualizer.Settings.DeformationScale.DeformationDataIndex;
						dataIndexSetterDeformations.SetupData();
					}
					break;
			}
		}

		private void setupEmbeddedControls()
		{
			// COLOR SCALE
			colorScaleSetter.DataVisualizer = dataVisualizer;

			// SCALARS
			setDataIndexSetter(dataIndexSetterScalars, DataIndexSetter.DataFilterOptions.Scalars, dataVisualizer.Settings.ScalarDataIndex,
				(s, e) =>
				{
					dataVisualizer.Settings.ScalarDataIndex = dataIndexSetterScalars.SelectedDataIndex;
					dataVisualizer.Settings.ScalarDataDescription = dataIndexSetterScalars.SelectedDataIndexDescription;
					checkBoxVectorMagnitudes.Enabled = dataIndexSetterScalars.SelectedDataTypeIsVector;
				}
			);
			dataVisualizer.Settings.ScalarDataDescription = dataIndexSetterScalars.SelectedDataIndexDescription;
			checkBoxShowScalarData.Enabled = dataIndexSetterScalars.DataTypesLength > 0;

			// VECTORS
			setDataIndexSetter(dataIndexSetterVectors, DataIndexSetter.DataFilterOptions.Vectors, dataVisualizer.Settings.VectorDataIndex,
				(s, e) => dataVisualizer.Settings.VectorDataIndex = dataIndexSetterVectors.SelectedDataIndex
				);
			checkBoxShowVectorData.Enabled = dataIndexSetterVectors.DataTypesLength > 0;

			// DEFORMATIONS
			setDataIndexSetter(dataIndexSetterDeformations, DataIndexSetter.DataFilterOptions.Vectors, dataVisualizer.Settings.DeformationScale.DeformationDataIndex,
				(s, e) => dataVisualizer.SetDeformationDataIndex(dataIndexSetterDeformations.SelectedDataIndex)
				);
			checkBoxDrawDeformed.Enabled = dataIndexSetterDeformations.DataTypesLength > 0;
			//dataIndexSetterDeformations.TryToSetDisplacementDataIndex();
		}

		private void setDataIndexSetter(DataIndexSetter dataIndexSetter, DataIndexSetter.DataFilterOptions dataFilter, DataIndex dataIndex, EventHandler selectedDataIndexChangedHandler)
		{
			dataIndexSetter.SelectedDataIndexChanged += selectedDataIndexChangedHandler; // must be added before dataIndexSetter.DataVisualizer is set

			dataIndexSetter.DataFilter = dataFilter;
			dataIndexSetter.SelectedDataIndex = dataIndex;
			dataIndexSetter.DataVisualizer = this.dataVisualizer;
			dataIndexSetter.SetupData();
		}

		private void setupDeformationScale()
		{
			DeformationScale scale = dataVisualizer.Settings.DeformationScale;

			checkBoxDrawDeformed.Checked = scale.DrawDeformed;
			trackBarDeformationMultiplier.Value = (int)(scale.RelativeScale * (float)trackBarDeformationMultiplier.Maximum);
			updateEnabledStateOfTabPageControls(tabPageDeformations, checkBoxDrawDeformed.Checked, checkBoxDrawDeformed);

			labelRelativeScale.Text = string.Format("{0}%", Math.Round(scale.RelativeScale * 100f, 2));

			textBoxScaleValue.Text = scale.AbsoluteScale.ToString();

			switch (scale.Type)
			{
				case DeformationScale.Types.Absolute:
					radioButtonAbsolute.Checked = true;
					radioButtonRelative.Checked = false;
					labelRelativeScale.Visible = false;
					textBoxScaleValue.Visible = true;
					trackBarDeformationMultiplier.Visible = false;
					break;
				case DeformationScale.Types.Relative:
					radioButtonAbsolute.Checked = false;
					radioButtonRelative.Checked = true;
					labelRelativeScale.Visible = true;
					textBoxScaleValue.Visible = false;
					trackBarDeformationMultiplier.Visible = true;
					break;
				default:
					throw new NotSupportedException();
			}
		}

		private void setupColorScale()
		{
			comboBoxColorScaleType.Items.Clear();
			comboBoxColorScaleType.Items.AddRange(Enum.GetValues(typeof(ColorScale.Types)).Cast<object>().ToArray());
			if (dataVisualizer != null)
			{
				comboBoxColorScaleType.SelectedItem = dataVisualizer.Settings.ColorScale.Type;
			}
		}

		private static void updateEnabledStateOfTabPageControls(TabPage tabPage, bool enabledState, params Control[] exceptList)
		{
			Debug.Assert(tabPage != null && exceptList != null);
			foreach (Control control in tabPage.Controls)
			{
				if (!exceptList.Contains(control))
					control.Enabled = enabledState;
			}
		}

		private void updateAbsoluteDeformationScaleValue()
		{
			float absoluteScale;
			if (float.TryParse(textBoxScaleValue.Text, out absoluteScale))
				dataVisualizer.Settings.DeformationScale.AbsoluteScale = absoluteScale;
		}

		private void onTextBoxVectorLengthFactorTextChanged()
		{
			if (initialization)
				return;
			double factor;
			if (double.TryParse(textBoxVectorLengthFactor.Text, out factor))
			{
				dataVisualizer.Settings.VectorLengthFactor = factor;
			}
		}

		#endregion

		#region Event handlers

		private void comboBoxColorScaleType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;

			if (comboBoxColorScaleType.SelectedItem != null && dataVisualizer != null)
			{
				ColorScale.Types type = (ColorScale.Types)comboBoxColorScaleType.SelectedItem;
				dataVisualizer.Settings.ColorScale.Type = type;
				dataVisualizer.Settings.OnPropertyChanged("ColorScale");
			}
		}

		private void checkBoxDrawIsoAreas_CheckedChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;

			if (dataVisualizer != null)
			{
				dataVisualizer.Settings.DrawIsoAreas = checkBoxDrawIsoAreas.Checked;
			}
		}

		private void trackBarDeformationMultiplier_Scroll(object sender, EventArgs e)
		{
			if (initialization)
				return;
			float relativeScale = (float)trackBarDeformationMultiplier.Value / (float)trackBarDeformationMultiplier.Maximum;
			if (dataVisualizer != null)
			{
				dataVisualizer.Settings.DeformationScale.RelativeScale = relativeScale;
			}
			labelRelativeScale.Text = string.Format("{0}%", Math.Round(relativeScale * 100f, 2));

		}

		private void comboBoxIsoAreasSubIntervalNumber_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;

			if (dataVisualizer != null)
			{
				dataVisualizer.Settings.IsoAreasSubIntervalNumber = (int)comboBoxIsoAreasSubIntervalNumber.SelectedItem;
			}
		}

		private void checkBoxShowScalarData_CheckedChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;
			if (checkBoxShowScalarData.Checked)
			{
				dataVisualizer.Settings.ShowScalars = true;
			}
			else
			{
				dataVisualizer.Settings.ShowScalars = false;
				checkBoxVectorMagnitudes.Checked = false;
			}
			updateEnabledStateOfTabPageControls(tabPageScalars, checkBoxShowScalarData.Checked, checkBoxShowScalarData);
		}

		private void checkBoxShowVectorData_CheckedChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;
			if (checkBoxShowVectorData.Checked)
				dataVisualizer.Settings.ShowVectors = true;
			else
				dataVisualizer.Settings.ShowVectors = false;
			updateEnabledStateOfTabPageControls(tabPageVectors, checkBoxShowVectorData.Checked, checkBoxShowVectorData);
		}

		private void checkBoxDrawDeformed_CheckedChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;
			if (dataVisualizer != null)
			{
				dataVisualizer.Settings.BeginUpdate();
				{
					dataVisualizer.Settings.DeformationScale.DrawDeformed = checkBoxDrawDeformed.Checked;
				}
				dataVisualizer.Settings.EndUpdate();
			}
			updateEnabledStateOfTabPageControls(tabPageDeformations, checkBoxDrawDeformed.Checked, checkBoxDrawDeformed);
		}

		private void checkBoxShowLegend_CheckedChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;
			if (dataVisualizer != null)
			{
				dataVisualizer.Settings.ShowColorScaleLegend = checkBoxShowLegend.Checked;
			}
		}

		private void comboBoxDisplayMethod_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;

			ScalarDataDisplayMethod displayMethod = (ScalarDataDisplayMethod)comboBoxDisplayMethod.SelectedItem;

			if (dataVisualizer != null)
			{
				try
				{
					longOpNotifier.Begin();
					dataVisualizer.Settings.DisplayMethod = displayMethod;
				}
				finally
				{
					longOpNotifier.End();
				}
			}
		}

		private void checkBoxVectorMagnitudes_CheckedChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;

			dataIndexSetterScalars.ShowVectorMagnitudes = checkBoxVectorMagnitudes.Checked;
		}

		private void radioButtonAbsolute_CheckedChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;
			dataVisualizer.Settings.DeformationScale.Type = (radioButtonAbsolute.Checked) ? DeformationScale.Types.Absolute : DeformationScale.Types.Relative;
			setupDeformationScale();
		}

		private void radioButtonRelative_CheckedChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;
			dataVisualizer.Settings.DeformationScale.Type = (radioButtonRelative.Checked) ? DeformationScale.Types.Relative : DeformationScale.Types.Absolute;
			setupDeformationScale();
		}

		private void textBoxScaleValue_KeyDown(object sender, KeyEventArgs e)
		{
			if (initialization)
				return;
			if (e.KeyCode == Keys.Enter)
			{
				updateAbsoluteDeformationScaleValue();
			}
		}

		private void textBoxScaleValue_Leave(object sender, EventArgs e)
		{
			if (initialization)
				return;
			updateAbsoluteDeformationScaleValue();
		}

		private void textBoxVectorLengthFactor_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				onTextBoxVectorLengthFactorTextChanged();
				e.Handled = true;
			}
		}

		private void textBoxVectorLengthFactor_Leave(object sender, EventArgs e)
		{
			onTextBoxVectorLengthFactorTextChanged();
		}

		private void checkBoxMoveEndOfArrowsToNodes_CheckedChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;
			dataVisualizer.Settings.MoveEndOfArrowsToNodes = checkBoxMoveEndOfArrowsToNodes.Checked;
		}

		#endregion

		#region Overrides

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				if (dataVisualizer != null)
				{
					dataVisualizer.Settings.PropertyChanged -= dataVisualizerSettings_PropertyChanged;
				}
			}
			base.Dispose(disposing);
		}

		#endregion

	}
}

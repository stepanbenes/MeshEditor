using MeshEditor.CoreInterface;
using MeshEditor.DataVisualizer.Layers;
using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class LayersForm : Form
	{

		private enum VectorDirection
		{
			General,
			X,
			Y,
			Z
		}
		
		public static T[] GetValuesOfEnum<T>(params T[] except) where T : struct // Enum is not enabled here, used struct instead
		{
			// maybe check for some errors? (T is not enum etc...)
			return Enum.GetValues(typeof(T)).Cast<T>().Except(except).ToArray();
		}

		#region Fields, constructor

		IList<ILayer> layers;
		Vector3 meshDimensions;
		EventHandler redrawMeshHandler;
		bool initializingLayerList;

		public LayersForm(IList<ILayer> layers, EventHandler redrawMeshHandler, Vector3 meshDimensions, bool postprocessDataLoaded)
		{
			Debug.Assert(layers != null && redrawMeshHandler != null);

			InitializeComponent();

			if (!postprocessDataLoaded)
			{
				tabControlSectionSettings.TabPages.Remove(tabPageIsoSurface);
			}

			this.layers = layers;
			this.redrawMeshHandler = redrawMeshHandler;
			this.meshDimensions = meshDimensions;

			comboBoxDirection.DataSource = GetValuesOfEnum<VectorDirection>(except: VectorDirection.General /**/);
			setupLayerList();
		}

		#endregion

		#region Private methods

		private void setupLayerList()
		{
			initializingLayerList = true;
			int selectedIndex = checkedListBoxLayers.SelectedIndex;
			checkedListBoxLayers.Items.Clear();
			foreach (ILayer layer in layers)
			{
				checkedListBoxLayers.Items.Add(layer.ToString(), layer.Visible);
			}
			if (selectedIndex < checkedListBoxLayers.Items.Count)
				checkedListBoxLayers.SelectedIndex = selectedIndex;
			initializingLayerList = false;
		}

		#endregion

		#region Event handlers

		private void buttonAddCrossSection_Click(object sender, EventArgs e)
		{
			Vector3 normal;
			float maxOffset;
			switch ((VectorDirection)comboBoxDirection.SelectedItem)
			{
				case VectorDirection.X:
					normal = Vector3.UnitX;
					maxOffset = meshDimensions.X * 0.5f;
					break;
				case VectorDirection.Y:
					normal = Vector3.UnitY;
					maxOffset = meshDimensions.Y * 0.5f;
					break;
				case VectorDirection.Z:
					normal = Vector3.UnitZ;
					maxOffset = meshDimensions.Z * 0.5f;
					break;
				//case VectorDirection.General:
				//	normal = new Vector3(1f, 1f, 1f);
				//	normal.Normalize();
				//	maxOffset = meshDimensions.Length * 0.5f;
				//	break;
				default:
					throw new NotSupportedException();
			}

			float fromOffset;
			float toOffset;

			if (!float.TryParse(textBoxFromOffset.Text, out fromOffset))
			{
				MessageBox.Show("'From' parameter must be floating-point number.", "Can not recognize 'From' parameter", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (!float.TryParse(textBoxToOffset.Text, out toOffset))
			{
				MessageBox.Show("'To' parameter must be floating-point number.", "Can not recognize 'To' parameter", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (toOffset <= fromOffset)
			{
				MessageBox.Show("'To' parameter must be greater than 'From' parameter.", "Can not generate cross sections", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			
			int layerCount = (int)numericUpDownCount.Value;
			
			Debug.Assert(layerCount > 0);

			float offset = fromOffset;
			float step = (layerCount > 1) ? (toOffset - fromOffset) / (layerCount - 1) : 0f;

			for (int i = 0; i < layerCount; i++, offset += step)
			{
				// CREATE NEW LAYER ---------------------------------------
				ILayer layer = new CrossSection(normal, offset, maxOffset);
				//layer.DisplayStyle = MeshEditor.Graphics.RenderMode.FacesLines;
				//layer.Name = string.Format("{0} {1}:{2:G3}", crossSectionString, (VectorDirection)comboBoxDirection.SelectedItem, offset);
				layer.RedrawNeeded += redrawMeshHandler;
				// --------------------------------------------------------
				layers.Add(layer);
			}

			if (redrawMeshHandler != null)
				redrawMeshHandler(this, EventArgs.Empty);

			setupLayerList();
		}

		private void buttonRemove_Click(object sender, EventArgs e)
		{
			int index = checkedListBoxLayers.SelectedIndex;
			if(index < 0)
				return;

			checkedListBoxLayers.Items.RemoveAt(index);

			// remove layer from layers list ---
			layers[index].RedrawNeeded -= redrawMeshHandler;
			layers[index].Dispose();
			layers.RemoveAt(index);
			// ---------------------------------

			if(index >= checkedListBoxLayers.Items.Count)
				index = checkedListBoxLayers.Items.Count - 1;
			checkedListBoxLayers.SelectedIndex = index;

			if (redrawMeshHandler != null)
				redrawMeshHandler(this, EventArgs.Empty);
		}
		
		private void checkedListBoxLayers_SelectedIndexChanged(object sender, EventArgs e)
		{
			buttonRemove.Enabled = checkedListBoxLayers.SelectedIndex >= 0;
			setupLayerOptions();
			if (!initializingLayerList)
				tabControlSectionSettings.SelectedTab = tabPageLayerOptions;
		}

		private void setupLayerOptions()
		{
			ILayer layer = null;
			if (checkedListBoxLayers.SelectedIndex >= 0)
				layer = layers[checkedListBoxLayers.SelectedIndex];
			propertyGridLayerOptions.SelectedObject = layer;
		}

		private void checkedListBoxLayers_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			layers[e.Index].Visible = (e.NewValue == CheckState.Checked);

			if (redrawMeshHandler != null)
				redrawMeshHandler(this, EventArgs.Empty);
		}

		private void buttonAddIsoSurface_Click(object sender, EventArgs e)
		{
			double sectionValue;

			if (!double.TryParse(textBoxIsoSurfaceDataValue.Text, out sectionValue))
			{
				MessageBox.Show("'Data value' parameter must be floating-point number.", "Can not recognize 'Data value' parameter", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// CREATE NEW LAYER ---------------------------------------
			ILayer isoSurface = new IsoSurfaceSection(sectionValue);
			//isoSurface.Name = string.Format("Iso-surface section Value:{0:G3}", sectionValue);
			isoSurface.RedrawNeeded += redrawMeshHandler;
			// --------------------------------------------------------
			layers.Add(isoSurface);

			if (redrawMeshHandler != null)
				redrawMeshHandler(this, EventArgs.Empty);

			setupLayerList();
		}

		private void propertyGridLayerOptions_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			setupLayerList();
			if (redrawMeshHandler != null)
				redrawMeshHandler(this, EventArgs.Empty);
		}

		#endregion

	}
}

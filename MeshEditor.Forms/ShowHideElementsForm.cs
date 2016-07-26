using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using MeshEditor.Cuts;
using MeshEditor.Graphics;
using System.Linq;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// Pomoci tohoto formulare lze zvolit, ktere prvky budou skryty a ktere zobrazeny. 
	/// Prvky lze filtrovat bud na zaklade cisla jim prirazene vlastnosti,
	/// nebo podle jejich typu (je mozno skryt napriklad vsechny 1D elementy).
	/// </summary>
	public partial class ShowHideElementsForm : Form
	{

		#region Fields, Constructor

		private SceneFacade sceneFacade;
		private SortedDictionary<Property, bool> allProperties;
		private LongOpNotifier longOpNotifier;

		public ShowHideElementsForm(SceneFacade sceneFacade, LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			this.sceneFacade = sceneFacade;
			this.longOpNotifier = longOpNotifier;
			initLists();
		}

		#endregion

		#region Initialization

		private void initLists()
		{
			CutInfo lastUsedCutInfo = sceneFacade.GetValue(AvailableValue.LastUsedCutInfo) as CutInfo;

			this.allProperties = sceneFacade.GetValue(AvailableValue.MeshElementPropertiesSorted) as SortedDictionary<Property, bool>;

			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;

			initPropertyList(statistics, lastUsedCutInfo);
			initElementTypeList(statistics, lastUsedCutInfo);

			// init or hide Data Value Limit page
			initValueLimit(lastUsedCutInfo);
		}

		private void initValueLimit(CutInfo lastUsedCutInfo)
		{
			IDataVisualizer dataVisualizer = sceneFacade.GetValue(AvailableValue.DataVisualizer) as IDataVisualizer;
			if (dataVisualizer != null)
			{
				if (lastUsedCutInfo != null && lastUsedCutInfo.ValueLimit != null)
				{
					if (lastUsedCutInfo.ValueLimit.Maximum != null)
					{
						textBoxMaximum.Text = lastUsedCutInfo.ValueLimit.Maximum.ToString();
						checkBoxMaximum.Checked = true;
					}
					else
					{
						textBoxMaximum.Text = dataVisualizer.GetMaximumDataValue().ToString();
					}
					if (lastUsedCutInfo.ValueLimit.Minimum != null)
					{
						textBoxMinimum.Text = lastUsedCutInfo.ValueLimit.Minimum.ToString();
						checkBoxMinimum.Checked = true;
					}
					else
					{
						textBoxMinimum.Text = dataVisualizer.GetMinimumDataValue().ToString();
					}
					checkBoxAllNodesInRange.Checked = (lastUsedCutInfo.HitDecision == CutInfo.ItemHitDecision.AllNodes);
				}
				else
				{
					textBoxMaximum.Text = dataVisualizer.GetMaximumDataValue().ToString();
					textBoxMinimum.Text = dataVisualizer.GetMinimumDataValue().ToString();
				}
			}
			else
			{
				tabControl.TabPages.Remove(tabPageValueLimit);
			}
		}

		private void initPropertyList(MeshStatistics statistics, CutInfo lastUsedCutInfo)
		{
			foreach (Property property in allProperties.Keys)
			{
				ListViewItem item = new ListViewItem(property.ToString());
				item.Tag = property;
				string description;
				if (statistics.PropertyComments.TryGetValue(property, out description))
					item.SubItems.Add(description);
				// --------------------------------------------------
				item.Checked = allProperties[property];
				//item.Checked = (lastUsedCutInfo == null || lastUsedCutInfo.ElementPropertiesToShow == null || lastUsedCutInfo.ElementPropertiesToShow.Contains(property));
				// --------------------------------------------------
				// set property color as background
				item.BackColor = PropertyColorProvider.Get(property);
				// if property color is too dark, use light foreground color
				item.ForeColor = Utilities.Functions.GetContrastColor(PropertyColorProvider.Get(property));
				// --------------------------------------------------
				listViewProperties.Items.Add(item);
			}
		}

		private void initElementTypeList(MeshStatistics statistics, CutInfo lastUsedCutInfo)
		{
			ElementType[] allTypes = statistics.GetIncludedElementTypesArray();

			foreach (ElementType type in allTypes)
			{
				ListViewItem item = new ListViewItem(type.ToString());
				// --------------------------------------------------
				item.Checked = (lastUsedCutInfo == null || lastUsedCutInfo.ElementTypesToShow == null || lastUsedCutInfo.ElementTypesToShow.Contains(type));
				// --------------------------------------------------
				listViewElementTypes.Items.Add(item);
			}
		}

		private void doIt()
		{
			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
			if (sceneFacade.ContainsMesh && statistics != null)
			{
				Cursor temp = this.Cursor;
				this.Cursor = Cursors.WaitCursor;
				using (longOpNotifier.Begin("Updating set of visible elements"))
				{
					CutInfo cutInfo = new CutInfo();

					cutInfo.Action = CutInfo.ActionType.ShowHideElements;
					cutInfo.ElementPropertiesToShow = getPropertiesToShow(statistics);
					cutInfo.ElementTypesToShow = getElementTypesToShow(statistics);

					try
					{
						cutInfo.ValueLimit = getDataValueLimitToShow();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Value limit error");
					}

					cutInfo.HitDecision = checkBoxAllNodesInRange.Checked ? CutInfo.ItemHitDecision.AllNodes : CutInfo.ItemHitDecision.SomeNodes;

					sceneFacade.PerformAction(AvailableAction.CutMesh, cutInfo);
				}
				this.Cursor = temp;
			}
			
		}

		private Property[] getPropertiesToShow(MeshStatistics statistics)
		{
			Property[] result = new Property[listViewProperties.CheckedItems.Count];
			int index = 0;
			foreach (ListViewItem item in listViewProperties.CheckedItems)
				result[index++] = (Property)item.Tag;
			return result;
		}

		private ElementType[] getElementTypesToShow(MeshStatistics statistics)
		{
			ElementType[] allTypes = statistics.GetIncludedElementTypesArray();
			ElementType[] result = new ElementType[listViewElementTypes.CheckedItems.Count];
			for (int i = 0; i < result.Length; i++)
				result[i] = allTypes[listViewElementTypes.CheckedIndices[i]];
			return result;
		}

		private DataValueRange getDataValueLimitToShow()
		{
			//throw new NotImplementedException(); // TODO: check if min is less than max
			double? min = null, max = null;
			if (checkBoxMinimum.Checked)
				min = double.Parse(textBoxMinimum.Text);
			if (checkBoxMaximum.Checked)
				max = double.Parse(textBoxMaximum.Text);

			ensureMonotony(ref min, ref max);

			return (min == null && max == null) ? null : new DataValueRange(min, max, checkBoxInverse.Checked); // return null, if value limit is not set
		}

		private void ensureMonotony(ref double? min, ref double? max)
		{
			if (min != null && max != null && min > max)
			{
				// minimum is greater than maximum!
				// swap values
				double? temp = min;
				min = max;
				max = temp;
				// swap texts in textboxes
				string tempText = textBoxMinimum.Text;
				textBoxMinimum.Text = textBoxMaximum.Text;
				textBoxMaximum.Text = tempText;
				// inform user
				throw new ArgumentException("Maximum must be greater than mininum. Values swapped.");
			}
		}

		#endregion

		#region User action handlers

		private void buttonClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void buttonApply_Click(object sender, EventArgs e)
		{
			doIt();
		}
		
		private void checkBoxMinimum_CheckedChanged(object sender, EventArgs e)
		{
			textBoxMinimum.Enabled = checkBoxMinimum.Checked;
			checkBoxInverse.Enabled = checkBoxAllNodesInRange.Enabled = checkBoxMinimum.Checked || checkBoxMaximum.Checked;
		}

		private void checkBoxMaximum_CheckedChanged(object sender, EventArgs e)
		{
			textBoxMaximum.Enabled = checkBoxMaximum.Checked;
			checkBoxInverse.Enabled = checkBoxAllNodesInRange.Enabled = checkBoxMinimum.Checked || checkBoxMaximum.Checked;
		}

		#region CheckBoxes Check Properties

		bool checkAllPropertiesStateChanging, checkSelectedPropertiesStateChanging;
		bool itemCheckPropertiesChanging;

		private void checkBoxCheckAllProperties_CheckedChanged(object sender, EventArgs e)
		{
			if (checkAllPropertiesStateChanging)
				return;

			itemCheckPropertiesChanging = true;
			{
				foreach (ListViewItem item in listViewProperties.Items)
					item.Checked = checkBoxCheckAllProperties.Checked;
			}
			itemCheckPropertiesChanging = false;

			updateCheckSelectedPropertiesState();
		}

		private void checkBoxCheckSelectedProperties_CheckedChanged(object sender, EventArgs e)
		{
			if (checkSelectedPropertiesStateChanging)
				return;

			itemCheckPropertiesChanging = true;
			{
				foreach (ListViewItem item in listViewProperties.SelectedItems)
					item.Checked = checkBoxCheckSelectedProperties.Checked;
			}
			itemCheckPropertiesChanging = false;

			updateCheckAllPropertiesState();
		}

		private void listViewProperties_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			if (itemCheckPropertiesChanging)
				return;
			updateCheckSelectedPropertiesState();
		}

		private void listViewProperties_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (itemCheckPropertiesChanging)
				return;
			updateCheckAllPropertiesState();
			updateCheckSelectedPropertiesState();
		}

		private void updateCheckAllPropertiesState()
		{
			bool none = true;
			bool all = true;
			foreach (ListViewItem item in listViewProperties.Items)
			{
				if (item.Checked)
				{
					none = false;
				}
				else
				{
					all = false;
				}
			}

			checkAllPropertiesStateChanging = true;
			{
				if (none)
					checkBoxCheckAllProperties.CheckState = CheckState.Unchecked;
				else if (all)
					checkBoxCheckAllProperties.CheckState = CheckState.Checked;
				else
					checkBoxCheckAllProperties.CheckState = CheckState.Indeterminate;
			}
			checkAllPropertiesStateChanging = false;
		}

		private void updateCheckSelectedPropertiesState()
		{
			bool none = true;
			bool all = true;
			foreach (ListViewItem item in listViewProperties.SelectedItems)
			{
				if (item.Checked)
				{
					none = false;
				}
				else
				{
					all = false;
				}
			}

			checkSelectedPropertiesStateChanging = true;
			{
				if (none)
					checkBoxCheckSelectedProperties.CheckState = CheckState.Unchecked;
				else if (all)
					checkBoxCheckSelectedProperties.CheckState = CheckState.Checked;
				else
					checkBoxCheckSelectedProperties.CheckState = CheckState.Indeterminate;
			}
			checkSelectedPropertiesStateChanging = false;
		}

		#endregion

		#region Checkboxes Check Element Types

		bool checkAllElementTypesStateChanging, checkSelectedElementTypesStateChanging;
		bool itemCheckElementTypesChanging;

		private void checkBoxCheckAllElementTypes_CheckedChanged(object sender, EventArgs e)
		{
			if (checkAllElementTypesStateChanging)
				return;

			itemCheckElementTypesChanging = true;
			{
				foreach (ListViewItem item in listViewElementTypes.Items)
					item.Checked = checkBoxCheckAllElementTypes.Checked;
			}
			itemCheckElementTypesChanging = false;

			updateCheckSelectedElementTypesState();
		}

		private void checkBoxCheckSelectedElementTypes_CheckedChanged(object sender, EventArgs e)
		{
			if (checkSelectedElementTypesStateChanging)
				return;

			itemCheckElementTypesChanging = true;
			{
				foreach (ListViewItem item in listViewElementTypes.SelectedItems)
					item.Checked = checkBoxCheckSelectedElementTypes.Checked;
			}
			itemCheckElementTypesChanging = false;

			updateCheckAllElementTypesState();
		}

		private void listViewElementTypes_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			if (itemCheckElementTypesChanging)
				return;
			updateCheckSelectedElementTypesState();
		}

		private void listViewElementTypes_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (itemCheckElementTypesChanging)
				return;
			updateCheckAllElementTypesState();
			updateCheckSelectedElementTypesState();
		}

		private void updateCheckAllElementTypesState()
		{
			bool none = true;
			bool all = true;
			foreach (ListViewItem item in listViewElementTypes.Items)
			{
				if (item.Checked)
				{
					none = false;
				}
				else
				{
					all = false;
				}
			}

			checkAllElementTypesStateChanging = true;
			{
				if (none)
					checkBoxCheckAllElementTypes.CheckState = CheckState.Unchecked;
				else if (all)
					checkBoxCheckAllElementTypes.CheckState = CheckState.Checked;
				else
					checkBoxCheckAllElementTypes.CheckState = CheckState.Indeterminate;
			}
			checkAllElementTypesStateChanging = false;
		}

		private void updateCheckSelectedElementTypesState()
		{
			bool none = true;
			bool all = true;
			foreach (ListViewItem item in listViewElementTypes.SelectedItems)
			{
				if (item.Checked)
				{
					none = false;
				}
				else
				{
					all = false;
				}
			}

			checkSelectedElementTypesStateChanging = true;
			{
				if (none)
					checkBoxCheckSelectedElementTypes.CheckState = CheckState.Unchecked;
				else if (all)
					checkBoxCheckSelectedElementTypes.CheckState = CheckState.Checked;
				else
					checkBoxCheckSelectedElementTypes.CheckState = CheckState.Indeterminate;
			}
			checkSelectedElementTypesStateChanging = false;
		}

		#endregion
		
		#endregion

	}
}

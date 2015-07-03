using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using Wintellect.PowerCollections;
using MeshEditor.Cuts;
using MeshEditor.Graphics;

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

		private static string previousMeshFilename;
		private static Set<ElementType> checkedElementTypes;
		
		static ShowHideElementsForm()
		{
			previousMeshFilename = null;
			checkedElementTypes = null;
		}

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
			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
			if (statistics == null)
				return;

			this.allProperties = (SortedDictionary<Property, bool>)sceneFacade.GetValue(AvailableValue.MeshElementPropertiesSorted);

			initPropertyList(statistics);
			initElementTypeList(statistics);
		}

		private void initPropertyList(MeshStatistics statistics)
		{
			listViewProperties.Columns.Add("Property", 100, HorizontalAlignment.Right);
			listViewProperties.Columns.Add("Description", 280);

			foreach (Property property in allProperties.Keys)
			{
				ListViewItem item = new ListViewItem(property.ToString());
				item.Tag = property;
				string description;
				if (statistics.PropertyComments.TryGetValue(property, out description))
					item.SubItems.Add(description);
				// --------------------------------------------------
				item.Checked = allProperties[property];
				// --------------------------------------------------
				// set property color as background
				item.BackColor = PropertyColorProvider.Get(property);
				// if property color is too dark, use light foreground color
				if (Utilities.Functions.GetLuminanceOfColor(PropertyColorProvider.Get(property)) < 0.5f)
					item.ForeColor = Color.White;
				else
					item.ForeColor = Color.Black;
				// --------------------------------------------------
				listViewProperties.Items.Add(item);
			}
		}

		private void initElementTypeList(MeshStatistics statistics)
		{
			listViewElementTypes.Columns.Add("Element type", 200);

			ElementType[] allTypes = statistics.GetIncludedElementTypesArray();

			foreach (ElementType type in allTypes)
			{
				ListViewItem item = new ListViewItem(type.ToString());
				item.Checked = true;
				// --------------------------------------------------
				if (sceneFacade.MeshFilename == previousMeshFilename && checkedElementTypes != null && !checkedElementTypes.Contains(type))
					item.Checked = false;
				else
					item.Checked = true;
				// --------------------------------------------------
				listViewElementTypes.Items.Add(item);
			}
		}

		private void doIt()
		{
			Cursor temp = this.Cursor;
			this.Cursor = Cursors.WaitCursor;
			longOpNotifier.Begin();

			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
			if (sceneFacade.ContainsMesh && statistics != null)
			{
				CutInfo cutInfo = new CutInfo();

				cutInfo.Action = CutInfo.ActionType.ShowHideElements;
				cutInfo.ElementPropertiesToShow = getPropertiesToShow(statistics);
				cutInfo.ElementTypesToShow = getElementTypesToShow(statistics);

				sceneFacade.PerformAction(AvailableAction.CutMesh, cutInfo);
				// ...
				saveState(statistics);
			}
			longOpNotifier.End();
			this.Cursor = temp;
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

		private void saveState(MeshStatistics statistics)
		{
			ElementType[] allTypes = statistics.GetIncludedElementTypesArray();

			previousMeshFilename = sceneFacade.MeshFilename;
			checkedElementTypes = new Set<ElementType>();
			foreach (int index in listViewElementTypes.CheckedIndices)
				checkedElementTypes.Add(allTypes[index]);
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
		
		private void listViewProperties_SelectedIndexChanged(object sender, EventArgs e)
		{
			listViewProperties.SelectedIndices.Clear();
		}
		
		private void listViewElementTypes_SelectedIndexChanged(object sender, EventArgs e)
		{
			listViewElementTypes.SelectedIndices.Clear();
		}

		private void buttonSelectAllProperties_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in listViewProperties.Items)
				item.Checked = true;
		}

		private void buttonSelectNoneProperties_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in listViewProperties.Items)
				item.Checked = false;
		}

		private void buttonSelectAllElementTypes_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in listViewElementTypes.Items)
				item.Checked = true;
		}

		private void buttonSelectNoneElementTypes_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in listViewElementTypes.Items)
				item.Checked = false;
		}

		#endregion

	}
}

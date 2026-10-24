using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MeshEditor.AvaloniaUI.Controls;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Data;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Shows/hides elements by property, element type and data value range (port of MeshEditor.WinUI.ShowHideElementsForm).
	/// </summary>
	public partial class ShowHideElementsDialog : Window
	{
		#region Fields, Constructor

		private readonly SceneFacade sceneFacade;
		private SortedDictionary<Property, bool> allProperties;
		private readonly LongOpNotifier longOpNotifier;

		internal readonly CheckedListPanel listViewProperties;
		internal readonly CheckedListPanel listViewElementTypes;

		public ShowHideElementsDialog()
			: this(SceneFacade.GetEmptyScene(), new LongOpNotifier())
		{ }

		public ShowHideElementsDialog(SceneFacade sceneFacade, LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			this.sceneFacade = sceneFacade;
			this.longOpNotifier = longOpNotifier;

			listViewProperties = new CheckedListPanel("Show elements with property", ("Property", 100), ("Description", 280));
			listViewElementTypes = new CheckedListPanel("Show elements of type", ("Element types", 200));
			tabPagePropertyNumbers.Content = listViewProperties;
			tabPageElementTypes.Content = listViewElementTypes;

			initLists();
		}

		#endregion

		#region Initialization

		private void initLists()
		{
			CutInfo lastUsedCutInfo = sceneFacade.GetValue(AvailableValue.LastUsedCutInfo) as CutInfo;
			this.allProperties = sceneFacade.GetValue(AvailableValue.MeshElementPropertiesSorted) as SortedDictionary<Property, bool> ?? new SortedDictionary<Property, bool>();
			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
			if (statistics != null)
			{
				initPropertyList(statistics);
				initElementTypeList(statistics, lastUsedCutInfo);
			}
			initValueLimit(lastUsedCutInfo);
		}

		private void initValueLimit(CutInfo lastUsedCutInfo)
		{
			if (sceneFacade.GetValue(AvailableValue.DataVisualizer) is IDataVisualizer dataVisualizer)
			{
				if (lastUsedCutInfo != null && lastUsedCutInfo.ValueLimit != null)
				{
					if (lastUsedCutInfo.ValueLimit.Maximum != null)
					{
						textBoxMaximum.Text = lastUsedCutInfo.ValueLimit.Maximum.ToString();
						checkBoxMaximum.IsChecked = true;
					}
					else
					{
						textBoxMaximum.Text = dataVisualizer.GetMaximumDataValue().ToString();
					}
					if (lastUsedCutInfo.ValueLimit.Minimum != null)
					{
						textBoxMinimum.Text = lastUsedCutInfo.ValueLimit.Minimum.ToString();
						checkBoxMinimum.IsChecked = true;
					}
					else
					{
						textBoxMinimum.Text = dataVisualizer.GetMinimumDataValue().ToString();
					}
					checkBoxAllNodesInRange.IsChecked = (lastUsedCutInfo.HitDecision == CutInfo.ItemHitDecision.AllNodes);
				}
				else
				{
					textBoxMaximum.Text = dataVisualizer.GetMaximumDataValue().ToString();
					textBoxMinimum.Text = dataVisualizer.GetMinimumDataValue().ToString();
				}
			}
			else
			{
				tabControl.Items.Remove(tabPageValueLimit);
			}
		}

		private void initPropertyList(MeshStatistics statistics)
		{
			foreach (Property property in allProperties.Keys)
			{
				statistics.PropertyComments.TryGetValue(property, out string description);
				var color = PropertyColorProvider.Get(property);
				var contrast = Utilities.Functions.GetContrastColor(color);
				listViewProperties.Items.Add(new CheckedListItem(property, allProperties[property], property.ToString(), description ?? string.Empty)
				{
					Background = new SolidColorBrush(ColorPickerDialog.FromDrawingColor(color)),
					Foreground = new SolidColorBrush(ColorPickerDialog.FromDrawingColor(contrast)),
				});
			}
		}

		private void initElementTypeList(MeshStatistics statistics, CutInfo lastUsedCutInfo)
		{
			foreach (ElementType type in statistics.GetIncludedElementTypesArray())
			{
				bool isChecked = lastUsedCutInfo == null || lastUsedCutInfo.ElementTypesToShow == null || lastUsedCutInfo.ElementTypesToShow.Contains(type);
				listViewElementTypes.Items.Add(new CheckedListItem(type, isChecked, type.ToString()));
			}
		}

		internal async void doIt()
		{
			MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
			if (sceneFacade.ContainsMesh && statistics != null)
			{
				var previousCursor = Cursor;
				Cursor = new Cursor(StandardCursorType.Wait);
				string valueLimitError = null;
				using (longOpNotifier.Begin("Updating set of visible elements"))
				{
					CutInfo cutInfo = new CutInfo();
					cutInfo.Action = CutInfo.ActionType.ShowHideElements;
					cutInfo.ElementPropertiesToShow = getPropertiesToShow();
					cutInfo.ElementTypesToShow = getElementTypesToShow();
					try
					{
						cutInfo.ValueLimit = getDataValueLimitToShow();
					}
					catch (Exception ex)
					{
						valueLimitError = ex.Message;
					}
					cutInfo.HitDecision = checkBoxAllNodesInRange.IsChecked == true ? CutInfo.ItemHitDecision.AllNodes : CutInfo.ItemHitDecision.SomeNodes;
					sceneFacade.PerformAction(AvailableAction.CutMesh, cutInfo);
				}
				Cursor = previousCursor;
				if (valueLimitError != null)
					await MessageBox.Show(valueLimitError, "Value limit error", owner: this);
			}
		}

		private Property[] getPropertiesToShow() => listViewProperties.CheckedItems.Select(i => (Property)i.Tag).ToArray();

		private ElementType[] getElementTypesToShow() => listViewElementTypes.CheckedItems.Select(i => (ElementType)i.Tag).ToArray();

		private DataValueRange getDataValueLimitToShow()
		{
			double? min = null, max = null;
			if (checkBoxMinimum.IsChecked == true)
				min = double.Parse(textBoxMinimum.Text);
			if (checkBoxMaximum.IsChecked == true)
				max = double.Parse(textBoxMaximum.Text);
			ensureMonotony(ref min, ref max); // check if min is less than max
			return (min == null && max == null) ? null : new DataValueRange(min, max, checkBoxInverse.IsChecked == true); // null if value limit is not set
		}

		private void ensureMonotony(ref double? min, ref double? max)
		{
			if (min != null && max != null && min > max)
			{
				(textBoxMinimum.Text, textBoxMaximum.Text) = (textBoxMaximum.Text, textBoxMinimum.Text);
				throw new ArgumentException("Maximum must be greater than mininum. Values swapped.");
			}
		}

		#endregion

		#region User action handlers

		private void buttonClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void buttonApply_Click(object sender, RoutedEventArgs e)
		{
			doIt();
		}

		private void updateValueLimitControls()
		{
			if (checkBoxInverse == null)
				return; // XAML is being loaded
			textBoxMinimum.IsEnabled = checkBoxMinimum.IsChecked == true;
			textBoxMaximum.IsEnabled = checkBoxMaximum.IsChecked == true;
			checkBoxInverse.IsEnabled = checkBoxAllNodesInRange.IsEnabled = checkBoxMinimum.IsChecked == true || checkBoxMaximum.IsChecked == true;
		}

		private void checkBoxMinimum_CheckedChanged(object sender, RoutedEventArgs e) => updateValueLimitControls();

		private void checkBoxMaximum_CheckedChanged(object sender, RoutedEventArgs e) => updateValueLimitControls();

		#endregion
	}
}

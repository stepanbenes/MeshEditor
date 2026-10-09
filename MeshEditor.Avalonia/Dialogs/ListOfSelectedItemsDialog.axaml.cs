using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using MeshEditor.CoreInterface;
using MeshEditor.Data;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Shows the numbers of the selected entities, allows to copy them and to add/remove a property of the selected nodes
	/// (port of MeshEditor.WinUI.ListOfSelectedItemsForm).
	/// </summary>
	public partial class ListOfSelectedItemsDialog : Window
	{
		#region Fields, constructor

		private readonly SceneFacade sceneFacade;
		private readonly int nodeCount, elementCount, faceCount, edgeCount;

		public ListOfSelectedItemsDialog()
			: this(SceneFacade.GetEmptyScene())
		{ }

		public ListOfSelectedItemsDialog(SceneFacade sceneFacade)
		{
			this.sceneFacade = sceneFacade;
			InitializeComponent();
			sceneFacade.GetSelectionSummary(out nodeCount, out elementCount, out faceCount, out edgeCount);
			if (nodeCount > 0)
				comboBoxEntityType.SelectedIndex = 0;
			else if (elementCount > 0)
				comboBoxEntityType.SelectedIndex = 1;
			else if (faceCount > 0)
				comboBoxEntityType.SelectedIndex = 2;
			else if (edgeCount > 0)
				comboBoxEntityType.SelectedIndex = 3;
			else
				comboBoxEntityType.SelectedIndex = -1;
		}

		public new async Task ShowDialog(Window owner)
		{
			await this.ShowModalAsync(owner);
		}

		#endregion

		#region Event handlers

		private void buttonClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void selectallToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			richTextBox.SelectAll();
			richTextBox.Focus();
		}

		private async void copyToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty(richTextBox.Text) && Clipboard != null)
				await Clipboard.SetTextAsync(richTextBox.Text);
		}

		private async void copyselectionToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty(richTextBox.SelectedText) && Clipboard != null)
				await Clipboard.SetTextAsync(richTextBox.SelectedText);
		}

		private void comboBoxEntityType_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
		{
			if (linkLabelAddProperty == null)
				return; // XAML is being loaded
			linkLabelAddProperty.IsVisible = linkLabelRemoveProperty.IsVisible = (comboBoxEntityType.SelectedIndex == 0);
			fillTextBox();
		}

		private void textBoxContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			copySelectionToolStripMenuItem.IsVisible = !string.IsNullOrEmpty(richTextBox.SelectedText);
		}

		private void checkBoxShowProperties_CheckedChanged(object sender, RoutedEventArgs e)
		{
			fillTextBox();
		}

		private async void linkLabelAddProperty_LinkClicked(object sender, RoutedEventArgs e)
		{
			var propertyToAdd = await getPropertyValue("Specify property to be added");
			if (propertyToAdd.HasValue)
			{
				sceneFacade.PerformAction(AvailableAction.AddPropertyToSelectedNodes, propertyToAdd.Value);
				fillTextBox();
			}
		}

		private async void linkLabelRemoveProperty_LinkClicked(object sender, RoutedEventArgs e)
		{
			var propertyToRemove = await getPropertyValue("Specify property to be removed");
			if (propertyToRemove.HasValue)
			{
				sceneFacade.PerformAction(AvailableAction.RemovePropertyFromSelectedNodes, propertyToRemove.Value);
				fillTextBox();
			}
		}

		#endregion

		#region Private methods

		private async Task<Property?> getPropertyValue(string description)
		{
			var form = new InputValueDialog("Insert property number", description);
			int value = 0;
			form.InputValueValidating += (s, ea) =>
			{
				if (!int.TryParse(form.InputValue, out value))
					ea.Reject("Inserted value is not an integer", "Please input valid integer value" + Environment.NewLine + "in range <" + int.MinValue + "; " + int.MaxValue + ">");
			};
			if (await form.ShowDialog(this))
				return new Property(value);
			return null;
		}

		private void fillTextBox()
		{
			if (labelItems == null || richTextBox == null || sceneFacade == null)
				return; // XAML is being loaded
			bool showCompleteInfo = checkBoxShowCompleteInfo.IsChecked == true;
			switch (comboBoxEntityType.SelectedIndex)
			{
				case 0: // Nodes
					labelItems.Text = "(" + nodeCount + " nodes)";
					richTextBox.Text = sceneFacade.GetDescriptionOfSelectedItems(ItemTypeToSelect.Node, showCompleteInfo);
					break;
				case 1: // Elements
					labelItems.Text = "(" + elementCount + " elements)";
					richTextBox.Text = sceneFacade.GetDescriptionOfSelectedItems(ItemTypeToSelect.Element, showCompleteInfo);
					break;
				case 2: // Faces
					labelItems.Text = "(" + faceCount + " faces)";
					richTextBox.Text = sceneFacade.GetDescriptionOfSelectedItems(ItemTypeToSelect.Face, showCompleteInfo);
					break;
				case 3: // Edges
					labelItems.Text = "(" + edgeCount + " edges)";
					richTextBox.Text = sceneFacade.GetDescriptionOfSelectedItems(ItemTypeToSelect.Edge, showCompleteInfo);
					break;
				default:
					richTextBox.Text = string.Empty;
					break;
			}
		}

		#endregion
	}
}

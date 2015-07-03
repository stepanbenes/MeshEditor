using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MeshEditor.CoreInterface;
using MeshEditor.Utilities;
using MeshEditor.Data;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// Tento dialog umozni uzivateli zobrazit seznam cisel vybranych entit
	/// a dale s nimi pracovat, hlavne pak tyto cisla zkopirovat a ulozit do samostatného souboru.
	/// Doplnkovou funkci je pak moznost pridavat ci odebirat cisla vlastnosti vybranym uzlum.
	/// </summary>
	public partial class ListOfSelectedItemsForm : Form
	{

		#region Fields, constructor

		private SceneFacade sceneFacade;
		private int nodeCount, elementCount, faceCount, edgeCount;

		public ListOfSelectedItemsForm(SceneFacade sceneFacade)
		{
			InitializeComponent();
			this.sceneFacade = sceneFacade;

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

		#endregion

		#region Event handlers

		private void buttonClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void selectallToolStripMenuItem_Click(object sender, EventArgs e)
		{
			richTextBox.SelectAll();
			richTextBox.Focus();
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(richTextBox.Text))
				Clipboard.SetText(richTextBox.Text);
		}

		private void copyselectionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(richTextBox.SelectedText))
				Clipboard.SetText(richTextBox.SelectedText);
		}

		private void comboBoxEntityType_SelectedIndexChanged(object sender, EventArgs e)
		{
			linkLabelAddProperty.Visible = linkLabelRemoveProperty.Visible = (comboBoxEntityType.SelectedIndex == 0);
			// ================================================================================================
			fillTextBox();
		}

		private void textBoxContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			copySelectionToolStripMenuItem.Visible = !string.IsNullOrEmpty(richTextBox.SelectedText);
		}

		private void checkBoxShowProperties_CheckedChanged(object sender, EventArgs e)
		{
			fillTextBox();
		}

		private void linkLabelAddProperty_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Property propertyToAdd;
			if (getPropertyValue(out propertyToAdd, "Specify property to be added"))
			{
				sceneFacade.PerformAction(AvailableAction.AddPropertyToSelectedNodes, propertyToAdd);
				fillTextBox();
				//MessageBox.Show("Property " + propertyToAdd + " has been added to selected nodes.", "Property has been added", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void linkLabelRemoveProperty_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Property propertyToRemove;
			if (getPropertyValue(out propertyToRemove, "Specify property to be removed"))
			{
				sceneFacade.PerformAction(AvailableAction.RemovePropertyFromSelectedNodes, propertyToRemove);
				fillTextBox();
				//MessageBox.Show("Property " + propertyToRemove + " has been removed from selected nodes.", "Property has been removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
	
		#endregion

		#region Private methods

		private bool getPropertyValue(out Property property, string description)
		{
			property = Property.Zero;

			InputValueForm form = new InputValueForm("Insert property number", description);
			int value = 0;

			form.InputValueValidating += delegate(object sender, CancelEventArgs ea)
			{
				if (!int.TryParse(form.InputValue, out value))
				{
					ea.Cancel = true;
					MessageBox.Show("Please input valid integer value" + Environment.NewLine + "in range <" + int.MinValue + "; " + int.MaxValue + ">", "Inserted value is not an integer", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			};

			if (form.ShowDialog() == DialogResult.OK)
			{
				property = new Property(value);
				return true;
			}
			return false;
		}

		private void fillTextBox()
		{
			richTextBox.Text = string.Empty;
			StringBuilder text = new StringBuilder();
			switch (comboBoxEntityType.SelectedIndex)
			{
				case 0: // Nodes
					labelItems.Text = "(" + nodeCount + " nodes)";
					richTextBox.Text = sceneFacade.GetDescriptionOfSelectedItems(MeshEditor.Data.ItemTypeToSelect.Node, checkBoxShowCompleteInfo.Checked);
					break;
				case 1: // Elements
					labelItems.Text = "(" + elementCount + " elements)";
					richTextBox.Text = sceneFacade.GetDescriptionOfSelectedItems(MeshEditor.Data.ItemTypeToSelect.Element, checkBoxShowCompleteInfo.Checked);
					break;
				case 2: // Faces
					labelItems.Text = "(" + faceCount + " faces)";
					richTextBox.Text = sceneFacade.GetDescriptionOfSelectedItems(MeshEditor.Data.ItemTypeToSelect.Face, checkBoxShowCompleteInfo.Checked);
					break;
				case 3: // Edges
					labelItems.Text = "(" + edgeCount + " edges)";
					richTextBox.Text = sceneFacade.GetDescriptionOfSelectedItems(MeshEditor.Data.ItemTypeToSelect.Edge, checkBoxShowCompleteInfo.Checked);
					break;
				default:
					richTextBox.Text = string.Empty;
					break;
			}
		}

		#endregion
	
	}
}

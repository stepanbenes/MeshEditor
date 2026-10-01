using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MeshEditor.CoreInterface;
using MeshEditor.Data;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// Dialog for entering the property number of the selected entities with a link to edit the property comment
	/// (port of MeshEditor.WinUI.PropertyInputForm).
	/// </summary>
	public class PropertyInputDialog : InputValueDialog
	{
		private readonly SceneFacade sceneFacade;

		/// <summary>
		/// Last confirmed property number (independent of InputValueDialog.SavedValue like in the WinForms UI).
		/// </summary>
		public new static string SavedValue = "0";

		public PropertyInputDialog(string caption, string infoText, SceneFacade sceneFacade)
			: base(caption, infoText, SavedValue)
		{
			this.sceneFacade = sceneFacade;

			var link = new HyperlinkButton
			{
				Name = "linkLabelEditPropertyComment",
				Content = "Edit property comment",
				Padding = new Thickness(0),
				Margin = new Thickness(0, 6, 0, 0),
			};
			link.Click += linkLabelEditPropertyComment_LinkClicked;
			ContentBelowValue = link;
		}

		protected override void RememberValue(string value)
		{
			SavedValue = value;
		}

		internal HyperlinkButton EditPropertyCommentLink => (HyperlinkButton)ContentBelowValue;

		private async void linkLabelEditPropertyComment_LinkClicked(object sender, RoutedEventArgs e)
		{
			if (await validateInput())
			{
				if (int.TryParse(InputValue, out int value))
				{
					Property property = new Property(value);
					MeshStatistics statistics = sceneFacade.GetValue(AvailableValue.MeshStatistics) as MeshStatistics;
					statistics.PropertyComments.TryGetValue(property, out string oldComment);
					var inputValueForm = new InputValueDialog("Set property comment", "Property " + property + ":", oldComment)
					{
						SizeToContent = SizeToContent.Height,
						Width = 500,
					};
					if (await inputValueForm.ShowDialog(this))
					{
						// save comment
						if (string.IsNullOrEmpty(inputValueForm.InputValue))
							statistics.PropertyComments.Remove(property);
						else
							statistics.PropertyComments[property] = inputValueForm.InputValue;
						if (oldComment != inputValueForm.InputValue)
						{
							// the mesh now has unsaved changes
							sceneFacade.SetValue(AvailableValue.UnsavedChangesInMesh, true);
						}
					}
				}
				else
				{
					Debug.Assert(false);
				}
			}
			else
			{
				focusTextbox();
			}
		}
	}
}

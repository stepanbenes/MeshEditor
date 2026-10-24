using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Ports of MeshInfoForm, PropertyCommandForm and HistogramViewer.
	/// </summary>
	public class MeshInfoDialogTests
	{
		// 2x2 quadrilaterals (GiD format) with element properties 1 and 2
		private static string meshPath => Path.Combine(AppContext.BaseDirectory, "TestData", "props.msh");

		private static async Task<MainWindow> openWindowWithMesh()
		{
			var window = new MainWindow(new[] { meshPath }) { WindowState = WindowState.Normal, Width = 800, Height = 600 };
			window.Show();
			var timeout = DateTime.UtcNow.AddSeconds(30);
			while (!window.ActiveViewport.SceneFacade.ContainsMesh || window.ActiveViewport.IOActionInProgress)
			{
				if (DateTime.UtcNow > timeout)
					Assert.Fail("mesh was not loaded in time");
				Dispatcher.UIThread.RunJobs();
				await Task.Delay(20);
			}
			return window;
		}

		private static async Task<MeshInfoDialog> openMeshInfo(MainWindow window)
		{
			var dialog = new MeshInfoDialog(window.ActiveViewport.SceneFacade, window.LongOpNotifier);
			dialog.Show(window);
			await dialog.NodeCountComputed;
			Dispatcher.UIThread.RunJobs();
			return dialog;
		}

		private static T waitForWindow<T>(Func<T> find) where T : Window
		{
			for (int i = 0; i < 200; i++)
			{
				Dispatcher.UIThread.RunJobs();
				var window = find();
				if (window != null)
					return window;
			}
			Assert.Fail($"{typeof(T).Name} was not opened");
			return null;
		}

		[AvaloniaTest]
		public async Task Shows_basic_info_and_property_descriptions()
		{
			var window = await openWindowWithMesh();
			var dialog = await openMeshInfo(window);

			Assert.That(dialog.labelNodeCount.Text, Is.EqualTo("Node count: 9"));
			Assert.That(dialog.labelElementCount.Text, Is.EqualTo("Element count: 4"));
			Assert.That(dialog.labelBeamCount.Text, Is.EqualTo("(Beam count: 0)"));
			Assert.That(dialog.labelFaceCount.Text, Is.EqualTo("Face count: 4"));
			Assert.That(dialog.labelEdgeCount.Text, Is.EqualTo("Edge count: 12"));

			Assert.That(dialog.Rows.Select(r => r.PropertyText), Is.EqualTo(new[] { "1", "2" }), "sorted by property");
			Assert.That(dialog.Rows.All(r => r.EntityText == "Region"), Is.True);
			Assert.That(dialog.dataGridViewPropertyDescriptions.SelectedIndex, Is.EqualTo(0), "first row is current");
		}

		[AvaloniaTest]
		public async Task Border_limit_sliders_keep_their_order()
		{
			var window = await openWindowWithMesh();
			var dialog = await openMeshInfo(window);

			Assert.That(dialog.firstBorderLimitLabel.Text, Is.EqualTo("First border limit: 10°"));
			Assert.That(dialog.secondBorderLimitLabel.Text, Is.EqualTo("Second border limit: 30°"));
			Assert.That(dialog.histogramViewer.Histogram, Is.Not.Null);

			dialog.firstBorderLimitTrackBar.Value = 50;
			Assert.That(dialog.secondBorderLimitTrackBar.Value, Is.EqualTo(50), "second limit follows the first one");
			Assert.That(dialog.histogramViewer.FirstLimit, Is.EqualTo(50));

			dialog.secondBorderLimitTrackBar.Value = 20;
			Assert.That(dialog.firstBorderLimitTrackBar.Value, Is.EqualTo(20), "first limit follows the second one");
			Assert.That(dialog.secondBorderLimitLabel.Text, Is.EqualTo("Second border limit: 20°"));
		}

		[AvaloniaTest]
		public async Task Edit_comment_updates_all_rows_of_the_property()
		{
			var window = await openWindowWithMesh();
			var scene = window.ActiveViewport.SceneFacade;
			var dialog = await openMeshInfo(window);
			dialog.dataGridViewPropertyDescriptions.SelectedIndex = 1;

			Task edit = dialog.editPropertyComment();
			var input = waitForWindow(() => dialog.OwnedWindows.OfType<InputValueDialog>().FirstOrDefault());
			Assert.That(input.Title, Is.EqualTo("Set description of property"));
			Assert.That(input.labelText.Text, Is.EqualTo("Property 2:"));
			input.textBoxValue.Text = "steel";
			input.buttonOK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			await edit;

			Assert.That(dialog.Rows[1].Comment, Is.EqualTo("steel"));
			var statistics = (MeshStatistics)scene.GetValue(AvailableValue.MeshStatistics);
			Assert.That(statistics.PropertyComments[new Property(2)], Is.EqualTo("steel"));
			Assert.That((bool)scene.GetValue(AvailableValue.UnsavedChangesInMesh), Is.True);
		}

		[AvaloniaTest]
		public async Task Edit_commands_with_the_command_editor()
		{
			var window = await openWindowWithMesh();
			var scene = window.ActiveViewport.SceneFacade;
			var dialog = await openMeshInfo(window);

			Task edit = dialog.editPropertyCommands();
			var editor = waitForWindow(() => dialog.OwnedWindows.OfType<PropertyCommandDialog>().FirstOrDefault());
			Assert.That(editor.labelPropertyNumber.Text, Is.EqualTo("1 (Region)"));
			Assert.That(editor.buttonRemoveCommand.IsEnabled, Is.False, "no command yet");

			editor.buttonAddCommand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(editor.Commands.Count, Is.EqualTo(1));
			Assert.That(editor.comboBoxAllCommands.SelectedItem, Is.EqualTo("Command " + editor.Commands[0].Type));
			// one row per variable of the command pattern (the core numbers them, e.g. "ndof1", "prop1")
			Assert.That(editor.VariableRows.Select(r => r.Name), Is.EqualTo(editor.Commands[0].GetAllVariables()));

			var editable = editor.VariableRows.FirstOrDefault(r => !r.ReadOnly);
			if (editable != null)
			{
				editable.ValueBox.Text = "3";
				editor.dataGridViewVariables_CellEndEdit(editable);
				Assert.That(editor.Commands[0].VariableValueMap[editable.Name], Is.EqualTo("3"));
			}
			string expected = editor.Commands[0].ToString();
			Assert.That(editor.labelResultText.Text, Is.EqualTo(expected));

			editor.buttonOK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			await edit;

			Assert.That(dialog.Rows[0].Commands, Is.EqualTo(expected));
			var statistics = (MeshStatistics)scene.GetValue(AvailableValue.MeshStatistics);
			Assert.That(statistics.PropertyCommands[new PropertyEntityPair(new Property(1), EntityType.Region)].Count, Is.EqualTo(1));
			Assert.That((bool)scene.GetValue(AvailableValue.UnsavedChangesInMesh), Is.True);
		}

		[AvaloniaTest]
		public void Command_editor_cancel_keeps_original_commands()
		{
			var original = new PropertyCommand(new Property(5), PropertyCommand.CommandType.ndofn);
			var saved = new System.Collections.Generic.List<PropertyCommand> { original };
			var editor = new PropertyCommandDialog(new PropertyEntityPair(new Property(5), EntityType.Vertex), saved);
			Task<bool> result = editor.ShowDialog(null);
			Dispatcher.UIThread.RunJobs();

			Assert.That(editor.comboBoxAllCommands.SelectedIndex, Is.EqualTo(0));
			editor.buttonRemoveCommand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Assert.That(editor.Commands, Is.Empty);
			editor.buttonCancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(result.IsCompleted && !result.Result, Is.True);
			Assert.That(saved, Is.EqualTo(new[] { original }), "the editor works on clones");
		}
	}
}

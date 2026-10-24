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
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Port of CutEditorForm (cut / select area by expression).
	/// </summary>
	public class CutEditorDialogTests
	{
		// 2x2 quadrilaterals in the plane z = 0, x and y in <0; 2>
		private static string meshPath => Path.Combine(AppContext.BaseDirectory, "TestData", "props.msh");

		private static async Task<(MainWindow window, CutEditorDialog editor)> openEditor()
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
			var editor = new CutEditorDialog(window.ActiveViewport.SceneFacade, window.LongOpNotifier);
			editor.Show(window);
			Dispatcher.UIThread.RunJobs();
			return (window, editor);
		}

		[AvaloniaTest]
		public async Task Initial_state_like_the_WinForms_form()
		{
			var (_, editor) = await openEditor();

			Assert.That(editor.textBoxExpression.Text, Is.EqualTo("x >= 0.0 and y >= 0.0"));
			Assert.That(editor.tabControl.SelectedItem, Is.SameAs(editor.tabPageCuttingPlanes));
			Assert.That(editor.comboBoxAction.SelectedIndex, Is.EqualTo(0));
			Assert.That(editor.buttonDoIt.Content, Is.EqualTo("Cut"));
			Assert.That(editor.checkBoxFullEntityMatch.IsChecked, Is.True);
			Assert.That(editor.comboBoxCutType.IsVisible, Is.False, "cut type only with 2+ planes");

			editor.comboBoxAction.SelectedIndex = 1;
			Assert.That(editor.buttonDoIt.Content, Is.EqualTo("Select"));
		}

		[AvaloniaTest]
		public async Task Cut_by_expression_and_restore_mesh()
		{
			var (window, editor) = await openEditor();
			var scene = window.ActiveViewport.SceneFacade;
			editor.tabControl.SelectedItem = editor.tabPageExpression;
			editor.textBoxExpression.Text = "x <= 1.0";

			await editor.doIt();
			Dispatcher.UIThread.RunJobs();

			Assert.That((bool)scene.GetValue(AvailableValue.MeshHasHiddenElements), Is.True);
			Assert.That(scene.GetValue(AvailableValue.ElementCount), Is.EqualTo(2));

			editor.buttonRestoreMesh.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(scene.GetValue(AvailableValue.ElementCount), Is.EqualTo(4));
		}

		[AvaloniaTest]
		public async Task Select_elements_by_expression()
		{
			var (window, editor) = await openEditor();
			var scene = window.ActiveViewport.SceneFacade;
			editor.tabControl.SelectedItem = editor.tabPageExpression;
			editor.textBoxExpression.Text = "y >= 1.0";
			editor.comboBoxAction.SelectedIndex = 1; // Select elements

			await editor.doIt();
			Dispatcher.UIThread.RunJobs();

			Assert.That((string)scene.GetValue(AvailableValue.SelectedItemsDescription), Does.StartWith("2 elements"));
			Assert.That(scene.GetValue(AvailableValue.ElementCount), Is.EqualTo(4), "selection does not cut");
		}

		[AvaloniaTest]
		public async Task Invalid_expression_is_reported_and_nothing_is_cut()
		{
			var (window, editor) = await openEditor();
			var scene = window.ActiveViewport.SceneFacade;
			editor.tabControl.SelectedItem = editor.tabPageExpression;
			editor.textBoxExpression.Text = "x >=";

			Task doIt = editor.doIt();
			MessageBox error = null;
			for (int i = 0; i < 200 && error == null; i++)
			{
				Dispatcher.UIThread.RunJobs();
				error = editor.OwnedWindows.OfType<MessageBox>().FirstOrDefault();
			}
			Assert.That(error, Is.Not.Null);
			Assert.That(error.Title, Is.EqualTo("Inserted expression is invalid"));
			error.Close();
			await doIt;

			Assert.That((bool)scene.GetValue(AvailableValue.MeshHasHiddenElements), Is.False);
		}

		[AvaloniaTest]
		public async Task Closing_resets_the_editor_mode()
		{
			var (_, editor) = await openEditor();
			editor.buttonCreateNewCutPlane.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(SceneFacade.EditorMode, Is.EqualTo(EditorMode.PickCuttingPlanePoint), "picking points of the plane on the mesh");
			Assert.That(editor.listBoxCutPlanes.ItemCount, Is.EqualTo(1), "hint to pick the points");

			editor.Close();
			Dispatcher.UIThread.RunJobs();
			Assert.That(SceneFacade.EditorMode, Is.EqualTo(EditorMode.Orbit));
		}
	}
}

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Port of ShowHideElementsForm.
	/// </summary>
	public class ShowHideElementsDialogTests
	{
		// 2x2 quadrilaterals with element properties 1 (2 elements) and 2 (2 elements)
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

		[AvaloniaTest]
		public async Task Lists_properties_and_element_types_without_value_limit()
		{
			var window = await openWindowWithMesh();
			var dialog = new ShowHideElementsDialog(window.ActiveViewport.SceneFacade, window.LongOpNotifier);
			dialog.Show(window);
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.listViewProperties.Items.Select(i => i.Columns[0]), Is.EqualTo(new[] { "1", "2" }));
			Assert.That(dialog.listViewProperties.Items.All(i => i.IsChecked), Is.True);
			Assert.That(dialog.listViewProperties.CheckAll.IsChecked, Is.True);
			Assert.That(dialog.listViewElementTypes.Items.Select(i => i.Columns[0]), Is.EqualTo(new[] { "QuadLinear" }));
			Assert.That(dialog.tabControl.Items.Cast<TabItem>().Select(t => t.Header), Does.Not.Contain("Value limit"), "no data visualizer -> no value limit tab");
		}

		[AvaloniaTest]
		public async Task Hiding_a_property_hides_its_elements()
		{
			var window = await openWindowWithMesh();
			var scene = window.ActiveViewport.SceneFacade;
			var dialog = new ShowHideElementsDialog(scene, window.LongOpNotifier);
			dialog.Show(window);
			Dispatcher.UIThread.RunJobs();

			dialog.listViewProperties.Items.First(i => (Property)i.Tag == new Property(1)).IsChecked = false;
			Assert.That(dialog.listViewProperties.CheckAll.IsChecked, Is.Null, "some checked -> indeterminate");

			dialog.doIt();
			Dispatcher.UIThread.RunJobs();

			Assert.That((bool)scene.GetValue(AvailableValue.MeshHasHiddenElements), Is.True);
			Assert.That(scene.GetValue(AvailableValue.ElementCount), Is.EqualTo(2), "only the 2 elements with property 2 remain visible");

			dialog.listViewProperties.CheckAll.IsChecked = true;
			Assert.That(dialog.listViewProperties.Items.All(i => i.IsChecked), Is.True);
			dialog.doIt();
			Dispatcher.UIThread.RunJobs();
			Assert.That(scene.GetValue(AvailableValue.ElementCount), Is.EqualTo(4));
		}

		[AvaloniaTest]
		public void Check_all_and_check_selected_states()
		{
			var panel = new Controls.CheckedListPanel("label", ("Property", 100));
			var window = new Window { Content = panel };
			window.Show();
			for (int i = 0; i < 3; i++)
				panel.Items.Add(new Controls.CheckedListItem(i, false, i.ToString()));
			Dispatcher.UIThread.RunJobs();
			Assert.That(panel.CheckAll.IsChecked, Is.False);

			panel.CheckAll.IsChecked = true;
			Assert.That(panel.Items.All(i => i.IsChecked), Is.True);

			panel.SelectedItems.Add(panel.Items[0]);
			panel.SelectedItems.Add(panel.Items[1]);
			Dispatcher.UIThread.RunJobs();
			Assert.That(panel.CheckSelected.IsChecked, Is.True);

			panel.CheckSelected.IsChecked = false;
			Assert.That(panel.Items.Select(i => i.IsChecked), Is.EqualTo(new[] { false, false, true }));
			Assert.That(panel.CheckAll.IsChecked, Is.Null);
		}
	}
}

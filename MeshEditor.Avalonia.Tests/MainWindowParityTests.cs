using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Verifies that the menu, tool strip and context menu of the Avalonia main window match the WinForms MainForm 1:1
	/// (same items in the same order, same texts with access keys, shortcuts, visibility and tool tips).
	/// </summary>
	public class MainWindowParityTests
	{
		private static readonly WinFormsDesignerReference reference = WinFormsDesignerReference.MainForm;

		[AvaloniaTest]
		public void MenuStrip_matches_WinForms()
		{
			var window = new MainWindow(Array.Empty<string>());
			var problems = new List<string>();
			compareMenuItems("menuStrip", window.menuStrip.Items, problems);
			Assert.That(problems, Is.Empty, string.Join(Environment.NewLine, problems));
		}

		[AvaloniaTest]
		public void ToolStrip_matches_WinForms()
		{
			var window = new MainWindow(Array.Empty<string>());
			var toolStripPanel = (StackPanel)window.toolStrip.Child;
			var problems = new List<string>();

			var expected = reference.GetChildren("toolStrip");
			var actual = toolStripPanel.Children.Select(c => c is Separator ? "-" : c.Name).ToList();
			Assert.That(actual, Is.EqualTo(expected), "tool strip items");

			foreach (var control in toolStripPanel.Children.Where(c => c is not Separator))
			{
				// WinForms shows the Text as tool tip when ToolTipText is not set
				string expectedTip = reference.GetToolTipText(control.Name) ?? reference.GetText(control.Name);
				string actualTip = ToolTip.GetTip(control) as string;
				if (expectedTip != actualTip)
					problems.Add($"{control.Name}: tool tip '{actualTip}' != '{expectedTip}'");
			}

			var dropDown = window.propertyColorsTtoolStripDropDownButton;
			var flyoutItems = ((MenuFlyout)dropDown.Flyout).Items;
			compareMenuItems("propertyColorsTtoolStripDropDownButton", flyoutItems, problems);

			Assert.That(problems, Is.Empty, string.Join(Environment.NewLine, problems));
		}

		[AvaloniaTest]
		public void ViewportContextMenu_matches_WinForms()
		{
			var window = new MainWindow(Array.Empty<string>());
			var contextMenu = window.ActiveViewport.ContextMenu;

			string describe(IEnumerable<object> items) => string.Join(" | ", items.Select(i => i is Separator ? "-" : (string)((MenuItem)i).Header));
			string describeReference(string parent) => string.Join(" | ", reference.GetChildren(parent).Select(n => n == "-" ? "-" : WinFormsDesignerReference.ToAvaloniaAccessText(reference.GetText(n))));

			Assert.That(describe(contextMenu.Items), Is.EqualTo(describeReference("contextMenuStrip")));
			var selectMenu = (MenuItem)contextMenu.Items[0];
			Assert.That(describe(selectMenu.Items), Is.EqualTo(describeReference("selectToolStripMenuItem")));
		}

		private static void compareMenuItems(string parentName, IEnumerable<object> items, List<string> problems)
		{
			var expected = reference.GetChildren(parentName);
			var actual = items.Select(i => i is Separator ? "-" : ((Control)i).Name).ToList();
			if (!expected.SequenceEqual(actual))
			{
				problems.Add($"{parentName}: items [{string.Join(", ", actual)}] != [{string.Join(", ", expected)}]");
				return;
			}

			foreach (var menuItem in items.OfType<MenuItem>())
			{
				string name = menuItem.Name;
				string expectedHeader = WinFormsDesignerReference.ToAvaloniaAccessText(reference.GetText(name));
				if (!Equals(menuItem.Header, expectedHeader))
					problems.Add($"{name}: header '{menuItem.Header}' != '{expectedHeader}'");

				string expectedShortcut = reference.GetShortcut(name);
				string actualShortcut = menuItem.InputGesture?.ToString();
				if (expectedShortcut != actualShortcut)
					problems.Add($"{name}: shortcut '{actualShortcut}' != '{expectedShortcut}'");

				if (menuItem.IsVisible != reference.IsVisible(name))
					problems.Add($"{name}: visible {menuItem.IsVisible} != {reference.IsVisible(name)}");

				string expectedTip = reference.GetToolTipText(name);
				if (expectedTip != null && expectedTip != ToolTip.GetTip(menuItem) as string)
					problems.Add($"{name}: tool tip '{ToolTip.GetTip(menuItem)}' != '{expectedTip}'");

				if (reference.HasChildren(name) || menuItem.Items.Count > 0)
					compareMenuItems(name, menuItem.Items, problems);
			}
		}
	}
}

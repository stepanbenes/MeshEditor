using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
	/// Port of SettingsForm (WinForms PropertyGrid over SceneSettings).
	/// </summary>
	public class SettingsDialogTests
	{
		private static PropertyDescriptor property(SettingsDialog dialog, string name) =>
			dialog.propertyGrid.DisplayedProperties.Single(p => p.Name == name);

		[TearDown]
		public void TearDown()
		{
			SceneSettings.Reset(); // tests change the global settings
		}

		[AvaloniaTest]
		public void Shows_categories_alphabetically_and_hides_non_browsable_properties()
		{
			SceneSettings.Reset();
			var dialog = new SettingsDialog(new List<SceneFacade>());
			dialog.Show();
			Dispatcher.UIThread.RunJobs();

			var displayed = dialog.propertyGrid.DisplayedProperties;
			Assert.That(displayed.Select(p => p.Name), Does.Not.Contain("ShowOpenGLLowVersionMessage"), "[Browsable(false)]");
			// like the WinForms PropertyGrid: categories alphabetically, properties in declaration order
			Assert.That(displayed.Take(3).Select(p => p.DisplayName), Is.EqualTo(new[] { "Smooth lines", "Smooth points", "Face lighting" }));
			var categories = displayed.Select(p => string.IsNullOrEmpty(p.Category) || p.Category == "Misc" ? "Misc" : p.Category).Distinct().ToList();
			Assert.That(categories, Is.EqualTo(categories.OrderBy(c => c, StringComparer.CurrentCulture).ToList()));
			Assert.That(property(dialog, "OpenGLVersion").IsReadOnly, Is.True);
		}

		[AvaloniaTest]
		public void Cancel_restores_changed_settings()
		{
			SceneSettings.Reset();
			float originalPointSize = SceneSettings.Instance.PointSize;
			var originalFaceColor = SceneSettings.Instance.FaceColor;
			var originalShading = SceneSettings.Instance.ShadingModel;

			var dialog = new SettingsDialog(new List<SceneFacade>());
			dialog.Show();
			Dispatcher.UIThread.RunJobs();

			dialog.propertyGrid.SetPropertyValue(property(dialog, "PointSize"), originalPointSize + 3);
			dialog.propertyGrid.SetPropertyValue(property(dialog, "FaceColor"), System.Drawing.Color.Orange);
			dialog.propertyGrid.SetPropertyValue(property(dialog, "LineSmooth"), !SceneSettings.Instance.LineSmooth);
			Assert.That(SceneSettings.Instance.PointSize, Is.EqualTo(originalPointSize + 3), "applied immediately");
			Assert.That(Scene.PointSize, Is.EqualTo(originalPointSize + 3));

			dialog.buttonCancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(SceneSettings.Instance.PointSize, Is.EqualTo(originalPointSize));
			Assert.That(Scene.PointSize, Is.EqualTo(originalPointSize), "the renderer settings are restored too");
			Assert.That(SceneSettings.Instance.FaceColor.ToArgb(), Is.EqualTo(originalFaceColor.ToArgb()));
			Assert.That(SceneSettings.Instance.ShadingModel, Is.EqualTo(originalShading));
		}

		[AvaloniaTest]
		public void Ok_keeps_changed_settings_and_reset_restores_defaults()
		{
			SceneSettings.Reset();
			float defaultBeamWidth = SceneSettings.Instance.BeamWidth;

			var dialog = new SettingsDialog(new List<SceneFacade>());
			dialog.Show();
			Dispatcher.UIThread.RunJobs();
			dialog.propertyGrid.SetPropertyValue(property(dialog, "BeamWidth"), defaultBeamWidth + 1);
			dialog.buttonOK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(SceneSettings.Instance.BeamWidth, Is.EqualTo(defaultBeamWidth + 1));

			var second = new SettingsDialog(new List<SceneFacade>());
			second.Show();
			Dispatcher.UIThread.RunJobs();
			second.buttonReset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(SceneSettings.Instance.BeamWidth, Is.EqualTo(defaultBeamWidth));
			second.buttonOK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
		}

		[AvaloniaTest]
		public void Color_change_updates_color_buffers_of_all_scenes()
		{
			SceneSettings.Reset();
			var dialog = new SettingsDialog(new List<SceneFacade>());
			dialog.Show();
			Dispatcher.UIThread.RunJobs();

			var faceColor = property(dialog, "FaceColor");
			Assert.That(faceColor.Attributes.Contains(new UpdateColorBuffersAttribute()), Is.True);
			Assert.That(property(dialog, "ShadingModel").Attributes.Contains(new RecreateBuffersAttribute()), Is.True);
			Assert.DoesNotThrow(() => dialog.propertyGrid.SetPropertyValue(faceColor, System.Drawing.Color.Red));
			Assert.That(SceneSettings.Instance.FaceColor.ToArgb(), Is.EqualTo(System.Drawing.Color.Red.ToArgb()));
		}
	}
}

using System;
using System.Threading.Tasks;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.Common.Logging;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Ports of AboutDialog, ScreenshotOptionsForm, CheckMessageBox and ProgressViewForm.
	/// </summary>
	public class SmallDialogsTests
	{
		[AvaloniaTest]
		public void AboutDialog_shows_product_information()
		{
			var dialog = new AboutDialog();
			dialog.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(dialog.Title, Is.EqualTo("About MeshEditor"));
			Assert.That(dialog.labelProductName.Text, Is.EqualTo("MeshEditor"));
			Assert.That(dialog.labelVersion.Text, Does.Match(@"^Version \d+\.\d+\.\d+"));
			Assert.That(dialog.labelCopyright.Text, Does.StartWith("Copyright"));
			Assert.That(dialog.labelCompanyName.Text, Is.EqualTo("Stepan Benes"));
			Assert.That(dialog.textBoxDescription.Text, Does.StartWith("3D graphical editor for finite element meshes"));
			Assert.That(dialog.logoPictureBox.Source, Is.Not.Null);
		}

		[AvaloniaTest]
		public void ScreenshotOptions_remember_choice_and_name_the_button()
		{
			var first = new ScreenshotOptionsDialog();
			Task<bool> result = first.ShowDialog(null);
			Dispatcher.UIThread.RunJobs();
			first.UseSelectionArea = false;
			Dispatcher.UIThread.RunJobs();
			Assert.That(first.buttonOK.Content, Is.EqualTo("Save"));

			first.UseSelectionArea = true;
			Dispatcher.UIThread.RunJobs();
			Assert.That(first.buttonOK.Content, Is.EqualTo("Select"));
			first.buttonOK.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(result.IsCompleted && result.Result, Is.True);

			var second = new ScreenshotOptionsDialog();
			Assert.That(second.UseSelectionArea, Is.True, "choice is remembered");
			Task<bool> cancelled = second.ShowDialog(null);
			Dispatcher.UIThread.RunJobs();
			second.UseSelectionArea = false;
			second.buttonCancel.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(cancelled.IsCompleted && !cancelled.Result, Is.True);
			Assert.That(new ScreenshotOptionsDialog().UseSelectionArea, Is.True, "cancel does not change the remembered choice");
		}

		[AvaloniaTest]
		public void CheckMessageBox_reports_check_box_state()
		{
			var box = new CheckMessageBox("Some message", "Low OpenGL version");
			Task shown = box.ShowDialog(null);
			Dispatcher.UIThread.RunJobs();

			Assert.That(box.Title, Is.EqualTo("Low OpenGL version"));
			Assert.That(box.label1.Text, Is.EqualTo("Some message"));
			Assert.That(box.checkBox1.Content, Is.EqualTo("Don't show this again"));
			Assert.That(box.IsChecked, Is.False);

			box.checkBox1.IsChecked = true;
			box.buttonOK.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(shown.IsCompleted, Is.True);
			Assert.That(box.IsChecked, Is.True);
		}

		[AvaloniaTest]
		public void ProgressView_shows_percentage_cancels_and_closes_only_on_quit()
		{
			var logger = new MemoryLogger();
			var progress = new ProgressViewDialog("Loading mesh", enableCancellation: true, logger);
			int cancelCount = 0;
			progress.Cancel += (s, e) => cancelCount++;
			progress.Show();
			Dispatcher.UIThread.RunJobs();

			progress.SetProgressState(42);
			progress.OperationName = "Reading nodes";
			Assert.That(progress.Title, Is.EqualTo("Loading mesh (42%)"));
			Assert.That(progress.labelOperationName.Text, Is.EqualTo("Reading nodes"));
			Assert.That(progress.progressBar.Value, Is.EqualTo(42));

			progress.SetProgressState(-1);
			Assert.That(progress.progressBar.IsIndeterminate, Is.True);

			Assert.That(progress.listBoxLog.IsVisible, Is.False);
			logger.LogWarning("Something odd");
			Dispatcher.UIThread.RunJobs();
			Assert.That(progress.listBoxLog.IsVisible, Is.True, "log appears when a record is reported");
			Assert.That(progress.listBoxLog.Items[0], Is.EqualTo("[Warning] Something odd"));

			progress.Close(); // the close button acts as Cancel
			Dispatcher.UIThread.RunJobs();
			Assert.That(progress.IsVisible, Is.True, "the user can't close it");
			Assert.That(cancelCount, Is.EqualTo(1));
			Assert.That(progress.buttonCancel.IsEnabled, Is.False);

			progress.Quit();
			Dispatcher.UIThread.RunJobs();
			Assert.That(progress.IsVisible, Is.False);
		}

	}
}

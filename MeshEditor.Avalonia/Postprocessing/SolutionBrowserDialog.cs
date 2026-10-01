using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.Common.Logging;
using MeshEditor.CoreInterface;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.AvaloniaUI.Postprocessing
{
	/// <summary>
	/// List of solutions grouped by project name (port of MeshEditor.DataVisualizer.UI.SolutionListView).
	/// </summary>
	public class SolutionListView : Panel
	{
		internal readonly DataGrid listView;
		private readonly TextBlock labelNotification = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray, IsHitTestVisible = false };

		public event EventHandler SelectedSolutionChanged;
		public event EventHandler SolutionListDoubleClick;

		public SolutionListView()
		{
			listView = new DataGrid
			{
				IsReadOnly = true,
				SelectionMode = DataGridSelectionMode.Single,
				CanUserSortColumns = false,
				HeadersVisibility = DataGridHeadersVisibility.Column,
				GridLinesVisibility = DataGridGridLinesVisibility.None,
				BorderBrush = Brush.Parse("#ABADB3"),
				BorderThickness = new Thickness(1),
				Columns =
				{
					new DataGridTextColumn { Header = "Solution ID", Binding = new Avalonia.Data.Binding(nameof(ISolutionInfo.Id)), Width = new DataGridLength(90) },
					new DataGridTextColumn { Header = "Location", Binding = new Avalonia.Data.Binding(nameof(ISolutionInfo.Location)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) },
				},
			};
			listView.SelectionChanged += (s, e) => SelectedSolutionChanged?.Invoke(this, EventArgs.Empty);
			listView.DoubleTapped += (s, e) => SolutionListDoubleClick?.Invoke(this, EventArgs.Empty);
			Children.Add(listView);
			Children.Add(labelNotification);
			Notification = null;
		}

		public ISolutionInfo SelectedSolution => listView.SelectedItem as ISolutionInfo;

		public string Notification
		{
			get => labelNotification.Text;
			set
			{
				labelNotification.Text = value ?? "";
				labelNotification.IsVisible = !string.IsNullOrWhiteSpace(value);
			}
		}

		public void SetSolutions(IEnumerable<ISolutionInfo> solutions)
		{
			var items = solutions.OrderBy(s => s.ProjectName).ThenBy(s => s.Id).ToList();
			var view = new DataGridCollectionView(items);
			view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(ISolutionInfo.ProjectName)));
			listView.ItemsSource = view;
			listView.SelectedItem = null;
			Notification = (items.Count == 0) ? "No solutions found" : null;
		}
	}

	/// <summary>
	/// Selection of a local solution to open (port of MeshEditor.DataVisualizer.UI.SolutionBrowserForm without the remote solutions).
	/// </summary>
	public class SolutionBrowserDialog : Window
	{
		public enum SolutionLocationType
		{
			Undefined,
			Local
		}

		private readonly CancellationTokenSource formClosedCancellationSource = new CancellationTokenSource();
		internal readonly SolutionListView localSolutionListView = new SolutionListView();
		internal readonly Button buttonOk = new Button { Content = "OK", Width = 100, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = true };
		internal readonly Button buttonCancel = new Button { Content = "Cancel", Width = 100, HorizontalContentAlignment = HorizontalAlignment.Center, IsCancel = true };
		internal readonly Button buttonBrowse = new Button { Content = "Browse...", Width = 100, HorizontalContentAlignment = HorizontalAlignment.Center };
		internal readonly Button buttonDeleteLocalSolution = new Button { Content = "Delete", Width = 100, HorizontalContentAlignment = HorizontalAlignment.Center };
		internal readonly Button buttonChangeDefaultSolutionDirectory = new Button { Content = "Change default solution directory...", HorizontalContentAlignment = HorizontalAlignment.Center };
		private bool dialogResult;

		public SolutionBrowserDialog()
		{
			Title = "Open solution";
			Width = 731;
			Height = 562;
			MinWidth = 450;
			MinHeight = 300;
			CanMinimize = false;
			ShowInTaskbar = false;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			var localPageButtons = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
			DockPanel.SetDock(buttonChangeDefaultSolutionDirectory, Dock.Left);
			DockPanel.SetDock(buttonDeleteLocalSolution, Dock.Right);
			localPageButtons.Children.Add(buttonChangeDefaultSolutionDirectory);
			localPageButtons.Children.Add(buttonDeleteLocalSolution);
			localPageButtons.Children.Add(new Panel());
			var localPage = new DockPanel { Margin = new Thickness(8) };
			DockPanel.SetDock(localPageButtons, Dock.Bottom);
			localPage.Children.Add(localPageButtons);
			localPage.Children.Add(localSolutionListView);
			var tabControl = new TabControl { Items = { new TabItem { Header = "Local solutions", Content = localPage } } };

			var bottom = new DockPanel { Margin = new Thickness(12, 8, 12, 12) };
			DockPanel.SetDock(buttonBrowse, Dock.Left);
			bottom.Children.Add(buttonBrowse);
			var okCancel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { buttonOk, buttonCancel } };
			bottom.Children.Add(okCancel);
			var root = new DockPanel();
			DockPanel.SetDock(bottom, Dock.Bottom);
			root.Children.Add(bottom);
			root.Children.Add(tabControl);
			Content = root;

			buttonOk.Click += (s, e) => close(true);
			buttonCancel.Click += (s, e) => close(false);
			buttonBrowse.Click += buttonBrowse_Click;
			buttonDeleteLocalSolution.Click += buttonDeleteLocalSolution_Click;
			buttonChangeDefaultSolutionDirectory.Click += buttonChangeDefaultSolutionDirectory_Click;
			localSolutionListView.SelectedSolutionChanged += localSolutionListView_SelectedSolutionChanged;
			localSolutionListView.SolutionListDoubleClick += (s, e) =>
			{
				if (localSolutionListView.SelectedSolution != null)
					close(true);
			};

			LoadingTask = initLocalSolutionListAsync(formClosedCancellationSource.Token); // start loading LOCAL solutions
			updateButtonStates();
		}

		public SolutionLocationType SolutionLocation { get; private set; }
		public string LocalSolutionFileName { get; private set; }

		/// <summary>
		/// Loading of the local solution list (for UI tests).
		/// </summary>
		internal Task LoadingTask { get; private set; }

		/// <summary>
		/// Shows the dialog, returns true if a solution was chosen (DialogResult.OK).
		/// </summary>
		public new async Task<bool> ShowDialog(Window owner)
		{
			await base.ShowDialog(owner);
			return dialogResult;
		}

		private void close(bool result)
		{
			dialogResult = result;
			Close();
		}

		protected override void OnClosed(EventArgs e)
		{
			formClosedCancellationSource.Cancel();
			base.OnClosed(e);
		}

		private async Task initLocalSolutionListAsync(CancellationToken cancellationToken)
		{
			string taskName = "Loading local solutions";
			localSolutionListView.Notification = taskName + "...";

			var logger = new MemoryLogger();
			IEnumerable<ISolutionInfo> solutions = Enumerable.Empty<ISolutionInfo>();
			try
			{
				solutions = await SolutionHub.EnumerateAllLocalSolutionsAsync(SolutionHub.GetLocalStorageDefaultDirectory(), true, cancellationToken, logger);
			}
			catch (OperationCanceledException)
			{ }
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show(taskName, ex, logger);
			}
			localSolutionListView.SetSolutions(solutions);
			updateButtonStates();
		}

		private void updateButtonStates()
		{
			buttonOk.IsEnabled = buttonDeleteLocalSolution.IsEnabled = localSolutionListView.IsEnabled && localSolutionListView.SelectedSolution != null;
		}

		private async void buttonDeleteLocalSolution_Click(object sender, RoutedEventArgs e)
		{
			ISolutionInfo selectedSolution = localSolutionListView.SelectedSolution;
			Debug.Assert(selectedSolution != null);
			if (selectedSolution != null)
			{
				object buttonText = buttonDeleteLocalSolution.Content;
				buttonDeleteLocalSolution.IsEnabled = false;
				buttonDeleteLocalSolution.Content = "Deleting...";
				localSolutionListView.IsEnabled = false;
				var logger = new MemoryLogger();
				try
				{
					var solutionHub = SolutionHub.OpenLocal(selectedSolution.Location, logger);
					await solutionHub.DeleteAsync(layerIdOrName: null, deleteAll: true, cancellationToken: formClosedCancellationSource.Token);
				}
				catch (Exception ex)
				{
					await ExceptionReportDialog.Show("Deleting local solution", ex, logger);
				}
				finally
				{
					buttonDeleteLocalSolution.Content = buttonText;
					localSolutionListView.IsEnabled = true;
					updateButtonStates();
				}

				await initLocalSolutionListAsync(formClosedCancellationSource.Token);
			}
		}

		private void localSolutionListView_SelectedSolutionChanged(object sender, EventArgs e)
		{
			ISolutionInfo selectedSolution = localSolutionListView.SelectedSolution;
			if (selectedSolution != null)
			{
				LocalSolutionFileName = selectedSolution.Location;
				SolutionLocation = SolutionLocationType.Local;
			}
			else
			{
				LocalSolutionFileName = null;
				SolutionLocation = SolutionLocationType.Undefined;
			}
			updateButtonStates();
		}

		private async void buttonChangeDefaultSolutionDirectory_Click(object sender, RoutedEventArgs e)
		{
			var options = new FolderPickerOpenOptions { Title = "Select default solution directory", AllowMultiple = false };
			string currentDirectory = SolutionHub.GetLocalStorageDefaultDirectory();
			if (!string.IsNullOrEmpty(currentDirectory) && System.IO.Directory.Exists(currentDirectory))
				options.SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(currentDirectory);
			var folders = await StorageProvider.OpenFolderPickerAsync(options);
			string path = folders.FirstOrDefault()?.TryGetLocalPath();
			if (path != null)
			{
				SolutionHub.SetLocalStorageDefaultDirectory(path);
				await initLocalSolutionListAsync(formClosedCancellationSource.Token);
			}
		}

		private async void buttonBrowse_Click(object sender, RoutedEventArgs e)
		{
			var options = new FilePickerOpenOptions
			{
				Title = "Open",
				AllowMultiple = false,
				FileTypeFilter = FileDialogFilter.Parse($"Solution files (*{SceneFacade.SolutionFileExtension})|*{SceneFacade.SolutionFileExtension}|All files (*.*)|*.*"),
			};
			string currentDirectory = SolutionHub.GetLocalStorageDefaultDirectory();
			if (!string.IsNullOrEmpty(currentDirectory) && System.IO.Directory.Exists(currentDirectory))
				options.SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(currentDirectory);
			var files = await StorageProvider.OpenFilePickerAsync(options);
			string path = files.FirstOrDefault()?.TryGetLocalPath();
			if (path != null)
			{
				LocalSolutionFileName = path;
				SolutionLocation = SolutionLocationType.Local;
				close(true);
			}
		}
	}
}

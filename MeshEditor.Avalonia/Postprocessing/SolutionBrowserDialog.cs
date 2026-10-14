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
using Avalonia.Threading;
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
	/// Selection of a local or remote solution to open (port of MeshEditor.DataVisualizer.UI.SolutionBrowserForm).
	/// The remote solutions tab is shown only when the remote storage (Azure Blob Storage) is configured. It lists the remote solutions
	/// of the user signed in with a Microsoft account.
	/// </summary>
	public class SolutionBrowserDialog : Window
	{
		public enum SolutionLocationType
		{
			Undefined,
			Local,
			Remote
		}

		private readonly CancellationTokenSource formClosedCancellationSource = new CancellationTokenSource();
		internal readonly TabControl tabControl;
		internal readonly TabItem tabPageLocalSolutions;
		internal readonly TabItem tabPageRemoteSolutions;
		internal readonly SolutionListView localSolutionListView = new SolutionListView();
		internal readonly SolutionListView remoteSolutionListView = new SolutionListView();
		internal readonly Button buttonUploadLocalSolution = new Button { Content = "Upload to remote", MinWidth = 100, HorizontalContentAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
		internal readonly Button buttonDeleteRemoteSolution = new Button { Content = "Delete", Width = 100, HorizontalContentAlignment = HorizontalAlignment.Center };
		internal readonly Button buttonRefreshRemoteSolutions = new Button { Content = "Refresh", Width = 100, HorizontalContentAlignment = HorizontalAlignment.Center };
		internal readonly TextBlock labelRemoteUser = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
		internal readonly Button buttonSignIn = new Button { Content = "Sign in with Microsoft account", MinWidth = 100, HorizontalContentAlignment = HorizontalAlignment.Center };
		internal readonly Button buttonSignOut = new Button { Content = "Sign out", Width = 100, HorizontalContentAlignment = HorizontalAlignment.Center };
		private bool remoteSolutionsTabOpened = false;
		private CancellationTokenSource signInCancellationSource;
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
			var localPageRightButtons = new StackPanel { Orientation = Orientation.Horizontal, Children = { buttonUploadLocalSolution, buttonDeleteLocalSolution } };
			DockPanel.SetDock(localPageRightButtons, Dock.Right);
			localPageButtons.Children.Add(buttonChangeDefaultSolutionDirectory);
			localPageButtons.Children.Add(localPageRightButtons);
			localPageButtons.Children.Add(new Panel());
			var localPage = new DockPanel();
			DockPanel.SetDock(localPageButtons, Dock.Bottom);
			localPage.Children.Add(localPageButtons);
			localPage.Children.Add(localSolutionListView);

			var remotePageButtons = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
			DockPanel.SetDock(buttonRefreshRemoteSolutions, Dock.Left);
			DockPanel.SetDock(buttonDeleteRemoteSolution, Dock.Right);
			remotePageButtons.Children.Add(buttonRefreshRemoteSolutions);
			remotePageButtons.Children.Add(buttonDeleteRemoteSolution);
			remotePageButtons.Children.Add(new Panel());
			var remoteUserBar = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
			var signInOutButtons = new StackPanel { Orientation = Orientation.Horizontal, Children = { buttonSignIn, buttonSignOut } };
			DockPanel.SetDock(signInOutButtons, Dock.Right);
			remoteUserBar.Children.Add(signInOutButtons);
			remoteUserBar.Children.Add(labelRemoteUser);
			var remotePage = new DockPanel();
			DockPanel.SetDock(remoteUserBar, Dock.Top);
			DockPanel.SetDock(remotePageButtons, Dock.Bottom);
			remotePage.Children.Add(remoteUserBar);
			remotePage.Children.Add(remotePageButtons);
			remotePage.Children.Add(remoteSolutionListView);

			tabPageLocalSolutions = new TabItem { Header = "Local solutions", Content = localPage };
			tabPageRemoteSolutions = new TabItem { Header = "Remote solutions", Content = remotePage };
			tabControl = new TabControl { Margin = new Thickness(12, 12, 12, 0), Padding = new Thickness(0, 6, 0, 0), Items = { tabPageLocalSolutions, tabPageRemoteSolutions } };

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
			buttonUploadLocalSolution.Click += buttonUploadLocalSolution_Click;
			buttonDeleteRemoteSolution.Click += buttonDeleteRemoteSolution_Click;
			buttonRefreshRemoteSolutions.Click += (s, e) => RemoteLoadingTask = initRemoteSolutionListAsync(formClosedCancellationSource.Token);
			buttonSignIn.Click += buttonSignIn_Click;
			buttonSignOut.Click += buttonSignOut_Click;
			remoteSolutionListView.SelectedSolutionChanged += (s, e) => updateSelectedSolution();
			remoteSolutionListView.SolutionListDoubleClick += (s, e) =>
			{
				if (remoteSolutionListView.SelectedSolution != null)
					close(true);
			};
			tabControl.SelectionChanged += tabControl_SelectionChanged;

			LoadingTask = initLocalSolutionListAsync(formClosedCancellationSource.Token); // start loading LOCAL solutions

			// remote solutions only when the remote storage is set up (they are loaded when the tab is opened)
			bool remoteStorageConfigured = SolutionHub.IsRemoteStorageConfigured;
			tabPageRemoteSolutions.IsVisible = remoteStorageConfigured;
			buttonUploadLocalSolution.IsVisible = remoteStorageConfigured;
			setRemoteUser(null);
			updateButtonStates();
		}

		public SolutionLocationType SolutionLocation { get; private set; }
		public string LocalSolutionFileName { get; private set; }
		public int? RemoteSolutionId { get; private set; }

		/// <summary>
		/// User signed in to the remote storage (null until the remote solutions tab is opened or when nobody is signed in).
		/// </summary>
		public RemoteUser RemoteUser { get; private set; }

		/// <summary>
		/// Loading of the local solution list (for UI tests).
		/// </summary>
		internal Task LoadingTask { get; private set; }

		/// <summary>
		/// Sign-in check and loading of the remote solution list (for UI tests), null until the remote solutions tab is opened.
		/// </summary>
		internal Task RemoteLoadingTask { get; private set; }

		/// <summary>
		/// Loads the remote solution list (tests replace it).
		/// </summary>
		internal static Func<CancellationToken, MemoryLogger, Task<IEnumerable<ISolutionInfo>>> EnumerateRemoteSolutionsAsync { get; set; } =
			(cancellationToken, logger) => SolutionHub.EnumerateAllRemoteSolutionsAsync(cancellationToken, logger);

		/// <summary>
		/// Shows the dialog, returns true if a solution was chosen (DialogResult.OK).
		/// </summary>
		public new async Task<bool> ShowDialog(Window owner)
		{
			await this.ShowModalAsync(owner);
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
			signInCancellationSource?.Cancel();
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

		private async Task initRemoteSolutionListAsync(CancellationToken cancellationToken)
		{
			string taskName = "Loading remote solutions";
			remoteSolutionListView.SetSolutions(Enumerable.Empty<ISolutionInfo>());
			remoteSolutionListView.Notification = taskName + "...";

			var logger = new MemoryLogger();
			IEnumerable<ISolutionInfo> solutions = Enumerable.Empty<ISolutionInfo>();
			try
			{
				solutions = await EnumerateRemoteSolutionsAsync(cancellationToken, logger);
			}
			catch (OperationCanceledException)
			{ }
			catch (RemoteSignInRequiredException)
			{
				setRemoteUser(null); // the sign-in expired or was revoked
				return;
			}
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show(taskName, ex, logger);
			}
			remoteSolutionListView.SetSolutions(solutions);
			updateButtonStates();
		}

		/// <summary>
		/// Checks who is signed in (the sign-in is remembered), then loads the remote solutions of the user.
		/// </summary>
		private async Task initRemoteSolutionsTabAsync(CancellationToken cancellationToken)
		{
			remoteSolutionListView.SetSolutions(Enumerable.Empty<ISolutionInfo>());
			remoteSolutionListView.Notification = "Loading remote solutions...";
			RemoteUser user = null;
			try
			{
				user = await SolutionHub.GetRemoteUserAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show("Reading the Microsoft account sign-in", ex, new MemoryLogger());
			}
			setRemoteUser(user);
			if (user != null)
			{
				await initRemoteSolutionListAsync(cancellationToken);
			}
		}

		private void setRemoteUser(RemoteUser user)
		{
			RemoteUser = user;
			labelRemoteUser.Text = user != null ? $"Signed in as {user}" : "Not signed in";
			buttonSignIn.IsVisible = user == null;
			buttonSignOut.IsVisible = user != null;
			if (user == null)
			{
				remoteSolutionListView.SetSolutions(Enumerable.Empty<ISolutionInfo>());
				remoteSolutionListView.Notification = "Sign in with your Microsoft account to see your remote solutions";
			}
			updateButtonStates();
		}

		/// <summary>
		/// Sign-in in the web browser (or with a device code when no browser can be opened). Returns false if it failed or was cancelled.
		/// </summary>
		private async Task<bool> signInAsync()
		{
			if (signInCancellationSource != null)
			{
				signInCancellationSource.Cancel(); // the button cancels the sign-in in progress
				return false;
			}

			signInCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(formClosedCancellationSource.Token);
			object buttonText = buttonSignIn.Content;
			buttonSignIn.Content = "Cancel sign-in";
			buttonUploadLocalSolution.IsEnabled = false;
			labelRemoteUser.Text = "Signing in, continue in the web browser...";
			Window deviceCodeWindow = null;
			try
			{
				var user = await SolutionHub.SignInToRemoteStorageAsync(message => Dispatcher.UIThread.InvokeAsync(() =>
				{
					// no web browser could be opened: the user enters the code in a browser on another device
					deviceCodeWindow = createDeviceCodeWindow(message);
					deviceCodeWindow.Show(this);
				}).GetTask(), signInCancellationSource.Token);
				setRemoteUser(user);
				return user != null;
			}
			catch (OperationCanceledException)
			{ }
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show("Signing in with Microsoft account", ex, new MemoryLogger());
			}
			finally
			{
				deviceCodeWindow?.Close();
				signInCancellationSource.Dispose();
				signInCancellationSource = null;
				buttonSignIn.Content = buttonText;
			}
			setRemoteUser(RemoteUser); // restore the state before the sign-in
			return false;
		}

		private static Window createDeviceCodeWindow(string message)
		{
			return new Window
			{
				Title = "Sign in with Microsoft account",
				SizeToContent = SizeToContent.WidthAndHeight,
				MaxWidth = 560,
				CanResize = false,
				CanMinimize = false,
				ShowInTaskbar = false,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Content = new StackPanel
				{
					Margin = new Thickness(16),
					Spacing = 8,
					Children =
					{
						new SelectableTextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
						new TextBlock { Text = "This window closes when the sign-in is finished.", Foreground = Brushes.Gray },
					},
				},
			};
		}

		private async void buttonSignIn_Click(object sender, RoutedEventArgs e)
		{
			if (await signInAsync())
			{
				RemoteLoadingTask = initRemoteSolutionListAsync(formClosedCancellationSource.Token);
				await RemoteLoadingTask;
			}
		}

		private async void buttonSignOut_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				await SolutionHub.SignOutFromRemoteStorageAsync();
			}
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show("Signing out", ex, new MemoryLogger());
			}
			setRemoteUser(null);
			updateSelectedSolution();
		}

		private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!ReferenceEquals(e.Source, tabControl))
				return; // bubbled from the solution grids

			if (tabControl.SelectedItem == tabPageRemoteSolutions && !remoteSolutionsTabOpened)
			{
				openRemoteSolutionsTab();
			}
			updateSelectedSolution();
		}

		private void openRemoteSolutionsTab()
		{
			remoteSolutionsTabOpened = true;
			RemoteLoadingTask = initRemoteSolutionsTabAsync(formClosedCancellationSource.Token);
		}

		private void updateSelectedSolution()
		{
			LocalSolutionFileName = null;
			RemoteSolutionId = null;
			SolutionLocation = SolutionLocationType.Undefined;

			if (tabControl.SelectedItem == tabPageLocalSolutions && localSolutionListView.SelectedSolution is ISolutionInfo localSolution)
			{
				LocalSolutionFileName = localSolution.Location;
				SolutionLocation = SolutionLocationType.Local;
			}
			else if (tabControl.SelectedItem == tabPageRemoteSolutions && remoteSolutionListView.SelectedSolution is ISolutionInfo remoteSolution)
			{
				RemoteSolutionId = remoteSolution.Id;
				SolutionLocation = SolutionLocationType.Remote;
			}
			updateButtonStates();
		}

		private void updateButtonStates()
		{
			bool localSelected = localSolutionListView.IsEnabled && localSolutionListView.SelectedSolution != null;
			bool remoteSelected = remoteSolutionListView.IsEnabled && remoteSolutionListView.SelectedSolution != null;
			buttonDeleteLocalSolution.IsEnabled = localSelected;
			buttonUploadLocalSolution.IsEnabled = localSelected && signInCancellationSource == null;
			buttonDeleteRemoteSolution.IsEnabled = remoteSelected;
			buttonRefreshRemoteSolutions.IsEnabled = remoteSolutionListView.IsEnabled && RemoteUser != null;
			buttonOk.IsEnabled = tabControl.SelectedItem == tabPageRemoteSolutions ? remoteSelected : localSelected;
		}

		private async void buttonUploadLocalSolution_Click(object sender, RoutedEventArgs e)
		{
			ISolutionInfo selectedSolution = localSolutionListView.SelectedSolution;
			Debug.Assert(selectedSolution != null);
			if (selectedSolution != null)
			{
				// the solution is uploaded as a solution of the signed-in user: sign in first if needed
				if (RemoteUser == null)
				{
					try
					{
						setRemoteUser(await SolutionHub.GetRemoteUserAsync(formClosedCancellationSource.Token));
					}
					catch (Exception ex)
					{
						await ExceptionReportDialog.Show("Reading the Microsoft account sign-in", ex, new MemoryLogger());
						return;
					}
					if (RemoteUser == null && !await signInAsync())
						return;
				}

				object buttonText = buttonUploadLocalSolution.Content;
				buttonUploadLocalSolution.IsEnabled = false;
				buttonUploadLocalSolution.Content = "Uploading...";
				localSolutionListView.IsEnabled = false;
				var logger = new MemoryLogger();
				try
				{
					var remoteSolution = await SolutionHub.UploadLocalSolutionToRemoteAsync(selectedSolution.Location, formClosedCancellationSource.Token, logger);
					remoteSolutionsTabOpened = false; // reload the remote solutions
					await MessageBox.Show($"Solution '{selectedSolution.ProjectName}' was uploaded as remote solution {remoteSolution.Id} of {RemoteUser}.", "Upload to remote", MessageBoxButtons.OK, MessageBoxIcon.Information, owner: this);
				}
				catch (OperationCanceledException)
				{ }
				catch (Exception ex)
				{
					await ExceptionReportDialog.Show("Uploading solution to remote storage", ex, logger);
				}
				finally
				{
					buttonUploadLocalSolution.Content = buttonText;
					localSolutionListView.IsEnabled = true;
					updateButtonStates();
				}
			}
		}

		private async void buttonDeleteRemoteSolution_Click(object sender, RoutedEventArgs e)
		{
			ISolutionInfo selectedSolution = remoteSolutionListView.SelectedSolution;
			Debug.Assert(selectedSolution != null);
			if (selectedSolution != null)
			{
				if (await MessageBox.Show($"Delete remote solution {selectedSolution.Id} ('{selectedSolution.ProjectName}') with all its layers?", "Delete remote solution", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, owner: this) != MessageBoxResult.Yes)
					return;

				object buttonText = buttonDeleteRemoteSolution.Content;
				buttonDeleteRemoteSolution.IsEnabled = false;
				buttonDeleteRemoteSolution.Content = "Deleting...";
				remoteSolutionListView.IsEnabled = false;
				var logger = new MemoryLogger();
				try
				{
					var solutionHub = SolutionHub.OpenRemote(selectedSolution.Id, logger);
					await solutionHub.DeleteAsync(layerIdOrName: null, deleteAll: true, cancellationToken: formClosedCancellationSource.Token);
				}
				catch (Exception ex)
				{
					await ExceptionReportDialog.Show("Deleting remote solution", ex, logger);
				}
				finally
				{
					buttonDeleteRemoteSolution.Content = buttonText;
					remoteSolutionListView.IsEnabled = true;
					updateButtonStates();
				}

				RemoteLoadingTask = initRemoteSolutionListAsync(formClosedCancellationSource.Token);
				await RemoteLoadingTask;
			}
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
			updateSelectedSolution();
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
				RemoteSolutionId = null;
				SolutionLocation = SolutionLocationType.Local;
				close(true);
			}
		}
	}
}

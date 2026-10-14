using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.AzureStorage;

namespace MeshEditor.AvaloniaUI.Postprocessing
{
	/// <summary>
	/// Configuration of the remote solution storage (Azure Blob Storage connection string).
	/// The connection string is kept in the secure storage of the operating system, never in the configuration file.
	/// </summary>
	public class RemoteStorageDialog : Window
	{
		private readonly CancellationTokenSource formClosedCancellationSource = new CancellationTokenSource();
		internal readonly TextBox textBoxConnectionString = new TextBox { PasswordChar = '•', RevealPassword = false, PlaceholderText ="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net" };
		internal readonly TextBlock labelStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
		internal readonly Button buttonSave = new Button { Content = "Test and save", MinWidth = 100, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = true };
		internal readonly Button buttonRemove = new Button { Content = "Remove", MinWidth = 100, HorizontalContentAlignment = HorizontalAlignment.Center };
		internal readonly Button buttonClose = new Button { Content = "Close", MinWidth = 100, HorizontalContentAlignment = HorizontalAlignment.Center, IsCancel = true };

		public RemoteStorageDialog()
		{
			Title = "Remote storage";
			Width = 560;
			SizeToContent = SizeToContent.Height;
			CanResize = false;
			CanMinimize = false;
			ShowInTaskbar = false;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			var checkBoxReveal = new CheckBox { Content = "Show", Margin = new Thickness(8, 0, 0, 0) };
			checkBoxReveal.IsCheckedChanged += (s, e) => textBoxConnectionString.RevealPassword = checkBoxReveal.IsChecked == true;
			var connectionStringRow = new DockPanel();
			DockPanel.SetDock(checkBoxReveal, Dock.Right);
			connectionStringRow.Children.Add(checkBoxReveal);
			connectionStringRow.Children.Add(textBoxConnectionString);

			var buttons = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
			DockPanel.SetDock(buttonRemove, Dock.Left);
			buttons.Children.Add(buttonRemove);
			buttons.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { buttonSave, buttonClose } });

			Content = new StackPanel
			{
				Margin = new Thickness(12),
				Children =
				{
					new TextBlock { Text = "Azure Storage connection string of the account with the remote solutions:", Margin = new Thickness(0, 0, 0, 4) },
					connectionStringRow,
					new TextBlock
					{
						Text = "The connection string is stored in the secure storage of the operating system (Windows DPAPI, macOS Keychain, Linux keyring), not in the configuration file. " +
							$"It can also be provided by the environment variable {AzureStorageCredentialStore.ConnectionStringEnvironmentVariable}.",
						TextWrapping = TextWrapping.Wrap,
						Foreground = Brushes.Gray,
						Margin = new Thickness(0, 6, 0, 0),
					},
					labelStatus,
					buttons,
				},
			};

			buttonSave.Click += buttonSave_Click;
			buttonRemove.Click += buttonRemove_Click;
			buttonClose.Click += (s, e) => Close();
			textBoxConnectionString.TextChanged += (s, e) => updateState();

			updateState();
		}

		public new async Task ShowDialog(Window owner)
		{
			await this.ShowModalAsync(owner);
		}

		protected override void OnClosed(EventArgs e)
		{
			formClosedCancellationSource.Cancel();
			base.OnClosed(e);
		}

		private void updateState(string status = null)
		{
			bool providedByEnvironment = AzureStorageCredentialStore.IsProvidedByEnvironment;
			bool configured = SolutionHub.IsRemoteStorageConfigured;
			textBoxConnectionString.IsEnabled = !providedByEnvironment;
			buttonSave.IsEnabled = !providedByEnvironment && !string.IsNullOrWhiteSpace(textBoxConnectionString.Text);
			buttonRemove.IsEnabled = !providedByEnvironment && configured;
			labelStatus.Text = status ?? (providedByEnvironment ? $"Remote storage is configured by the environment variable {AzureStorageCredentialStore.ConnectionStringEnvironmentVariable}."
										: configured ? "Remote storage is configured."
										: "Remote storage is not configured.");
		}

		private async void buttonSave_Click(object sender, RoutedEventArgs e)
		{
			string connectionString = textBoxConnectionString.Text?.Trim();
			buttonSave.IsEnabled = false;
			labelStatus.Text = "Connecting...";
			try
			{
				await SolutionHub.TestRemoteStorageConnectionAsync(connectionString, formClosedCancellationSource.Token);
				AzureStorageCredentialStore.SetConnectionString(connectionString);
				textBoxConnectionString.Text = string.Empty;
				updateState("Connection succeeded, the connection string is stored.");
			}
			catch (OperationCanceledException)
			{ }
			catch (Exception ex)
			{
				updateState($"Connection failed: {ex.Message}");
			}
		}

		private async void buttonRemove_Click(object sender, RoutedEventArgs e)
		{
			if (await MessageBox.Show("Remove the stored connection string?", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, owner: this) != MessageBoxResult.Yes)
				return;
			try
			{
				AzureStorageCredentialStore.Clear();
				updateState();
			}
			catch (Exception ex)
			{
				updateState($"Removing the connection string failed: {ex.Message}");
			}
		}
	}
}

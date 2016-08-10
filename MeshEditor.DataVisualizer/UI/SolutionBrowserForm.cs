using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.DataVisualizer.Services;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class SolutionBrowserForm : Form
	{
		public enum SolutionLocationType
		{
			Undefined,
			Local,
			Remote
		}

		CancellationTokenSource formClosedCancellationSource = new CancellationTokenSource();
		bool remoteSolutionsTabOpened = false;

		public SolutionBrowserForm()
		{
			InitializeComponent();

			var ignoredTask = initLocalSolutionListAsync(formClosedCancellationSource.Token); // start loading LOCAL solutions

			updateButtonStates();
		}

		public SolutionLocationType SolutionLocation { get; private set; }
		public int? RemoteSolutionId { get; private set; } //(listBoxRemoteSolutions.SelectedItem as SolutionThumbnail?)?.SolutionId;
		public string LocalSolutionFileName { get; private set; }

		private async Task initLocalSolutionListAsync(CancellationToken cancellationToken)
		{
			string taskName = "Loading local solutions";
			localSolutionListView.Notification = taskName + "...";
			
			var logger = new MemoryLogger();
			IEnumerable<ISolutionInfo> solutions = Enumerable.Empty<ISolutionInfo>();
			try
			{
				solutions = await SolutionHub.EnumerateAllLocalSolutionsAsync(SolutionHub.GetLocalStorageDefaultDirectory(), cancellationToken, logger);
			}
			catch (OperationCanceledException)
			{ }
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
			localSolutionListView.SetSolutions(solutions);
			updateButtonStates();
		}

		private async Task initRemoteSolutionListAsync(CancellationToken cancellationToken)
		{
			string taskName = "Loading remote solutions";
			
			remoteSolutionListView.Notification = taskName + "...";

			var logger = new MemoryLogger();
			IEnumerable<ISolutionInfo> solutions = Enumerable.Empty<ISolutionInfo>();
			try
			{
				solutions = await SolutionHub.EnumerateAllRemoteSolutionsAsync(cancellationToken, logger);
			}
			catch (OperationCanceledException)
			{ }
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
			remoteSolutionListView.SetSolutions(solutions);
			updateButtonStates();
		}

		protected override void OnClosed(EventArgs e)
		{
			formClosedCancellationSource.Cancel();
			base.OnClosed(e);
		}

		private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (tabControl.SelectedTab == tabPageRemoteSolutions && !remoteSolutionsTabOpened)
			{
				var ignoredTask = initRemoteSolutionListAsync(formClosedCancellationSource.Token);
				remoteSolutionsTabOpened = true;
			}
			updateButtonStates();
		}

		private void buttonOpenRemoteSolutionInBrowser_Click(object sender, EventArgs e)
		{
			ISolutionInfo selectedSolution = remoteSolutionListView.SelectedSolution;
			if (selectedSolution != null)
			{
				try
				{
					var location = new Uri(selectedSolution.Location);
					UriBuilder uriBuilder = new UriBuilder(location);
					uriBuilder.Path = $"/postprocess/{selectedSolution.Id}";
					Process.Start(uriBuilder.Uri.ToString());
				}
				catch (Exception ex)
				{
					new ExceptionReportForm("Open remote solution in browser", ex, logger: null).ShowDialog();
				}
			}
		}

		private void updateButtonStates()
		{
			if (tabControl.SelectedTab == tabPageLocalSolutions)
			{
				buttonOk.Enabled = buttonDeleteLocalSolution.Enabled = localSolutionListView.Enabled && localSolutionListView.SelectedSolution != null;
			}
			else if (tabControl.SelectedTab == tabPageRemoteSolutions)
			{
				buttonOk.Enabled = buttonDeleteRemoteSolution.Enabled = buttonRemoteSolutionOpenInBrowser.Enabled = remoteSolutionListView.Enabled && remoteSolutionListView.SelectedSolution != null;
			}
			else
			{
				buttonOk.Enabled = buttonDeleteLocalSolution.Enabled = buttonDeleteRemoteSolution.Enabled = buttonRemoteSolutionOpenInBrowser.Enabled = false;
			}
		}

		private async void buttonDeleteLocalSolution_Click(object sender, EventArgs e)
		{
			ISolutionInfo selectedSolution = localSolutionListView.SelectedSolution;
			Debug.Assert(selectedSolution != null);
			if (selectedSolution != null)
			{
				string buttonText = buttonDeleteLocalSolution.Text;
				buttonDeleteLocalSolution.Enabled = false;
				buttonDeleteLocalSolution.Text = "Deleting...";
				localSolutionListView.Enabled = false;
				try
				{
					var logger = new MemoryLogger();
					try
					{
						var solutionHub = SolutionHub.CreateLocal(selectedSolution.Location, logger);
						await solutionHub.DeleteAsync(cancellationToken: formClosedCancellationSource.Token, layerIdOrName: null, deleteAll: true);
					}
					catch (Exception ex)
					{
						new ExceptionReportForm("Deleting local solution", ex, logger).ShowDialog();
					}

					await initLocalSolutionListAsync(formClosedCancellationSource.Token);
				}
				finally
				{
					buttonDeleteLocalSolution.Text = buttonText;
					localSolutionListView.Enabled = true;
					updateButtonStates();
				}
			}
		}

		private async void buttonDeleteRemoteSolution_Click(object sender, EventArgs e)
		{
			ISolutionInfo selectedSolution = remoteSolutionListView.SelectedSolution;
			Debug.Assert(selectedSolution != null);
			if (selectedSolution != null)
			{
				string buttonText = buttonDeleteRemoteSolution.Text;
				buttonDeleteRemoteSolution.Enabled = false;
				buttonDeleteRemoteSolution.Text = "Deleting...";
				remoteSolutionListView.Enabled = false;
				try
				{
					var logger = new MemoryLogger();
					try
					{
						var solutionHub = SolutionHub.CreateRemote(selectedSolution.Id, logger);
						await solutionHub.DeleteAsync(cancellationToken: formClosedCancellationSource.Token, layerIdOrName: null, deleteAll: true);
					}
					catch (Exception ex)
					{
						new ExceptionReportForm("Deleting remote solution", ex, logger).ShowDialog();
					}

					await initRemoteSolutionListAsync(formClosedCancellationSource.Token);
				}
				finally
				{
					buttonDeleteRemoteSolution.Text = buttonText;
					remoteSolutionListView.Enabled = true;
					updateButtonStates();
				}
			}
		}

		private void buttonBrowseLocalSolutions_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "Solution files (*.solution.json)|*.solution.json|All files (*.*)|*.*";
			dialog.FilterIndex = 0;
			dialog.AutoUpgradeEnabled = true;
			//dialog.InitialDirectory = PostprocessViewControl.GetDefaultSolutionDirectory().Replace('/', '\\'); // TODO: test on mono
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				LocalSolutionFileName = dialog.FileName;
				SolutionLocation = SolutionLocationType.Local;
				DialogResult = DialogResult.OK; // close dialog
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

		private void remoteSolutionListView_SelectedSolutionChanged(object sender, EventArgs e)
		{
			ISolutionInfo selectedSolution = remoteSolutionListView.SelectedSolution;
			if (selectedSolution != null)
			{
				RemoteSolutionId = selectedSolution.Id;
				SolutionLocation = SolutionLocationType.Remote;
			}
			else
			{
				RemoteSolutionId = null;
				SolutionLocation = SolutionLocationType.Undefined;
			}
			updateButtonStates();
		}

		private void localSolutionListView_SolutionListDoubleClick(object sender, EventArgs e)
		{
			if (localSolutionListView.SelectedSolution != null)
			{
				DialogResult = DialogResult.OK; // close dialog
			}
		}

		private void remoteSolutionListView_SolutionListDoubleClick(object sender, EventArgs e)
		{
			if (remoteSolutionListView.SelectedSolution != null)
			{
				DialogResult = DialogResult.OK; // close dialog
			}
		}
	}
}

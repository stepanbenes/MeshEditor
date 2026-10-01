using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.IO;
using MeshEditor.Common.Logging;
using MeshEditor.CoreInterface;
using MeshEditor.Common;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class SolutionBrowserForm : Form
	{
		public enum SolutionLocationType
		{
			Undefined,
			Local
		}

		CancellationTokenSource formClosedCancellationSource = new CancellationTokenSource();

		public SolutionBrowserForm()
		{
			InitializeComponent();

			_ = initLocalSolutionListAsync(formClosedCancellationSource.Token); // start loading LOCAL solutions

			updateButtonStates();
		}

		public SolutionLocationType SolutionLocation { get; private set; }
		public string LocalSolutionFileName { get; private set; }

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
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
			localSolutionListView.SetSolutions(solutions);
			updateButtonStates();
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			formClosedCancellationSource.Cancel();
			base.OnFormClosed(e);
		}

		private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			updateButtonStates();
		}

		private void updateButtonStates()
		{
			if (tabControl.SelectedTab == tabPageLocalSolutions)
			{
				buttonOk.Enabled = buttonDeleteLocalSolution.Enabled = localSolutionListView.Enabled && localSolutionListView.SelectedSolution != null;
			}
			else
			{
				buttonOk.Enabled = buttonDeleteLocalSolution.Enabled = false;
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
				var logger = new MemoryLogger();
				try
				{
					var solutionHub = SolutionHub.OpenLocal(selectedSolution.Location, logger);
					await solutionHub.DeleteAsync(layerIdOrName: null, deleteAll: true, cancellationToken: formClosedCancellationSource.Token);
				}
				catch (Exception ex)
				{
					new ExceptionReportForm("Deleting local solution", ex, logger).ShowDialog();
				}
				finally
				{
					buttonDeleteLocalSolution.Text = buttonText;
					localSolutionListView.Enabled = true;
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

		private void localSolutionListView_SolutionListDoubleClick(object sender, EventArgs e)
		{
			if (localSolutionListView.SelectedSolution != null)
			{
				DialogResult = DialogResult.OK; // close dialog
			}
		}

		private async void buttonChangeDefaultSolutionDirectory_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
			{
				SelectedPath = SolutionHub.GetLocalStorageDefaultDirectory()
			};

			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				SolutionHub.SetLocalStorageDefaultDirectory(folderBrowserDialog.SelectedPath);
				await initLocalSolutionListAsync(formClosedCancellationSource.Token);
			}
		}

		private void buttonBrowse_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog
			{
				Filter = $"Solution files (*{SceneFacade.SolutionFileExtension})|*{SceneFacade.SolutionFileExtension}|All files (*.*)|*.*",
				FilterIndex = 0,
				AutoUpgradeEnabled = true,
				InitialDirectory = SolutionHub.GetLocalStorageDefaultDirectory()
			};

			if (dialog.ShowDialog() == DialogResult.OK)
			{
				LocalSolutionFileName = dialog.FileName;
				SolutionLocation = SolutionLocationType.Local;
				DialogResult = DialogResult.OK; // close dialog
			}
		}
	}
}

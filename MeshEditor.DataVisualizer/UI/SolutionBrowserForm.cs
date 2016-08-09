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
		private struct SolutionThumbnail
		{
			public SolutionThumbnail(int solutionId, string projectName, string location)
			{
				SolutionId = solutionId;
				ProjectName = projectName;
				Location = location;
			}

			public int SolutionId { get; }
			public string ProjectName { get; }
			public string Location { get; }

			public override string ToString()
			{
				return $"Solution id: {SolutionId}, Project name: {ProjectName}, Location: {Location}";
			}
		}

		public enum SolutionLocationType
		{
			Undefined,
			Local,
			Remote
		}

		CancellationTokenSource cancellationTokenSource;
		Task initLocalSolutionsTask, initRemoteSolutionsTask;

		public SolutionBrowserForm()
		{
			InitializeComponent();
			
			cancellationTokenSource = new CancellationTokenSource();
			initLocalSolutionsTask = initLocalSolutionListAsync(cancellationTokenSource.Token);

			updateButtonStates();
		}

		public SolutionLocationType SolutionLocation { get; private set; }
		public int? RemoteSolutionId { get; private set; } //(listBoxRemoteSolutions.SelectedItem as SolutionThumbnail?)?.SolutionId;
		public string LocalSolutionFileName { get; private set; }

		private async Task initLocalSolutionListAsync(CancellationToken cancellationToken)
		{
			string taskName = "Loading local solutions";
			var logger = new MemoryLogger();
			try
			{
				listBoxLocalSolutions.Items.Add(taskName + "...");
				IEnumerable<ISolutionInfo> solutions = await SolutionHub.EnumerateAllLocalSolutionsAsync(SolutionHub.GetLocalStorageDefaultDirectory(), cancellationToken, logger);
				populateSolutionListBox(listBoxLocalSolutions, solutions);
			}
			catch (OperationCanceledException)
			{ }
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
		}

		private async Task initRemoteSolutionListAsync(CancellationToken cancellationToken)
		{
			string taskName = "Loading remote solutions";
			var logger = new MemoryLogger();
			try
			{
				listBoxRemoteSolutions.Items.Add(taskName + "...");
				IEnumerable<ISolutionInfo> solutions = await SolutionHub.EnumerateAllRemoteSolutionsAsync(cancellationToken, logger);
				populateSolutionListBox(listBoxRemoteSolutions, solutions);
			}
			catch (OperationCanceledException)
			{ }
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
		}

		private void populateSolutionListBox(ListBox listBoxSolutions, IEnumerable<ISolutionInfo> solutions)
		{
			listBoxSolutions.Items.Clear();
			foreach (var solution in solutions.Select(s => new SolutionThumbnail(s.Id, s.ProjectName, s.Location)))
			{
				listBoxSolutions.Items.Add(solution);
			}
			if (listBoxSolutions.Items.Count == 0)
			{
				listBoxSolutions.Items.Add("No solutions found");
			}
			updateButtonStates();
		}

		protected override void OnClosed(EventArgs e)
		{
			if (initLocalSolutionsTask != null && initLocalSolutionsTask.Status != TaskStatus.RanToCompletion)
			{
				cancellationTokenSource.Cancel();
			}
			if (initRemoteSolutionsTask != null && initRemoteSolutionsTask.Status != TaskStatus.RanToCompletion)
			{
				cancellationTokenSource.Cancel();
			}
			base.OnClosed(e);
		}

		private void listBoxLocalSolutions_SelectedIndexChanged(object sender, EventArgs e)
		{
			SolutionThumbnail? selectedSolution = listBoxLocalSolutions.SelectedItem as SolutionThumbnail?;
			if (selectedSolution.HasValue)
			{
				LocalSolutionFileName = selectedSolution?.Location;
				SolutionLocation = SolutionLocationType.Local;

				updateButtonStates();
			}
		}

		private void listBoxRemoteSolutions_SelectedIndexChanged(object sender, EventArgs e)
		{
			SolutionThumbnail? selectedSolution = listBoxRemoteSolutions.SelectedItem as SolutionThumbnail?;
			if (selectedSolution.HasValue)
			{
				RemoteSolutionId = selectedSolution?.SolutionId;
				SolutionLocation = SolutionLocationType.Remote;

				updateButtonStates();
			}
		}

		private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (tabControl.SelectedTab == tabPageRemoteSolutions && initRemoteSolutionsTask == null)
			{
				initRemoteSolutionsTask = initRemoteSolutionListAsync(cancellationTokenSource.Token);
			}
			updateButtonStates();
		}

		private void buttonOpenRemoteSolutionInBrowser_Click(object sender, EventArgs e)
		{
			SolutionThumbnail? selectedSolution = listBoxRemoteSolutions.SelectedItem as SolutionThumbnail?;
			if (selectedSolution.HasValue)
			{
				var url = $"http://mesheditor.azurewebsites.net/postprocess/{selectedSolution.Value.SolutionId}";
				//var url = selectedSolution.Value.Location;
				Process.Start(url);
			}
		}

		private void listBoxLocalSolutions_DoubleClick(object sender, EventArgs e)
		{
			if (listBoxLocalSolutions.SelectedItem is SolutionThumbnail)
			{
				DialogResult = DialogResult.OK; // close dialog
			}
		}

		private void listBoxRemoteSolutions_DoubleClick(object sender, EventArgs e)
		{
			if (listBoxRemoteSolutions.SelectedItem is SolutionThumbnail)
			{
				DialogResult = DialogResult.OK; // close dialog
			}
		}

		private void updateButtonStates()
		{
			if (tabControl.SelectedTab == tabPageLocalSolutions)
			{
				buttonOk.Enabled = buttonDeleteLocalSolution.Enabled = (listBoxLocalSolutions.SelectedItem is SolutionThumbnail);
			}
			else if (tabControl.SelectedTab == tabPageRemoteSolutions)
			{
				buttonOk.Enabled = buttonDeleteRemoteSolution.Enabled = buttonRemoteSolutionOpenInBrowser.Enabled = (listBoxRemoteSolutions.SelectedItem is SolutionThumbnail);
			}
			else
			{
				buttonOk.Enabled = buttonDeleteLocalSolution.Enabled = buttonDeleteRemoteSolution.Enabled = buttonRemoteSolutionOpenInBrowser.Enabled = false;
			}
		}

		private void buttonDeleteLocalSolution_Click(object sender, EventArgs e)
		{
			SolutionThumbnail? selectedSolution = listBoxLocalSolutions.SelectedItem as SolutionThumbnail?;
			if (selectedSolution.HasValue)
			{
				var logger = new MemoryLogger();
				try
				{
					var solutionHub = SolutionHub.CreateLocal(selectedSolution.Value.Location, logger);
					solutionHub.Delete(layerIdOrName: null, deleteAll: true);
					initLocalSolutionsTask = initLocalSolutionListAsync(cancellationTokenSource.Token);
				}
				catch (Exception ex)
				{
					new ExceptionReportForm("Deleting local solution", ex, logger).ShowDialog();
				}
			}
		}

		private void buttonDeleteRemoteSolution_Click(object sender, EventArgs e)
		{
			SolutionThumbnail? selectedSolution = listBoxRemoteSolutions.SelectedItem as SolutionThumbnail?;
			if (selectedSolution.HasValue)
			{
				var logger = new MemoryLogger();
				try
				{
					var solutionHub = SolutionHub.CreateRemote(selectedSolution.Value.SolutionId, logger);
					solutionHub.Delete(layerIdOrName: null, deleteAll: true);
					initRemoteSolutionsTask = initRemoteSolutionListAsync(cancellationTokenSource.Token);
				}
				catch (Exception ex)
				{
					new ExceptionReportForm("Deleting remote solution", ex, logger).ShowDialog();
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
	}
}

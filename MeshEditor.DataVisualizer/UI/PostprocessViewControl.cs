using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.SolutionManager;
using MeshEditor.CoreInterface;
using MeshEditor.DataVisualizer;
using MeshEditor.SolutionManager.IO;
using MeshEditor.DataVisualizer.IO;
using System.Diagnostics;
using MeshEditor.LayerManager.Data;
using System.Threading;

namespace MeshEditor.WinUI
{
	public partial class PostprocessViewControl : ContentViewControl
	{
		#region Fields, constructor

		Control contentPanel;
		SolutionHub solutionHub;
		SceneFacade activeScene;
		bool changingActiveScene;
		Dictionary<Guid, SummaryFile> layerSummaryCache = new Dictionary<Guid, SummaryFile>();
		LongOpNotifier longOpNotifier;

		public PostprocessViewControl(LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			this.longOpNotifier = longOpNotifier;
			splitContainer1.FixedPanel = FixedPanel.Panel1;

			layersTreeView.LayerSelectionChanged += layersTreeView_LayerSelectionChanged;
			dataSelectionControl.DataSelectionChanged += dataSelectionControl_DataSelectionChanged;
			visualizerSettingsControl.SettingsChanged += visualizerSettingsControl_SettingsChanged;
		}

		#endregion

		#region Properties

		public override Control Content
		{
			get { return contentPanel; }
			set
			{
				if (contentPanel != value)
				{
					if (contentPanel != null)
						splitContainer1.Panel2.Controls.Remove(contentPanel);
					contentPanel = value;
					if (contentPanel != null)
						splitContainer1.Panel2.Controls.Add(contentPanel);
				}
			}
		}

		public int SplitterDistance
		{
			get { return splitContainer1.SplitterDistance; }
			set { splitContainer1.SplitterDistance = value; }
		}

		public SceneFacade ActiveScene
		{
			get { return activeScene; }
			set
			{
				if (activeScene != value)
				{
					activeScene = value;
					onActiveSceneChanged();
				}
			}
		}

		#endregion

		#region Public methods

		public async Task LoadLocalSolution(string solutionFileFullPath)
		{
			await Task.Yield();

			loadSolution(SolutionHub.CreateLocal(solutionFileFullPath));

			//	var layers = solutionHub.EnumerateAllLayers();

			//	// TODO: load master layer and its data, show layers panel

			//	var masterLayer = layers.Single(l => l.Name == "master");
			//	string masterLayerFilename = Path.Combine(Path.GetDirectoryName(dialog.FileName), masterLayer.Id.ToString(), "mesh.json");
			//	activeControl.LoadFiles(masterLayerFilename);

			//	await Task.Delay(2000); // mesh is beeing loaded asynchronously, it must be loaded before data can begin to load, so wait some time

			//	var dataVisualizer = new ExactDataVisualizer();
			//	setNewDataVisualizer(dataVisualizer);
			//	var resultFiles = Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(dialog.FileName), masterLayer.Id.ToString()), "result.*");
			//	dataVisualizer.LoadData(new ApproximationParameters(loadInternalEntities: true), resultFiles.ToArray(), longOpNotifier);
			//	dataVisualizer.FinishUp();
		}

		public async Task LoadRemoteSolution(int solutionId)
		{
			await Task.Yield();

			loadSolution(SolutionHub.CreateRemote(solutionId));
		}

		#endregion

		#region Private methods

		private void loadSolution(SolutionHub solutionHub)
		{
			this.solutionHub = solutionHub;
			updateLayerTree();
		}

		private void updateLayerTree()
		{
			if (solutionHub == null)
				return;

			var layers = solutionHub.EnumerateAllLayers();
			layersTreeView.SetLayerTree(layers);
		}

		private void onActiveSceneChanged()
		{
			var layerDataVisualizer = ActiveScene.GetValue(AvailableValue.DataVisualizer) as LayerDataVisualizer;
			try
			{
				changingActiveScene = true;
				if (layerDataVisualizer != null)
				{
					layersTreeView.SetSelectedLayer(layerDataVisualizer.LayerId);
					dataSelectionControl.UpdateDataSource(getSummaryFileFor(layerDataVisualizer.LayerId), layerDataVisualizer.DataSelection);
					visualizerSettingsControl.Settings = layerDataVisualizer.Settings;
				}
				else
				{
					layersTreeView.SetSelectedLayer(null);
					dataSelectionControl.UpdateDataSource(null, null);
					visualizerSettingsControl.Settings = null;
				}
			}
			finally
			{
				changingActiveScene = false;
			}
		}

		private SummaryFile getSummaryFileFor(Guid layerId)
		{
			SummaryFile summary;
			if (!layerSummaryCache.TryGetValue(layerId, out summary))
				summary = layerSummaryCache[layerId] = solutionHub.LoadLayerSummary(layerId);
			return summary;
		}

		private async Task loadLayerAsync(Guid layerId, CancellationToken cancellationToken)
		{
			var summary = getSummaryFileFor(layerId);
			dataSelectionControl.UpdateDataSource(summary, null);

			var firstMesh = summary.Meshes.FirstOrDefault();
			if (firstMesh != null)
			{
				int? elementPropertyAttributeIndex = firstMesh?.Attributes.FirstOrDefault(a => a.FieldName == "ElementProperty")?.Index;
				var dataVisualizer = new LayerDataVisualizer(layerId);
				dataVisualizer.MeshReloadRequested += parser => ActiveScene.ReloadMesh(parser);
				await dataVisualizer.UpdateDataSelectionAsync(solutionHub, new DataSelection(firstMesh.Index, elementPropertyAttributeIndex), cancellationToken);
				ActiveScene.SetValue(AvailableValue.DataVisualizer, dataVisualizer);
				ActiveScene.PerformAction(AvailableAction.UpdateColorBuffers);

				visualizerSettingsControl.Settings = dataVisualizer.Settings;
			}
		}

		CancellationTokenSource currentCts;

		private CancellationToken beginCancellableOperation()
		{
			if (currentCts != null)
			{
				currentCts.Cancel();
				endCancellableOperation();
			}

			//longOpNotifier.Begin();

			currentCts = new CancellationTokenSource();
			return currentCts.Token;
		}

		private void endCancellableOperation()
		{
			//longOpNotifier.End();

			if (currentCts != null)
			{
				currentCts.Dispose();
				currentCts = null;
			}
		}

		private async void layersTreeView_LayerSelectionChanged(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			if (e.LayerId.HasValue)
			{
				try
				{
					var cancellationToken = beginCancellableOperation();
					await loadLayerAsync(e.LayerId.Value, cancellationToken);
				}
				catch (OperationCanceledException)
				{ }
				finally
				{
					endCancellableOperation();
				}
			}
			else
			{
				// TODO: clear scene
				// !! do not allow to hide all layers for now
				//throw new NotImplementedException();
			}
		}

		private async void dataSelectionControl_DataSelectionChanged(object sender, DataSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			var layerDataVisualizer = ActiveScene.GetValue(AvailableValue.DataVisualizer) as LayerDataVisualizer;
			Debug.Assert(layerDataVisualizer != null);
			if (layerDataVisualizer == null)
				return;

			try
			{
				var cancellationToken = beginCancellableOperation();
				await layerDataVisualizer.UpdateDataSelectionAsync(solutionHub, e.DataSelection, cancellationToken);
			}
			catch (OperationCanceledException)
			{ }
			finally
			{
				endCancellableOperation();
			}

			// update colors
			ActiveScene.PerformAction(AvailableAction.UpdateColorBuffers);
		}

		private void visualizerSettingsControl_SettingsChanged(object sender, EventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			// update colors
			ActiveScene.PerformAction(AvailableAction.UpdateColorBuffers);
		}

		#endregion
	}
}

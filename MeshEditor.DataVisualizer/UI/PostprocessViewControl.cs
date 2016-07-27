using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.SolutionManager;
using MeshEditor.CoreInterface;
using MeshEditor.DataVisualizer;
using System.Diagnostics;
using MeshEditor.LayerManager.Data;
using System.Threading;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.Services;

namespace MeshEditor.WinUI
{
	public partial class PostprocessViewControl : ContentViewControl
	{
		#region Static members

		public static string GetDefaultSolutionDirectory()
		{
			return SolutionHub.GetLocalStorageDefaultDirectory();
		}

		#endregion

		#region Fields, constructor

		Control contentPanel;
		SolutionHub solutionHub;
		MemoryLogger logger;
		SceneFacade activeScene;
		bool changingActiveScene;
		Dictionary<Guid, SummaryFile> layerSummaryCache = new Dictionary<Guid, SummaryFile>();

		LongOpNotifier longOpNotifier;
		Dictionary<LongOpNotifier.Token, CancellationTokenSource> cancellationTokenSources = new Dictionary<LongOpNotifier.Token, CancellationTokenSource>();

		public PostprocessViewControl(LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			this.longOpNotifier = longOpNotifier;
			this.longOpNotifier.CancellationRequested += longOpNotifier_CancellationRequested;

			mainSplitContainer.FixedPanel = FixedPanel.Panel1;

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
						mainSplitContainer.Panel2.Controls.Remove(contentPanel);
					contentPanel = value;
					if (contentPanel != null)
						mainSplitContainer.Panel2.Controls.Add(contentPanel);
				}
			}
		}

		public int SplitterDistance
		{
			get { return mainSplitContainer.SplitterDistance; }
			set { mainSplitContainer.SplitterDistance = value; }
		}

		public SceneFacade ActiveScene
		{
			get { return activeScene; }
			set
			{
				if (activeScene != value)
				{
					activeScene = value;
					var firedAndForgottenTask = updateDataSelectionInLeftPanelAsync();
				}
			}
		}

		#endregion

		#region Public methods

		public async Task LoadLocalSolutionAsync(string solutionFileFullPath)
		{
			logger = new MemoryLogger();
			solutionHub = SolutionHub.CreateLocal(solutionFileFullPath, logger);
			await loadSolutionAsync(solutionHub);
		}

		public async Task LoadLocalSolutionAsync(int solutionId)
		{
			logger = new MemoryLogger();
			solutionHub = SolutionHub.CreateLocal(solutionId, logger);
			await loadSolutionAsync(solutionHub);
		}

		public async Task LoadRemoteSolutionAsync(int solutionId)
		{
			logger = new MemoryLogger();
			solutionHub = SolutionHub.CreateRemote(solutionId, logger);
			await loadSolutionAsync(solutionHub);
		}

		#endregion

		#region Private methods

		private async Task loadSolutionAsync(SolutionHub solutionHub)
		{
			LongOpNotifier.Token operationToken = beginLongOperation("Loading solution");
			try
			{
				await updateLayerTreeAsync(operationToken);
			}
			catch (OperationCanceledException)
			{ }
			finally
			{
				endLongOperation(operationToken);
			}
		}

		private async Task updateLayerTreeAsync(LongOpNotifier.Token operationToken)
		{
			Debug.Assert(solutionHub != null);
			Debug.Assert(ActiveScene != null);
			longOpNotifier.UpdateState(operationToken, "Loading list of layers");
			var layers = await solutionHub.EnumerateAllLayersAsync(cancellationTokenSources[operationToken].Token);
			layersTreeView.SetLayerTree(layers);

			if (layers.Any()) // load first layer
			{
				Guid layerId = layers.First().Id;
				await loadLayerAsync(layerId, ActiveScene, operationToken);
				layersTreeView.SetSelectedLayer(layerId);
			}
		}

		private async Task updateDataSelectionInLeftPanelAsync()
		{
			var layerDataVisualizer = ActiveScene.GetValue(AvailableValue.DataVisualizer) as LayerDataVisualizer;
			if (layerDataVisualizer != null)
			{
				LongOpNotifier.Token operationToken = beginLongOperation("Loading layer summary");
				try
				{
					var summary = await getSummaryFileForLayerAsync(layerDataVisualizer.LayerId, cancellationTokenSources[operationToken].Token);
					dataSelectionControl.UpdateDataSource(summary, layerDataVisualizer.DataSelection);
				}
				catch (OperationCanceledException)
				{
					dataSelectionControl.UpdateDataSource(null, null);
				}
				finally
				{
					endLongOperation(operationToken);
				}

				try
				{
					changingActiveScene = true;
					layersTreeView.SetSelectedLayer(layerDataVisualizer.LayerId);
				}
				finally
				{
					changingActiveScene = false;
				}
				visualizerSettingsControl.Settings = layerDataVisualizer.Settings;
			}
			else
			{
				layersTreeView.SetSelectedLayer(null);
				dataSelectionControl.UpdateDataSource(null, null);
				visualizerSettingsControl.Settings = null;
			}
		}

		private async Task<SummaryFile> getSummaryFileForLayerAsync(Guid layerId, CancellationToken cancellationToken)
		{
			SummaryFile summary;
			if (!layerSummaryCache.TryGetValue(layerId, out summary))
			{
				summary = await solutionHub.LoadLayerSummaryAsync(layerId, cancellationToken);
				layerSummaryCache[layerId] = summary;
			}
			return summary;
		}

		private async Task loadLayerAsync(Guid layerId, SceneFacade scene, LongOpNotifier.Token operationToken)
		{
			longOpNotifier.UpdateState(operationToken, "Loading layer summary");
			CancellationToken cancellationToken = cancellationTokenSources[operationToken].Token;
			var summary = await getSummaryFileForLayerAsync(layerId, cancellationToken);

			var firstMesh = summary.Meshes.FirstOrDefault();
			if (firstMesh != null)
			{
				int? elementPropertyAttributeIndex = firstMesh?.Attributes.FirstOrDefault(a => a.FieldName == AttributeDescription.KnownAttributeNames.ElementProperty)?.Index;
				var dataVisualizer = new LayerDataVisualizer(layerId);

				Action<string, int> progressReport = (operationName, percentDone) => longOpNotifier.UpdateState(operationToken, operationName, percentDone);

				await dataVisualizer.UpdateDataSelectionAsync(solutionHub, new DataSelection(firstMesh.Index, elementPropertyAttributeIndex), cancellationToken, ActiveScene, progressReport);

				scene.SetValue(AvailableValue.DataVisualizer, dataVisualizer);
				scene.PerformAction(AvailableAction.UpdateColorBuffers);
				visualizerSettingsControl.Settings = dataVisualizer.Settings;
			}

			dataSelectionControl.UpdateDataSource(summary, null);
		}

		private LongOpNotifier.Token beginLongOperation(string taskName)
		{
			mainSplitContainer.Panel1.Enabled = false;
			//cancelOperation(); // cancel ongoing operation
			var operationToken = longOpNotifier.Begin(taskName, isCancellable: true);
			cancellationTokenSources[operationToken] = new CancellationTokenSource();
			return operationToken;
		}

		private void cancelOperation(LongOpNotifier.Token operationToken)
		{
			CancellationTokenSource cts;
			if (cancellationTokenSources.TryGetValue(operationToken, out cts))
			{
				cts.Cancel();
			}
		}

		private void endLongOperation(LongOpNotifier.Token operationToken)
		{
			longOpNotifier.End(operationToken);
			CancellationTokenSource cts;
			if (cancellationTokenSources.TryGetValue(operationToken, out cts))
			{
				cts.Dispose();
				cancellationTokenSources.Remove(operationToken);
			}
			mainSplitContainer.Panel1.Enabled = true;
		}

		private async void layersTreeView_LayerSelectionChanged(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			if (e.LayerId.HasValue)
			{
				LongOpNotifier.Token operationToken = beginLongOperation("Loading layer");
				try
				{
					await loadLayerAsync(e.LayerId.Value, ActiveScene, operationToken);
				}
				catch (OperationCanceledException)
				{ }
				finally
				{
					endLongOperation(operationToken);
					await updateDataSelectionInLeftPanelAsync();
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

			LongOpNotifier.Token operationToken = beginLongOperation("Updating data selection");
			try
			{
				var originalScene = ActiveScene;
				Action<string, int> progressReport = (operationName, percentDone) => longOpNotifier.UpdateState(operationToken, operationName, percentDone);
				await layerDataVisualizer.UpdateDataSelectionAsync(solutionHub, e.DataSelection, cancellationTokenSources[operationToken].Token, originalScene, progressReport);
				// update colors
				originalScene.PerformAction(AvailableAction.UpdateColorBuffers);
			}
			catch (OperationCanceledException)
			{ }
			finally
			{
				endLongOperation(operationToken);
				await updateDataSelectionInLeftPanelAsync();
			}
		}

		private void visualizerSettingsControl_SettingsChanged(object sender, EventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			// update colors
			ActiveScene.PerformAction(AvailableAction.UpdateColorBuffers);
		}

		private void longOpNotifier_CancellationRequested(LongOpNotifier.Token operationToken)
		{
			cancelOperation(operationToken);
		}

		#endregion

		#region Overrides

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var token in cancellationTokenSources.Keys)
					cancelOperation(token);

				if (components != null)
				{
					components.Dispose();
				}

				longOpNotifier.CancellationRequested -= longOpNotifier_CancellationRequested;
			}
			base.Dispose(disposing);
		}

		#endregion
	}
}

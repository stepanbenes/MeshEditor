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
using MeshEditor.SolutionManager.IO;
using MeshEditor.Data;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class PostprocessViewControl : ContentViewControl
	{

		#region Fields, constructor

		Control contentPanel;
		SolutionHub solutionHub;
		ISolutionDescription solutionDescription;
		MemoryLogger logger;
		SceneFacade activeScene;
		bool changingActiveScene;
		Dictionary<Guid, SummaryFile> layerSummaryCache = new Dictionary<Guid, SummaryFile>();

		LongOpNotifier longOpNotifier;
		Dictionary<LongOpNotifier.Token, CancellationTokenSource> cancellationTokenSources = new Dictionary<LongOpNotifier.Token, CancellationTokenSource>();

		public PostprocessViewControl(LongOpNotifier longOpNotifier)
		{
			InitializeComponent();

			activeScene = new SceneFacade(new PostprocessScene());

			this.longOpNotifier = longOpNotifier;
			this.longOpNotifier.CancellationRequested += longOpNotifier_CancellationRequested;

			mainSplitContainer.FixedPanel = FixedPanel.Panel1;
			mainSplitContainer.SplitterDistance = 220;

			layersTreeView.LayerUnselected += layersTreeView_LayerUnselected;
			layersTreeView.LayerSelected += layersTreeView_LayerSelected;
			layersTreeView.LayerChecked += layersTreeView_LayerChecked;
			layersTreeView.LayerUnchecked += layersTreeView_LayerUnchecked;
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
			await loadSolutionWithErrorHandlingAsync();
		}

		public async Task LoadRemoteSolutionAsync(int solutionId)
		{
			logger = new MemoryLogger();
			solutionHub = SolutionHub.CreateRemote(solutionId, logger);
			await loadSolutionWithErrorHandlingAsync();
		}

		#endregion

		#region Private methods

		#region Event handlers

		private void layersTreeView_LayerUnselected(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			if (!layersTreeView.IsLayerChecked(e.Layer.Id))
			{
				((PostprocessScene)ActiveScene.GetUnderlyingSceneObject()).RemoveMeshFromLayer(e.Layer.Id);
			}
		}

		private async void layersTreeView_LayerSelected(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			ActiveScene.SetValue(AvailableValue.SelectedLayerId, e.Layer.Id);
			await updateDataSelectionInLeftPanelAsync();

			var visibleLayerIds = ActiveScene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
			if (!visibleLayerIds.Contains(e.Layer.Id))
			{
				await loadLayerWithErrorHandlingAsync(e.Layer);
			}
		}

		private void layersTreeView_LayerChecked(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;
			Debug.Assert(e.Layer != null);
			Debug.Assert((ActiveScene.GetValue(AvailableValue.SelectedLayerId) as Guid?) == e.Layer.Id);

			var visibleLayerIds = ActiveScene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
			Debug.Assert(!visibleLayerIds.Contains(e.Layer.Id));
			visibleLayerIds.Add(e.Layer.Id);
			Debug.Assert(visibleLayerIds.Contains(e.Layer.Id));
		}

		private void layersTreeView_LayerUnchecked(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;
			Debug.Assert(e.Layer != null);
			Debug.Assert((ActiveScene.GetValue(AvailableValue.SelectedLayerId) as Guid?) == e.Layer.Id);

			var visibleLayerIds = ActiveScene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
			Debug.Assert(visibleLayerIds.Contains(e.Layer.Id));
			visibleLayerIds.Remove(e.Layer.Id);
			Debug.Assert(!visibleLayerIds.Contains(e.Layer.Id));
		}

		private async void dataSelectionControl_DataSelectionChanged(object sender, DataSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			const string taskName = "Updating data selection";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				try
				{
					var originalScene = ActiveScene;
					Action<string, int> progressReport = (operationName, percentDone) => longOpNotifier.UpdateState(operationToken, operationName, percentDone);
					await ((PostprocessScene)ActiveScene.GetUnderlyingSceneObject()).UpdateLayerAsync(solutionHub, e.DataSelection, buildSolutionTitle(), progressReport, cancellationTokenSources[operationToken].Token);

					// update colors, repaint mesh in all windows, compute visible nodes, update caption, status, ...
					originalScene.PerformAction(AvailableAction.Refresh);
				}
				catch (OperationCanceledException)
				{ }
				finally
				{
					endLongOperation(operationToken);
				}
			}
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
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

		#region Solution data loading

		private async Task loadSolutionWithErrorHandlingAsync()
		{
			const string taskName = "Loading solution";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				try
				{
					await loadSolutionAsync(operationToken);
				}
				catch (OperationCanceledException)
				{ }
				finally
				{
					endLongOperation(operationToken);
				}
			}
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
		}

		private async Task loadSolutionAsync(LongOpNotifier.Token operationToken)
		{
			Debug.Assert(solutionHub != null);
			longOpNotifier.UpdateState(operationToken, "Loading layer tree");
			solutionDescription = await solutionHub.GetSolutionDescriptionAsync(cancellationTokenSources[operationToken].Token);
			var layers = solutionDescription.Layers;
			layersTreeView.SetLayerTree(layers);

			if (layers.Count > 0) // load first layer
			{
				Debug.Assert(ActiveScene != null);
				ActiveScene.SetValue(AvailableValue.SelectedLayerId, layers[0].Id);
				layersTreeView.SetSelectedLayer(layers[0].Id);
				await loadLayerAsync(layers[0], operationToken);
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

		private async Task loadLayerAsync(ILayerInfo layerInfo, LongOpNotifier.Token operationToken)
		{
			longOpNotifier.UpdateState(operationToken, "Loading layer summary");
			CancellationToken cancellationToken = cancellationTokenSources[operationToken].Token;
			var summary = await getSummaryFileForLayerAsync(layerInfo.Id, cancellationToken);

			var firstMesh = summary.Meshes.FirstOrDefault();
			if (firstMesh != null)
			{
				var originalScene = ActiveScene;
				int? elementPropertyAttributeIndex = firstMesh?.Attributes.FirstOrDefault(a => a.FieldName == AttributeDescription.KnownAttributeNames.ElementProperty)?.Index;
				Action<string, int> progressReport = (operationName, percentDone) => longOpNotifier.UpdateState(operationToken, operationName, percentDone);
				var dataVisualizerController = await ((PostprocessScene)ActiveScene.GetUnderlyingSceneObject()).UpdateLayerAsync(solutionHub, new DataSelection(layerInfo.Id, firstMesh.Index, elementPropertyAttributeIndex), buildSolutionTitle(), progressReport, cancellationToken);

				// update colors, repaint mesh in all windows, compute visible nodes, update caption, status, ...
				originalScene.PerformAction(AvailableAction.Refresh);

				visualizerSettingsControl.Settings = dataVisualizerController?.Settings;
			}

			dataSelectionControl.UpdateDataSource(summary, null);
		}

		private async Task loadLayerWithErrorHandlingAsync(ILayerInfo layerInfo)
		{
			const string taskName = "Loading layer";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				try
				{
					await loadLayerAsync(layerInfo, operationToken);
				}
				catch (OperationCanceledException)
				{ }
				finally
				{
					endLongOperation(operationToken);
				}
			}
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
		}

		#endregion

		#region UI update

		private async Task updateDataSelectionInLeftPanelAsync()
		{
			var selectedLayerId = ActiveScene.GetValue(AvailableValue.SelectedLayerId) as Guid?;
			var visibleLayerIds = ActiveScene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
			var layerDataVisualizer = ActiveScene.GetValue(AvailableValue.DataVisualizer) as LayerDataVisualizer;
			if (selectedLayerId.HasValue && layerDataVisualizer != null)
			{
				const string taskName = "Loading layer summary";
				try
				{
					LongOpNotifier.Token operationToken = beginLongOperation(taskName);
					try
					{
						var originalScene = ActiveScene;
						var summary = await getSummaryFileForLayerAsync(selectedLayerId.Value, cancellationTokenSources[operationToken].Token);
						if (ActiveScene != originalScene) // active scene changed during operation
							return;
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
				}
				catch (Exception ex)
				{
					dataSelectionControl.UpdateDataSource(null, null);
					new ExceptionReportForm(taskName, ex, logger).ShowDialog();
				}

				visualizerSettingsControl.Settings = layerDataVisualizer.Settings;
			}
			else
			{
				dataSelectionControl.UpdateDataSource(null, null);
				visualizerSettingsControl.Settings = null;
			}

			try
			{
				changingActiveScene = true;
				layersTreeView.SetCheckedLayers(visibleLayerIds);
				layersTreeView.SetSelectedLayer(selectedLayerId);
			}
			finally
			{
				changingActiveScene = false;
			}
		}

		private string buildSolutionTitle()
		{
			if (string.IsNullOrEmpty(solutionDescription.Location) ||
				solutionDescription.Location.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
			{
				return (solutionDescription.ProjectName + " (remote solution)").Trim();
			}
			else
			{
				return solutionDescription.Location;
			}
		}

		#endregion

		#region Operation progress control

		private LongOpNotifier.Token beginLongOperation(string taskName)
		{
			//mainSplitContainer.Panel1.Enabled = false;
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
			//mainSplitContainer.Panel1.Enabled = true;
		}

		#endregion

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.SolutionManager;
using MeshEditor.CoreInterface;
using System.Diagnostics;
using MeshEditor.LayerManager.Data;
using System.Threading;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.SolutionManager.IO;
using MeshEditor.Common.Logging;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class PostprocessViewControl : ContentViewControl
	{

		#region Fields, constructor

		Control contentPanel;
		SolutionHub solutionHub;
		MemoryLogger logger;
		SceneFacade activeScene;
		bool changingActiveScene;
		Dictionary<Guid, SummaryFile> layerSummaryCache = new Dictionary<Guid, SummaryFile>();

		LongOpNotifier longOpNotifier;
		CancellationTokenSource currentOperationCancellationTokenSource;

		public PostprocessViewControl(LongOpNotifier longOpNotifier)
		{
			InitializeComponent();

			activeScene = new SceneFacade(new PostprocessScene());

			this.longOpNotifier = longOpNotifier;
			this.longOpNotifier.CancellationRequested += longOpNotifier_CancellationRequested;

			mainSplitContainer.FixedPanel = FixedPanel.Panel1;
			mainSplitContainer.SplitterDistance = 220;

			layersTreeView.LayerSelected += layersTreeView_LayerSelected;
			layersTreeView.LayerChecked += layersTreeView_LayerChecked;
			layersTreeView.LayerUnchecked += layersTreeView_LayerUnchecked;
			layersTreeView.LayerReloadRequested += layersTreeView_LayerReloadRequested;
			layersTreeView.LayerFilterRequested += layersTreeView_LayerFilterRequested;
			layersTreeView.LayerDeleteRequested += layersTreeView_LayerDeleteRequested;

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
			solutionHub = SolutionHub.OpenLocal(solutionFileFullPath, logger);
			_ = await loadSolutionWithErrorHandlingAsync();
		}

		public async Task LoadRemoteSolutionAsync(int solutionId)
		{
			logger = new MemoryLogger();
			solutionHub = SolutionHub.OpenRemote(solutionId, logger);
			_ = await loadSolutionWithErrorHandlingAsync();
		}

		#endregion

		#region Private methods

		#region Event handlers

		private async void layersTreeView_LayerSelected(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			var targetScene = ActiveScene;

			((PostprocessScene)targetScene.GetUnderlyingSceneObject()).RemoveMeshFromAllUncheckedLayers();

			targetScene.SetValue(AvailableValue.SelectedLayerId, e.Layer.Id);
			await updateDataSelectionInLeftPanelAsync();

			var visibleLayerIds = targetScene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
			if (!visibleLayerIds.Contains(e.Layer.Id))
			{
				await loadLayerWithErrorHandlingAsync(e.Layer, targetScene);
			}
		}

		private void layersTreeView_LayerChecked(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

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

			Debug.Assert((ActiveScene.GetValue(AvailableValue.SelectedLayerId) as Guid?) == e.Layer.Id);

			var visibleLayerIds = ActiveScene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
			Debug.Assert(visibleLayerIds.Contains(e.Layer.Id));
			visibleLayerIds.Remove(e.Layer.Id);
			Debug.Assert(!visibleLayerIds.Contains(e.Layer.Id));
		}

		private async void layersTreeView_LayerReloadRequested(object sender, LayerSelectionEventArgs e)
		{
			((PostprocessScene)ActiveScene.GetUnderlyingSceneObject()).RemoveMeshFromAllUncheckedLayers();
			await loadLayerWithErrorHandlingAsync(e.Layer, ActiveScene);
		}

		private async void layersTreeView_LayerFilterRequested(object sender, LayerFilterEventArgs e)
		{
			FilterParamsForm filterParamsForm;

			switch (e.FilterType)
			{
				case LayerManager.Filters.FilterType.Deformation:
					var layerSummary = await getSummaryFileForLayerAsync(e.Layer.Id, CancellationToken.None); // layer should be already loaded, shoul run synchronously and return layer summary from cache
					filterParamsForm = new DeformationFilterParamsForm(
						availableVectorFields: layerSummary.Fields.Where(pair => pair.Value.Components.Count == 3).Select(pair => pair.Key))
					{
						Owner = this.ParentForm
					};
					break;
				default:
					throw new NotSupportedException($"Filter type '{e.FilterType}' is not supported in UI");
			}

			if (filterParamsForm.ShowDialog() == DialogResult.OK)
			{
				var filterParams = filterParamsForm.GetOutput();

				// TODO: extract method

				const string taskName = "Generating filter layer";
				try
				{
					LongOpNotifier.Token operationToken = beginLongOperation(taskName);
					try
					{
						var cancellationToken = currentOperationCancellationTokenSource.Token;
						var parentLayerIdentifier = e.Layer.Id.ToString();

						var filterLayerInfo = await Task.Run(() => solutionHub.Filter(parentLayerIdOrName: parentLayerIdentifier, filterTypeName: e.FilterType.ToString(), filterParameters: filterParams.FilterParameters, keyTimeSteps: filterParams.KeyTimeSteps, compressionParameters: filterParams.CompressionParameters, fieldName: filterParams.ConstraintFieldName, newLayerName: filterParams.LayerName), cancellationToken);

						layersTreeView.SetCheckedFlagOfLayer(e.Layer.Id, true);
						ActiveScene.SetValue(AvailableValue.RenderMode, MeshEditor.Graphics.RenderMode.BorderLines);
						layersTreeView.AddNewLayer(e.Layer.Id, filterLayerInfo, selectNewLayer: true);

						cancellationToken.ThrowIfCancellationRequested();
					}
					finally
					{
						endLongOperation(operationToken);
					}
				}
				catch (OperationCanceledException)
				{
					// probably because of close solution command, do nothing
				}
				catch (Exception ex)
				{
					new ExceptionReportForm(taskName, ex, logger).ShowDialog();
				}
			}
		}

		private async void layersTreeView_LayerDeleteRequested(object sender, LayerSelectionEventArgs e)
		{
			// TODO: add confirmation dialog

			const string taskName = "Deleting layer";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				try
				{
					var cancellationToken = currentOperationCancellationTokenSource.Token;
					await solutionHub.DeleteAsync(e.Layer.Id.ToString(), cancellationToken: cancellationToken);
					layersTreeView.RemoveLayer(e.Layer.Id, selectParentLayer: true);
					cancellationToken.ThrowIfCancellationRequested();
				}
				finally
				{
					endLongOperation(operationToken);
				}
			}
			catch (OperationCanceledException)
			{
				// probably because of close solution command, do nothing
			}
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
		}

		private async void dataSelectionControl_DataSelectionChanged(object sender, DataSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			const string taskName = "Updating data selection";
			try
			{
				var targetScene = ActiveScene;
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				try
				{
					Action<string, int> progressReport = (operationName, percentDone) => longOpNotifier.UpdateState(operationToken, operationName, percentDone);
					var cancellationToken = currentOperationCancellationTokenSource.Token;
					await ((PostprocessScene)ActiveScene.GetUnderlyingSceneObject()).UpdateLayerAsync(solutionHub, e.LayerId, e.LayerName, e.DataSelection, progressReport, cancellationToken);
					cancellationToken.ThrowIfCancellationRequested();
				}
				catch (OperationCanceledException)
				{ }
				finally
				{
					endLongOperation(operationToken);

					// update colors, repaint mesh in all windows, compute visible nodes, update caption, status, ...
					targetScene.PerformAction(AvailableAction.Refresh);
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
			if (currentOperationCancellationTokenSource != null)
			{
				currentOperationCancellationTokenSource.Cancel();
				currentOperationCancellationTokenSource.Dispose();
				currentOperationCancellationTokenSource = null;
			}
		}

		#endregion

		#region Solution data loading

		private async Task<ISolutionDescription> loadSolutionWithErrorHandlingAsync()
		{
			ISolutionDescription solutionDescription = null;
			const string taskName = "Loading solution";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				try
				{
					var cancellationToken = currentOperationCancellationTokenSource.Token;
					solutionDescription = await loadSolutionAsync(operationToken, cancellationToken);
					cancellationToken.ThrowIfCancellationRequested();
				}
				finally
				{
					endLongOperation(operationToken);
				}
			}
			catch (OperationCanceledException)
			{
				// probably because of close solution command, do nothing
			}
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
			layersTreeView.SetSelectedLayer(solutionDescription?.Layers.FirstOrDefault()?.Id); // select first layer
			return solutionDescription;
		}

		private async Task<ISolutionDescription> loadSolutionAsync(LongOpNotifier.Token operationToken, CancellationToken cancellationToken)
		{
			Debug.Assert(solutionHub != null);
			longOpNotifier.UpdateState(operationToken, "Loading layer tree");
			var postprocessScene = (PostprocessScene)ActiveScene.GetUnderlyingSceneObject();
			var solutionDescription = await solutionHub.GetSolutionDescriptionAsync(cancellationToken);
			postprocessScene.ProjectName = solutionDescription.ProjectName;
			layersTreeView.SetLayerTree(solutionDescription.Layers);
			return solutionDescription;
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

		private async Task loadLayerAsync(ILayerInfo layerInfo, SceneFacade targetScene, LongOpNotifier.Token operationToken, CancellationToken cancellationToken)
		{
			longOpNotifier.UpdateState(operationToken, "Loading layer summary");
			var summary = await getSummaryFileForLayerAsync(layerInfo.Id, cancellationToken);
			var firstMesh = summary.Meshes.FirstOrDefault();
			DataSelection dataSelection = null;
			if (firstMesh != null)
			{
				Action<string, int> progressReport = (operationName, percentDone) => longOpNotifier.UpdateState(operationToken, operationName, percentDone);
				dataSelection = new DataSelection(timeStep: firstMesh.TimeSteps.First(), mesh: firstMesh);
				var dataVisualizerController = await ((PostprocessScene)ActiveScene.GetUnderlyingSceneObject()).UpdateLayerAsync(solutionHub, layerInfo.Id, layerInfo.Name, dataSelection, progressReport, cancellationToken);

				// update colors, repaint mesh in all windows, compute visible nodes, update caption, status, ...
				targetScene.PerformAction(AvailableAction.Refresh);

				visualizerSettingsControl.Settings = dataVisualizerController?.Settings;
			}
			dataSelectionControl.UpdateDataSource(summary, dataSelection);
		}

		private async Task loadLayerWithErrorHandlingAsync(ILayerInfo layerInfo, SceneFacade targetScene)
		{
			const string taskName = "Loading layer";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				try
				{
					var cancellationToken = currentOperationCancellationTokenSource.Token;
					await loadLayerAsync(layerInfo, targetScene, operationToken, cancellationToken);
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
						var cancellationToken = currentOperationCancellationTokenSource.Token;
						var targetScene = ActiveScene;
						var summary = await getSummaryFileForLayerAsync(selectedLayerId.Value, cancellationToken);
						if (ActiveScene != targetScene) // active scene changed during operation
							return;
						cancellationToken.ThrowIfCancellationRequested();
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

		#endregion

		#region Operation progress control

		private LongOpNotifier.Token beginLongOperation(string taskName)
		{
			if (currentOperationCancellationTokenSource != null)
			{
				currentOperationCancellationTokenSource.Cancel();
				currentOperationCancellationTokenSource.Dispose();
			}
			logger.ClearHistory(); // clear records from previous operation
			var operationToken = longOpNotifier.Begin(taskName, isCancellable: true, logger: logger);
			currentOperationCancellationTokenSource = new CancellationTokenSource();
			return operationToken;
		}

		private void endLongOperation(LongOpNotifier.Token operationToken)
		{
			longOpNotifier.End(operationToken);
			if (currentOperationCancellationTokenSource != null)
			{
				currentOperationCancellationTokenSource.Dispose();
				currentOperationCancellationTokenSource = null;
			}
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
				if (currentOperationCancellationTokenSource != null)
				{
					currentOperationCancellationTokenSource.Cancel();
					currentOperationCancellationTokenSource.Dispose();
					currentOperationCancellationTokenSource = null;
				}

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

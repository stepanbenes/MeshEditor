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
			this.longOpNotifier = longOpNotifier;
			this.longOpNotifier.CancellationRequested += longOpNotifier_CancellationRequested;

			mainSplitContainer.FixedPanel = FixedPanel.Panel1;

			layersTreeView.LayerSelectionChanged += layersTreeView_LayerSelectionChanged;
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
			const string taskName = "Loading solution";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				try
				{
					Debug.Assert(solutionHub != null);
					Debug.Assert(ActiveScene != null);
					longOpNotifier.UpdateState(operationToken, "Loading layer tree");
					solutionDescription = await solutionHub.GetSolutionDescriptionAsync(cancellationTokenSources[operationToken].Token);
					var layers = solutionDescription.Layers;
					layersTreeView.SetLayerTree(layers);

					//if (layers.Count > 0) // load first layer
					//{
					//	ActiveScene.SetCurrentLayer(layers[0].Id);
					//	layersTreeView.SetSelectedLayer(layers[0]);
					//	await loadLayerAsync(layers[0], ActiveScene, operationToken);
					//}
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

		private async Task updateDataSelectionInLeftPanelAsync()
		{
			var selectedLayerId = ActiveScene.GetValue(AvailableValue.SelectedLayerId) as Guid?;
			var visibleLayerIds = ActiveScene.GetValue(AvailableValue.VisibleLayersIds) as IReadOnlyCollection<Guid>;
			var layerDataVisualizer = ActiveScene.GetValue(AvailableValue.DataVisualizer) as LayerDataVisualizer;
			if (selectedLayerId.HasValue && layerDataVisualizer != null)
			{
				const string taskName = "Loading layer summary";
				try
				{
					LongOpNotifier.Token operationToken = beginLongOperation(taskName);
					try
					{
						var summary = await getSummaryFileForLayerAsync(selectedLayerId.Value, cancellationTokenSources[operationToken].Token);
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
				string solutionDescriptionText = (string.IsNullOrEmpty(solutionDescription.Location) || solutionDescription.Location.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)) ? (solutionDescription.ProjectName + " (remote solution)").Trim() : solutionDescription.Location;
				var dataVisualizer = new LayerDataVisualizer(solutionDescriptionText);

				int? elementPropertyAttributeIndex = firstMesh?.Attributes.FirstOrDefault(a => a.FieldName == AttributeDescription.KnownAttributeNames.ElementProperty)?.Index;

				Action<string, int> progressReport = (operationName, percentDone) => longOpNotifier.UpdateState(operationToken, operationName, percentDone);

				await dataVisualizer.UpdateDataSelectionAsync(solutionHub, new DataSelection(layerId, firstMesh.Index, elementPropertyAttributeIndex), cancellationToken, ActiveScene, progressReport);

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

			ActiveScene.SetValue(AvailableValue.SelectedLayerId, e.Layer?.Id);
			await updateDataSelectionInLeftPanelAsync();
		}

		private async void layersTreeView_LayerChecked(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			Debug.Assert(e.Layer != null);
			Debug.Assert((ActiveScene.GetValue(AvailableValue.SelectedLayerId) as Guid?) == e.Layer.Id);

			const string taskName = "Loading layer";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				try
				{
					await loadLayerAsync(e.Layer.Id, ActiveScene, operationToken);
				}
				catch (OperationCanceledException)
				{ }
				finally
				{
					endLongOperation(operationToken);
					await updateDataSelectionInLeftPanelAsync();
				}
			}
			catch (Exception ex)
			{
				new ExceptionReportForm(taskName, ex, logger).ShowDialog();
			}
		}

		private void layersTreeView_LayerUnchecked(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			Debug.Assert(e.Layer != null);

			ActiveScene.RemoveMeshFromLayer(e.Layer.Id);
			dataSelectionControl.UpdateDataSource(null, null);
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

			const string taskName = "Updating data selection";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
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

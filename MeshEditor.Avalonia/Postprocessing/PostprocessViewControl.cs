using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MeshEditor.Common.Logging;
using MeshEditor.CoreInterface;
using MeshEditor.DataVisualizer;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.AvaloniaUI.Postprocessing
{
	/// <summary>
	/// Postprocessor layout: layers and data selection on the left, the viewports (content) on the right
	/// (port of MeshEditor.DataVisualizer.UI.PostprocessViewControl).
	/// </summary>
	public class PostprocessViewControl : Grid
	{
		#region Fields, constructor

		private Control contentPanel;
		private readonly Border contentHost = new Border();
		private SolutionHub solutionHub;
		private MemoryLogger logger = new MemoryLogger();
		private SceneFacade activeScene;
		private bool changingActiveScene;
		private readonly Dictionary<Guid, SummaryFile> layerSummaryCache = new Dictionary<Guid, SummaryFile>();
		private readonly LongOpNotifier longOpNotifier;
		private CancellationTokenSource currentOperationCancellationTokenSource;
		private LongOpNotifier.Token currentOperationToken;
		private CancellationToken userCancelledOperation; // the last operation cancelled by the user (not superseded by another operation)
		private readonly List<(ILayerInfo Layer, SceneFacade Scene)> postponedLayerLoads = new List<(ILayerInfo, SceneFacade)>(); // loads superseded by another operation

		internal readonly LayersTreeViewControl layersTreeView = new LayersTreeViewControl();
		internal readonly DataSelectionControl dataSelectionControl = new DataSelectionControl();

		public PostprocessViewControl(LongOpNotifier longOpNotifier)
		{
			// left panel: layers (top) / data selection (bottom); right panel: viewports
			// (WinForms split containers without a fixed panel keep the proportions of the designer: 220/1453 and 243/1060)
			ColumnDefinitions = new ColumnDefinitions("220*,4,1233*");
			ColumnDefinitions[0].MinWidth = 120;
			var leftPanel = new Grid { RowDefinitions = new RowDefinitions("243*,4,817*"), Margin = new Thickness(4) };
			leftPanel.RowDefinitions[0].MinHeight = leftPanel.RowDefinitions[2].MinHeight = 60;
			leftPanel.Children.Add(layersTreeView);
			var leftSplitter = new GridSplitter { ResizeDirection = GridResizeDirection.Rows, Background = Brushes.Transparent };
			SetRow(leftSplitter, 1);
			leftPanel.Children.Add(leftSplitter);
			var dataSelectionScroll = new ScrollViewer { Content = dataSelectionControl, Margin = new Thickness(0, 4, 0, 0) };
			SetRow(dataSelectionScroll, 2);
			leftPanel.Children.Add(dataSelectionScroll);
			Children.Add(leftPanel);
			var mainSplitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns, Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)) };
			SetColumn(mainSplitter, 1);
			Children.Add(mainSplitter);
			SetColumn(contentHost, 2);
			Children.Add(contentHost);

			activeScene = new SceneFacade(new PostprocessScene(drawOnlyVisibleLayers: true));
			this.longOpNotifier = longOpNotifier;
			this.longOpNotifier.CancellationRequested += longOpNotifier_CancellationRequested;

			layersTreeView.LayerSelected += layersTreeView_LayerSelected;
			layersTreeView.LayerChecked += layersTreeView_LayerChecked;
			layersTreeView.LayerUnchecked += layersTreeView_LayerUnchecked;
			layersTreeView.LayerReloadRequested += layersTreeView_LayerReloadRequested;
			layersTreeView.LayerFilterRequested += layersTreeView_LayerFilterRequested;
			layersTreeView.LayerDeleteRequested += layersTreeView_LayerDeleteRequested;

			dataSelectionControl.DataSelectionChanged += dataSelectionControl_DataSelectionChanged;
			dataSelectionControl.VisualizerSettingsChanged += dataSelectionControl_VisualizerSettingsChanged;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Viewports of the main window.
		/// </summary>
		public Control Content
		{
			get => contentPanel;
			set
			{
				if (contentPanel != value)
				{
					contentPanel = value;
					contentHost.Child = value;
				}
			}
		}

		public SceneFacade ActiveScene
		{
			get => activeScene;
			set
			{
				if (activeScene != value)
				{
					activeScene = value;
					_ = updateDataSelectionInLeftPanelAsync();
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

		/// <summary>
		/// Releases the long operation notification (control is removed from the main window).
		/// </summary>
		public void Detach()
		{
			postponedLayerLoads.Clear();
			if (currentOperationCancellationTokenSource != null)
			{
				userCancelledOperation = currentOperationCancellationTokenSource.Token;
				currentOperationCancellationTokenSource.Cancel();
				currentOperationCancellationTokenSource.Dispose();
				currentOperationCancellationTokenSource = null;
			}
			longOpNotifier.CancellationRequested -= longOpNotifier_CancellationRequested;
			contentHost.Child = null;
		}

		#endregion

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

			if (!((PostprocessScene)targetScene.GetUnderlyingSceneObject()).IsLayerLoaded(e.Layer.Id))
				await loadLayerWithErrorHandlingAsync(e.Layer, targetScene);
		}

		private async void layersTreeView_LayerChecked(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;
			var targetScene = ActiveScene;
			var visibleLayerIds = targetScene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
			if (!visibleLayerIds.Contains(e.Layer.Id))
				visibleLayerIds.Add(e.Layer.Id);
			bool isSelected = targetScene.GetValue(AvailableValue.SelectedLayerId) as Guid? == e.Layer.Id; // the selected layer is loaded by the selection
			if (!isSelected && !((PostprocessScene)targetScene.GetUnderlyingSceneObject()).IsLayerLoaded(e.Layer.Id))
				await loadLayerWithErrorHandlingAsync(e.Layer, targetScene);
			targetScene.PerformAction(AvailableAction.Refresh);
		}

		private void layersTreeView_LayerUnchecked(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;
			var visibleLayerIds = ActiveScene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
			visibleLayerIds.Remove(e.Layer.Id);
			ActiveScene.PerformAction(AvailableAction.Refresh);
		}

		private async void layersTreeView_LayerReloadRequested(object sender, LayerSelectionEventArgs e)
		{
			var postprocessScene = (PostprocessScene)ActiveScene.GetUnderlyingSceneObject();
			postprocessScene.RemoveMeshFromAllUncheckedLayers();
			postprocessScene.ForgetStateOfRemovedLayer(e.Layer.Id); // reload with the default data selection
			await loadLayerWithErrorHandlingAsync(e.Layer, ActiveScene);
		}

		private async void layersTreeView_LayerFilterRequested(object sender, LayerFilterEventArgs e)
		{
			FilterParamsDialog filterParamsForm;
			bool modal;
			switch (e.FilterType)
			{
				case FilterType.Deformation:
					{
						var layerSummary = await getSummaryFileForLayerAsync(e.Layer.Id, CancellationToken.None); // layer is loaded already (summary from cache)
						filterParamsForm = new DeformationFilterParamsDialog(
							availableVectorFields: layerSummary.Fields.Where(pair => pair.Value.Components.Count <= 3).Select(pair => pair.Key));
						modal = true;
					}
					break;
				case FilterType.Slice:
					filterParamsForm = new SliceFilterParamsDialog(ActiveScene);
					modal = false;
					break;
				case FilterType.IsoSurface:
					{
						var layerSummary = await getSummaryFileForLayerAsync(e.Layer.Id, CancellationToken.None);
						filterParamsForm = new IsoSurfaceFilterParamsDialog(
							availableFieldComponents: layerSummary.Fields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Components.Keys.ToList().AsEnumerable()));
						modal = true;
					}
					break;
				default:
					throw new NotSupportedException($"Filter type '{e.FilterType}' is not supported in UI");
			}

			if (await filterParamsForm.ShowAsync(TopLevel.GetTopLevel(this) as Window, modal))
			{
				var filterParams = filterParamsForm.FilterParams;
				const string taskName = "Generating filter layer";
				try
				{
					LongOpNotifier.Token operationToken = beginLongOperation(taskName);
					try
					{
						var cancellationToken = currentOperationCancellationTokenSource.Token;
						var parentLayerIdentifier = e.Layer.Id.ToString();
						var filterLayerInfo = await Task.Run(() => solutionHub.Filter(parentLayerIdOrName: parentLayerIdentifier, filterTypeName: e.FilterType.ToString(), filterParameters: filterParams.FilterParameters, keyTimeSteps: filterParams.KeyTimeSteps, compressionParameters: filterParams.CompressionParameters, fieldName: filterParams.ConstraintFieldName, newLayerName: filterParams.LayerName, cancellationToken: cancellationToken));
						layersTreeView.SetCheckedFlagOfLayer(e.Layer.Id, true);
						ActiveScene.SetValue(AvailableValue.RenderMode, MeshEditor.Graphics.RenderMode.BorderLines);
						layersTreeView.AddNewLayer(e.Layer.Id, filterLayerInfo, selectNewLayer: true);
						layersTreeView.SetCheckedFlagOfLayer(filterLayerInfo.Id, true); // show the new layer
						cancellationToken.ThrowIfCancellationRequested();
					}
					finally
					{
						endLongOperation(operationToken);
					}
				}
				catch (OperationCanceledException)
				{ }
				catch (Exception ex)
				{
					await ExceptionReportDialog.Show(taskName, ex, logger);
				}
			}
		}

		private async void layersTreeView_LayerDeleteRequested(object sender, LayerSelectionEventArgs e)
		{
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
			{ }
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show(taskName, ex, logger);
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
					targetScene.PerformAction(AvailableAction.Refresh);
				}
			}
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show(taskName, ex, logger);
			}
		}

		private void dataSelectionControl_VisualizerSettingsChanged(object sender, EventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;
			ActiveScene.PerformAction(AvailableAction.UpdateColorBuffers);
		}

		private void longOpNotifier_CancellationRequested(LongOpNotifier.Token operationToken)
		{
			// the user doesn't want the layers whose load was postponed either
			foreach (var (layer, scene) in postponedLayerLoads)
				uncheckLayerIfNotLoaded(layer.Id, scene);
			postponedLayerLoads.Clear();
			if (currentOperationCancellationTokenSource != null)
			{
				userCancelledOperation = currentOperationCancellationTokenSource.Token;
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
			{ }
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show(taskName, ex, logger);
			}
			var firstLayerId = solutionDescription?.Layers.FirstOrDefault()?.Id;
			layersTreeView.SetSelectedLayer(firstLayerId); // select and show first layer
			if (firstLayerId.HasValue)
				layersTreeView.SetCheckedFlagOfLayer(firstLayerId.Value, true);
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
			if (!layerSummaryCache.TryGetValue(layerId, out SummaryFile summary))
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
			IVisualizerSettings visualizerSettings = null;
			if (firstMesh != null)
			{
				Action<string, int> progressReport = (operationName, percentDone) => longOpNotifier.UpdateState(operationToken, operationName, percentDone);
				var postprocessScene = (PostprocessScene)ActiveScene.GetUnderlyingSceneObject();
				dataSelection = getDataSelectionOfRemovedLayer(postprocessScene, layerInfo.Id, summary)
					?? new DataSelection(timeStep: firstMesh.TimeSteps.FirstOrDefault() /* zero if no time step is present */, mesh: firstMesh);
				var dataVisualizerController = await postprocessScene.UpdateLayerAsync(solutionHub, layerInfo.Id, layerInfo.Name, dataSelection, progressReport, cancellationToken);
				targetScene.PerformAction(AvailableAction.Refresh);
				visualizerSettings = dataVisualizerController?.Settings;
			}
			if (targetScene == ActiveScene && targetScene.GetValue(AvailableValue.SelectedLayerId) as Guid? == layerInfo.Id) // a checked layer can be loaded without being selected
				dataSelectionControl.UpdateDataSource(summary, dataSelection, visualizerSettings);
		}

		/// <summary>
		/// Data selection the layer had before its mesh was removed (when another layer was selected), if it is still valid for the layer summary.
		/// </summary>
		private static DataSelection getDataSelectionOfRemovedLayer(PostprocessScene postprocessScene, Guid layerId, SummaryFile summary)
		{
			var lastDataSelection = postprocessScene.GetDataSelectionOfRemovedLayer(layerId);
			if (lastDataSelection == null)
				return null;
			var mesh = summary.Meshes.FirstOrDefault(m => m.Index == lastDataSelection.Mesh.Index && m.TimeSteps.Contains(lastDataSelection.TimeStep));
			if (mesh == null)
				return null;
			return new DataSelection(lastDataSelection.TimeStep, lastDataSelection.FieldName, lastDataSelection.ComponentName, lastDataSelection.VectorFieldName, mesh);
		}

		private async Task loadLayerWithErrorHandlingAsync(ILayerInfo layerInfo, SceneFacade targetScene)
		{
			const string taskName = "Loading layer";
			try
			{
				LongOpNotifier.Token operationToken = beginLongOperation(taskName);
				var cancellationToken = currentOperationCancellationTokenSource.Token;
				try
				{
					await loadLayerAsync(layerInfo, targetScene, operationToken, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					if (cancellationToken == userCancelledOperation)
						uncheckLayerIfNotLoaded(layerInfo.Id, targetScene); // don't leave a checked layer without mesh
					else
						postponedLayerLoads.Add((layerInfo, targetScene)); // superseded by another operation (e.g. checking another layer): load it after that one
				}
				finally
				{
					endLongOperation(operationToken);
				}
			}
			catch (Exception ex)
			{
				await ExceptionReportDialog.Show(taskName, ex, logger);
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
						dataSelectionControl.UpdateDataSource(summary, layerDataVisualizer.DataSelection, layerDataVisualizer.Settings);
					}
					catch (OperationCanceledException)
					{
						dataSelectionControl.UpdateDataSource(null, null, layerDataVisualizer.Settings);
					}
					finally
					{
						endLongOperation(operationToken);
					}
				}
				catch (Exception ex)
				{
					dataSelectionControl.UpdateDataSource(null, null, layerDataVisualizer.Settings);
					await ExceptionReportDialog.Show(taskName, ex, logger);
				}
			}
			else
			{
				dataSelectionControl.UpdateDataSource(null, null, null);
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
			currentOperationToken = operationToken;
			return operationToken;
		}

		private void endLongOperation(LongOpNotifier.Token operationToken)
		{
			longOpNotifier.End(operationToken);
			if (operationToken != currentOperationToken)
				return; // superseded: the cancellation token source belongs to the operation that replaced this one
			currentOperationToken = LongOpNotifier.Token.None;
			if (currentOperationCancellationTokenSource != null)
			{
				currentOperationCancellationTokenSource.Dispose();
				currentOperationCancellationTokenSource = null;
			}
			if (postponedLayerLoads.Count > 0)
				Dispatcher.UIThread.Post(loadPostponedLayers);
		}

		/// <summary>
		/// Loads the layers whose load was cancelled by a newer operation, if they are still checked or selected.
		/// </summary>
		private async void loadPostponedLayers()
		{
			while (postponedLayerLoads.Count > 0)
			{
				if (currentOperationCancellationTokenSource != null)
					return; // another operation is running, its end posts this again
				var (layer, scene) = postponedLayerLoads[0];
				postponedLayerLoads.RemoveAt(0);
				var visibleLayerIds = scene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
				bool wanted = visibleLayerIds.Contains(layer.Id) || scene.GetValue(AvailableValue.SelectedLayerId) as Guid? == layer.Id;
				if (wanted && !((PostprocessScene)scene.GetUnderlyingSceneObject()).IsLayerLoaded(layer.Id))
					await loadLayerWithErrorHandlingAsync(layer, scene);
			}
		}

		private void uncheckLayerIfNotLoaded(Guid layerId, SceneFacade scene)
		{
			var visibleLayerIds = scene.GetValue(AvailableValue.VisibleLayersIds) as ICollection<Guid>;
			if (!visibleLayerIds.Contains(layerId) || ((PostprocessScene)scene.GetUnderlyingSceneObject()).IsLayerLoaded(layerId) || scene.GetValue(AvailableValue.SelectedLayerId) as Guid? == layerId)
				return;
			if (scene == ActiveScene)
				layersTreeView.SetCheckedFlagOfLayer(layerId, false); // raises LayerUnchecked
			else
				visibleLayerIds.Remove(layerId);
		}

		#endregion
	}
}

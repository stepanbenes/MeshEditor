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

namespace MeshEditor.WinUI
{
	public partial class PostprocessViewControl : ContentViewControl
	{
		#region Fields, constructor

		Control contentPanel;
		SolutionHub solutionHub;
		SceneFacade activeScene;
		bool changingActiveScene;
		bool changingDataSelectionSource;

		public PostprocessViewControl()
		{
			InitializeComponent();
			splitContainer1.FixedPanel = FixedPanel.Panel1;

			layersTreeView.SelectedLayerChanged += layersTreeView_SelectedLayerChanged;
			dataSelectionControl.DataSelectionChanged += dataSelectionControl_DataSelectionChanged;
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
					layersTreeView.SelectedLayerId = layerDataVisualizer.LayerId;
					dataSelectionControl.UpdateDataSelection(layerDataVisualizer.DataSelection);
				}
				else
				{
					layersTreeView.SelectedLayerId = null;
					dataSelectionControl.UpdateDataSelection(null);
				}
			}
			finally
			{
				changingActiveScene = false;
			}
		}

		private void loadLayer(Guid layerId)
		{
			var summary = solutionHub.LoadLayerSummary(layerId);

			try
			{
				changingDataSelectionSource = true;
				dataSelectionControl.UpdateDataSource(summary);
			}
			finally
			{
				changingDataSelectionSource = false;
			}

			var dataSelection = dataSelectionControl.GetDataSelection();

			if (dataSelection != null)
			{
				var geometry = solutionHub.LoadGeometry(layerId, dataSelection.MeshIndex);
				ActiveScene.ReloadMesh(new LayerMeshFileParser(geometry));

				var dataVisualizer = new LayerDataVisualizer(layerId);
				dataVisualizer.UpdateDataSelection(solutionHub, dataSelection);
				ActiveScene.SetValue(AvailableValue.DataVisualizer, dataVisualizer);
				ActiveScene.PerformAction(AvailableAction.UpdateColorBuffers);
			}
		}

		private void layersTreeView_SelectedLayerChanged(object sender, LayerSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene)
				return;

			if (e.LayerId.HasValue)
			{
				loadLayer(e.LayerId.Value);
			}
			else
			{
				// TODO: clear scene
				throw new NotImplementedException();
			}
		}

		private void dataSelectionControl_DataSelectionChanged(object sender, DataSelectionEventArgs e)
		{
			Debug.Assert(ActiveScene != null);
			if (changingActiveScene || changingDataSelectionSource)
				return;

			var layerDataVisualizer = ActiveScene.GetValue(AvailableValue.DataVisualizer) as LayerDataVisualizer;
			if (layerDataVisualizer == null)
				return;

			if (e.DataSelection != null && layerDataVisualizer.DataSelection?.MeshIndex != e.DataSelection.MeshIndex)
			{
				var geometry = solutionHub.LoadGeometry(layerDataVisualizer.LayerId, e.DataSelection.MeshIndex);
				ActiveScene.ReloadMesh(new LayerMeshFileParser(geometry));
			}

			layerDataVisualizer.UpdateDataSelection(solutionHub, e.DataSelection);

			ActiveScene.PerformAction(AvailableAction.UpdateColorBuffers);
		}

		#endregion
	}
}

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

		public PostprocessViewControl()
		{
			InitializeComponent();
			splitContainer1.FixedPanel = FixedPanel.Panel1;
			layersTreeView.SelectedLayerChanged += layersTreeView_SelectedLayerChanged;
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

		public async Task LoadRemoteSolution()
		{
			await Task.Yield();

			RemoteSolutionsForm remoteSolutionsForm = new RemoteSolutionsForm { Owner = Application.OpenForms?[0] };

			if (remoteSolutionsForm.ShowDialog() == DialogResult.OK)
			{
				int? selectedSolutionId = remoteSolutionsForm.SelectedSolutionId;
				if (selectedSolutionId.HasValue)
				{
					loadSolution(SolutionHub.CreateRemote(selectedSolutionId.Value));
				}
			}
		}

		#endregion

		#region Private methods

		private void loadSolution(SolutionHub solutionHub)
		{
			this.solutionHub = solutionHub;
			// TODO:
			//ActiveScene.SetValue(AvailableValue.DataVisualizer, new LayerDataVisualizer());
			updateLayerTree();
		}

		private void updateLayerTree()
		{
			if (solutionHub == null)
				return;

			var layers = solutionHub.EnumerateAllLayers();
			layersTreeView.SetLayerTree(layers);
		}

		bool updatingControlPanel;

		private void onActiveSceneChanged()
		{
			var layerDataVisualizer = ActiveScene.GetValue(AvailableValue.DataVisualizer) as LayerDataVisualizer;
			try
			{
				updatingControlPanel = true;
				if (layerDataVisualizer != null)
				{
					layersTreeView.SelectedLayerId = layerDataVisualizer.LayerId;
				}
				else
				{
					layersTreeView.SelectedLayerId = null;
				}
			}
			finally
			{
				updatingControlPanel = false;
			}
		}

		private IDataVisualizer createDataVisualizerFor(Guid layerId, int meshIndex)
		{
			return new LayerDataVisualizer(layerId, meshIndex);
		}

		private void loadLayer(Guid layerId)
		{
			var summary = solutionHub.LoadLayerSummary(layerId);
			var firstMesh = summary.Meshes.First(); // TODO: handle case when Meshes is null or empty 
			var geometry = solutionHub.LoadGeometry(layerId, firstMesh.Index);

			ActiveScene.LoadMesh(new LayerMeshFileParser(geometry), null, null);
			ActiveScene.SetValue(AvailableValue.DataVisualizer, createDataVisualizerFor(layerId, firstMesh.Index));
		}

		private void layersTreeView_SelectedLayerChanged(object sender, LayerEventArgs e)
		{
			Debug.Assert(ActiveScene != null);

			if (updatingControlPanel)
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

			ActiveScene.PerformAction(AvailableAction.Refresh);
		}

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using System.Globalization;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class SliceFilterParamsForm : FilterParamsForm
	{
		readonly SceneFacade scene;
		int requiredNumberOfPoints;

		public SliceFilterParamsForm(SceneFacade scene)
		{
			InitializeComponent();
			this.scene = scene;
			this.scene.CutPlaneDefinitionPointsChanged += scene_CutPlaneDefinitionPointsChanged;
			this.FormClosed += form_FormClosed;
		}

		private void clearCutPlanesAndPoints()
		{
			scene.CutPlanes.Clear(); // hide cut planes
			scene.PerformAction(AvailableAction.ClearPlaneDefinitionPoints); // remove definition points and refresh scene
		}

		#region Event handlers

		private void form_FormClosed(object sender, FormClosedEventArgs e)
		{
			scene.CutPlaneDefinitionPointsChanged -= scene_CutPlaneDefinitionPointsChanged;
			SceneFacade.EditorMode = EditorMode.Orbit;
			clearCutPlanesAndPoints();
		}

		private void scene_CutPlaneDefinitionPointsChanged(object sender, EventArgs e)
		{
			if (scene.CutPlaneDefinitionPointsCount >= requiredNumberOfPoints)
			{
				labelSuggestion.Visible = false;
				scene.PerformAction(AvailableAction.CreateCutPlane);
				if (scene.CutPlanes.Count > 0)
				{
					textBoxLayerName.Text = "slice " + scene.CutPlanes[0].Offset.ToString("0.00", CultureInfo.InvariantCulture);
				}
			}
		}

		private void buttonSelectTwoPoints_Click(object sender, EventArgs e)
		{
			requiredNumberOfPoints = 2;
			clearCutPlanesAndPoints();
			SceneFacade.EditorMode = EditorMode.PickCuttingPlanePoint;
			labelSuggestion.Visible = true;
		}

		private void buttonSelectThreePoints_Click(object sender, EventArgs e)
		{
			requiredNumberOfPoints = 3;
			clearCutPlanesAndPoints();
			SceneFacade.EditorMode = EditorMode.PickCuttingPlanePoint;
			labelSuggestion.Visible = true;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			CutPlane cutPlane = scene.CutPlanes.Single();
			var normal = cutPlane.NormalVector;
			var offset = cutPlane.Offset;
			string layerNameText = textBoxLayerName.Text;

			FilterParams = new FilterParams(
				filterParameters: new[] { normal.X, normal.Y, normal.Z, offset }.Select(p => p.ToString(CultureInfo.InvariantCulture)).ToArray(),
				keyTimeSteps: new decimal[0], /**/
				compressionParameters: new string[0], /**/
				layerName: string.IsNullOrWhiteSpace(layerNameText) ? null : layerNameText,
				constraintFieldName: null
			);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		#endregion
	}
}

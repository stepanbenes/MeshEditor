using MeshEditor.CoreInterface;
using MeshEditor.DataVisualizer.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class AnimationCreatorForm : Form
	{

		#region Fields, constructor

		IDataVisualizerController dataVisualizer;
		bool initialization;
		LongOpNotifier longOpNotifier;
		Timer animationTimer;
		Func<int, int, Bitmap> screenshotTaker;
		string meshFileName;
		bool dataIsContinuousInTime;
		
		Dictionary<double, int> scalarDataIndicesForTimes;
		Dictionary<double, int> vectorDataIndicesForTimes;
		Dictionary<double, int> deformationDataIndicesForTimes;

		public AnimationCreatorForm(IDataVisualizerController dataVisualizer, LongOpNotifier longOpNotifier, Func<int, int, Bitmap> screenshotTaker, string meshFileName)
		{
			InitializeComponent();
			Debug.Assert(dataVisualizer != null && longOpNotifier != null && screenshotTaker != null && !string.IsNullOrEmpty(meshFileName));
			this.dataVisualizer = dataVisualizer;
			this.longOpNotifier = longOpNotifier;
			this.screenshotTaker = screenshotTaker;
			this.meshFileName = meshFileName;
			setupData();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			stopAnimationTimer();
		}

		#endregion

		#region Private methods

		private void setupData()
		{
			initialization = true;
			try
			{
				comboBoxStep.Items.AddRange(Enumerable.Range(0, 10).Cast<object>().ToArray());
				comboBoxStep.SelectedIndex = 1;

				fillTimeStepsList();
			}
			finally
			{
				initialization = false;
			}
		}

		private void fillTimeStepsList()
		{
			listViewTimeSteps.Items.Clear();

			IntervalD timeRange;
			dataIsContinuousInTime = dataVisualizer.IsContinuousInTime(out timeRange);
			
			if (dataIsContinuousInTime)
			{
				decimal step = computeDecadicDivisionStepOfRange(timeRange);

				decimal time;
				int index = 0;
				do
				{
					time = Math.Min((decimal)timeRange.Min + step * index, (decimal)timeRange.Max);
					listViewTimeSteps.Items.Add(time.ToString());
					++index;
				} while (time < (decimal)timeRange.Max);
			}
			else
			{
				createDataIndicesForTimes();

				if (scalarDataIndicesForTimes != null)
				{
					foreach (double time in scalarDataIndicesForTimes.Keys)
					{
						listViewTimeSteps.Items.Add(time.ToString());
					}
				}
			}

			selectAppropriateTimeSteps();
		}

		private void createDataIndicesForTimes()
		{
			Debug.Assert(!dataIsContinuousInTime);

			// SCALARS
			scalarDataIndicesForTimes = null;
			int currentScalarDataIndex = dataVisualizer.Settings.ScalarDataIndex.Index;
			if (currentScalarDataIndex < 0) // if ShowVectorMagnitudes, invert the value
				currentScalarDataIndex = ~currentScalarDataIndex;
			DataType currentScalarDataType = dataVisualizer.DataIndexMap.Where(pair => pair.Value == currentScalarDataIndex).Select(pair => pair.Key.DataType).FirstOrDefault();
			if (currentScalarDataType != null)
			{
				scalarDataIndicesForTimes = dataVisualizer.DataIndexMap.Where(pair => pair.Key.DataType.Equals(currentScalarDataType)).ToDictionary(/*key:*/pair => pair.Key.Time, /*value:*/pair => pair.Value);
			}
			// VECTORS
			vectorDataIndicesForTimes = null;
			DataType currentVectorDataType = dataVisualizer.DataIndexMap.Where(pair => pair.Value == dataVisualizer.Settings.VectorDataIndex.Index).Select(pair => pair.Key.DataType).FirstOrDefault();
			if (currentVectorDataType != null)
			{
				vectorDataIndicesForTimes = dataVisualizer.DataIndexMap.Where(pair => pair.Key.DataType.Equals(currentVectorDataType)).ToDictionary(/*key:*/pair => pair.Key.Time, /*value:*/pair => pair.Value);
			}
			// DEFORMATIONS
			deformationDataIndicesForTimes = null;
			DataType currentDeformationDataType = dataVisualizer.DataIndexMap.Where(pair => pair.Value == dataVisualizer.Settings.DeformationScale.DeformationDataIndex.Index).Select(pair => pair.Key.DataType).FirstOrDefault();
			if (currentDeformationDataType != null)
			{
				deformationDataIndicesForTimes = dataVisualizer.DataIndexMap.Where(pair => pair.Key.DataType.Equals(currentDeformationDataType)).ToDictionary(/*key:*/pair => pair.Key.Time, /*value:*/pair => pair.Value);
			}
		}

		private static decimal computeDecadicDivisionStepOfRange(IntervalD range)
		{
			int order = range.GetOrder() - 2;
			decimal step = 1m;
			if (order > 0)
			{
				for (int i = 0; i < order; i++)
					step *= 10m;
			}
			else if (order < 0)
			{
				for (int i = 0; i > order; i--)
					step /= 10m;
			}
			return step;
		}

		private void selectAppropriateTimeSteps()
		{
			Debug.Assert(comboBoxStep.SelectedItem != null);
			int step = (int)comboBoxStep.SelectedItem;

			for (int i = 0; i < listViewTimeSteps.Items.Count; i++)
			{
				listViewTimeSteps.Items[i].Checked = step > 0 ? i % step == 0 : false;
			}

			// check last step
			if (listViewTimeSteps.Items.Count > 1)
			{
				listViewTimeSteps.Items[listViewTimeSteps.Items.Count - 1].Checked = step > 0;
			}
		}

		private void buttonCreateAnimation_Click(object sender, EventArgs ea)
		{
			if (initialization)
				return;

			if (animationTimer != null && animationTimer.Enabled)
			{
				stopAnimationTimer();
				return;
			}

			if (!dataIsContinuousInTime)
			{
				createDataIndicesForTimes();
			}

			double[] timeSteps = listViewTimeSteps.CheckedItems.Cast<ListViewItem>().Select(item => double.Parse(item.Text)).ToArray();

			double fps;
			if (!double.TryParse(textBoxFPS.Text, out fps))
			{
				fps = 10.0;
				textBoxFPS.Text = fps.ToString();
			}

			if (timeSteps.Length <= 1 || fps <= 0.0)
			{
				// TODO: inform user
				return;
			}

			// create directory for animation
			string directory = Path.Combine(Path.GetDirectoryName(meshFileName), "animation");
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
				Debug.Assert(Directory.Exists(directory));
			}
			string screenshotFileFormat = Path.Combine(directory, "frame_{0:" + new string('0', (int)(Math.Log10(timeSteps.Length) + 1.0)) + "}.png");

			double timeRange = timeSteps[timeSteps.Length - 1] - timeSteps[0];

			Debug.Assert(timeRange > 0.0);

			int timeIndex = 0;

			animationTimer = new Timer();
			animationTimer.Interval = (int)(1000.0 / fps);
			animationTimer.Tick += (s, e) =>
				{
					double currentTime = timeSteps[timeIndex];
					// --- CHANGE TIME for scalars, vectors and deformation -------------------------------------------------------------------------
					changeCurrentTime(currentTime);
					// --- TAKE SCREENSHOT ---------------------------------------------------------------------------------------------------------
					if (checkBoxSaveToFiles.Checked)
					{
						string filename = string.Format(screenshotFileFormat, timeIndex + 1);
						takeScreenshot(filename);
					}
					// -----------------------------------------------------------------------------------------------------------------------------

					progressBar.Value = (int)((currentTime - timeSteps[0]) * 100.0 / timeRange);

					if (timeIndex >= timeSteps.Length - 1)
					{
						stopAnimationTimer();

						if (checkBoxRepeat.Checked)
						{
							buttonCreateAnimation_Click(null, null);
						}
					}
					else
					{
						++timeIndex;
					}
				};

			buttonCreateAnimation.Text = "Stop";
			progressBar.Visible = true;
			animationTimer.Start(); // GO GO GO!
		}

		private void changeCurrentTime(double currentTime)
		{
			if (dataIsContinuousInTime)
			{
				if (dataVisualizer.Settings.ShowScalars)
					dataVisualizer.Settings.ScalarDataIndex = dataVisualizer.Settings.ScalarDataIndex.WithTime(currentTime);
				if (dataVisualizer.Settings.ShowVectors)
					dataVisualizer.Settings.VectorDataIndex = dataVisualizer.Settings.VectorDataIndex.WithTime(currentTime);
				if (dataVisualizer.Settings.DeformationScale.DrawDeformed)
					dataVisualizer.SetDeformationDataIndex(dataVisualizer.Settings.DeformationScale.DeformationDataIndex.WithTime(currentTime));
			}
			else
			{
				int dataIndex;
				
				if (dataVisualizer.Settings.ShowScalars && scalarDataIndicesForTimes != null && scalarDataIndicesForTimes.TryGetValue(currentTime, out dataIndex))
				{
					if (dataVisualizer.Settings.ScalarDataIndex.Index < 0) // if ShowVectorMagnitudes => preserve this setting
						dataIndex = ~dataIndex;
					dataVisualizer.Settings.ScalarDataIndex = dataVisualizer.Settings.ScalarDataIndex.WithIndex(dataIndex);
				}
				if (dataVisualizer.Settings.ShowVectors && vectorDataIndicesForTimes != null && vectorDataIndicesForTimes.TryGetValue(currentTime, out dataIndex))
				{
					dataVisualizer.Settings.VectorDataIndex = dataVisualizer.Settings.VectorDataIndex.WithIndex(dataIndex);
				}
				if (dataVisualizer.Settings.DeformationScale.DrawDeformed && deformationDataIndicesForTimes != null && deformationDataIndicesForTimes.TryGetValue(currentTime, out dataIndex))
				{
					dataVisualizer.SetDeformationDataIndex(dataVisualizer.Settings.DeformationScale.DeformationDataIndex.WithIndex(dataIndex));
				}
			}

			// update time in window legend
			if (dataVisualizer.Settings.ShowScalars)
			{
				updateTimeInScalarDataDescription(currentTime);
			}
		}

		private void updateTimeInScalarDataDescription(double currentTime)
		{
			string description = dataVisualizer.Settings.ScalarDataDescription;

			string[] lines = description.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

			StringBuilder text = new StringBuilder();
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].StartsWith("Time", StringComparison.InvariantCultureIgnoreCase))
					text.AppendFormat("Time: {0}", currentTime); // replace the number in line showing time
				else
					text.AppendLine(lines[i]);
			}

			dataVisualizer.Settings.ScalarDataDescription = text.ToString();
		}

		private void takeScreenshot(string filename)
		{
			Debug.Assert(screenshotTaker != null);
			int width = 0; // zero means "keep current window size"
			int height = 0;
			using (Bitmap screenshot = screenshotTaker(width, height))
			{
				screenshot.Save(filename, System.Drawing.Imaging.ImageFormat.Png); // image format must correspond to file extension (.png)
			}
		}

		private void stopAnimationTimer()
		{
			if (animationTimer != null)
			{
				animationTimer.Stop();
				animationTimer.Dispose();
				animationTimer = null;
				buttonCreateAnimation.Text = "Start";
				progressBar.Value = 0;
				progressBar.Visible = false;
			}
		}

		private void comboBoxStep_SelectedIndexChanged(object sender, EventArgs e)
		{
			selectAppropriateTimeSteps();
		}

		#endregion

		#region Public methods

		#endregion

	}
}

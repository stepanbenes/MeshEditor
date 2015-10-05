using MeshEditor.CoreInterface;
using MeshEditor.DataVisualizer.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class DataVisualizerLoaderForm : Form
	{

		#region Static members

		private static bool savedLoadInternalEntities = true;
		private static ApproximationMethod savedApproximationMethod = ApproximationMethod.TrilinearRegression;
		private static DataVisualizerTypes savedDataVisualizerTypes = DataVisualizerTypes.Exact; // Exact is default
		private static bool savedCompressTime;
		private static double[] savedKeyTimes;

		#endregion

		#region Fields, constructor

		IDataVisualizer dataVisualizer;
		IApproximationParameters approximationParameters;
		DataVisualizerTypes dataVisualizerType;
		LongOpNotifier longOpNotifier;

		public DataVisualizerLoaderForm(IDataVisualizer dataVisualizer, LongOpNotifier longOpNotifier)
		{
			Debug.Assert(longOpNotifier != null);

			this.dataVisualizer = dataVisualizer;
			this.longOpNotifier = longOpNotifier;

			InitializeComponent();

			approximationParameters = new ApproximationParameters(savedApproximationMethod, savedLoadInternalEntities, savedCompressTime, savedKeyTimes);
			
			comboBoxApproximationMethod.Items.AddRange(Enum.GetValues(typeof(ApproximationMethod)).Cast<object>().ToArray());
			comboBoxApproximationMethod.SelectedItem = approximationParameters.Method;
			checkBoxLoadInternalEntities.Checked = approximationParameters.LoadInternalEntities;
			if (savedKeyTimes != null)
				textBoxKeyTimeSteps.Text = string.Join(" ", savedKeyTimes.Select(kt => kt.ToString()).ToArray());

			setupFilesList();
			updateUIState();
			setupDataVisualizerType();
		}

		#endregion

		#region Properties, Events

		public event EventHandler NeedInitialize, NeedRefresh;

		public IDataVisualizer DataVisualizer
		{
			get { return dataVisualizer; }
		}

		#endregion

		#region Private methods

		private double[] parseKeyTimes()
		{
			string[] parts = textBoxKeyTimeSteps.Text.Split(';', ' ', '\t');
			List<double> keyTimes = new List<double>();
			foreach (string part in parts)
			{
				double keyTime;
				if (double.TryParse(part, out keyTime))
					keyTimes.Add(keyTime);
			}
			return keyTimes.ToArray();
		}

		private void setupFilesList(bool greyLoadedFiles = true)
		{
			listViewFiles.Items.Clear();
			if (dataVisualizer != null)
			{
				Color foreColor = greyLoadedFiles ? Color.Gray : Color.Black;
				listViewFiles.Items.AddRange(dataVisualizer.LoadedFiles.Select(file => new ListViewItem(file) { ForeColor = foreColor }).ToArray());
			}
			updateUIState();
		}

		private void setupApproximationQualityText()
		{
			BackgroundWorker worker = new BackgroundWorker();
			longOpNotifier.Begin();
			labelApproximationQualityText.Text = "Computing...";
			linkLabelApproximationQuality.Enabled = false;
			worker.DoWork += (s, ea) =>
			{
				ApproximationQuality apxQuality = null;
				if (dataVisualizer != null)
				{
					apxQuality = dataVisualizer.GetApproximationQuality(longOpNotifier);
				}
				ea.Result = apxQuality;
			};
			worker.RunWorkerCompleted += (s, ea) =>
			{
				labelApproximationQualityText.Text = (ea.Result != null) ? ea.Result.ToString() : "[None]";
				longOpNotifier.End();
				linkLabelApproximationQuality.Enabled = true;
			};
			worker.RunWorkerAsync();
		}

		private void setupDataVisualizerType()
		{
			comboBoxDataVisualizerType.Items.AddRange(Enum.GetValues(typeof(DataVisualizerTypes)).Cast<object>().ToArray());
			comboBoxDataVisualizerType.SelectedItem = getCurrentDataVisualizerType();
		}

		private DataVisualizerTypes getCurrentDataVisualizerType()
		{
			if (dataVisualizer == null)
				return savedDataVisualizerTypes;
			else if (dataVisualizer is ExactDataVisualizer)
				return DataVisualizerTypes.Exact;
			else if (dataVisualizer is TimetreeDataVisualizer)
				return DataVisualizerTypes.Timetree;
			else if (dataVisualizer is OctreeDataVisualizer)
				return DataVisualizerTypes.Octree;
			throw new NotSupportedException();
		}

		private void buttonClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void loadData()
		{
			longOpNotifier.Begin();

			if (dataVisualizer == null)
			{
				dataVisualizer = dataVisualizerFactory(dataVisualizerType);

				EventHandler handler = NeedInitialize;
				if (handler != null) // inform UI - data visualizer will be initialized
					handler(this, EventArgs.Empty);
			}

			string[] filenames = listViewFiles.Items.Cast<ListViewItem>().Select(item => item.Text).ToArray();

			approximationParameters.FixedTimes = parseKeyTimes();

			linkLabelApproximationQuality.Visible = labelApproximationQualityText.Visible = false;

			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += (s, e) =>
				{
					try
					{
						dataVisualizer.LoadData(approximationParameters, filenames, longOpNotifier);
					}
					catch (Exception ex)
					{
						e.Result = ex;
					}
				};
			worker.RunWorkerCompleted += (s, e) =>
				{
					longOpNotifier.End();

					Exception ex = e.Result as Exception;
					if (ex != null) // deal with exceptions
					{
						reportError(ex);
					}

					dataVisualizer.FinishUp(); // finish off creation of data visualizer object (sets default values of DataVisualizerController settings)

					savedApproximationMethod = approximationParameters.Method;
					savedLoadInternalEntities = approximationParameters.LoadInternalEntities;
					savedCompressTime = approximationParameters.CompressTime;
					savedKeyTimes = approximationParameters.FixedTimes;

					setupFilesList();
					
					labelApproximationQualityText.Text = string.Empty;
					linkLabelApproximationQuality.Visible = labelApproximationQualityText.Visible = true;

					EventHandler handler = NeedRefresh;
					if (handler != null) // inform UI - refresh scene
						handler(this, EventArgs.Empty);
				};
			worker.RunWorkerAsync();

		}

		private static void reportError(Exception ex)
		{
			DataLoadingException dataEx = ex as DataLoadingException;
			if (dataEx != null)
			{
				StringBuilder message = new StringBuilder();
				message.AppendLine(dataEx.Message);
				if (!string.IsNullOrEmpty(dataEx.Filename))
				{
					message.AppendLine();
					message.Append(string.Format("File name: \"{0}\"", System.IO.Path.GetFileName(dataEx.Filename)));
				}
				if (dataEx.LineNumber > 0)
				{
					message.AppendLine();
					message.Append(string.Format("Line number: {0}", dataEx.LineNumber));
				}
				MessageBox.Show(message.ToString(), "Error while loading data", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else if (ex != null)
			{
				MessageBox.Show(ex.Message, "Error while loading data", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static IDataVisualizer dataVisualizerFactory(DataVisualizerTypes type)
		{
			switch (type)
			{
				case DataVisualizerTypes.Exact:
					return new ExactDataVisualizer();
				case DataVisualizerTypes.Octree:
					return new OctreeDataVisualizer();
				case DataVisualizerTypes.Timetree:
					return new TimetreeDataVisualizer();
				default:
					throw new NotSupportedException();
			}
		}

		private void listViewFiles_SelectedIndexChanged(object sender, EventArgs e)
		{
			buttonRemove.Enabled = listViewFiles.SelectedItems != null && listViewFiles.SelectedItems.Count > 0 && (dataVisualizer == null || listViewFiles.SelectedItems.Cast<ListViewItem>().All(item => !dataVisualizer.LoadedFiles.Contains(item.Text)));
		}

		private void buttonAddFiles_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "GiD result files (*.res)|*.res|VTK XML unstructured grid result files (*.vtu)|*.vtu|All files (*.*)|*.*";
			dialog.FilterIndex = 0;
			dialog.Multiselect = true;
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				foreach (string filename in dialog.FileNames)
				{
					if (!listViewFiles.Items.Cast<ListViewItem>().Select(item => item.Text).Contains(filename))
					{
						listViewFiles.Items.Add(new ListViewItem(filename));
					}
				}
				updateUIState();
			}
		}

		private void buttonRemove_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in listViewFiles.SelectedItems)
			{
				listViewFiles.Items.Remove(item);
			}
			updateUIState();
		}

		private void buttonUnload_Click(object sender, EventArgs e)
		{
			setupFilesList(greyLoadedFiles: false);

			dataVisualizer = null;// new OctreeDataVisualizer();

			updateUIState();

			var handler = NeedInitialize;
			if (handler != null) // inform UI
				handler(this, EventArgs.Empty);

			handler = NeedRefresh;
			if (handler != null) // inform UI
				handler(this, EventArgs.Empty);
		}

		private void buttonReload_Click(object sender, EventArgs e)
		{
			dataVisualizer = null;
			loadData();
		}

		private void buttonLoad_Click(object sender, EventArgs e)
		{
			loadData();
		}

		private void buttonClear_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in listViewFiles.Items.Cast<ListViewItem>().ToArray())
			{
				if (dataVisualizer == null || !dataVisualizer.LoadedFiles.Contains(item.Text))
					listViewFiles.Items.Remove(item);
			}
			updateUIState();
		}

		private void comboBoxApproximationMethod_SelectedIndexChanged(object sender, EventArgs e)
		{
			approximationParameters.Method = (ApproximationMethod)comboBoxApproximationMethod.SelectedItem;
			updateUIState();
		}

		private void comboBoxDataVisualizerType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBoxDataVisualizerType.SelectedItem == null)
				dataVisualizerType = DataVisualizerTypes.Exact;
			else
				dataVisualizerType = (DataVisualizerTypes)comboBoxDataVisualizerType.SelectedItem;
			savedDataVisualizerTypes = dataVisualizerType;
			updateUIState();
		}

		private void updateUIState()
		{
			buttonLoad.Enabled = (dataVisualizer == null && listViewFiles.Items.Count > 0) || (dataVisualizer != null && listViewFiles.Items.Cast<ListViewItem>().Any(item => !dataVisualizer.LoadedFiles.Contains(item.Text)));
			buttonReload.Enabled = (dataVisualizer != null);
			buttonUnload.Enabled = (dataVisualizer != null);

			buttonClear.Enabled = listViewFiles.Items.Count > 0 && (dataVisualizer == null || listViewFiles.Items.Cast<ListViewItem>().Any(item => !dataVisualizer.LoadedFiles.Contains(item.Text)));

			labelApproximationMethodHeader.Visible = comboBoxApproximationMethod.Visible = (dataVisualizerType == DataVisualizerTypes.Octree || dataVisualizerType == DataVisualizerTypes.Timetree);
			linkLabelApproximationQuality.Visible = labelApproximationQualityText.Visible = (dataVisualizer != null && getCurrentDataVisualizerType() != DataVisualizerTypes.Exact);
			checkBoxCompressTime.Visible = labelKeyTimeSteps.Visible = textBoxKeyTimeSteps.Visible = (dataVisualizerType == DataVisualizerTypes.Timetree);

			labelKeyTimeSteps.Enabled = textBoxKeyTimeSteps.Enabled = checkBoxCompressTime.Checked = approximationParameters.CompressTime;
		}

		private void checkBoxLoadInternalEntities_CheckedChanged(object sender, EventArgs e)
		{
			approximationParameters.LoadInternalEntities = checkBoxLoadInternalEntities.Checked;
			updateUIState();
		}

		private void checkBoxCompressTime_CheckedChanged(object sender, EventArgs e)
		{
			approximationParameters.CompressTime = checkBoxCompressTime.Checked;
			updateUIState();
		}

		private void linkLabelApproximationQuality_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			setupApproximationQualityText();
		}

		#endregion

	}
}

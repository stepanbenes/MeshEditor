using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MeshEditor.DataVisualizer.Data;
using System.Diagnostics;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class DataIndexSetter : UserControl
	{

		public enum DataFilterOptions
		{
			None,
			Scalars,
			Vectors
		}

		#region Fields, ctor

		IDataVisualizerController dataVisualizer;
		DataFilterOptions dataFilter;
		DataType[] allDataTypes;
		Dictionary<DataType, List<KeyValuePair<int, double>>> dataTimes;
		IntervalD continuousTimeRange;
		
		DataIndex selectedDataIndex;
		
		bool initialization;
		bool updatingCurrentTimeValue;

		bool showVectorMagnitudes;

		public DataIndexSetter()
		{
			InitializeComponent();
		}

		#endregion

		#region Properties, Events

		public IDataVisualizerController DataVisualizer
		{
			get { return dataVisualizer; }
			set
			{
				if (dataVisualizer != value)
				{
					dataVisualizer = value;
				}
			}
		}

		public int DataTypesLength
		{
			get { return allDataTypes != null ? allDataTypes.Length : 0; }
		}

		public DataFilterOptions DataFilter
		{
			get { return dataFilter; }
			set { dataFilter = value; }
		}

		public DataIndex SelectedDataIndex
		{
			get { return selectedDataIndex; }
			set
			{
				if (selectedDataIndex != value)
				{
					selectedDataIndex = value;
					updateDataIndexDescription();
					var handler = SelectedDataIndexChanged;
					if (handler != null)
						handler(this, EventArgs.Empty);
				}
			}
		}

		public string SelectedDataIndexDescription
		{
			get;
			private set;
		}

		public event EventHandler SelectedDataIndexChanged;

		public bool ShowVectorMagnitudes
		{
			get { return showVectorMagnitudes; }
			set
			{
				if (showVectorMagnitudes != value)
				{
					showVectorMagnitudes = value;
					if (showVectorMagnitudes)
					{
						DataFilter = DataFilterOptions.Vectors;
						if (comboBoxDataComponent.Items.Count > 0)
						{
							comboBoxDataComponent.SelectedIndex = 0;
						}
					}
					else
					{
						DataFilter = DataFilterOptions.Scalars;
					}
					SetupData();
					changeSelectedDataIndex();
				}
			}
		}

		public bool SelectedDataTypeIsVector
		{
			get
			{
				DataType selectedDataType = comboBoxDataType.SelectedItem as DataType;
				return selectedDataType != null && selectedDataType.CompoundType == DataType.CompoundTypes.Vector;
			}
		}

		#endregion

		#region Public methods

		public void SetupData()
		{
			initialization = true;

			comboBoxDataType.Items.Clear();
			if (dataVisualizer != null)
			{
				comboBoxDataComponent.Visible = (DataFilter == DataFilterOptions.Scalars);

				DataType selectedDataType;
				int selectedComponentIndex;
				double selectedTime;
				processDataIndexMap(out selectedDataType, out selectedComponentIndex, out selectedTime);
				Debug.Assert(allDataTypes != null && dataTimes != null);

				if (allDataTypes.Length > 0)
				{
					comboBoxDataType.Items.AddRange(allDataTypes);
					comboBoxDataType.DisplayMember = "Name";

					comboBoxDataType.SelectedItem = selectedDataType;

					setupComponents();

					if (comboBoxDataComponent.Items.Count > 0)
					{
						comboBoxDataComponent.SelectedIndex = selectedComponentIndex;
					}
				}
				else
				{
					SelectedDataIndex = SelectedDataIndex.WithIndex(getCurrentDataIndex());
				}

				setupTimes();
				comboBoxDataTime.SelectedItem = selectedTime;

				updateDataIndexDescription();
			}
			initialization = false;
		}

		//public void TryToSetDisplacementDataIndex()
		//{
		//	Debug.Assert(dataVisualizer != null);
		//	if (allDataTypes.Length > 0)
		//	{
		//		DataInfo dataInfo = dataVisualizer.DataIndexMap.Keys.Where(key => dataTypeIsDisplacement(key.DataType)).FirstOrDefault();
		//		if (dataInfo != null)
		//		{
		//			comboBoxDataType.SelectedItem = dataInfo.DataType;
		//			if (!dataVisualizer.IsContinuousInTime(out continuousTimeRange))
		//			{
		//				comboBoxDataTime.SelectedItem = dataInfo.Time;
		//			}
		//			SelectedDataIndex = SelectedDataIndex.ChangeIndex(dataVisualizer.DataIndexMap[dataInfo]);
		//		}
		//	}
		//}

		//private bool dataTypeIsDisplacement(DataType dataType)
		//{
		//	string name = dataType.Name;
		//	return string.Equals(name, "Displacement", StringComparison.InvariantCultureIgnoreCase) || string.Equals(name, "Displacements", StringComparison.InvariantCultureIgnoreCase);
		//}

		#endregion

		#region Private methods

		private void setupComponents()
		{
			comboBoxDataComponent.Items.Clear();
			DataType selectedDataType = comboBoxDataType.SelectedItem as DataType;
			if (selectedDataType != null)
			{
				comboBoxDataComponent.Items.AddRange(selectedDataType.Components);
				comboBoxDataComponent.DisplayMember = "Name";
				comboBoxDataComponent.SelectedIndex = 0;
			}
		}

		private void setupTimes()
		{
			comboBoxDataTime.Items.Clear();
			DataType selectedDataType = comboBoxDataType.SelectedItem as DataType;
			if (selectedDataType != null)
			{
				comboBoxDataTime.Items.AddRange(dataTimes[selectedDataType].Select(pair => (object)pair.Value).ToArray());
				comboBoxDataTime.SelectedIndex = 0;
			}

			// ------------------------------------------------------------------

			if (dataVisualizer.IsContinuousInTime(out continuousTimeRange))
			{
				comboBoxDataTime.Visible = false;
				textBoxCurrentTime.Visible = true;
				trackBarCurrentTime.Visible = true;

				// SelectedDataIndex.Time is out of range of continuousTimeRange (when dataIndex changes, current data value can have different time range in which is defined)?
				SelectedDataIndex = SelectedDataIndex.WithTime(continuousTimeRange.CutValue(SelectedDataIndex.Time));

				textBoxCurrentTime.Text = SelectedDataIndex.Time.ToString();
				if (initialization)
					setupTrackBarCurrentTimeValue(SelectedDataIndex.Time);
			}
			else
			{
				comboBoxDataTime.Visible = true;
				textBoxCurrentTime.Visible = false;
				trackBarCurrentTime.Visible = false;
			}
		}

		private void processDataIndexMap(out DataType selectedDataType, out int selectedComponentIndex, out double selectedTime)
		{
			Debug.Assert(dataVisualizer != null);

			List<DataType> dataTypes = new List<DataType>();
			HashSet<DataType> processedDataTypes = new HashSet<DataType>();
			this.dataTimes = new Dictionary<DataType, List<KeyValuePair<int, double>>>();

			selectedDataType = null;
			selectedComponentIndex = 0;
			selectedTime = 1;

			int dataIndex = SelectedDataIndex.Index;
			if (dataIndex < 0)
				dataIndex = ~dataIndex;

			foreach (var dataPair in dataVisualizer.DataIndexMap)
			{
				//Debug.Assert(dataPair.Key.Location == DataLocation.Nodes); // nothing else is allowed for now

				DataType dataType = dataPair.Key.DataType;

				if ((DataFilter == DataFilterOptions.Vectors) && (dataType.CompoundType != DataType.CompoundTypes.Vector)) // filter vector data types
					continue;

				if (!processedDataTypes.Contains(dataType))
				{
					// --------------------------------------
					// all data types
					dataTypes.Add(dataType);
					// data times
					Debug.Assert(!dataTimes.ContainsKey(dataType));
					dataTimes[dataType] = new List<KeyValuePair<int, double>>();
					dataTimes[dataType].Add(new KeyValuePair<int, double>(dataPair.Value, dataPair.Key.Time));
					// --------------------------------------
					processedDataTypes.Add(dataType);
				}
				else
				{
					Debug.Assert(dataTimes.ContainsKey(dataType));
					dataTimes[dataType].Add(new KeyValuePair<int, double>(dataPair.Value, dataPair.Key.Time));
				}

				// selected data
				if (dataIndex >= dataPair.Value && dataIndex < dataPair.Value + dataType.ComponentCount)
				{
					selectedDataType = dataType;
					selectedComponentIndex = dataIndex - dataPair.Value;
					selectedTime = dataPair.Key.Time;
				}
			}
			this.allDataTypes = dataTypes.ToArray();
		}

		private void setupTrackBarCurrentTimeValue(double timeValue)
		{
			if (continuousTimeRange.Length > 0.0)
			{
				trackBarCurrentTime.Value = (int)((timeValue - continuousTimeRange.Min) * (double)trackBarCurrentTime.Maximum / continuousTimeRange.Length);
			}
		}

		private void changeSelectedDataIndex()
		{
			SelectedDataIndex = SelectedDataIndex.WithIndex(getCurrentDataIndex());

			dataVisualizer.IsContinuousInTime(out continuousTimeRange);
			onTextBoxCurrentTimeTextChanged();
		}

		private void onTextBoxCurrentTimeTextChanged()
		{
			if (initialization || updatingCurrentTimeValue)
				return;

			double time;
			if (double.TryParse(textBoxCurrentTime.Text, out time) && time != SelectedDataIndex.Time)
			{
				double correctedTime = continuousTimeRange.CutValue(time); // cut inserted value by time range
				updatingCurrentTimeValue = true;
				{
					textBoxCurrentTime.Text = correctedTime.ToString();
					setupTrackBarCurrentTimeValue(correctedTime);
				}
				updatingCurrentTimeValue = false;
				SelectedDataIndex = SelectedDataIndex.WithTime(correctedTime);
			}
		}

		private int getCurrentDataIndex()
		{
			DataType selectedDataType = comboBoxDataType.SelectedItem as DataType;
			//Debug.Assert(selectedDataType != null);
			if (selectedDataType != null && comboBoxDataTime.SelectedIndex >= 0 && comboBoxDataComponent.SelectedIndex >= 0)
			{
				// WARNING: this line can throw exception, if data was not loaded correctly and some times are missing
				//int index = dataTimes[selectedDataType][comboBoxDataTime.SelectedIndex].Key + comboBoxDataComponent.SelectedIndex;

				List<KeyValuePair<int, double>> timeList;
				if (dataTimes.TryGetValue(selectedDataType, out timeList))
				{
					if (comboBoxDataTime.SelectedIndex >= 0 && comboBoxDataTime.SelectedIndex < timeList.Count)
					{
						int index = timeList[comboBoxDataTime.SelectedIndex].Key + comboBoxDataComponent.SelectedIndex;
						if (ShowVectorMagnitudes)
							index = ~index;
						return index;
					}
				}
			}
			
			// data not found
			return int.MaxValue; // represents unknown value (-1 is not suitable - negative sign represents ShowVectorMagnitudes flag)
		}

		private void updateDataIndexDescription()
		{
			StringBuilder text = new StringBuilder();

			if (dataVisualizer != null)
			{
				if (comboBoxDataType.SelectedItem != null)
					text.AppendLine(comboBoxDataType.SelectedItem.ToString());
				if (DataFilter == DataFilterOptions.Scalars && comboBoxDataComponent.SelectedItem != null)
					text.AppendLine(comboBoxDataComponent.SelectedItem.ToString());
				if (ShowVectorMagnitudes)
				{
					Debug.Assert(DataFilter == DataFilterOptions.Vectors);
					text.AppendLine("[Vector magnitudes]");
				}

				string currentTime = null;
				if (dataVisualizer.IsContinuousInTime(out continuousTimeRange))
					currentTime = textBoxCurrentTime.Text;
				else if (comboBoxDataTime.SelectedItem != null)
					currentTime = comboBoxDataTime.SelectedItem.ToString();

				if (currentTime != null)
					text.AppendFormat("Time: {0}", currentTime);
			}

			SelectedDataIndexDescription = text.ToString();
		}

		#endregion

		#region Event handlers

		private void comboBoxDataType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;

			Debug.Assert(dataVisualizer != null && comboBoxDataType.SelectedIndex >= 0);
			if (dataVisualizer != null)
			{
				dataVisualizer.Settings.BeginUpdate();
				try
				{
					changeSelectedDataIndex();
					setupComponents();
					setupTimes();
				}
				finally
				{
					dataVisualizer.Settings.EndUpdate();
				}
			}
		}

		private void comboBoxDataComponent_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;

			Debug.Assert(dataVisualizer != null && comboBoxDataComponent.SelectedIndex >= 0);
			if (dataVisualizer != null)
			{
				changeSelectedDataIndex();
			}
		}

		private void comboBoxTime_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (initialization)
				return;

			Debug.Assert(dataVisualizer != null && comboBoxDataTime.SelectedIndex >= 0);
			if (dataVisualizer != null)
			{
				changeSelectedDataIndex();
			}
		}

		private void trackBarCurrentTime_Scroll(object sender, EventArgs e)
		{
			if (initialization || updatingCurrentTimeValue)
				return;

			double time = (double)trackBarCurrentTime.Value * continuousTimeRange.Length / (double)trackBarCurrentTime.Maximum + continuousTimeRange.Min;
			double correctedTime = continuousTimeRange.CutValue(time); // cut inserted value by time range
			updatingCurrentTimeValue = true;
			{
				textBoxCurrentTime.Text = correctedTime.ToString();
			}
			updatingCurrentTimeValue = false;
			SelectedDataIndex = SelectedDataIndex.WithTime(correctedTime);
		}

		private void textBoxCurrentTime_Leave(object sender, EventArgs e)
		{
			onTextBoxCurrentTimeTextChanged();
		}

		private void textBoxCurrentTime_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				onTextBoxCurrentTimeTextChanged();
				e.Handled = true;
			}
		}

		#endregion

	}
}

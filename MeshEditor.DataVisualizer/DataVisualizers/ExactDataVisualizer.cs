using MeshEditor.CoreInterface;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer
{
	public class ExactDataVisualizer : DataVisualizerBase
	{

		#region Fields

		Dictionary<int, Dictionary<int, double>> nodeValues;
		//Dictionary<int, Dictionary<Element, Dictionary<Node, double>>> elementNodeValues;
		Dictionary<int, IntervalD> dataValueRangeMap;

		private int dataRecordsCount;

		//public bool ShowElementNodesValues { get; set; }

		#endregion

		#region Overrides

		public override void Initialize(Mesh mesh)
		{
			base.Initialize(mesh);
			this.dataRecordsCount = 0;
			//ShowElementNodesValues = false; // TODO: deal with element nodes feature
		}

		public override void LoadData(IApproximationParameters approximationParameters, string[] filenames, LongOpNotifier longOpNotifier)
		{
			Debug.Assert(filenames != null && filenames.Length > 0);
			Debug.Assert(longOpNotifier != null);
			if (filenames == null || filenames.Length == 0)
				return;

			// --- INIT MAPS ---------------------------------------------
			if (nodeValues == null)
				nodeValues = new Dictionary<int, Dictionary<int, double>>();
			//if (elementNodeValues == null)
			//	elementNodeValues = new Dictionary<int, Dictionary<Element, Dictionary<Node, double>>>();
			if (dataValueRangeMap == null)
				dataValueRangeMap = new Dictionary<int, IntervalD>();
			createNodeIndexMap(approximationParameters.LoadInternalEntities);
			//Dictionary<int, Element> elementMap = null;
			// -----------------------------------------------------------

			foreach (string filename in filenames)
			{
				if (!loadedFiles.Add(filename))
					continue; // already loaded

				string taskName = "Loading " + Path.GetFileName(filename);

				using (IDataFileParser dataParser = DataParserFactory.Create(filename))
				{
					DataInfo dataInfo;
					while ((dataInfo = dataParser.ReadNextResult()) != null)
					{
						// TODO: check if results are not loaded already

						if (longOpNotifier.IsCancelled)
						{
							return;
						}

						longOpNotifier.ReportProgress((int)dataParser.PercentageRead, taskName, operationName: string.Format("Time: {0}  Data: {1}", dataInfo.Time, dataInfo.DataType.Name));

						if (dataInfo.Location == DataLocation.GaussPoints)
						{
							throw new NotSupportedException("Gauss-point location not yet supported.");
							//if (elementMap == null)
							//	elementMap = createElementMap();
							//fillElementNodeValues(dataInfo, dataParser.ReadResultBlock(), elementMap, dataInfo.LocationInfo, approximationParameters.GPExptrapolationStrategy);
						}
						else // DataLocation.Nodes
						{
							Debug.Assert(dataInfo.Location == DataLocation.Nodes);
							fillNodeValues(dataInfo, dataParser.ReadResultBlock());
						}
						
						DataIndexMap[dataInfo] = dataIndexCounter;
						dataIndexCounter += dataInfo.DataType.ComponentCount;
					}
				}
			}
		}

		public override void FinishUp()
		{
			updateColorScale();
		}

		public override int GetDataColor(Node node, Element element)
		{
			//if (ShowElementNodesValues)
			//{
			//	Debug.Assert(element != null);
			//	Dictionary<Element, Dictionary<Node, double>> dict;
			//	if (elementNodeValues.TryGetValue(Settings.ScalarDataIndex.Index, out dict))
			//	{
			//		double value;
			//		if (dict.ContainsKey(element) && dict[element].TryGetValue(node, out value))
			//			return GetColorForDataValue(value);
			//	}
			//}
			//else
			//{
				Debug.Assert(nodeValues.ContainsKey(Settings.ScalarDataIndex.Index));
				return GetColorForDataValue(GetDataValue(node, Settings.ScalarDataIndex));
			//}
			//return ColorScale.UndefinedValueColor;
		}

		public override double GetDataValue(Node node, DataIndex dataIndex)
		{
			Debug.Assert(nodeValues.ContainsKey(dataIndex.Index));
			double value;
			if (nodeValues[dataIndex.Index].TryGetValue(node.ID, out value))
				return value;
			return double.NaN;
		}

		public override double GetDataValue(Node node, DataIndex dataIndex, out float maxError)
		{
			Debug.Assert(nodeValues.ContainsKey(dataIndex.Index));
			maxError = 0.0f; // showing exact data
			double value;
			if (nodeValues[dataIndex.Index].TryGetValue(node.ID, out value))
				return value;
			return double.NaN;
		}

		public override ApproximationQuality GetApproximationQuality(LongOpNotifier longOpNotifier)
		{
			ApproximationQuality apxQuality = new ApproximationQuality();
			apxQuality.MemoryConsumption = dataRecordsCount * sizeof(double);
			apxQuality.CompressionRatio = 1.0f;
			// Approximation error is set to zero at default
			return apxQuality;
		}
		
		protected override void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnSettingsPropertyChanged(sender, e);

			string[] propertyNames = e.PropertyName.Split(';');
			foreach (string propertyName in propertyNames)
			{
				switch (propertyName)
				{
					//case "DisplayMethod":
					//	break;
					case "ScalarDataIndex":
						if (Settings.ScalarDataIndex.Index < 0 && !nodeValues.ContainsKey(Settings.ScalarDataIndex.Index))
						{ // if ShowVectorMagnitudes, create new dataValue map and fill it with vector lengths
							createValueMapForVectorMagnitudes();
						}
						updateColorScale();
						break;
				}
			}
		}

		protected override IntervalD GetDataValueRange(int dataIndex)
		{
			IntervalD range;
			if (dataValueRangeMap.TryGetValue(dataIndex, out range))
				return range;
			return IntervalD.Zero;
		}

		public override int[] GetEntitiesWithMaximumDataValue()
		{
			List<int> maxEntities = new List<int>();
			Dictionary<int, double> dataValues;
			if (nodeValues.TryGetValue(Settings.ScalarDataIndex.Index, out dataValues))
			{
				double maxValue = GetMaximumDataValue();
				foreach (var pair in dataValues)
				{
					if (pair.Value == maxValue)
						maxEntities.Add(pair.Key);
				}
			}
			return maxEntities.ToArray();
		}

		public override int[] GetEntitiesWithMinimumDataValue()
		{
			List<int> minEntities = new List<int>();
			Dictionary<int, double> dataValues;
			if (nodeValues.TryGetValue(Settings.ScalarDataIndex.Index, out dataValues))
			{
				double minValue = GetMinimumDataValue();
				foreach (var pair in dataValues)
				{
					if (pair.Value == minValue)
						minEntities.Add(pair.Key);
				}
			}
			return minEntities.ToArray();
		}

		public override double GetMaximumDataValue()
		{
			IntervalD interval;
			if (dataValueRangeMap.TryGetValue(Settings.ScalarDataIndex.Index, out interval))
				return interval.Max;
			return double.NaN;
		}

		public override double GetMinimumDataValue()
		{
			IntervalD interval;
			if (dataValueRangeMap.TryGetValue(Settings.ScalarDataIndex.Index, out interval))
				return interval.Min;
			return double.NaN;
		}

		#endregion

		#region Private methods

		private void updateColorScale()
		{
			IntervalD range = GetDataValueRange(Settings.ScalarDataIndex.Index);
			Settings.ColorScale.SetMinMaxValue(range.Min, range.Max);
		}

		private void fillNodeValues(DataInfo dataInfo, IEnumerable<DataValue> dataValues)
		{
			foreach (NodeValue nodeValue in dataValues)
			{
				if (!nodeIndexMap.ContainsKey(nodeValue.EntityNumber))
					continue;
				for (int componentIndex = 0; componentIndex < dataInfo.DataType.ComponentCount; componentIndex++)
				{
					Dictionary<int, double> dict;
					if (!nodeValues.TryGetValue(dataIndexCounter + componentIndex, out dict))
						dict = nodeValues[dataIndexCounter + componentIndex] = new Dictionary<int, double>();
					if (!dict.ContainsKey(nodeValue.EntityNumber))
						dataRecordsCount++;

					double value = (nodeValue.ValueComponents.Length > componentIndex) ? nodeValue.ValueComponents[componentIndex] : 0.0; // check if component is specified, if not insert default value (zero)

					dict[nodeValue.EntityNumber] = value;

					// min max value range
					IntervalD range;
					if (!dataValueRangeMap.TryGetValue(dataIndexCounter + componentIndex, out range))
						range = IntervalD.InvertedMaxMin;
					range.MergeWith(value);
					dataValueRangeMap[dataIndexCounter + componentIndex] = range;
				}
			}
		}

		//private void fillElementNodeValues(DataInfo dataInfo, IEnumerable<DataValue> dataValues, Dictionary<int, Element> elementMap, GaussPointsInfo gaussPointsInfo, GaussPointsExtrapolationStrategy strategy)
		//{
		//	Debug.Assert(dataInfo != null);
		//	Debug.Assert(dataValues != null);
		//	Debug.Assert(elementMap != null);
		//	Debug.Assert(gaussPointsInfo != null);

		//	foreach (ElementValue elementValue in dataValues)
		//	{
		//		if (!elementMap.ContainsKey(elementValue.EntityNumber))
		//			continue;
		//		for (int componentIndex = 0; componentIndex < dataInfo.DataType.ComponentCount; componentIndex++)
		//		{
		//			Dictionary<Element, Dictionary<Node, double>> dict;
		//			if (!elementNodeValues.TryGetValue(dataIndexCounter + componentIndex, out dict))
		//				dict = elementNodeValues[dataIndexCounter + componentIndex] = new Dictionary<Element, Dictionary<Node, double>>();
		//			//if (!dict.ContainsKey(elementMap[elementValue.EntityNumber]))
		//			//	dataRecordsCount++;

		//			double[] gaussPointValues = new double[elementValue.ValueComponents.GetLength(0)];
		//			if (elementValue.ValueComponents.GetLength(1) > componentIndex) // check if component is specified, if not default value will be zero
		//			{
		//				for (int i = 0; i < gaussPointValues.Length; i++)
		//					gaussPointValues[i] = elementValue.ValueComponents[i, componentIndex];
		//			}

		//			Element element = elementMap[elementValue.EntityNumber];
		//			Dictionary<Node, double> valueMap = gaussPointsInfo.ExtrapolateElementGaussPointValuesToNodes(element, gaussPointValues, strategy);
		//			dict[element] = valueMap;

		//			// min max value range
		//			IntervalD range;
		//			if (!dataValueRangeMap.TryGetValue(dataIndexCounter + componentIndex, out range))
		//				range = IntervalD.InvertedMaxMin;
		//			range.MergeWith(valueMap.Values);
		//			dataValueRangeMap[dataIndexCounter + componentIndex] = range;
		//		}
		//	}
		//}

		private void createValueMapForVectorMagnitudes()
		{
			Debug.Assert(Settings.ScalarDataIndex.Index < 0 && !nodeValues.ContainsKey(Settings.ScalarDataIndex.Index));

			var vectorLenghtMap = new Dictionary<int, double>();
			IntervalD valueRange = IntervalD.InvertedMaxMin;
			DataIndex baseDataIndex = Settings.ScalarDataIndex.WithIndex(~Settings.ScalarDataIndex.Index);
			foreach (int nodeID in nodeValues[baseDataIndex.Index].Keys)
			{
				double x = nodeValues[baseDataIndex.Index][nodeID];
				double y = nodeValues[baseDataIndex.Index + 1][nodeID];
				double z = nodeValues[baseDataIndex.Index + 2][nodeID];
				double vectorLength = Math.Sqrt(x * x + y * y + z * z);
				vectorLenghtMap[nodeID] = vectorLength;
				valueRange.MergeWith(vectorLength);
			}

			nodeValues[Settings.ScalarDataIndex.Index] = vectorLenghtMap; // setup new dataValue
			dataValueRangeMap[Settings.ScalarDataIndex.Index] = valueRange; // setup range of new dataValue
		}

		#endregion

	}
}

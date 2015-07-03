using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Octree;
using OpenTK;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.Mathematics;
using MeshEditor.CoreInterface;
using MeshEditor.DataVisualizer.IO;
using System.Diagnostics;
using System.IO;
using OpenTK.Graphics.OpenGL;
using System.ComponentModel;
using MeshEditor.Graphics;

namespace MeshEditor.DataVisualizer
{
	public class OctreeDataVisualizer : DataVisualizerBase
	{

		#region Fields

		protected TreeNode octreeRoot;
		protected Dictionary<int, double> currentDataValueCache;
		protected bool loadedExactDataValues;

		private object dataValueCacheCreationLocker = new object();

		#endregion

		#region Overrides

		public override void Initialize(Mesh mesh)
		{
			base.Initialize(mesh);

			// non-uniform dimensions => block/prism
			//octreeRoot = new InternalNode(mesh.LowerBound, mesh.UpperBound);

			// uniform dimensions => cube
			float maxDim = Math.Max(Math.Max(mesh.UpperBound.X - mesh.LowerBound.X, mesh.UpperBound.Y - mesh.LowerBound.Y), mesh.UpperBound.Z - mesh.LowerBound.Z);

			Vector3 lowerBound = mesh.LowerBound;
			Vector3 upperBound;
			upperBound.X = Math.Max(lowerBound.X + maxDim, mesh.UpperBound.X); // avoid rounding error
			upperBound.Y = Math.Max(lowerBound.Y + maxDim, mesh.UpperBound.Y);
			upperBound.Z = Math.Max(lowerBound.Z + maxDim, mesh.UpperBound.Z);

			octreeRoot = new InternalNode(lowerBound, upperBound);
		}

		public override void DrawItems(PropertyColorsMode propertyColorsMode)
		{
			base.DrawItems(propertyColorsMode);

			if (Settings.DrawGrid)
			{
				bool lightEnabled = GL.IsEnabled(EnableCap.Lighting);
				if (lightEnabled)
					GL.Disable(EnableCap.Lighting);

				GL.LineWidth(1f);
				GL.Disable(EnableCap.Lighting);
				GL.Color3(1f, 0f, 0f); // red

				octreeRoot.Draw(Settings.ScalarDataIndex);

				if (lightEnabled)
					GL.Enable(EnableCap.Lighting);
			}
		}

		public override void LoadData(IApproximationParameters approximationParameters, string[] filenames, LongOpNotifier longOpNotifier)
		{
			Debug.Assert(filenames != null && filenames.Length > 0);
			Debug.Assert(approximationParameters != null);
			Debug.Assert(longOpNotifier != null);
			if (filenames == null || filenames.Length == 0)
				return;

			// --- INIT MAPS ---------------------------------------------
			createNodeIndexMap(approximationParameters.LoadInternalEntities);
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
						if (dataInfo.Location != DataLocation.Nodes)
							throw new NotSupportedException("Gauss-point location not yet supported.");

						// TODO: check if results are not loaded already

						longOpNotifier.ReportProgress((int)dataParser.PercentageRead, taskName, operationName: string.Format("Time: {0}  Data: {1}", dataInfo.Time, dataInfo.DataType.Name));

						DataValue[] dataValues = dataParser.ReadResultBlock().Where(v => nodeIndexMap.ContainsKey(v.EntityNumber)).ToArray();

						if (dataValues.Length == 0) // result block is empty
							continue;

						int componentCount = dataInfo.DataType.ComponentCount;
						for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
						{
							// TODO: parallelize this loop

							if (longOpNotifier.IsCancelled)
							{
								return;
							}

							DataValueComponent[] components = new DataValueComponent[dataValues.Length];
							for (int i = 0; i < dataValues.Length; i++)
							{
								NodeValue dataValue = (NodeValue)dataValues[i];
								double value = (dataValue.ValueComponents.Length > componentIndex) ? dataValue.ValueComponents[componentIndex] : 0.0; // check if component is specified, if not insert default value (zero)
								components[i] = new DataValueComponent(dataValue.EntityNumber, value);
							}
							octreeRoot.InsertDataValues(components, dataIndexCounter + componentIndex, nodeIndexMap, /*initial depth = */ 0, /*globalRange = */ double.NaN, approximationParameters.Method);

							//writeDataComponentsToFile(Path.GetDirectoryName(mesh.Filename), dataIndexCounter + componentIndex, components);
						}

						// compute data summary, store it to octree and clear temporary data value lists
						//octreeRoot.CreateDataCatalog(dataInfo, dataIndex);
						DataIndexMap[dataInfo] = dataIndexCounter;
						dataIndexCounter += dataInfo.DataType.ComponentCount;
					}
					//octreeRoot.ClearTemporaryData();
				}
			}
		}

		public override void FinishUp()
		{
			updateColorScale();
		}

		//public int GetDataColor(Vector3 position)
		//{
		//	// TODO: remove this method if it is not necessary (e.g. cross-sections?)

		//	DataAbstract dataAbstract = octreeRoot.GetDataAbstract(ref position, CurrentDataIndex);
		//	if (dataAbstract != null)
		//	{
		//		return convertValueToColor(dataAbstract.GetAverageValue(), CurrentDataIndex);
		//	}
		//	return UndefinedValueColor;
		//}

		public override int GetDataColor(Node node, Element element)
		{
			// NOTE: element parameter is ignored in OctreeDataVisualizer
			return GetColorForDataValue(getDataValue(node, Settings.ScalarDataIndex));
		}

		public override double GetDataValue(Node node, DataIndex dataIndex)
		{
			return getDataValue(node, dataIndex);
		}

		public override double GetDataValue(Node node, DataIndex dataIndex, out float error)
		{
			if (loadedExactDataValues && dataIndex == Settings.ScalarDataIndex)
			{
				error = 0.0f;
			}
			else
			{
				Vector3 position;
				GetOriginalNodePosition(node, out position);
				DataAbstract dataAbstract = octreeRoot.GetDataAbstract(ref position, dataIndex.Index);
				if (dataAbstract != null)
					error = dataAbstract.MaxError;
				else
					error = float.NaN;
			}
			return getDataValue(node, dataIndex);
		}

		protected virtual DataIndex translateDataIndex(int index)
		{
			return new DataIndex(index, 0.0 /*ignore time*/);
		}

		public override ApproximationQuality GetApproximationQuality(LongOpNotifier longOpNotifier)
		{
			ApproximationQuality apxQuality = new ApproximationQuality();

			// =============================================================================================
			// === COMPUTE APPROXIMATION ERROR ===

			float globalMaxRelativeError = 0.0f;
			float globalSumAverageRelativeError = 0.0f;
			int globalItemCount = 0;

			int dataIndex = 0;

			foreach (string filename in loadedFiles)
			{
				using (IDataFileParser dataParser = DataParserFactory.Create(filename))
				{
					DataInfo dataInfo;
					while ((dataInfo = dataParser.ReadNextResult()) != null)
					{
						longOpNotifier.ReportProgress((int)((double)dataIndex / (double)dataIndexCounter * 100.0), "Computing Approximation Quality Metrics", "Computing Approximation Error...");

						List<double>[] exactValues = new List<double>[dataInfo.DataType.ComponentCount];
						List<double>[] approximatedValues = new List<double>[dataInfo.DataType.ComponentCount];
						for (int i = 0; i < dataInfo.DataType.ComponentCount; i++)
						{
							exactValues[i] = new List<double>();
							approximatedValues[i] = new List<double>();
						}

						foreach (NodeValue dataValue in dataParser.ReadResultBlock())
						{
							int nodeID = dataValue.EntityNumber;
							if (nodeIndexMap.ContainsKey(nodeID))
							{
								for (int componentIndex = 0; componentIndex < dataValue.ValueComponents.Length; componentIndex++)
								{
									double exactValue = dataValue.ValueComponents[componentIndex];
									double approximatedValue = getDataValue(nodeIndexMap[nodeID], translateDataIndex(dataIndex + componentIndex));

									exactValues[componentIndex].Add(exactValue);
									approximatedValues[componentIndex].Add(approximatedValue);
								}
							}
						}


						// ----------------------------

						for (int componentIndex = 0; componentIndex < dataInfo.DataType.ComponentCount; componentIndex++)
						{
							double localMaxError = 0.0f;
							double localSumAverageError = 0.0f;
							int localItemCount = 0;
							double maxValue = double.MinValue, minValue = double.MaxValue;

							for (int i = 0; i < exactValues[componentIndex].Count; i++)
							{
								double exactValue = exactValues[componentIndex][i];
								double approximatedValue = approximatedValues[componentIndex][i];

								maxValue = Math.Max(maxValue, exactValue);
								minValue = Math.Min(minValue, exactValue);
								double error = Math.Abs(exactValue - approximatedValue);
								localMaxError = Math.Max(localMaxError, error);
								localSumAverageError += error;
								++localItemCount;
							}

							if (localItemCount == 0)
								continue;

							double localAverageError = localSumAverageError / localItemCount;

							double baseValue = maxValue - minValue;

							Debug.Assert(baseValue >= 0.0);

							if (Math.Abs(baseValue) < Mathematics.Common.Epsilon)
							{
								baseValue = Math.Max(Math.Abs(maxValue), Math.Abs(minValue));
								if (Math.Abs(baseValue) < Mathematics.Common.Epsilon)
								{
									//baseValue = double.Epsilon;
									baseValue = 1.0;
								}
							}

							// TODO: deal with range==zero

							float localMaxRelativeError = (float)(localMaxError / baseValue);
							float localAverageRelativeError = (float)(localAverageError / baseValue);

							globalMaxRelativeError = Math.Max(globalMaxRelativeError, localMaxRelativeError);
							globalSumAverageRelativeError += localAverageRelativeError * localItemCount;
							globalItemCount += localItemCount;

							if (dataIndex == Settings.ScalarDataIndex.Index) // currently displayed data value
							{
								apxQuality.CurrentDataAverageRelativeError = localAverageRelativeError;
								apxQuality.CurrentDataMaxRelativeError = localMaxRelativeError;
							}

							// ----------------------------
						}
						dataIndex += dataInfo.DataType.ComponentCount;
					}
				}
			}

			apxQuality.MaxRelativeError = globalMaxRelativeError;
			apxQuality.AverageRelativeError = globalSumAverageRelativeError / globalItemCount;

			// =============================================================================================
			// === COMPUTE MEMORY CONSUMPTION ===

			longOpNotifier.ReportProgress(-1, "Computing Approximation Quality Metrics", "Computing Memory Consumption...");

			//using (Stream s = new MemoryStream())
			//{
			//	System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			//	formatter.Serialize(s, octreeRoot);
			//	apxQuality.MemoryConsumption = s.Length;
			//}

			Stack<TreeNode> tree = new Stack<TreeNode>();
			tree.Push(octreeRoot);
			while (tree.Count > 0)
			{
				TreeNode node = tree.Pop();

				apxQuality.MemoryConsumption += Vector3.SizeInBytes * 3; // Center, LowerBounds, UpperBounds
				foreach (DataAbstract data in node.DataCatalog.Values)
				{
					apxQuality.MemoryConsumption += sizeof(int) + IntPtr.Size; // KeyValuePair
					apxQuality.MemoryConsumption += data.GetSizeInBytes(); // dataAbstract
				}
				apxQuality.MemoryConsumption += IntPtr.Size; // DataCatalog

				InternalNode parent = node as InternalNode;
				if (parent != null)
				{
					foreach (TreeNode child in parent.Children)
						tree.Push(child);

					apxQuality.MemoryConsumption += parent.Children.Length * IntPtr.Size + IntPtr.Size; // children array
				}

				// LeafNode does not add any fields
			}

			long uncompressedSize = nodeIndexMap.Count * dataIndexCounter * sizeof(double); // nodeCount X dataTypesCount X valueItemSize

			apxQuality.CompressionRatio = (float)((double)apxQuality.MemoryConsumption / (double)uncompressedSize);

			return apxQuality;
		}

		protected override void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			string[] propertyNames = e.PropertyName.Split(';');
			foreach (string propertyName in propertyNames)
			{
				switch (propertyName)
				{
					case "DisplayMethod":
						switch (Settings.DisplayMethod)
						{
							case ScalarDataDisplayMethod.Approximation:
								currentDataValueCache = null; // clear data cache
								loadedExactDataValues = false;
								updateColorScale();
								break;
							case ScalarDataDisplayMethod.ApproximationError:
								loadDataApproximationErrors();
								break;
							case ScalarDataDisplayMethod.ExactValues:
								loadExactDataValues();
								break;
							default:
								break;
						}
						break;
					case "ScalarDataIndex":
						currentDataValueCache = null; // clear data cache
						updateColorScale();
						Settings.DisplayMethod = ScalarDataDisplayMethod.Approximation;
						break;
				}
			}

			base.OnSettingsPropertyChanged(sender, e);
		}

		protected override IntervalD GetDataValueRange(int dataIndex)
		{
			DataAbstract dataAbstract;
			if (octreeRoot.DataCatalog.TryGetValue(dataIndex, out dataAbstract))
			{
				return new IntervalD(dataAbstract.MinValue, dataAbstract.MaxValue);
			}
			else
			{
				return IntervalD.Zero;
			}
		}

		public override int[] GetEntitiesWithMaximumDataValue()
		{
			DataAbstract dataAbstract;
			if (octreeRoot.DataCatalog.TryGetValue(Settings.ScalarDataIndex.Index, out dataAbstract))
				return new int[] { dataAbstract.MaxValueEntityNumber }; // if data exists, return node number with maximum data value
			return new int[] { }; // return empty array
		}

		public override int[] GetEntitiesWithMinimumDataValue()
		{
			DataAbstract dataAbstract;
			if (octreeRoot.DataCatalog.TryGetValue(Settings.ScalarDataIndex.Index, out dataAbstract))
				return new int[] { dataAbstract.MinValueEntityNumber }; // if data exists, return node number with minimum data value
			return new int[] { }; // return empty array
		}

		public override double GetMaximumDataValue()
		{
			DataAbstract dataAbstract;
			if (octreeRoot.DataCatalog.TryGetValue(Settings.ScalarDataIndex.Index, out dataAbstract))
				return dataAbstract.MaxValue;
			return double.NaN;
		}

		public override double GetMinimumDataValue()
		{
			DataAbstract dataAbstract;
			if (octreeRoot.DataCatalog.TryGetValue(Settings.ScalarDataIndex.Index, out dataAbstract))
				return dataAbstract.MinValue;
			return double.NaN;
		}

		#endregion

		#region Private methods

		private Dictionary<int, double> createDataValueCache()
		{
			//Debug.Assert(currentDataValueCache == null);
			//Debug.Assert(!loadedExactDataValues);

			lock (dataValueCacheCreationLocker)
			{
				var current = currentDataValueCache;
				if (current != null)
					return current;
				var newCache = new Dictionary<int, double>();
				foreach (Node node in nodeIndexMap.Values)
				{
					double value = getValueInNode(node, Settings.ScalarDataIndex);
					//double value = octreeRoot.GetValueApproximationInNode(node, settings.CurrentDataIndex);
					if (!double.IsNaN(value))
						newCache[node.ID] = value; // /**/ WARNING: currentDataValueCache can be set to null in another thread while still in loop
				}
				currentDataValueCache = newCache;
				return newCache;
			}
		}

		private double getValueInNode(Node node, DataIndex dataIndex)
		{
			if (dataIndex.Index < 0)
			{
				dataIndex = dataIndex.WithIndex(~dataIndex.Index);
				double xValue = getScalarValueInNode(node, ref dataIndex);
				dataIndex = dataIndex.WithIndex(dataIndex.Index + 1);
				double yValue = getScalarValueInNode(node, ref dataIndex);
				dataIndex = dataIndex.WithIndex(dataIndex.Index + 1);
				double zValue = getScalarValueInNode(node, ref dataIndex);

				return Math.Sqrt(xValue * xValue + yValue * yValue + zValue * zValue); // return vector magnitude
			}

			return getScalarValueInNode(node, ref dataIndex);
		}

		private double getScalarValueInNode(Node node, ref DataIndex dataIndex)
		{
			Vector3 position;
			GetOriginalNodePosition(node, out position);
			Vector4 spacetime = new Vector4(position, (float)dataIndex.Time/*relevant only in derived class: TimetreeDataVisualizer*/);

			double value = octreeRoot.GetValueApproximationAt(ref spacetime, dataIndex.Index);
			if (double.IsNaN(value)) // is this test necessary?
				return double.NaN;
			return GetDataValueRange(dataIndex.Index).CutValue(value); // cut values with tree node minimum and maximum
		}

		private float scalarProjection(ref Vector3 A, ref Vector3 B)
		{
			float result;
			Vector3.Dot(ref A, ref B, out result);
			float BLength = B.Length;
			if (BLength <= float.Epsilon)
				return result;
			return result / (BLength * BLength);
		}

		private void updateColorScale()
		{
			IntervalD range;
			if (Settings.ScalarDataIndex.Index < 0) // Show vector magnitudes
			{
				createDataValueCache();
				range = IntervalD.InvertedMaxMin;
				foreach (double value in currentDataValueCache.Values)
					range.MergeWith(value);
			}
			else
			{
				range = GetDataValueRange(Settings.ScalarDataIndex.Index);
			}
			Settings.ColorScale.SetMinMaxValue(range.Min, range.Max);
		}

		private static bool pointIsInRegion(ref Vector3 point, ref Vector3 regionLowerBounds, ref Vector3 regionUpperBounds)
		{
			if (point.X < regionLowerBounds.X || point.X > regionUpperBounds.X)
				return false;
			if (point.Y < regionLowerBounds.Y || point.Y > regionUpperBounds.Y)
				return false;
			if (point.Z < regionLowerBounds.Z || point.Z > regionUpperBounds.Z)
				return false;
			return true;
		}

		//private void writeDataComponentsToFile(string directory, int dataIndex, DataValueComponent[] components)
		//{
		//	string filename = Path.Combine(directory, dataIndex.ToString() + DataCacheFileExtension);

		//	using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create, FileAccess.Write)))
		//	{
		//		foreach (DataValueComponent component in components)
		//		{
		//			if (component.Value != 0.0)
		//			{
		//				writer.Write(component.EntityNumber);
		//				writer.Write(component.Value);
		//			}
		//		}
		//	}
		//}

		//private IEnumerable<DataValueComponent> readDataComponentsFromFile(string directory, int dataIndex)
		//{
		//	string filename = Path.Combine(directory, dataIndex.ToString() + DataCacheFileExtension);
		//	if (!File.Exists(filename))
		//		yield break;
		//	using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read)))
		//	{
		//		byte[] bytes = new byte[sizeof(int) + sizeof(double)];
		//		while (true)
		//		{
		//			int count = reader.Read(bytes, 0, bytes.Length);
		//			if (count < bytes.Length)
		//				break;
		//			yield return new DataValueComponent(BitConverter.ToInt32(bytes, 0), BitConverter.ToDouble(bytes, sizeof(int)));
		//		}
		//	}
		//}

		private double getDataValue(Node node, DataIndex dataIndex)
		{
			if (dataIndex == Settings.ScalarDataIndex)
			{
				var cache = currentDataValueCache;
				if (cache == null)
				{
					cache = createDataValueCache(); // create value cache
				}
				double value;
				if (cache.TryGetValue(node.ID, out value))
					return value;
			}
			else
			{
				return getValueInNode(node, dataIndex);
			}
			return double.NaN;
		}

		private void loadDataApproximationErrors()
		{
			if (loadedExactDataValues) // exact data already loaded, compute difference with octree data
			{
				IntervalD valueRange = IntervalD.InvertedMaxMin;
				Dictionary<int, double> newCache = new Dictionary<int, double>();

				Debug.Assert(currentDataValueCache != null);
				foreach (int key in currentDataValueCache.Keys)
				{
					Debug.Assert(nodeIndexMap.ContainsKey(key));
					double value = currentDataValueCache[key] - getValueInNode(nodeIndexMap[key], Settings.ScalarDataIndex);
					newCache[key] = value;
					valueRange.MergeWith(value);
				}

				this.currentDataValueCache = newCache;

				// update color scale
				if (currentDataValueCache.Count == 0)
					valueRange = IntervalD.NaN;
				Settings.ColorScale.SetMinMaxValue(valueRange.Min, valueRange.Max);
			}
			else // must load exact data and compute difference
			{
				loadExactDataValues();
				loadDataApproximationErrors();
			}
		}

		private void loadExactDataValues()
		{
			int dataIndex = Settings.ScalarDataIndex.Index;
			bool showVectorMagnitudes = false;
			if (dataIndex < 0)
			{
				dataIndex = ~dataIndex;
				showVectorMagnitudes = true;
			}

			IntervalD range;
			bool continuousInTime = IsContinuousInTime(out range);

			DataInfo currentDataInfo = null;
			foreach (var pair in DataIndexMap)
			{
				bool equalsTime = true;
				if (continuousInTime)
				{
					equalsTime = pair.Key.Time.AlmostEquals(Settings.ScalarDataIndex.Time);
				}
				if (equalsTime && dataIndex >= pair.Value && dataIndex < pair.Value + pair.Key.DataType.ComponentCount)
				{
					currentDataInfo = pair.Key;
					break;
				}
			}

			if (currentDataInfo == null) // for current time there is no data, lets show NaNs
			{
				this.currentDataValueCache = new Dictionary<int, double>();
				loadedExactDataValues = true;
				Settings.ColorScale.SetMinMaxValue(double.NaN, double.NaN);
				return;
			}

			int componentIndex = dataIndex - DataIndexMap[currentDataInfo];

			IntervalD valueRange = IntervalD.InvertedMaxMin;
			Dictionary<int, double> newCache = new Dictionary<int, double>();

			using (IDataFileParser dataParser = DataParserFactory.Create(currentDataInfo.DataType.FileName, currentDataInfo.DataType.FilePosition))
			{
				DataInfo dataInfo;
				while ((dataInfo = dataParser.ReadNextResult()) != null)
				{
					bool isCurrent = dataInfo.Equals(currentDataInfo);
					foreach (NodeValue dataValue in dataParser.ReadResultBlock())
					{
						if (!isCurrent) // must read through data even if I don't care about them
							continue;

						int nodeID = dataValue.EntityNumber;
						if (nodeIndexMap.ContainsKey(nodeID))
						{
							double value;
							if (showVectorMagnitudes)
							{
								Debug.Assert(dataInfo.DataType.CompoundType == Data.DataType.CompoundTypes.Vector);
								Debug.Assert(componentIndex == 0);
								double x = dataValue.ValueComponents[componentIndex];
								double y = dataValue.ValueComponents[componentIndex + 1];
								double z = dataValue.ValueComponents[componentIndex + 2];
								value = Math.Sqrt(x * x + y * y + z * z);
							}
							else
							{
								value = dataValue.ValueComponents[componentIndex];
							}
							newCache[nodeID] = value;
							valueRange.MergeWith(value);
						}
					}
					if (isCurrent)
						break;
				}
			}

			this.currentDataValueCache = newCache;
			loadedExactDataValues = true;

			// update color scale
			Settings.ColorScale.SetMinMaxValue(valueRange.Min, valueRange.Max);
		}

		#endregion

	}
}

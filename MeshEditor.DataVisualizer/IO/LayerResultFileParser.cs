using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;
using MeshEditor.Utilities;

namespace MeshEditor.DataVisualizer.IO
{
	class LayerResultFileParser : IDataFileParser
	{
		IEnumerator<ComponentDataDescription> dataEnumerator;

		public LayerResultFileParser(string filename)
		{
			Filename = filename;
		}

		public int CurrentLineNumber => 0;

		public string Filename { get; }

		public double PercentageRead => 100.0;

		public void Dispose()
		{
			if (dataEnumerator != null)
			{
				dataEnumerator.Dispose();
				dataEnumerator = null;
			}
		}

		public DataInfo ReadNextResult()
		{
			if (dataEnumerator == null)
			{
				initDataEnumerator();
			}

			if (!dataEnumerator.MoveNext())
				return null;

			ComponentDataDescription data = dataEnumerator.Current;

			string componentName = data.ComponentName ?? System.IO.Path.GetFileNameWithoutExtension(Filename);
			DataType dataType = new DataType(data.FieldName + ": " + componentName, Filename, 0, DataType.CompoundTypes.Scalar, componentName);
			DataInfo dataInfo = new DataInfo(dataType, null, data.TimeStep, convertLocationTypeToDataLocation(data.Location));
			return dataInfo;
		}

		public IEnumerable<DataValue> ReadResultBlock()
		{
			if (dataEnumerator == null || dataEnumerator.Current == null)
			{
				throw new InvalidOperationException();
			}

			ComponentDataDescription data = dataEnumerator.Current;

			for (int index = 0; index < data.Values.Length; index++)
			{
				double value = data.Values[index];
				if (!double.IsNaN(value))
				{
					switch (data.Location)
					{
						case DataLocationType.Points:
						case DataLocationType.CellPoints:
							yield return new NodeValue(index, new[] { value });
							break;
						case DataLocationType.Cells:
							yield return new ElementValue(index, new[,] { { value } });
							break;
						default:
							throw new NotSupportedException();
					}
					
				}
			}
		}

		#region Private methods

		private static DataLocation convertLocationTypeToDataLocation(DataLocationType location)
		{
			switch (location)
			{
				case DataLocationType.Points:
					return DataLocation.Nodes;
				case DataLocationType.Cells:
					return DataLocation.Elements;
				case DataLocationType.CellPoints:
					return DataLocation.ElementNodes;
				default:
					throw new NotSupportedException();
			}
		}

		private void initDataEnumerator()
		{
			var localStorage = new LocalFileSystemStorageService(Path.GetDirectoryName(Filename));
			dataEnumerator = new LayerGenerator(sourceStorage: localStorage, destinationStorage: null).LoadData(Path.GetFileName(Filename)).GetEnumerator();
		}

		#endregion
	}
}

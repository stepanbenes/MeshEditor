using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Import;
using MeshEditor.Utilities;

namespace MeshEditor.DataVisualizer.IO
{
	class LayerResultFileParser : IDataFileParser
	{
		IEnumerator<DataDescription> dataEnumerator;

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

			DataDescription data = dataEnumerator.Current;

			string componentName = data.ComponentNames.Single() ?? System.IO.Path.GetFileNameWithoutExtension(Filename);
			DataType dataType = new DataType(data.Name + ": " + componentName, Filename, 0, convertFieldTypeToCoumpoundType(data.FieldType), componentName);
			DataInfo dataInfo = new DataInfo(dataType, null, data.TimeStep ?? 0, convertLocationTypeToDataLocation(data.Location));
			return dataInfo;
		}

		public IEnumerable<DataValue> ReadResultBlock()
		{
			if (dataEnumerator == null || dataEnumerator.Current == null)
			{
				throw new InvalidOperationException();
			}

			DataDescription data = dataEnumerator.Current;

			if (data.NumberOfComponents != 1)
			{
				throw new NotSupportedException();
			}

			for (int index = 0; index < data.Data.Length; index++)
			{
				double value = data.Data[index];
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

		private static DataType.CompoundTypes convertFieldTypeToCoumpoundType(FieldType fieldType)
		{
			switch (fieldType)
			{
				case FieldType.Scalar:
					return DataType.CompoundTypes.Scalar;
				case FieldType.Vector:
					return DataType.CompoundTypes.Vector;
				case FieldType.Tensor:
					return DataType.CompoundTypes.Matrix;
				default:
					throw new NotSupportedException();
			}
		}

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
			dataEnumerator = new LayerGenerator().LoadData(new Uri(Filename)).GetEnumerator();
		}

		#endregion
	}
}

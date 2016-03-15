using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.IO;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.DataVisualizer.IO
{
	class JsonDataFileParser : JsonFileParserBase, IDataFileParser
	{
		LayerResult resultFile;

		public JsonDataFileParser(string filename)
		 : base(filename)
		{ }

		public double PercentageRead => 100.0;

		public DataInfo ReadNextResult()
		{
			if (resultFile == null)
			{
				resultFile = ParseInput<LayerResult>();
				DataType dataType = new DataType($"{resultFile.FieldName}-{resultFile.ComponentName}", Filename, 0, DataType.CompoundTypes.Scalar, resultFile.ComponentName);
				return new DataInfo(dataType, "", resultFile.TimeSteps.Single(), DataLocation.Nodes);
			}
			else
			{
				return null;
			}
		}

		public IEnumerable<DataValue> ReadResultBlock()
		{
			Debug.Assert(resultFile != null);
			double[] data = convertBase64StringToArray<double>(resultFile.Data);
			for (int i = 0; i < data.Length; i++)
			{
				yield return new NodeValue(i, new[] { data[i] });
			}
		}

		public void Dispose()
		{
			resultFile = null;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.IO;

namespace MeshEditor.DataVisualizer.IO
{
	class JsonDataFileParser : JsonFileParserBase, IDataFileParser
	{
		private class ResultFile
		{
			public Guid LayerId { get; set; }
			public string FieldName { get; set; }
			public string ComponentName { get; set; }
			public int Index { get; set; }
			public double? TimeStep { get; set; }
			public Dictionary<string, object> Compression { get; set; }
			public string Data { get; set; }
		}

		ResultFile resultFile;

		public JsonDataFileParser(string filename)
		 : base(filename)
		{ }

		public double PercentageRead => 100.0;

		public DataInfo ReadNextResult()
		{
			if (resultFile == null)
			{
				resultFile = ParseInput<ResultFile>();
				DataType dataType = new DataType($"{resultFile.FieldName}-{resultFile.ComponentName}", Filename, 0, DataType.CompoundTypes.Scalar, resultFile.ComponentName);
				return new DataInfo(dataType, "", resultFile.TimeStep ?? 0, DataLocation.Nodes);
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
				yield return new NodeValue(i + 1, new[] { data[i] });
			}
		}

		public void Dispose()
		{
			resultFile = null;
		}
	}
}

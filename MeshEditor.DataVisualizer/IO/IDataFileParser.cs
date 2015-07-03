using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MeshEditor.DataVisualizer.Data;

namespace MeshEditor.DataVisualizer.IO
{
	public interface IDataFileParser : IDisposable
	{
		string Filename { get; }

		int CurrentLineNumber { get; }

		DataInfo ReadNextResult();

		IEnumerable<DataValue> ReadResultBlock();

		//long TotalBytes { get; }
		//long BytesRead { get; }

		double PercentageRead { get; }
	}
}

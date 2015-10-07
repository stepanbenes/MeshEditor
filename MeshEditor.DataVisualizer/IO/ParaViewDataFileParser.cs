using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.IO;

namespace MeshEditor.DataVisualizer.IO
{
	public class ParaViewDataFileParser : VTKXmlFileParserBase, IDataFileParser
	{

		#region Fields, constructor

		private Queue<KeyValuePair<string, double>> fileNameTimeStepQueue;
		private VTKXmlDataFileParser currentFileParser;

		public ParaViewDataFileParser(string filename)
		: base(filename)
		{ }

		#endregion

		#region IDataFileParser members

		public DataInfo ReadNextResult()
		{
			EnsureInputIsInitialized();

			if (currentFileParser != null)
			{
				var dataInfo = currentFileParser.ReadNextResult();
				if (dataInfo != null)
				{
					return dataInfo;
				}
				currentFileParser.Dispose();
				currentFileParser = null;
			}
			
			if (fileNameTimeStepQueue.Count > 0)
			{
				var fileNameTimePair = fileNameTimeStepQueue.Dequeue();
				currentFileParser = new VTKXmlDataFileParser(fileNameTimePair.Key, fileNameTimePair.Value);
				return ReadNextResult();
			}

			return null; // no more data
		}

		public IEnumerable<DataValue> ReadResultBlock()
		{
			if (currentFileParser == null)
			{
				throw new FileParserException("Can not read result block. Previous data was not processed entirely.", Filename, CurrentLineNumber, CurrentLinePosition);
			}

			return currentFileParser.ReadResultBlock();
		}

		#endregion

		#region Private methods

		private void EnsureInputIsInitialized()
		{
			if (!IsInputInitialized)
			{
				string fileType;
				InitInput(out fileType);
				if (fileType?.ToLower() != "collection")
				{
					throw new FileParserException($"VTK file type '{fileType}' is not supported. Only 'Collection' type is supported.", Filename, CurrentLineNumber, CurrentLinePosition);
				}
				ReadToCollectionElement();
			}
			Debug.Assert(IsInputInitialized);
			Debug.Assert(Input != null);
			Debug.Assert(fileNameTimeStepQueue != null);
        }

		private void ReadToCollectionElement()
		{
			if (!Input.ReadToDescendant("Collection"))
			{
				ThrowElementIsMissing("Collection");
			}

			if (!Input.ReadToDescendant("DataSet"))
			{
				ThrowElementIsMissing("DataSet");
			}

			fileNameTimeStepQueue = new Queue<KeyValuePair<string, double>>();

			do
			{
				double? timeStep = null;
				string dataFilename = null;
				while (Input.MoveToNextAttribute())
				{
					switch (Input.Name.ToLower())
					{
						case "timestep":
							timeStep = ParseFloat64(Input.Value);
							break;
						//case "group":
						//	break;
						//case "part":
						//	break;
						case "file":
							dataFilename = Input.Value;
							break;
					}
				}

				if (String.IsNullOrEmpty(dataFilename))
				{
					throw new FileParserException("Filename was not specified in DataSet element.", Filename, CurrentLineNumber, CurrentLinePosition);
				}

				if (!timeStep.HasValue)
				{
					throw new FileParserException("Time was not specified in DataSet element.", Filename, CurrentLineNumber, CurrentLinePosition);
				}

				string rootedDataFilename = Path.IsPathRooted(dataFilename) ? dataFilename : Path.Combine(Path.GetDirectoryName(this.Filename), dataFilename);
				fileNameTimeStepQueue.Enqueue(new KeyValuePair<string, double>(rootedDataFilename, timeStep.Value));

			} while (Input.ReadToNextSibling("DataSet"));
		}

		#endregion

	}
}

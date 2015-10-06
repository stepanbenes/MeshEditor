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
	public class ParaViewDataFileParser : IDataFileParser
	{

		#region Fields, constructor

		private readonly string filename;

		private StreamReader streamReader;
		private XmlReader input;

		private Queue<KeyValuePair<string, double>> fileNameTimeStepQueue;
		private VTKXMLDataFileParser currentFileParser;

		public ParaViewDataFileParser(string filename)
		{
			this.filename = filename;
		}

		#endregion

		#region IDataFileParser members

		public string Filename => filename;

		public int CurrentLineNumber
		{
			get
			{
				// NOTE: this is too coarse measure, underlying parser is ignored
				IXmlLineInfo xmlInfo = input as IXmlLineInfo;
				if (xmlInfo == null)
				{
					return -1;
				}
				return xmlInfo.LineNumber;
			}
		}

		public double PercentageRead
		{
			get
			{
				// NOTE: this is too coarse measure, underlying parser is ignored
				return ((double)streamReader.BaseStream.Position / (double)streamReader.BaseStream.Length) * 100.0;
			}
		}

		public DataInfo ReadNextResult()
		{
			if (fileNameTimeStepQueue == null)
			{
				initInput();
			}

			Debug.Assert(fileNameTimeStepQueue != null);

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
				currentFileParser = new VTKXMLDataFileParser(fileNameTimePair.Key, fileNameTimePair.Value);
				return ReadNextResult();
			}

			return null; // no more data
		}

		public IEnumerable<DataValue> ReadResultBlock()
		{
			if (currentFileParser == null)
			{
				throw new DataLoadingException("Can not read result block. Previous data was not processed entirely.", Filename, CurrentLineNumber);
			}

			return currentFileParser.ReadResultBlock();
		}

		#endregion

		#region Private methods

		private void initInput()
		{
			if (!File.Exists(filename))
			{
				throw new DataLoadingException($"Data file can't be found. ({filename})");
			}

			streamReader = new StreamReader(filename);
			input = XmlReader.Create(streamReader);

			validateVTKFileType();

			if (!input.ReadToDescendant("Collection"))
			{
				throwElementIsMissing("Collection");
			}

			if (!input.ReadToDescendant("DataSet"))
			{
				throwElementIsMissing("DataSet");
			}

			fileNameTimeStepQueue = new Queue<KeyValuePair<string, double>>();

			do
			{
				double? timeStep = null;
				string dataFilename = null;
				while (input.MoveToNextAttribute())
				{
					switch (input.Name.ToLower())
					{
						case "timestep":
							timeStep = parseFloat64(input.Value);
							break;
						//case "group":
						//	break;
						//case "part":
						//	break;
						case "file":
							dataFilename = input.Value;
							break;
					}
				}

				if (String.IsNullOrEmpty(dataFilename))
				{
					throw new DataLoadingException("Filename was not specified in DataSet element.", Filename, CurrentLineNumber);
				}

				if (!timeStep.HasValue)
				{
					throw new DataLoadingException("Time was not specified in DataSet element.", Filename, CurrentLineNumber);
				}

				string rootedDataFilename = Path.IsPathRooted(dataFilename) ? dataFilename : Path.Combine(Path.GetDirectoryName(this.Filename), dataFilename);
				fileNameTimeStepQueue.Enqueue(new KeyValuePair<string, double>(rootedDataFilename, timeStep.Value));

			} while (input.ReadToNextSibling("DataSet"));
		}

		private void validateVTKFileType()
		{
			if (!input.ReadToDescendant("VTKFile"))
			{
				throwElementIsMissing("VTKFile");
			}

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						if (input.Value.ToLower() != "collection")
						{
							throw new DataLoadingException($"Only collection file type is supported instead of '{input.Value}'.", Filename, CurrentLineNumber);
						}
						break;
					//case "version":
				}
			}

			if (!input.MoveToElement())
			{
				throwElementIsMissing("VTKFile");
			}
		}

		private void throwElementIsMissing(string elementName)
		{
			throw new DataLoadingException($"{elementName} element was not found.", Filename, CurrentLineNumber);
		}

		private double parseFloat64(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new DataLoadingException($"Floating-point number expected instead of '{text}'", Filename, CurrentLineNumber);
			}
			return result;
		}

		#endregion

		#region IDisposable Support

		public void Dispose()
		{
			if (streamReader != null)
			{
				streamReader.Dispose();
				streamReader = null;
			}
			if (input != null)
			{
				((IDisposable)input).Dispose();
				input = null;
			}
		}

		#endregion

	}
}

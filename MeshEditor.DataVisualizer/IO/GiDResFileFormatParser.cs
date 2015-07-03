using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MeshEditor.DataVisualizer.Data;
using System.Globalization;
using MeshEditor.IO;

namespace MeshEditor.DataVisualizer.IO
{
	public class GiDResFileFormatParser : IDataFileParser
	{

		#region Static members

		public static readonly string HeaderToken = "GiD Post Results File";
		public static readonly string CommentToken = "#";
		public static readonly string GaussPointsToken = "GaussPoints";
		public static readonly string ResultToken = "Result";
		public static readonly string ValuesToken = "Values";
		public static readonly string EndToken = "End";
		public static readonly string ComponentNamesToken = "ComponentNames";

		#endregion

		#region Fields, constructor, destructor

		private enum State
		{
			Init = 0,
			GaussPointsDescription,
			GaussPointsGivenNaturalCoordinates,
			ResultHeader,
			ResultValues,
            EOF
		}

		private string filename;
		private int lineNumber;
		private PositionAwareStreamReader input;
		private State state;
		private DataInfo currentDataInfo;
		private GaussPointsInfo currentGaussPointsInfo;
		private int currentGaussPointIndex;
		private Dictionary<string, GaussPointsInfo> gaussPointsDescriptions;
		
		private string[] elementTypesNamesCache;

		private long currentLineFilePosition;

		public GiDResFileFormatParser(string filename, long fileStartPosition = 0)
		{
			this.filename = filename;
			this.currentLineFilePosition = fileStartPosition;
			lineNumber = -1;
			input = null;
			state = State.Init;
			TotalBytes = new FileInfo(filename).Length;

			gaussPointsDescriptions = new Dictionary<string, GaussPointsInfo>();
		}

		~GiDResFileFormatParser()
		{
			Dispose(false);
		}

		#endregion

		#region IDataFileParser Members

		public string Filename
		{
			get { return filename; }
		}

		public int CurrentLineNumber
		{
			get { return lineNumber; }
		}

		public long TotalBytes
		{
			get;
			private set;
		}

		public long BytesRead
		{
			get;
			private set;
		}

		public double PercentageRead
		{
			get { return ((double)BytesRead / (double)TotalBytes) * 100.0; }
		}

		public DataInfo ReadNextResult()
		{
			if (input == null)
				initInput();

            if (state == State.EOF)
                return null; // End Of File was reached

			if (state != State.Init)
				throw new DataLoadingException("Can not read next result block. Previous result block was not processed entirely.", Filename, CurrentLineNumber);

			string line;
            currentDataInfo = null;

			while ((line = getNextLine()) != null)
			{
				++lineNumber;
				line = line.TrimStart();

				if (line.Equals(string.Empty) || line.StartsWith(CommentToken))
					continue;

                switch (state)
                {
                    case State.Init:
						if (line.StartsWith(GaussPointsToken, StringComparison.InvariantCultureIgnoreCase)) // GaussPoints
						{
							state = State.GaussPointsDescription;
							string[] tokens = splitLineToTokens(line);
							Debug.Assert(tokens.Length >= 4);

							string gaussPointsName = tokens[1].Trim('\"');
							Debug.Assert(string.Equals(tokens[2], "Elemtype", StringComparison.InvariantCultureIgnoreCase));
							GaussPointsInfo.ElementTypes elementType;
							bool success = Utilities.Functions.EnumTryParseIgnoreCase(tokens[3], out elementType, ref elementTypesNamesCache);
							Debug.Assert(success);

							this.currentGaussPointsInfo = new GaussPointsInfo(elementType);
							gaussPointsDescriptions[gaussPointsName] = this.currentGaussPointsInfo;

							if (tokens.Length >= 5)
							{
								currentGaussPointsInfo.MeshName = tokens[4].Trim('\"');
							}
						}
						else if (line.StartsWith(ResultToken, StringComparison.InvariantCultureIgnoreCase)) // Result
                        {
                            state = State.ResultHeader;
                            string[] tokens = splitLineToTokens(line);
                            Debug.Assert(tokens.Length >= 6);
                            // Result "result name" "analysis name" step_value my_result_type my_location "location name"
                            if (tokens.Length >= 6)
                            {
                                DataType dataType = new DataType(tokens[1].Trim('\"'), filename, currentLineFilePosition, convertDataTypeStringToCompoundTypeObject(tokens[4]));
								currentDataInfo = new DataInfo(dataType, tokens[2].Trim('\"'), parseDouble(tokens[3]), convertLocationStringToDataLocation(tokens[5]));
                                if (tokens.Length >= 7) // location name
                                {
									string locationName = tokens[6].Trim('\"');
									GaussPointsInfo locationInfo;
									if (gaussPointsDescriptions.TryGetValue(locationName, out locationInfo))
										currentDataInfo.LocationInfo = locationInfo;
                                }
                            }
                            else
                                throw new DataLoadingException("Result block is not complete.", Filename, CurrentLineNumber);
                        }
                        break;
					case State.GaussPointsDescription:
						{
							Debug.Assert(currentGaussPointsInfo != null);
							string[] tokens = splitLineToTokens(line);

							if (line.StartsWith("Number of Gauss Points:", StringComparison.InvariantCultureIgnoreCase))
							{
								int number = parseInteger(tokens[4]);
								Debug.Assert(currentGaussPointsInfo.IsAllowedGaussPointsNumber(number));
								currentGaussPointsInfo.SetGaussPointsNumber(number);
							}
							else if (line.StartsWith("Nodes", StringComparison.InvariantCultureIgnoreCase))
							{
								if (string.Equals(tokens[1], "included", StringComparison.InvariantCultureIgnoreCase))
								{
									currentGaussPointsInfo.NodesIncluded = true;
								}
								else
								{
									Debug.Assert(string.Equals(tokens[1], "not", StringComparison.InvariantCultureIgnoreCase));
									Debug.Assert(string.Equals(tokens[2], "included", StringComparison.InvariantCultureIgnoreCase));

									currentGaussPointsInfo.NodesIncluded = false;
								}
							}
							else if (line.StartsWith("Natural Coordinates:", StringComparison.InvariantCultureIgnoreCase))
							{
								switch (tokens[2].ToLower())
								{
									case "internal":
										currentGaussPointsInfo.SetInternalNaturalCoordinates();
										break;
									case "given":
										state = State.GaussPointsGivenNaturalCoordinates;
										currentGaussPointIndex = 0;
										break;
								}
							}
							else if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase))
							{
								Debug.Assert(string.Equals(tokens[1], GaussPointsToken, StringComparison.InvariantCultureIgnoreCase));
								state = State.Init; // back to initial state
							}
						}
						break;
					case State.GaussPointsGivenNaturalCoordinates:
						Debug.Assert(currentGaussPointsInfo != null);
						if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase))
						{
							state = State.Init; // back to initial state
						}
						else
						{
							string[] tokens = splitLineToTokens(line);
							for (int i = 0; i < tokens.Length; i++)
							{
								currentGaussPointsInfo.SetNaturalCoordinate(currentGaussPointIndex, i, parseDouble(tokens[i]));
							}
							++currentGaussPointIndex;
						}
						break;
                    case State.ResultHeader:
                        if (line.StartsWith(ComponentNamesToken, StringComparison.InvariantCultureIgnoreCase)) // ComponentNames
                        {
							Debug.Assert(currentDataInfo != null);
							if (currentDataInfo != null)
							{
								string[] tokens = splitLineToTokens(line);
								currentDataInfo.DataType.SetComponents(tokens.Skip(1).Select(name => name.Trim('\"')).ToArray());
							}
                        }
                        else if (line.StartsWith(ValuesToken, StringComparison.InvariantCultureIgnoreCase)) // Values
                        {
                            state = State.ResultValues;
							Debug.Assert(currentDataInfo != null);
							if (currentDataInfo != null && currentDataInfo.DataType.ComponentCount == 0)
							{
								// component names are not specified, add generic ones
								currentDataInfo.DataType.AddGenericComponentNames();
							}
							return currentDataInfo;
                        }
                        break;
                }
			}
			state = State.EOF;
			// no additional result block was found
			return null;
		}

		public IEnumerable<DataValue> ReadResultBlock()
		{
			//if (state == State.EOF)
			//	yield break;

			if (state != State.ResultValues || currentDataInfo == null)
				throw new DataLoadingException("Can not read result block. Previous data was not processed entirely.", Filename, CurrentLineNumber);

			//if (currentDataInfo.Location == DataLocation.GaussPoints)
			//	throw new NotImplementedException("Gauss-points location is not implemented yet.");

			if (currentDataInfo.Location == DataLocation.GaussPoints)
			{
				currentGaussPointsInfo = currentDataInfo.LocationInfo;
			}

			currentGaussPointIndex = 0;
			int elementID = -1;
			double[,] gaussPointsValues = null;

			string line;
			while ((line = getNextLine()) != null)
			{
				++lineNumber;
				line = line.TrimStart();

				if (line.StartsWith(CommentToken) || string.IsNullOrEmpty(line))
					continue;

				if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase)) // End values
				{
					Debug.Assert(line.Substring(EndToken.Length).TrimStart().StartsWith(ValuesToken, StringComparison.InvariantCultureIgnoreCase));
					state = State.Init;
					yield break;
				}
				
				// data value
				string[] tokens = splitLineToTokens(line);
				Debug.Assert(tokens.Length >= 1);
				switch (currentDataInfo.Location)
				{
					case DataLocation.Nodes:
						Debug.Assert(tokens.Length >= 2);
						if (tokens.Length >= 2)
						{
							NodeValue dataValue = new NodeValue(parseInteger(tokens[0]), tokens.Skip(1).Select(token => parseDouble(token)).ToArray());
							yield return dataValue;
						}
						break;
					case DataLocation.GaussPoints:
						Debug.Assert(currentGaussPointsInfo != null);

						int startIndex = 0;

						if (currentGaussPointIndex == 0)
						{
							startIndex = 1;
							elementID = parseInteger(tokens[0]);
							gaussPointsValues = new double[currentGaussPointsInfo.GPNumber, currentDataInfo.DataType.ComponentCount];
						}

						int componentCount = Math.Min(currentDataInfo.DataType.ComponentCount, tokens.Length - startIndex);

						for (int i = 0; i < componentCount; i++)
						{
							gaussPointsValues[currentGaussPointIndex, i] = parseDouble(tokens[startIndex + i]);
						}

						++currentGaussPointIndex;

						if (currentGaussPointIndex >= currentGaussPointsInfo.GPNumber)
						{
							currentGaussPointIndex = 0;
							ElementValue dataValue = new ElementValue(elementID, gaussPointsValues);
							yield return dataValue;
						}
						break;
				}
			}
			state = State.EOF;
		}

		#endregion

		#region Private methods

		private static readonly char[] whiteSpaceSeparators = { ' ', '\t' };

		private string[] splitLineToTokens(string line)
		{
			Debug.Assert(line != null);
			return line.Split(whiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
		}

		private int parseInteger(string text)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
				throw new DataLoadingException("Integer expected instead of \"" + text + "\"", Filename, lineNumber);
			return result;
		}

		private double parseDouble(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
				throw new DataLoadingException("Floating-point number expected instead of \"" + text + "\"", Filename, lineNumber);
			return result;
		}

		private void initInput()
		{
			Debug.Assert(input == null);

			if (filename == null || !File.Exists(filename))
			{
				throw new DataLoadingException("Mesh file can't be found.", Filename);
			}

			input = new PositionAwareStreamReader(filename); //File.OpenText(filename);
			
			TotalBytes = input.Length;

			input.Position = currentLineFilePosition; // set start position in file stream

			lineNumber = 0;
			state = State.Init;
		}

		private DataType.CompoundTypes convertDataTypeStringToCompoundTypeObject(string dataTypeString)
		{
			return (DataType.CompoundTypes)Enum.Parse(typeof(DataType.CompoundTypes), dataTypeString, /*ignoreCase: */ true);
		}

		private DataLocation convertLocationStringToDataLocation(string locationString)
		{
			Debug.Assert(!string.IsNullOrEmpty(locationString));
			switch (locationString.ToLower())
			{
				case "onnodes": // OnNodes
					return DataLocation.Nodes;
				case "ongausspoints": // OnGaussPoints
					return DataLocation.GaussPoints;
				default:
					throw new DataLoadingException(string.Format("Unknown data location ({0})", locationString), Filename, CurrentLineNumber);
			}
		}

		private string getNextLine()
		{
			currentLineFilePosition = input.Position; // .BytesRead is equivalent

			string line = input.ReadLine();
			BytesRead = input.BytesRead;
			return line;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			//if (disposing)
			//{
			//    // Free other state (managed objects).
			//}

			if (input != null)
			{
				input.Dispose();
				input = null;
			}
		}

		#endregion

	}
}

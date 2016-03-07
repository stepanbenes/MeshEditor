using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.FormatConverter.Import
{
	class GiDDataFormatParser : FormatParserBase, IDataImportService
	{
		#region Static members

		public static readonly string HeaderToken = "GiD Post Results File";
		public static readonly string CommentToken = "#";
		public static readonly string GaussPointsToken = "GaussPoints";
		public static readonly string ResultToken = "Result";
		public static readonly string ValuesToken = "Values";
		public static readonly string EndToken = "End";
		public static readonly string ComponentNamesToken = "ComponentNames";

		private static readonly char[] whiteSpaceSeparators = { ' ', '\t' };

		private enum ParserState
		{
			Init = 0,
			GaussPointsDescription,
			GaussPointsGivenNaturalCoordinates,
			ResultHeader,
			ResultValues,
			EOF
		}

		#endregion

		#region Fields, Constructor

		IStorageService storageService;
		IEnumerable<string> filenames;

		public GiDDataFormatParser(IStorageService storageService, IEnumerable<string> filenames)
		{
			this.storageService = storageService;
			this.filenames = filenames;
		}

		#endregion

		#region Public methods

		public IEnumerable<DataDescription> ReadData()
		{
			// TODO: replace asserts with throws

			foreach (string filename in filenames)
			{
				using (Stream fileStream = storageService.Load(filename))
				using (TextReader reader = new StreamReader(fileStream))
				{
					ParserState state = ParserState.Init;
					DataDescription currentDataDescription = null;
					List<double> currentDataValues = null;
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						line = line.TrimStart();

						if (line.Equals(string.Empty) || line.StartsWith(CommentToken))
							continue;

						switch (state)
						{
							case ParserState.Init:
								if (line.StartsWith(GaussPointsToken, StringComparison.InvariantCultureIgnoreCase)) // GaussPoints
								{
									//state = ParserState.GaussPointsDescription;
									//string[] tokens = splitLineToTokens(line);
									//Debug.Assert(tokens.Length >= 4);

									//string gaussPointsName = tokens[1].Trim('\"');
									//Debug.Assert(string.Equals(tokens[2], "Elemtype", StringComparison.InvariantCultureIgnoreCase));
									//GaussPointsInfo.ElementTypes elementType;
									//bool success = Utilities.Functions.EnumTryParseIgnoreCase(tokens[3], out elementType, ref elementTypesNamesCache);
									//Debug.Assert(success);

									//this.currentGaussPointsInfo = new GaussPointsInfo(elementType);
									//gaussPointsDescriptions[gaussPointsName] = this.currentGaussPointsInfo;

									//if (tokens.Length >= 5)
									//{
									//	currentGaussPointsInfo.MeshName = tokens[4].Trim('\"');
									//}
								}
								else if (line.StartsWith(ResultToken, StringComparison.InvariantCultureIgnoreCase)) // Result
								{
									state = ParserState.ResultHeader;
									string[] tokens = splitLineToTokens(line);
									Debug.Assert(tokens.Length >= 6);
									// Result "result name" "analysis name" step_value my_result_type my_location "location name"
									if (tokens.Length >= 6)
									{
										currentDataDescription = new DataDescription
										{
											Name = tokens[1].Trim('\"'),
											TimeStep = ParseFloat64(tokens[3]),
											//ComponentNames
											//FieldType
											//LocationType
											//NumberOfDataComponents
										};

										//DataType dataType = new DataType(tokens[1].Trim('\"'), filename, currentLineFilePosition, convertDataTypeStringToCompoundTypeObject(tokens[4]));
										//currentDataInfo = new DataInfo(dataType, tokens[2].Trim('\"'), ParseFloat64(tokens[3]), convertLocationStringToDataLocation(tokens[5]));
										//if (tokens.Length >= 7) // location name
										//{
										//	string locationName = tokens[6].Trim('\"');
										//	GaussPointsInfo locationInfo;
										//	if (gaussPointsDescriptions.TryGetValue(locationName, out locationInfo))
										//		currentDataInfo.LocationInfo = locationInfo;
										//}
									}
									else
										throw new FormatException("Result block is not complete.");
								}
								break;
							case ParserState.GaussPointsDescription:
								{
									//Debug.Assert(currentGaussPointsInfo != null);
									//string[] tokens = splitLineToTokens(line);

									//if (line.StartsWith("Number of Gauss Points:", StringComparison.InvariantCultureIgnoreCase))
									//{
									//	int number = parseInteger(tokens[4]);
									//	Debug.Assert(currentGaussPointsInfo.IsAllowedGaussPointsNumber(number));
									//	currentGaussPointsInfo.SetGaussPointsNumber(number);
									//}
									//else if (line.StartsWith("Nodes", StringComparison.InvariantCultureIgnoreCase))
									//{
									//	if (string.Equals(tokens[1], "included", StringComparison.InvariantCultureIgnoreCase))
									//	{
									//		currentGaussPointsInfo.NodesIncluded = true;
									//	}
									//	else
									//	{
									//		Debug.Assert(string.Equals(tokens[1], "not", StringComparison.InvariantCultureIgnoreCase));
									//		Debug.Assert(string.Equals(tokens[2], "included", StringComparison.InvariantCultureIgnoreCase));

									//		currentGaussPointsInfo.NodesIncluded = false;
									//	}
									//}
									//else if (line.StartsWith("Natural Coordinates:", StringComparison.InvariantCultureIgnoreCase))
									//{
									//	switch (tokens[2].ToLower())
									//	{
									//		case "internal":
									//			currentGaussPointsInfo.SetInternalNaturalCoordinates();
									//			break;
									//		case "given":
									//			state = ParserState.GaussPointsGivenNaturalCoordinates;
									//			currentGaussPointIndex = 0;
									//			break;
									//	}
									//}
									//else if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase))
									//{
									//	Debug.Assert(string.Equals(tokens[1], GaussPointsToken, StringComparison.InvariantCultureIgnoreCase));
									//	state = ParserState.Init; // back to initial state
									//}
								}
								break;
							case ParserState.GaussPointsGivenNaturalCoordinates:
								{
									//Debug.Assert(currentGaussPointsInfo != null);
									//if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase))
									//{
									//	state = ParserState.Init; // back to initial state
									//}
									//else
									//{
									//	string[] tokens = splitLineToTokens(line);
									//	for (int i = 0; i < tokens.Length; i++)
									//	{
									//		currentGaussPointsInfo.SetNaturalCoordinate(currentGaussPointIndex, i, ParseFloat64(tokens[i]));
									//	}
									//	++currentGaussPointIndex;
									//}
								}
								break;
							case ParserState.ResultHeader:
								if (line.StartsWith(ComponentNamesToken, StringComparison.InvariantCultureIgnoreCase)) // ComponentNames
								{
									//Debug.Assert(currentDataInfo != null);
									//if (currentDataInfo != null)
									//{
									//	string[] tokens = splitLineToTokens(line);
									//	currentDataInfo.DataType.SetComponents(tokens.Skip(1).Select(name => name.Trim('\"')).ToArray());
									//}
								}
								else if (line.StartsWith(ValuesToken, StringComparison.InvariantCultureIgnoreCase)) // Values
								{
									state = ParserState.ResultValues;
									//Debug.Assert(currentDataInfo != null);
									//if (currentDataInfo != null && currentDataInfo.DataType.ComponentCount == 0)
									//{
									//	// component names are not specified, add generic ones
									//	currentDataInfo.DataType.AddGenericComponentNames();
									//}
									//return currentDataInfo;
									currentDataValues = new List<double>();
								}
								break;
							case ParserState.ResultValues:
								if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase)) // End values
								{
									Debug.Assert(line.Substring(EndToken.Length).TrimStart().StartsWith(ValuesToken, StringComparison.InvariantCultureIgnoreCase));
									state = ParserState.Init;

									currentDataDescription.Data = currentDataValues.ToArray();
									yield return currentDataDescription;
								}
								else
								{
									//switch (location)
									//{
									//	default:
									//		break;
									//}

									string[] tokens = splitLineToTokens(line);
									Debug.Assert(tokens.Length >= 1);
									int nodeId = ParseInt32(tokens[0]);
									currentDataValues.AddRange(tokens.Skip(1).Select(token => ParseFloat64(token)));
								}
								break;
						}
					}
				}
			}
		}

		#endregion

		#region Private methods

		private static string[] splitLineToTokens(string line)
		{
			Debug.Assert(line != null);
			// TODO: parse correctly quoted tokens (enclosed by '"' characters)
			return line.Split(whiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
		}

		#endregion
	}
}

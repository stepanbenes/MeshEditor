using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
		private static readonly string tokensWithQuotesRegexPattern = @"[\""].+?[\""]|[^ ]+";
		private static readonly char[] quotesTrimChars = { '"' };

		private enum ParserState
		{
			Init = 0,
			GaussPointsDescription,
			GaussPointsGivenNaturalCoordinates,
			ResultHeader,
			ResultValues,
			EOF
		}

		private enum FileDataLocation
		{
			Unknown = 0,
			Nodes,
			GaussPoints
		}

		private class ParserData
		{
			public string Name { get; set; }
			public double? TimeStep { get; set; }
			public FieldType? FieldType { get; set; }
			public string LocationType { get; set; }
			public int? NumberOfComponents { get; set; }
			public string[] ComponentNames { get; set; }
			public List<double> DataValues { get; set; }
			public List<int> Ids { get; set; }
			public string ResultTypeString { get; set; }
			public FileDataLocation? Location { get; set; }

			public DataDescription CreateDataDescription(GeometryDescription geometry)
			{
				string[] finalComponentNames = ComponentNames ?? createGenericComponentNames(ResultTypeString);
				DataDescription data = new DataDescription
				{
					Name = Name,
					TimeStep = TimeStep,
					ComponentNames = finalComponentNames,
					FieldType = FieldType.Value,
					//LocationType,
					NumberOfComponents = NumberOfComponents ?? finalComponentNames.Length
				};

				Debug.Assert(data.NumberOfComponents > 0);

				data.Data = DataValues.ToArray();

				// TODO: place values to appropriate position according to PointIdIndexMap resp. CellIdIndexMap (depending on nodes or gauss-points data location)
				// TODO: do extrapolation if Location is Gauss-points
				// TODO: set LocationType and Data properties

				return data;
			}
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

		public IEnumerable<DataDescription> ReadData(GeometryDescription correspondingGeometry)
		{
			// TODO: replace asserts with throws

			foreach (string filename in filenames)
			{
				using (Stream fileStream = storageService.Load(filename))
				using (TextReader reader = new StreamReader(fileStream))
				{
					ParserState state = ParserState.Init;
					ParserData parserData = null;
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
									string[] tokens = splitLineToTokensWithQuotes(line);
									Debug.Assert(tokens.Length >= 6);
									// Result "result name" "analysis name" step_value my_result_type my_location "location name"
									if (tokens.Length >= 6)
									{
										parserData = new ParserData();

										parserData.ResultTypeString = tokens[4];
										parserData.Name = tokens[1];
										parserData.TimeStep = ParseFloat64(tokens[3]);
										parserData.FieldType = convertResultTypeStringToFieldType(parserData.ResultTypeString);
										parserData.Location = convertLocationStringToDataLocation(tokens[5]);

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
									string[] tokens = splitLineToTokensWithQuotes(line);
									parserData.ComponentNames = tokens.Skip(1).ToArray();
								}
								else if (line.StartsWith(ValuesToken, StringComparison.InvariantCultureIgnoreCase)) // Values
								{
									state = ParserState.ResultValues;
									parserData.Ids = new List<int>();
									parserData.DataValues = new List<double>();
								}
								break;
							case ParserState.ResultValues:
								if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase)) // End values
								{
									Debug.Assert(line.Substring(EndToken.Length).TrimStart().StartsWith(ValuesToken, StringComparison.InvariantCultureIgnoreCase));
									state = ParserState.Init;


									var dataDescription = parserData.CreateDataDescription(correspondingGeometry);
									yield return dataDescription;

									parserData = null;
								}
								else
								{
									string[] tokens = splitLineToTokens(line);
									Debug.Assert(tokens.Length >= 1);

									if (parserData.NumberOfComponents == null)
										parserData.NumberOfComponents = tokens.Length - 1;

									Debug.Assert(parserData.NumberOfComponents == tokens.Length - 1); // fill in missing values?

									// save id (point's or element's) to list, it will be useful after reading all data in this block
									int id = ParseInt32(tokens[0]);
									parserData.Ids.Add(id);
									parserData.DataValues.AddRange(tokens.Skip(1).Select(token => ParseFloat64(token)));
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
			return line.Split(whiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
		}

		private static string[] splitLineToTokensWithQuotes(string line)
		{
			// parse correctly quoted tokens (enclosed by '"' characters)
			return Regex.Matches(line, tokensWithQuotesRegexPattern)
				.Cast<Match>()
				.Select(m => m.Value.Trim(quotesTrimChars))
				.ToArray();
		}

		private static FieldType convertResultTypeStringToFieldType(string resultType)
		{
			Debug.Assert(resultType != null);
			switch (resultType.ToLower())
			{
				case "scalar":
					return FieldType.Scalar;
				case "vector":
					return FieldType.Vector;
				case "matrix":
				case "plaindeformationmatrix":
				case "mainmatrix":
				case "localaxes":
					return FieldType.Tensor;
				default:
					throw new NotSupportedException($"'{resultType}' result type is not supported.");
			}
		}

		private static string[] createGenericComponentNames(string resultType)
		{
			Debug.Assert(resultType != null);
			switch (resultType.ToLower())
			{
				case "scalar":
					return new[] { "value" };
				case "vector":
					return new[] { "X", "Y", "Z" }; // optional fourth component signed_module_value !!
				case "matrix":
					return new[] { "Sxx", "Syy", "Szz", "Sxy", "Syz", "Sxz" }; // in 2D only four components !!
				case "plaindeformationmatrix":
					return new[] { "Sxx", "Syy", "Sxy", "Szz" };
				case "mainmatrix":
					return new[] { "Si", "Sii", "Siii", "Vix", "Viy", "Viz", "Viix", "Viiy", "Viiz", "Viiix", "Viiiy", "Viiiz" };
				case "localaxes":
					return new[] { "euler_ang_1", "euler_ang_2", "euler_ang_3" };
				default:
					throw new FormatException($"'{resultType}' result type is not supported.");
			}
		}

		private FileDataLocation convertLocationStringToDataLocation(string locationString)
		{
			Debug.Assert(locationString != null);
			switch (locationString.ToLower())
			{
				case "onnodes": // OnNodes
					return FileDataLocation.Nodes;
				case "ongausspoints": // OnGaussPoints
					return FileDataLocation.GaussPoints;
				default:
					throw new FormatException($"Unknown data location ({locationString})");
			}
		}

		#endregion
	}
}

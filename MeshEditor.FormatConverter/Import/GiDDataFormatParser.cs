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
					string currentResultTypeString = null;
					FileDataLocation currentLocation = FileDataLocation.Unknown;

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
										currentResultTypeString = tokens[4];

										currentDataDescription = new DataDescription
										{
											Name = tokens[1],
											TimeStep = ParseFloat64(tokens[3]),
											FieldType = convertResultTypeStringToFieldType(currentResultTypeString),
											//LocationType =
										};

										currentLocation = convertLocationStringToDataLocation(tokens[5]);

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
									currentDataDescription.ComponentNames = tokens.Skip(1).ToArray();

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

									if (currentDataDescription.ComponentNames == null)
										currentDataDescription.ComponentNames = createGenericComponentNames(currentResultTypeString);
									if (currentDataDescription.NumberOfComponents == 0)
										currentDataDescription.NumberOfComponents = currentDataDescription.ComponentNames?.Length ?? 0;

									Debug.Assert(currentDataDescription.NumberOfComponents > 0);

									yield return currentDataDescription;

									currentDataDescription = null;
									currentDataValues = null;
									currentResultTypeString = null;
									currentLocation = FileDataLocation.Unknown;
								}
								else
								{
									string[] tokens = splitLineToTokens(line);
									Debug.Assert(tokens.Length >= 1);

									if (currentDataDescription.NumberOfComponents == 0)
										currentDataDescription.NumberOfComponents = tokens.Length - 1;

									Debug.Assert(currentDataDescription.NumberOfComponents == tokens.Length - 1); // fill in missing values?

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
			string[] names = null;
			switch (resultType.ToLower())
			{
				case "scalar":
					names = new[] { "value" };
					break;
				case "vector":
					names = new[] { "X", "Y", "Z" }; // optional fourth component signed_module_value !!
					break;
				case "matrix":
					names = new[] { "Sxx", "Syy", "Szz", "Sxy", "Syz", "Sxz" }; // in 2D only four components !!
					break;
				case "plaindeformationmatrix":
					names = new[] { "Sxx", "Syy", "Sxy", "Szz" };
					break;
				case "mainmatrix":
					names = new[] { "Si", "Sii", "Siii", "Vix", "Viy", "Viz", "Viix", "Viiy", "Viiz", "Viiix", "Viiiy", "Viiiz" };
					break;
				case "localaxes":
					names = new[] { "euler_ang_1", "euler_ang_2", "euler_ang_3" };
					break;
				default:
					break;
			}
			return names;
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

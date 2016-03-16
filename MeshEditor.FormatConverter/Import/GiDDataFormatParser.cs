using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.FormatConverter.Import
{
	partial class GiDDataFormatParser : FormatParserBase, IDataImportService
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
			public int LineCounter { get; set; }

			public string Name { get; set; }
			public double? TimeStep { get; set; }
			public FieldType? FieldType { get; set; }

			public int? NumberOfComponents { get; set; }
			public string[] ComponentNames { get; set; }
			public List<double> DataValues { get; set; }
			public List<int> Ids { get; set; }
			public string ResultTypeString { get; set; }
			public FileDataLocation? Location { get; set; }

			public string LocationName { get; set; }

			public IDictionary<string, GaussPointsInfo> GaussPointsDescriptions { get; } = new Dictionary<string, GaussPointsInfo>();

			public DataDescription CreateDataDescription(GeometryDescription geometry)
			{
				DataLocationType targetDataLocation = (Location == FileDataLocation.GaussPoints) ? DataLocationType.Cells : DataLocationType.Points; /**/

				string[] finalComponentNames = ComponentNames ?? createGenericComponentNames(ResultTypeString);
				DataDescription data = new DataDescription
				{
					Name = Name,
					TimeStep = TimeStep,
					ComponentNames = finalComponentNames,
					FieldType = FieldType.Value,
					Location = targetDataLocation,
					NumberOfComponents = NumberOfComponents ?? finalComponentNames.Length
				};

				data.Data = convertValues
				(
					DataValues,
					Ids,
					data.NumberOfComponents,
					geometry,
					targetDataLocation,
					Location.Value,
					(Location == FileDataLocation.GaussPoints) ? GaussPointsDescriptions[LocationName] : null
				);

				return data;
			}

			public void ClearResultBlockData()
			{
				LineCounter = 0;
				Name = null;
				TimeStep = null;
				FieldType = null;
				NumberOfComponents = null;
				ComponentNames = null;
				DataValues = null;
				Ids = null;
				ResultTypeString = null;
				Location = null;
				LocationName = null;
			}
		}

		#endregion

		#region Fields, Constructor

		IStorageService storageService;
		IEnumerable<Uri> uris;

		public GiDDataFormatParser(IStorageService storageService, IEnumerable<Uri> uris)
		{
			this.storageService = storageService;
			this.uris = uris;
		}

		#endregion

		#region Public methods

		public IEnumerable<DataDescription> ReadData(GeometryDescription correspondingGeometry)
		{
			// TODO: replace asserts with throws

			foreach (Uri uri in uris)
			{
				using (Stream fileStream = storageService.Load(uri))
				using (TextReader reader = new StreamReader(fileStream))
				{
					ParserState state = ParserState.Init;
					ParserData parserData = new ParserData();
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
									state = ParserState.GaussPointsDescription;
									string[] tokens = splitLineToTokensWithQuotes(line);
									Debug.Assert(tokens.Length >= 4);
									// GaussPoints "gauss_points_name" Elemtype my_type "mesh_name"
									string gaussPointsName = tokens[1];
									Debug.Assert(string.Equals(tokens[2], "Elemtype", StringComparison.InvariantCultureIgnoreCase));

									parserData.LocationName = gaussPointsName;
									parserData.GaussPointsDescriptions[gaussPointsName] = new GaussPointsInfo(gaussPointsName, tokens[3], (tokens.Length >= 5) ? tokens[4] : null);
								}
								else if (line.StartsWith(ResultToken, StringComparison.InvariantCultureIgnoreCase)) // Result
								{
									state = ParserState.ResultHeader;
									string[] tokens = splitLineToTokensWithQuotes(line);
									Debug.Assert(tokens.Length >= 6);
									// Result "result name" "analysis name" step_value my_result_type my_location "location name"
									if (tokens.Length >= 6)
									{
										parserData.ResultTypeString = tokens[4];
										parserData.Name = tokens[1];
										parserData.TimeStep = ParseFloat64(tokens[3]);
										parserData.FieldType = convertResultTypeStringToFieldType(parserData.ResultTypeString);
										parserData.Location = convertLocationStringToDataLocation(tokens[5]);

										if (tokens.Length >= 7) // location name
										{
											parserData.LocationName = tokens[6];
										}
									}
									else
										throw new FormatException("Result block is not complete.");
								}
								break;
							case ParserState.GaussPointsDescription:
								{
									Debug.Assert(parserData.GaussPointsDescriptions.ContainsKey(parserData.LocationName));
									var gpDescription = parserData.GaussPointsDescriptions[parserData.LocationName];
									string[] tokens = splitLineToTokens(line);

									if (line.StartsWith("Number of Gauss Points:", StringComparison.InvariantCultureIgnoreCase))
									{
										int numberOfGaussPoints = ParseInt32(tokens[4]);
										gpDescription.NumberOfGaussPoints = numberOfGaussPoints;
									}
									else if (line.StartsWith("Nodes", StringComparison.InvariantCultureIgnoreCase))
									{
										if (string.Equals(tokens[1], "included", StringComparison.InvariantCultureIgnoreCase))
										{
											gpDescription.NodesIncluded = true;
										}
										else
										{
											Debug.Assert(string.Equals(tokens[1], "not", StringComparison.InvariantCultureIgnoreCase));
											Debug.Assert(string.Equals(tokens[2], "included", StringComparison.InvariantCultureIgnoreCase));

											gpDescription.NodesIncluded = false;
										}
									}
									else if (line.StartsWith("Natural Coordinates:", StringComparison.InvariantCultureIgnoreCase))
									{
										switch (tokens[2].ToLower())
										{
											case "internal":
												gpDescription.NaturalCoordinatesType = GaussPointsInfo.NaturalCoordinatesTypes.Internal;
												gpDescription.SetInternalNaturalCoordinates();
												break;
											case "given":
												gpDescription.NaturalCoordinatesType = GaussPointsInfo.NaturalCoordinatesTypes.Given;
												state = ParserState.GaussPointsGivenNaturalCoordinates;
												break;
										}
									}
									else if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase))
									{
										Debug.Assert(string.Equals(tokens[1], GaussPointsToken, StringComparison.InvariantCultureIgnoreCase));
										state = ParserState.Init; // back to initial state
										parserData.LocationName = null;
									}
								}
								break;
							case ParserState.GaussPointsGivenNaturalCoordinates:
								{
									Debug.Assert(parserData.GaussPointsDescriptions.ContainsKey(parserData.LocationName));
									var gpDescription = parserData.GaussPointsDescriptions[parserData.LocationName];
									if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase))
									{
										state = ParserState.Init; // back to initial state
										parserData.LocationName = null;
									}
									else
									{
										string[] tokens = splitLineToTokens(line);
										gpDescription.AddNaturalCoordinates(tokens.Select(token => ParseFloat64(token)).ToArray());
									}
								}
								break;
							case ParserState.ResultHeader:
								if (line.StartsWith(ComponentNamesToken, StringComparison.InvariantCultureIgnoreCase)) // ComponentNames
								{
									string[] tokens = splitLineToTokensWithQuotes(line);
									parserData.ComponentNames = tokens.Skip(1).ToArray();
									parserData.NumberOfComponents = parserData.ComponentNames.Length;
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

									parserData.ClearResultBlockData();
								}
								else
								{
									string[] tokens = splitLineToTokens(line);
									Debug.Assert(tokens.Length >= 1);

									if (parserData.NumberOfComponents == null)
										parserData.NumberOfComponents = tokens.Length - 1;

									// save id (point's or element's) to list, it will be useful after reading all data in this block

									int numberOfLinesPerRecord = parserData.LocationName == null ? 1 : parserData.GaussPointsDescriptions[parserData.LocationName].NumberOfGaussPoints;
									if (parserData.LineCounter % numberOfLinesPerRecord == 0)
									{
										int id = ParseInt32(tokens[0]);
										parserData.Ids.Add(id);
										parserData.DataValues.AddRange(tokens.Skip(1).Select(token => ParseFloat64(token)).Concat(zeroes((parserData.NumberOfComponents ?? 0) - (tokens.Length - 1)))); // fill in missing values
									}
									else
									{
										parserData.DataValues.AddRange(tokens.Select(token => ParseFloat64(token)).Concat(zeroes((parserData.NumberOfComponents ?? 0) - tokens.Length))); // fill in missing values
									}
								}
								break;
						}
					}
				}
			}
		}

		private static IEnumerable<double> zeroes(int count)
		{
			if (count <= 0)
				return Enumerable.Empty<double>();
			return Enumerable.Repeat(0.0, count);
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

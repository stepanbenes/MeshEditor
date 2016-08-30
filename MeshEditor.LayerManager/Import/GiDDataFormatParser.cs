using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Storage;
using MeshEditor.LayerManager.Common;

namespace MeshEditor.LayerManager.Import
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
			Nodes,
			GaussPoints
		}

		private class ParsedField
		{
			public int LineCounter { get; set; }

			public string FieldName { get; set; }

			public double TimeStep { get; set; }

			public int NumberOfComponents => ComponentNames?.Length ?? 0;

			public string[] ComponentNames { get; set; }

			public List<double> DataValues { get; set; } = new List<double>();

			public List<int> Ids { get; set; } = new List<int>();

			public string ResultTypeString { get; set; }

			public FileDataLocation? Location { get; set; }

			public GaussPointsInfo GaussPointsDescription { get; set; }

			public bool IsMergeableWith(ParsedField other)
			{
				Debug.Assert(other != null);
				return
					FieldName != null &&
					FieldName.Equals(other.FieldName) &&
					TimeStep.Equals(other.TimeStep) &&
					Equals(ResultTypeString, other.ResultTypeString);
			}

			private static DataLocationType chooseCommonTargetDataLocationFor(IReadOnlyCollection<ParsedField> fields)
			{
				Debug.Assert(fields.Count > 0);
				Debug.Assert(fields.All(f => f.Location.HasValue));

				FileDataLocation fileDataLocation = fields.First().Location.Value;

				if (fields.Any(f => f.Location != fileDataLocation))
				{
					throw new NotSupportedException($"Following data field does not have the same data location type for all sub-mesh: {fields.First().FieldName} (time step: {fields.First().TimeStep}).");
				}

				switch (fileDataLocation)
				{
					case FileDataLocation.Nodes:
						return DataLocationType.Points;
					case FileDataLocation.GaussPoints:
						if (fields.All(f => f.GaussPointsDescription.NumberOfGaussPoints == 1))
							return DataLocationType.Cells;
						return DataLocationType.CellPoints;
					default:
						throw new NotSupportedException();
				}
			}

			/// <summary>
			/// Merges multiple data corresponing to same field and timestep together
			/// </summary>
			public static FieldDataDescription CreateMergedDataDescription(IReadOnlyCollection<ParsedField> fields, GeometryDescription geometry, GaussPointsExtrapolationStrategy gaussPointsExtrapolationStrategy)
			{
				// NOTE: fields is Stack<T>, so it is in reversed order!
				Debug.Assert(fields.Count > 0);

				DataLocationType targetDataLocation = chooseCommonTargetDataLocationFor(fields);

				var firstParsedField = fields.First();

				FieldDataDescription mergedField = new FieldDataDescription
				{
					FieldName = firstParsedField.FieldName,
					TimeStep = firstParsedField.TimeStep,
					ComponentNames = firstParsedField.ComponentNames,
					FieldType = convertResultTypeStringToFieldType(firstParsedField.ResultTypeString),
					Location = targetDataLocation,
				};

				double[] resultValues = createEmptyValueArray(geometry, targetDataLocation, firstParsedField.NumberOfComponents);

				foreach (var field in fields)
				{
					convertValues(
						field.DataValues,
						field.Ids,
						field.NumberOfComponents,
						geometry,
						targetDataLocation,
						field.Location.Value,
						field.GaussPointsDescription,
						gaussPointsExtrapolationStrategy,
						resultValues
					);
				}

				mergedField.Values = resultValues;

				return mergedField;

				//return fields.First().createDataDescription(geometry);
			}
		}

		#endregion

		#region Fields, Constructor

		IReadStorageService storageService;
		IEnumerable<string> recordNames;
		GaussPointsExtrapolationStrategy gaussPointsExtrapolationStrategy;

		public GiDDataFormatParser(IReadStorageService storageService, IEnumerable<string> recordNames, GaussPointsExtrapolationStrategy gaussPointsExtrapolationStrategy)
		{
			this.storageService = storageService;
			this.recordNames = recordNames;
			this.gaussPointsExtrapolationStrategy = gaussPointsExtrapolationStrategy;
		}

		#endregion

		#region Public methods

		public IEnumerable<FieldDataDescription> ReadData(GeometryDescription correspondingGeometry)
		{
			// TODO: replace asserts with throws

			foreach (string recordName in recordNames)
			{
				using (Stream fileStream = storageService.Load(recordName))
				using (TextReader reader = new StreamReader(fileStream))
				{
					ParserState state = ParserState.Init;
					Stack<ParsedField> parsedFieldsStack = new Stack<ParsedField>();

					IDictionary<string, GaussPointsInfo> gaussPointsDescriptions = new Dictionary<string, GaussPointsInfo>();
					GaussPointsInfo currentGaussPointsDescription = null;

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
									string[] tokens = line.SplitToTokensWithQuotes();
									Debug.Assert(tokens.Length >= 4);
									// GaussPoints "gauss_points_name" Elemtype my_type "mesh_name"
									string gaussPointsName = tokens[1];
									Debug.Assert(string.Equals(tokens[2], "Elemtype", StringComparison.InvariantCultureIgnoreCase));

									currentGaussPointsDescription = gaussPointsDescriptions[gaussPointsName] = new GaussPointsInfo(gaussPointsName, tokens[3], (tokens.Length >= 5) ? tokens[4] : null);
								}
								else if (line.StartsWith(ResultToken, StringComparison.InvariantCultureIgnoreCase)) // Result
								{
									var newParsedField = new ParsedField();

									state = ParserState.ResultHeader;
									string[] tokens = line.SplitToTokensWithQuotes();
									Debug.Assert(tokens.Length >= 6);
									// Result "result name" "analysis name" step_value my_result_type my_location "location name"
									if (tokens.Length >= 6)
									{
										newParsedField.FieldName = tokens[1];
										// "analysis name": ignored
										newParsedField.TimeStep = ParseFloat64(tokens[3]);
										newParsedField.ResultTypeString = tokens[4];
										newParsedField.ComponentNames = createGenericComponentNames(tokens[4]);
										newParsedField.Location = convertLocationStringToDataLocation(tokens[5]);

										if (tokens.Length >= 7) // location name
										{
											Debug.Assert(gaussPointsDescriptions.ContainsKey(tokens[6]));
											newParsedField.GaussPointsDescription = gaussPointsDescriptions[tokens[6]];
										}
									}
									else
										throw new FormatException("Result block is not complete.");

									if (parsedFieldsStack.Count > 0 && !parsedFieldsStack.Peek().IsMergeableWith(newParsedField))
									{
										// yield one or merge all accumulated fields
										yield return ParsedField.CreateMergedDataDescription(parsedFieldsStack, correspondingGeometry, gaussPointsExtrapolationStrategy);
										parsedFieldsStack.Clear();
									}

									parsedFieldsStack.Push(newParsedField);

								}
								break;
							case ParserState.GaussPointsDescription:
								{
									Debug.Assert(currentGaussPointsDescription != null);
									string[] tokens = splitLineToTokens(line);

									if (line.StartsWith("Number of Gauss Points:", StringComparison.InvariantCultureIgnoreCase))
									{
										int numberOfGaussPoints = ParseInt32(tokens[4]);
										currentGaussPointsDescription.NumberOfGaussPoints = numberOfGaussPoints;
									}
									else if (line.StartsWith("Nodes", StringComparison.InvariantCultureIgnoreCase))
									{
										if (string.Equals(tokens[1], "included", StringComparison.InvariantCultureIgnoreCase))
										{
											currentGaussPointsDescription.NodesIncluded = true;
										}
										else
										{
											Debug.Assert(string.Equals(tokens[1], "not", StringComparison.InvariantCultureIgnoreCase));
											Debug.Assert(string.Equals(tokens[2], "included", StringComparison.InvariantCultureIgnoreCase));

											currentGaussPointsDescription.NodesIncluded = false;
										}
									}
									else if (line.StartsWith("Natural Coordinates:", StringComparison.InvariantCultureIgnoreCase))
									{
										switch (tokens[2].ToLower())
										{
											case "internal":
												currentGaussPointsDescription.NaturalCoordinatesType = GaussPointsInfo.NaturalCoordinatesTypes.Internal;
												currentGaussPointsDescription.SetInternalNaturalCoordinates();
												break;
											case "given":
												currentGaussPointsDescription.NaturalCoordinatesType = GaussPointsInfo.NaturalCoordinatesTypes.Given;
												state = ParserState.GaussPointsGivenNaturalCoordinates;
												break;
											default:
												throw new FormatException("Unknown natural coordinates type.");
										}
									}
									else if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase))
									{
										Debug.Assert(string.Equals(tokens[1], GaussPointsToken, StringComparison.InvariantCultureIgnoreCase));
										state = ParserState.Init; // back to initial state
										currentGaussPointsDescription = null;
									}
								}
								break;
							case ParserState.GaussPointsGivenNaturalCoordinates:
								{
									Debug.Assert(currentGaussPointsDescription != null);
									if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase))
									{
										state = ParserState.Init; // back to initial state
										currentGaussPointsDescription = null;
									}
									else
									{
										string[] tokens = splitLineToTokens(line);
										currentGaussPointsDescription.AddNaturalCoordinates(tokens.Select(token => ParseFloat64(token)).ToArray());
									}
								}
								break;
							case ParserState.ResultHeader:
								if (line.StartsWith(ComponentNamesToken, StringComparison.InvariantCultureIgnoreCase)) // ComponentNames
								{
									string[] tokens = line.SplitToTokensWithQuotes();
									parsedFieldsStack.Peek().ComponentNames = tokens.Skip(1).ToArray();
								}
								else if (line.StartsWith(ValuesToken, StringComparison.InvariantCultureIgnoreCase)) // Values
								{
									state = ParserState.ResultValues;
								}
								break;
							case ParserState.ResultValues:
								if (line.StartsWith(EndToken, StringComparison.InvariantCultureIgnoreCase)) // End values
								{
									Debug.Assert(line.Substring(EndToken.Length).TrimStart().StartsWith(ValuesToken, StringComparison.InvariantCultureIgnoreCase));
									state = ParserState.Init;
								}
								else
								{
									string[] tokens = splitLineToTokens(line);
									Debug.Assert(tokens.Length >= 1);

									var parsedField = parsedFieldsStack.Peek();

									// save id (point's or element's) to list, it will be useful after reading all data in this block

									int numberOfLinesPerRecord = parsedField.GaussPointsDescription?.NumberOfGaussPoints ?? 1;
									if (parsedField.LineCounter % numberOfLinesPerRecord == 0)
									{
										int id = ParseInt32(tokens[0]);
										parsedField.Ids.Add(id);
										parsedField.DataValues.AddRange(tokens.Skip(1).Select(token => ParseFloat64(token)).Concat(zeroes(parsedField.NumberOfComponents - (tokens.Length - 1)))); // fill in missing values
									}
									else
									{
										parsedField.DataValues.AddRange(tokens.Select(token => ParseFloat64(token)).Concat(zeroes(parsedField.NumberOfComponents - tokens.Length))); // fill in missing values
									}
									parsedField.LineCounter++;
								}
								break;
						} // state switch
					} // end of file loop

					// yield one or merge all remaining accumulated fields
					yield return ParsedField.CreateMergedDataDescription(parsedFieldsStack, correspondingGeometry, gaussPointsExtrapolationStrategy);
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
					return new[] { "X", "Y", "Z" }; // WARNING: optional fourth component signed_module_value !!
				case "matrix":
					return new[] { "Sxx", "Syy", "Szz", "Sxy", "Syz", "Sxz" }; // WARNING: in 2D only four components !!
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

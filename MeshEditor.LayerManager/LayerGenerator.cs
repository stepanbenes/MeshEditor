using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Encoding;
using MeshEditor.LayerManager.Filters;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;
using MeshEditor.LayerManager.Common;
using MeshEditor.LayerManager.MeshFiltering;

namespace MeshEditor.LayerManager
{
	public class LayerGenerator
	{
		#region Static members

		private static readonly CellType DefaultCellType = CellType.TriangleLinear;

		private class CompressionCounter
		{
			long inputDataLength;
			long compressedDataLength;
			long encodedDataLength;

			public void Increment(CompressionParameters compression, EncodingParameters encoding)
			{
				inputDataLength += (long)compression.Rows * compression.Columns;
				compressedDataLength += encoding.OriginalLength;
				encodedDataLength += encoding.Length;
			}

			public double GetCompressionFactor()
			{
				return (double)compressedDataLength / inputDataLength;
			}

			public double GetEncodingFactor()
			{
				return (double)encodedDataLength / compressedDataLength;
			}

			public double GetOverallFactor()
			{
				return (double)encodedDataLength / inputDataLength;
			}

			public long GetMemoryConsumption()
			{
				return encodedDataLength * sizeof(double);
			}

			public override string ToString()
			{
				StringBuilder text = new StringBuilder();
				text.AppendLine("| OVERVIEW:");
				text.Append("| compression factor: ");
				text.AppendLine(GetCompressionFactor().ToString());
				text.Append("| encoding factor: ");
				text.AppendLine(GetEncodingFactor().ToString());
				text.Append("| overall factor: ");
				text.AppendLine(GetOverallFactor().ToString());
				text.Append("| memory consumption: ");
				text.Append(GetMemoryConsumption().ToString());
				text.Append(" bytes");
				return text.ToString();
			}
		}

		#endregion

		#region Fields, constructor

		IReadStorageService sourceStorage;
		IWriteStorageService destinationStorage;
		ISerializationService serializationService;
		ICompressionService compressionService;
		IEncodingService encodingService;
		IProgress<OperationState> progressReporter;

		public LayerGenerator(
			IReadStorageService sourceStorage,
			IWriteStorageService destinationStorage,
			ISerializationService serializationService = null,
			ICompressionService compressionService = null,
			IEncodingService encodingService = null,
			IProgress<OperationState> progressReporter = null)
		{
			this.sourceStorage = sourceStorage;
			this.destinationStorage = destinationStorage;
			this.serializationService = serializationService ?? new JsonSerializationService();
			this.compressionService = compressionService ?? new TransparentCompressionService();
			this.encodingService = encodingService ?? new Base64EncodingService();
			this.progressReporter = progressReporter;
		}

		#endregion

		#region Public methods

		#region Console app Entry points

		public SummaryLayerFile GenerateMasterLayer(string layerName, IGeometryImportService geometryImportService, IDataImportService dataImportService = null)
		{
			if (geometryImportService == null)
			{
				throw new ArgumentNullException(nameof(geometryImportService));
			}

			IReadOnlyList<AttributeDescription> attributeDescriptions;
			GeometryDescription geometry = geometryImportService.ReadGeometry(out attributeDescriptions);
			IEnumerable<DataDescription> dataDescriptions = dataImportService?.ReadData(geometry) ?? Enumerable.Empty<DataDescription>();
			SummaryLayerFile layerFile = generateLayerFiles(layerName, null, geometry, attributeDescriptions, dataDescriptions.Select(d => new[] { d }), filter: null);
			return layerFile;
		}

		public SummaryLayerFile GenerateFilterLayer(Guid parentLayerId, Filter filter, string layerName = null)
		{
			// find parentLayer in storage and download summary
			SummaryLayerFile parentLayer;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(parentLayerId)))
			{
				parentLayer = serializationService.Deserialize<SummaryLayerFile>(stream);
			}

			IMeshFilterCreator meshFilterCreator;
			string filterLayerName;

			switch (filter.Type)
			{
				case FilterType.Surface:
					{
						meshFilterCreator = new MeshSurfaceCreator((SurfaceFilter)filter);
						filterLayerName = layerName ?? "surface";
					}
					break;
				case FilterType.Slice:
					{
						var sliceFilter = (SliceFilter)filter;
						meshFilterCreator = new MeshSliceCreator(sliceFilter);
						filterLayerName = layerName ?? $"slice {sliceFilter.Offset}"; // TODO: use better name
					}
					break;
				case FilterType.Clip:
				// TODO: use MeshSliceGenerator
				case FilterType.IsoSurface:
				case FilterType.StreamLines:
					throw new NotImplementedException();
				case FilterType.AttributeSelection:
					{
						var attributeSelectionFilter = (AttributeSelectionFilter)filter;
						var attributeDescriptor = parentLayer.Attributes.Single(a => a.FieldName == attributeSelectionFilter.AttributeName);
						var attribute = LoadAttribute(getLayerAttributeRecordName(parentLayerId, attributeDescriptor.Index));
						meshFilterCreator = new MeshPartitionCreator(attributeSelectionFilter, attribute);
						filterLayerName = layerName ?? $"{attributeSelectionFilter.AttributeName}: {string.Join(", ", attributeSelectionFilter.AttributeSelection)}";
					}
					break;
				default:
					throw new NotSupportedException();
			}

			GeometryDescription originalGeometry = LoadGeometry(getLayerMeshRecordName(parentLayerId));
			GeometryDescription filteredGeometry = meshFilterCreator.Create(originalGeometry);

			// filter attributes
			var originalAttributeRecordNames = parentLayer.Attributes.Select(a => getLayerAttributeRecordName(parentLayerId, a.Index));
			IEnumerable<AttributeDescription> filteredAttributeDescriptions = filterAttributesByGeometry(filteredGeometry, originalAttributeRecordNames);

			// filter results
			var originalResultRecordNames = parentLayer.Results.Select(r => getLayerResultRecordName(parentLayerId, r.Index));
			IEnumerable<DataDescription> filteredDataDescriptions = filterDataByGeometry(filteredGeometry, originalResultRecordNames);

			return generateLayerFiles(filterLayerName, parentLayerId, filteredGeometry, filteredAttributeDescriptions, filteredDataDescriptions.Select(d => new[] { d }), filter);
		}

		public SummaryLayerFile CompressLayer(Guid layerId, string layerName = null, string fieldName = null, string componentName = null)
		{
			// find layer in storage and download summary
			SummaryLayerFile layerSummary;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(layerId)))
			{
				layerSummary = serializationService.Deserialize<SummaryLayerFile>(stream);
			}

			GeometryDescription geometry = LoadGeometry(getLayerMeshRecordName(layerId));
			var attributeRecordNames = layerSummary.Attributes.Select(a => getLayerAttributeRecordName(layerId, a.Index));
			IEnumerable<AttributeDescription> attributeDescriptions = attributeRecordNames.Select(record => LoadAttribute(record));
			Filter filter = new TimeCompressionFilter { FieldName = fieldName, ComponentName = componentName };

			var dataDescriptionGroups = from result in layerSummary.Results
										where (fieldName == null || fieldName == result.FieldName) && (componentName == null || componentName == result.ComponentName)
										group result by new { result.FieldName, result.ComponentName } into g
										select g.SelectMany(r => LoadData(getLayerResultRecordName(layerId, r.Index))).ToList(); /*WARNING: eager evaluation*/

			return generateLayerFiles(layerName ?? "time compression", layerId, geometry, attributeDescriptions, dataDescriptionGroups, filter);
		}

		public LayerDiff CreateDiff(Guid layerId)
		{
			SummaryLayerFile layerSummary;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(layerId)))
				layerSummary = serializationService.Deserialize<SummaryLayerFile>(stream);

			if (!layerSummary.ParentId.HasValue)
				throw new ArgumentException("Layer is master layer (has no parent), can't create diff.");

			SummaryLayerFile parentLayerSummary;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(layerSummary.ParentId.Value)))
				parentLayerSummary = serializationService.Deserialize<SummaryLayerFile>(stream);

			var firstResults = from result in parentLayerSummary.Results
							   select getLayerResultRecordName(parentLayerSummary.Id, result.Index) into uri
							   from data in LoadData(uri)
							   select data;

			var secondResults = from result in layerSummary.Results
								select getLayerResultRecordName(layerSummary.Id, result.Index) into uri
								from data in LoadData(uri)
								select data;

			var diffs = from a in firstResults
						join b in secondResults on new { Field = a.Name, Component = a.ComponentNames.Single(), a.TimeStep } equals new { Field = b.Name, Component = b.ComponentNames.Single(), b.TimeStep }
						select compareTwoDataDescriptions(a, b);

			int numberOfDataComponents = 0;
			int numberOfDataValues = 0;
			double maxRelativeError = double.MinValue;
			double averageRelativeErrorWeightedSum = 0.0;
			double standardDeviation = double.NaN; /**/

			foreach (var diff in diffs)
			{
				numberOfDataComponents += diff.NumberOfDataComponents;
				numberOfDataValues += diff.NumberOfDataValues;
				maxRelativeError = Math.Max(maxRelativeError, diff.MaxRelativeError);
				averageRelativeErrorWeightedSum += diff.AverageRelativeError * diff.NumberOfDataValues;
			}

			double averageRelativeError = averageRelativeErrorWeightedSum / numberOfDataValues;

			return new LayerDiff(numberOfDataComponents, numberOfDataValues, maxRelativeError, averageRelativeError, standardDeviation);
		}

		public void AppendDataToLayer(Guid layerId, IDataImportService dataImportService)
		{
			throw new NotImplementedException();
		}

		#endregion

		public GeometryDescription LoadGeometry(string record)
		{
			using (Stream meshStream = sourceStorage.Load(record))
			{
				MeshLayerFile layerMesh = serializationService.Deserialize<MeshLayerFile>(meshStream);
				return createGeometryFromLayerMesh(layerMesh);
			}
		}

		public IEnumerable<DataDescription> LoadData(string record)
		{
			using (Stream stream = sourceStorage.Load(record))
			{
				DataLayerFile layerResult = serializationService.Deserialize<DataLayerFile>(stream);
				return createDataDescriptionFromLayerResult(layerResult);
			}
		}

		public AttributeDescription LoadAttribute(string record)
		{
			using (Stream attributeStream = sourceStorage.Load(record))
			{
				DataLayerFile layerAttributes = serializationService.Deserialize<DataLayerFile>(attributeStream);
				return createAttributeDescriptionFromDataLayerAttribute(layerAttributes);
			}
		}

		#endregion

		#region Private methods

		private IEnumerable<AttributeDescription> filterAttributesByGeometry(GeometryDescription filteredGeometry, IEnumerable<string> originalAttributeRecordNames)
		{
			FilterGeometryEntityMapping mapping = (FilterGeometryEntityMapping)filteredGeometry.Mapping;
			foreach (AttributeDescription oldAttribute in originalAttributeRecordNames.Select(record => LoadAttribute(record)))
			{
				int[] newValues;
				switch (oldAttribute.Location)
				{
					case DataLocationType.Points:
						newValues = new int[filteredGeometry.NumberOfPoints];
						for (int newPointIndex = 0; newPointIndex < newValues.Length; newPointIndex++)
						{
							int oldIndex;
							EdgeIntersection oldEdgeIntersection;
							if (mapping.TryGetOldPointId(newPointIndex, out oldIndex))
							{
								newValues[newPointIndex] = oldAttribute.Values[oldIndex];
							}
							else if (mapping.TryGetOldPointEdgeIntersection(newPointIndex, out oldEdgeIntersection))
							{
								newValues[newPointIndex] = interpolateAttributeValue(
									firstAttributeValue: oldAttribute.Values[oldEdgeIntersection.FirstPointId],
									secondAttributeValue: oldAttribute.Values[oldEdgeIntersection.SecondPointId],
									edgeCoordinate: oldEdgeIntersection.Coordinate);
							}
							//else -> no attribute value (zero is default)
						}
						break;
					case DataLocationType.CellPoints:
						newValues = new int[filteredGeometry.CellConnectivity.Length];
						{
							for (int newCellPointIndex = 0; newCellPointIndex < newValues.Length; newCellPointIndex++)
							{
								int oldCellPointIndex;
								EdgeIntersection oldEdgeIntersection;
								if (mapping.TryGetOldCellPointId(newCellPointIndex, out oldCellPointIndex))
								{
									newValues[newCellPointIndex] = oldAttribute.Values[oldCellPointIndex];
								}
								else if (mapping.TryGetOldCellPointEdgeIntersection(newCellPointIndex, out oldEdgeIntersection))
								{
									newValues[newCellPointIndex] = interpolateAttributeValue(
										firstAttributeValue: oldAttribute.Values[oldEdgeIntersection.FirstPointId],
										secondAttributeValue: oldAttribute.Values[oldEdgeIntersection.SecondPointId],
										edgeCoordinate: oldEdgeIntersection.Coordinate);
								}
								//else -> no attribute value (zero is default)
							}
						}
						break;
					case DataLocationType.Cells:
						newValues = new int[filteredGeometry.NumberOfCells];
						for (int newCellIndex = 0; newCellIndex < newValues.Length; newCellIndex++)
						{
							int oldIndex;
							if (mapping.TryGetOldCellId(newCellIndex, out oldIndex))
							{
								newValues[newCellIndex] = oldAttribute.Values[oldIndex];
							}
							//else -> no attribute value (zero is default)
						}
						break;
					default:
						throw new NotSupportedException();
				}

				AttributeDescription newAttribute = new AttributeDescription
				{
					Name = oldAttribute.Name,
					Location = oldAttribute.Location,
					Values = newValues
				};

				yield return newAttribute;
			}
		}

		private IEnumerable<DataDescription> filterDataByGeometry(GeometryDescription filteredGeometry, IEnumerable<string> originalResultRecordNames)
		{
			const double EMPTY_VALUE = double.NaN;
			FilterGeometryEntityMapping mapping = (FilterGeometryEntityMapping)filteredGeometry.Mapping;
			foreach (DataDescription oldResult in originalResultRecordNames.SelectMany(record => LoadData(record)))
			{
				int componentCount = oldResult.NumberOfComponents;
				double[] newValues;

				switch (oldResult.Location)
				{
					case DataLocationType.Points:
						newValues = new double[filteredGeometry.NumberOfPoints * componentCount];
						for (int newPointIndex = 0; newPointIndex < filteredGeometry.NumberOfPoints; newPointIndex++)
						{
							int oldPointIndex;
							EdgeIntersection oldEdgeIntersection;
							if (mapping.TryGetOldPointId(newPointIndex, out oldPointIndex))
							{
								for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
								{
									newValues[newPointIndex * componentCount + componentIndex] = oldResult.Values[oldPointIndex * componentCount + componentIndex];
								}
							}
							else if (mapping.TryGetOldPointEdgeIntersection(newPointIndex, out oldEdgeIntersection))
							{
								for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
								{
									newValues[newPointIndex * componentCount + componentIndex] = interpolateDataValue(
										firstDataValue: oldResult.Values[oldEdgeIntersection.FirstPointId * componentCount + componentIndex],
										secondDataValue: oldResult.Values[oldEdgeIntersection.SecondPointId * componentCount + componentIndex],
										edgeCoordinate: oldEdgeIntersection.Coordinate);
								}
							}
							else
							{
								newValues.FillRange(EMPTY_VALUE, newPointIndex * componentCount, componentCount);
							}
						}
						break;
					case DataLocationType.CellPoints:
						newValues = new double[filteredGeometry.CellConnectivity.Length * componentCount];
						for (int newCellPointIndex = 0; newCellPointIndex < filteredGeometry.CellConnectivity.Length; newCellPointIndex++)
						{
							int oldCellPointIndex;
							EdgeIntersection oldEdgeIntersection;
							if (mapping.TryGetOldCellPointId(newCellPointIndex, out oldCellPointIndex))
							{
								for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
								{
									newValues[newCellPointIndex * componentCount + componentIndex] = oldResult.Values[oldCellPointIndex * componentCount + componentIndex];
								}
							}
							else if (mapping.TryGetOldCellPointEdgeIntersection(newCellPointIndex, out oldEdgeIntersection))
							{
								for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
								{
									newValues[newCellPointIndex * componentCount + componentIndex] = interpolateDataValue(
										firstDataValue: oldResult.Values[oldEdgeIntersection.FirstPointId * componentCount + componentIndex],
										secondDataValue: oldResult.Values[oldEdgeIntersection.SecondPointId * componentCount + componentIndex],
										edgeCoordinate: oldEdgeIntersection.Coordinate);
								}
							}
							else
							{
								newValues.FillRange(EMPTY_VALUE, newCellPointIndex * componentCount, componentCount);
							}
						}
						break;
					case DataLocationType.Cells:
						newValues = new double[filteredGeometry.NumberOfCells * componentCount];
						for (int newCellIndex = 0; newCellIndex < filteredGeometry.NumberOfCells; newCellIndex++)
						{
							int oldCellIndex;
							if (mapping.TryGetOldCellId(newCellIndex, out oldCellIndex))
							{
								for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
								{
									newValues[newCellIndex * componentCount + componentIndex] = oldResult.Values[oldCellIndex * componentCount + componentIndex];
								}
							}
							else
							{
								newValues.FillRange(EMPTY_VALUE, newCellIndex * componentCount, componentCount);
							}
						}
						break;
					default:
						throw new NotSupportedException();
				}

				DataDescription newResult = new DataDescription
				{
					Name = oldResult.Name,
					TimeStep = oldResult.TimeStep,
					ComponentNames = oldResult.ComponentNames,

					FieldType = oldResult.FieldType,
					Location = oldResult.Location,
					NumberOfComponents = componentCount,
					Values = newValues
				};

				yield return newResult;
			}
		}

		private static int interpolateAttributeValue(int firstAttributeValue, int secondAttributeValue, float edgeCoordinate)
		{
			if (firstAttributeValue != secondAttributeValue)
				return 0;
			return firstAttributeValue;
		}

		private static double interpolateDataValue(double firstDataValue, double secondDataValue, float edgeCoordinate)
		{
			//if (double.IsNaN(firstDataValue) || double.IsNaN(secondDataValue))
			//	return double.NaN;
			return firstDataValue + edgeCoordinate * (secondDataValue - firstDataValue);
		}

		private SummaryLayerFile generateLayerFiles(string layerName, Guid? parentLayerId, GeometryDescription geometry, IEnumerable<AttributeDescription> attributeDescriptions, IEnumerable<IReadOnlyList<DataDescription>> dataDescriptionGroups, Filter filter)
		{
			Guid layerId = Guid.NewGuid();

			SummaryLayerFile layerSummary = new SummaryLayerFile
			{
				Id = layerId,
				Name = layerName,
				ParentId = parentLayerId,
				Filter = filter,
			};

			string layerDirectory = $"{layerId}";

			progressReporter?.Report(new OperationState("Generating mesh file"));

			MeshLayerFile layerMesh = createLayerMeshFromGeometry(geometry, layerId);
			storeLayerFile(layerMesh, getLayerMeshRecordName(layerId));

			int attributeIndex = 1, resultIndex = 1;

			var attributeDescriptors = new List<DataLayerDescriptor>();
			foreach (var attribute in attributeDescriptions)
			{
				progressReporter?.Report(new OperationState($"Generating attribute file '{attribute.Name}'"));

				DataLayerFile elementPropertyAttributeLayer = createAttributeLayerFile(attribute.Name, attribute.Values, DataLocationType.Cells, layerId, attributeIndex);
				storeLayerFile(elementPropertyAttributeLayer, getLayerAttributeRecordName(layerId, elementPropertyAttributeLayer.Index));
				attributeDescriptors.Add(DataLayerDescriptor.CreateFrom(elementPropertyAttributeLayer));
				attributeIndex++;
			}
			layerSummary.Attributes = attributeDescriptors.ToArray();

			var resultDescriptors = new List<DataLayerDescriptor>();
			var timeStepsHashSet = new HashSet<double>();
			var compressionCounter = new CompressionCounter();
			foreach (var dataDescriptionGroup in dataDescriptionGroups)
			{
				DataDescription firstDataField = dataDescriptionGroup.FirstOrDefault();
				if (firstDataField != null)
				{
					IEnumerable<DataDescription> restDataFields = dataDescriptionGroup.Skip(1);

					progressReporter?.Report(new OperationState($"Generating result file for field '{firstDataField.Name}' {(dataDescriptionGroup.Count == 1 ? $"(time step: {firstDataField.TimeStep})" : $"({dataDescriptionGroup.Count} time steps)")}"));

					for (int componentIndex = 0; componentIndex < firstDataField.NumberOfComponents; componentIndex++)
					{
						var layerResult = createLayerResultFromDataDescriptions(firstDataField, restDataFields, dataDescriptionGroup.Count, componentIndex, layerId, resultIndex);
						resultDescriptors.Add(DataLayerDescriptor.CreateFrom(layerResult));
						foreach (var timeStep in layerResult.TimeSteps)
						{
							timeStepsHashSet.Add(timeStep);
						}
						storeLayerFile(layerResult, getLayerResultRecordName(layerId, layerResult.Index));
						compressionCounter.Increment(layerResult.Compression, layerResult.Encoding);
						resultIndex += 1;
					}
				}
			}

			layerSummary.TimeSteps = timeStepsHashSet.OrderBy(t => t).ToArray();
			layerSummary.Results = resultDescriptors.ToArray();

			progressReporter?.Report(new OperationState("Generating summary file"));

			storeLayerFile(layerSummary, getLayerSummaryRecordName(layerId));

			progressReporter?.Report(new OperationState(compressionCounter.ToString()));

			return layerSummary;
		}

		private MeshLayerFile createLayerMeshFromGeometry(GeometryDescription geometry, Guid layerId)
		{
			MeshLayerFile layerMesh = new MeshLayerFile { LayerId = layerId };

			layerMesh.NumberOfPoints = geometry.NumberOfPoints;
			layerMesh.PointCoordinates = encodeGeometryDataArray(geometry.PointCoordinates, trimEnd: false);

			layerMesh.NumberOfCells = geometry.NumberOfCells;

			layerMesh.CellConnectivity = encodeGeometryDataArray(geometry.CellConnectivity, trimEnd: false);

			// set cell types to null if all cells are of default type (e.g. linear triangles)
			if (!geometry.CellTypes.All(cellType => cellType == DefaultCellType))
			{
				layerMesh.CellTypes = encodeGeometryDataArray(geometry.CellTypes.Select(t => (byte)t).ToArray(), trimEnd: true);
			}
			else
			{
				layerMesh.CellTypes = null;
			}

			//MeshFaceGenerator faceGenerator = new MeshFaceGenerator();
			//faceGenerator.ProcessGeometry(geometry);
			//layerMesh.NumberOfTriangles = faceGenerator.NumberOfTriangles;
			//layerMesh.TriangleConnectivity = ConvertArrayToBase64String(faceGenerator.TriangleConnectivity);
			//layerMesh.NumberOfEdges = faceGenerator.NumberOfEdges;
			//layerMesh.EdgeConnectivity = ConvertArrayToBase64String(faceGenerator.EdgeConnectivity);

			return layerMesh;
		}

		private DataLayerFile createAttributeLayerFile(string attributeName, int[] attributeValues, DataLocationType location, Guid layerId, int dataIndex)
		{
			Debug.Assert(attributeValues != null);

			DataLayerFile attributeLayer = new DataLayerFile
			{
				LayerId = layerId,
				FieldName = attributeName,
				ComponentName = null,
				Index = dataIndex,
				TimeSteps = null,
				Location = location
			};

			EncodingParameters encoding;
			attributeLayer.Data = encodeAttributes(attributeValues, out encoding);
			attributeLayer.Encoding = encoding;
			return attributeLayer;
		}

		private GeometryDescription createGeometryFromLayerMesh(MeshLayerFile layerMesh)
		{
			GeometryDescription geometry = new GeometryDescription();

			geometry.PointCoordinates = decodeGeometryDataArray<float>(layerMesh.PointCoordinates, expandEnd: false);
			geometry.NumberOfCoordinateComponents = geometry.PointCoordinates.Length / layerMesh.NumberOfPoints;
			geometry.CellConnectivity = decodeGeometryDataArray<int>(layerMesh.CellConnectivity, expandEnd: false);

			if (layerMesh.CellTypes != null)
			{
				geometry.CellTypes = decodeGeometryDataArray<byte>(layerMesh.CellTypes, expandEnd: true, originalLength: layerMesh.NumberOfCells).Select(b => (CellType)b).ToArray();
				var offsets = new int[layerMesh.NumberOfCells];
				for (int i = 0, offset = 0; i < offsets.Length; i++)
				{
					offset += GeometryDescription.MapCellTypeToNumberOfPoints(geometry.CellTypes[i]);
					offsets[i] = offset;
				}
				geometry.CellOffsets = offsets;
			}
			else
			{
				int numberOfPointsPerCell = GeometryDescription.MapCellTypeToNumberOfPoints(DefaultCellType);
				geometry.CellTypes = Enumerable.Repeat(DefaultCellType, layerMesh.NumberOfCells).ToArray();
				geometry.CellOffsets = Enumerable.Range(1, layerMesh.NumberOfCells).Select(i => i * numberOfPointsPerCell).ToArray();
			}

			return geometry;
		}

		private DataLayerFile createLayerResultFromDataDescriptions(DataDescription firstDataField, IEnumerable<DataDescription> restDataFields, int dataFieldCount, int componentIndex, Guid layerId, int dataIndex)
		{
			Debug.Assert(firstDataField != null);
			Debug.Assert(restDataFields != null);
			Debug.Assert(dataFieldCount > 0);

			DataLayerFile layerResult = new DataLayerFile
			{
				LayerId = layerId,
				Index = dataIndex,
				FieldName = firstDataField.Name,
				ComponentName = firstDataField.ComponentNames?[componentIndex],
				Location = firstDataField.Location,
				TimeSteps = restDataFields.Prepend(firstDataField).Select(d => d.TimeStep ?? 0.0).ToArray()
			};

			var firstDataValues = extractComponentValues(firstDataField, componentIndex);
			var restDataValuesQuery = restDataFields.Select(d => extractComponentValues(d, componentIndex));

			CompressionParameters compression;
			EncodingParameters encoding;
			layerResult.Data = compressAndEncodeDataValues(restDataValuesQuery.Prepend(firstDataValues), dataFieldCount, firstDataValues.Length, out compression, out encoding);
			layerResult.Compression = compression;
			layerResult.Encoding = encoding;

			return layerResult;
		}

		private static double[] extractComponentValues(DataDescription dataField, int componentIndex)
		{
			int numberOfComponents = dataField.NumberOfComponents;
			double[] allValues = dataField.Values;
			if (numberOfComponents == 1)
			{
				return allValues;
			}
			else
			{
				double[] componentValues = new double[allValues.Length / numberOfComponents];
				for (int hip = 0, hop = componentIndex; hop < allValues.Length; hip += 1, hop += numberOfComponents)
				{
					componentValues[hip] = allValues[hop];
				}
				return componentValues;
			}
		}

		private IEnumerable<DataDescription> createDataDescriptionFromLayerResult(DataLayerFile layerResult)
		{
			int timeStepIndex = 0;
			foreach (double[] decompressedData in decodeAndDecompressData(layerResult.Data, layerResult.Encoding, layerResult.Compression))
			{
				DataDescription data = new DataDescription
				{
					Name = layerResult.FieldName,
					TimeStep = layerResult.TimeSteps[timeStepIndex++],
					ComponentNames = new[] { layerResult.ComponentName },
					FieldType = FieldType.Scalar,
					Location = layerResult.Location,
					NumberOfComponents = 1,
					Values = decompressedData
				};
				yield return data;
			}
		}

		private AttributeDescription createAttributeDescriptionFromDataLayerAttribute(DataLayerFile layerAttributes)
		{
			Debug.Assert(layerAttributes.Compression == null);
			AttributeDescription attribute = new AttributeDescription
			{
				Name = layerAttributes.FieldName,
				Location = layerAttributes.Location,
				Values = decodeAttributes(layerAttributes.Data, layerAttributes.Encoding)
			};
			return attribute;
		}

		private void storeLayerFile<T>(T layerObject, string record)
		{
			using (Stream stream = destinationStorage.Save(record))
			{
				serializationService.Serialize(layerObject, stream);
			}
		}

		private string compressAndEncodeDataValues(IEnumerable<double[]> dataValues, int rows, int columns, out CompressionParameters compressionParameters, out EncodingParameters encodingParameters)
		{
			double[] compressedValues = compressionService.Compress(dataValues, rows, columns, parameters: out compressionParameters);
			return encodingService.Encode(compressedValues, TrimOptions.BeginEnd, out encodingParameters);
		}

		private IEnumerable<double[]> decodeAndDecompressData(string data, EncodingParameters encodingParameters, CompressionParameters compressionParameters)
		{
			double[] compressedValues = encodingService.Decode<double>(data, TrimOptions.BeginEnd, encodingParameters);
			ICompressionService selectedCompressionService = CompressionServiceFactory.Create(compressionParameters.Method);
			IEnumerable<double[]> originalDataValues = selectedCompressionService.Decompress(compressedValues, compressionParameters);
			return originalDataValues;
		}

		private string encodeAttributes(int[] attributes, out EncodingParameters encodingParameters)
		{
			return encodingService.Encode(attributes, TrimOptions.BeginEnd, out encodingParameters);
		}

		private int[] decodeAttributes(string data, EncodingParameters encodingParameters)
		{
			return encodingService.Decode<int>(data, TrimOptions.BeginEnd, encodingParameters);
		}

		private string encodeGeometryDataArray<T>(T[] geometryData, bool trimEnd) where T : struct
		{
			EncodingParameters ignored;
			return encodingService.Encode(geometryData, trimEnd ? TrimOptions.End : TrimOptions.None, out ignored);
		}

		private T[] decodeGeometryDataArray<T>(string data, bool expandEnd, int originalLength = 0) where T : struct
		{
			EncodingParameters encodingParameters = new EncodingParameters { OriginalLength = originalLength, Length = originalLength };
			return encodingService.Decode<T>(data, expandEnd ? TrimOptions.End : TrimOptions.None, encodingParameters);
		}

		private string getLayerSummaryRecordName(Guid layerId)
		{
			return $"{layerId}/{layerId}.summary{serializationService.FileExtension}";
		}

		private string getLayerMeshRecordName(Guid layerId)
		{
			return $"{layerId}/{layerId}.mesh{serializationService.FileExtension}";
		}

		private string getLayerAttributeRecordName(Guid layerId, int index)
		{
			return $"{layerId}/{layerId}.{index}.attribute{serializationService.FileExtension}";
		}

		private string getLayerResultRecordName(Guid layerId, int index)
		{
			return $"{layerId}/{layerId}.{index}.result{serializationService.FileExtension}";
		}

		private static LayerDiff compareTwoDataDescriptions(DataDescription a, DataDescription b)
		{
			Debug.Assert(a.Values.Length == b.Values.Length);
			Debug.Assert(a.NumberOfComponents == b.NumberOfComponents);
			double maxRelativeError = double.MinValue;
			double averageRelativeErrorWeightedSum = 0.0;
			int numberOfDataValues = 0;
			for (int component = 0; component < a.NumberOfComponents; component++)
			{
				double minValue = double.MaxValue, maxValue = double.MinValue;
				double maxAbsoluteError = double.MinValue;
				double absoluteErrorSum = 0.0;
				int numberOfDataValuesPerComponent = 0;

				for (int i = component; i < a.Values.Length; i += a.NumberOfComponents)
				{
					if (double.IsNaN(a.Values[i]) || double.IsNaN(b.Values[i]))
						continue;
					minValue = Math.Min(minValue, Math.Min(a.Values[i], b.Values[i]));
					maxValue = Math.Max(maxValue, Math.Max(a.Values[i], b.Values[i]));
					double error = Math.Abs(a.Values[i] - b.Values[i]);
					maxAbsoluteError = Math.Max(maxAbsoluteError, error);
					absoluteErrorSum += error;
					numberOfDataValuesPerComponent += 1;
				}

				double range = maxValue - minValue;
				double maxRelativeErrorPerComponent = (range > 0.0) ? maxAbsoluteError / range : 0.0;
				double averageRelativeErrorPerComponent = (range > 0.0 && numberOfDataValuesPerComponent > 0) ? absoluteErrorSum / (range * numberOfDataValuesPerComponent) : 0.0;
				maxRelativeError = Math.Max(maxRelativeError, maxRelativeErrorPerComponent);
				averageRelativeErrorWeightedSum += averageRelativeErrorPerComponent * numberOfDataValuesPerComponent;
				numberOfDataValues += numberOfDataValuesPerComponent;
			}
			double averageRelativeError = (numberOfDataValues > 0) ? averageRelativeErrorWeightedSum / numberOfDataValues : 0.0;
			return new LayerDiff(a.NumberOfComponents, numberOfDataValues, maxRelativeError, averageRelativeError, standardDeviation: double.NaN);
		}

		#endregion
	}
}

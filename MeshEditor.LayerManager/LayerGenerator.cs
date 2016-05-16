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

		public SummaryFile GenerateMasterLayer(string layerName, IGeometryImportService geometryImportService, IDataImportService dataImportService = null)
		{
			if (geometryImportService == null)
			{
				throw new ArgumentNullException(nameof(geometryImportService));
			}

			Guid newLayerId = Guid.NewGuid();
			
			// TODO: support multiple meshes
			IReadOnlyList<AttributeDescription> attributeDescriptions;
			GeometryDescription geometry = geometryImportService.ReadGeometry(out attributeDescriptions);
			IEnumerable<FieldDataDescription> dataDescriptions = dataImportService?.ReadData(geometry) ?? Enumerable.Empty<FieldDataDescription>();
			int meshIndex = 1;
			int attributeIndex = 1;
			int resultIndex = 1;
			var meshFileDesriptor = generateDataFilesForMesh(meshIndex, newLayerId, geometry, attributeDescriptions, dataDescriptions.Select(d => new[] { d }), ref attributeIndex, ref resultIndex);
			// ------
			return generateSummaryFile(layerName, null, newLayerId, null, Enumerable.Repeat(meshFileDesriptor, 1));
		}

		public SummaryFile GenerateFilterLayer(Guid parentLayerId, Filter filter, string layerName = null)
		{
			// find parentLayer in storage and download summary
			SummaryFile parentLayer;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(parentLayerId)))
			{
				parentLayer = serializationService.Deserialize<SummaryFile>(stream);
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
						meshFilterCreator = null;
						filterLayerName = layerName ?? $"{attributeSelectionFilter.AttributeName}: {string.Join(", ", attributeSelectionFilter.AttributeSelection)}";
					}
					break;
				default:
					throw new NotSupportedException();
			}

			Guid newLayerId = Guid.NewGuid();
			var meshFileDescriptors = new List<MeshFileDescriptor>();
			int attributeIndex = 1;
			int resultIndex = 1;
			foreach (var parentMesh in parentLayer.Meshes)
			{
				switch (filter.Type)
				{
					case FilterType.AttributeSelection:
						{
							var attributeSelectionFilter = (AttributeSelectionFilter)filter;
							var attributeDescriptor = parentMesh.Attributes.Single(a => a.FieldName == attributeSelectionFilter.AttributeName);
							var attribute = LoadAttribute(getLayerAttributeRecordName(parentLayerId, attributeDescriptor.Index));
							meshFilterCreator = new MeshPartitionCreator(attributeSelectionFilter, attribute);
						}
						break;
				}

				GeometryDescription originalGeometry = LoadGeometry(getLayerMeshRecordName(parentLayerId, parentMesh.Index));
				GeometryDescription filteredGeometry = meshFilterCreator.Create(originalGeometry);

				// TODO: check if filteredGeometry is not empty

				// filter attributes
				var originalAttributeRecordNames = parentMesh.Attributes.Select(a => getLayerAttributeRecordName(parentLayerId, a.Index));
				IEnumerable<AttributeDescription> filteredAttributeDescriptions = filterAttributesByGeometry(filteredGeometry, originalAttributeRecordNames);

				// filter results
				var originalResultRecordNames = parentMesh.Results.Select(r => getLayerResultRecordName(parentLayerId, r.Index));
				IEnumerable<ComponentDataDescription> filteredDataDescriptions = filterDataByGeometry(filteredGeometry, originalResultRecordNames);

				var meshFileDesriptor = generateDataFilesForMesh(parentMesh.Index, newLayerId, filteredGeometry, filteredAttributeDescriptions, filteredDataDescriptions.Select(d => new[] { d }), ref attributeIndex, ref resultIndex);
				meshFileDescriptors.Add(meshFileDesriptor);
			}

			return generateSummaryFile(filterLayerName, parentLayerId, newLayerId, filter, meshFileDescriptors);
		}

		public SummaryFile CompressLayer(Guid layerId, IEnumerable<double> keyTimeSteps, string layerName = null, string fieldName = null, string componentName = null)
		{
			// find layer in storage and download summary
			SummaryFile layerSummary;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(layerId)))
			{
				layerSummary = serializationService.Deserialize<SummaryFile>(stream);
			}

			Guid compressedLayerId = Guid.NewGuid();
			var meshFileDescriptors = new List<MeshFileDescriptor>();
			int attributeIndex = 1;
			int resultIndex = 1;
			foreach (var mesh in layerSummary.Meshes)
			{
				GeometryDescription geometry = LoadGeometry(getLayerMeshRecordName(layerId, mesh.Index));
				var attributeRecordNames = mesh.Attributes.Select(a => getLayerAttributeRecordName(layerId, a.Index));
				IEnumerable<AttributeDescription> attributeDescriptions = attributeRecordNames.Select(record => LoadAttribute(record));

				var dataDescriptionGroups = from result in mesh.Results
											where (fieldName == null || fieldName == result.FieldName) && (componentName == null || componentName == result.ComponentName)
											group result by new { result.FieldName, result.ComponentName } into g
											from list in createDataDescriptionGroups(layerId, g, keyTimeSteps)
											select list;
				var meshFileDesriptor = generateDataFilesForMesh(mesh.Index, compressedLayerId, geometry, attributeDescriptions, dataDescriptionGroups, ref attributeIndex, ref resultIndex);
				meshFileDescriptors.Add(meshFileDesriptor);
			}
			return generateSummaryFile(layerName ?? "time compression", layerId, compressedLayerId, new TimeCompressionFilter { FieldName = fieldName, ComponentName = componentName }, meshFileDescriptors);
		}

		public LayerDiff CreateDiff(Guid layerId)
		{
			SummaryFile layerSummary;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(layerId)))
				layerSummary = serializationService.Deserialize<SummaryFile>(stream);

			if (!layerSummary.ParentId.HasValue)
				throw new ArgumentException("Layer is master layer (has no parent), can't create diff.");

			SummaryFile parentLayerSummary;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(layerSummary.ParentId.Value)))
			{
				parentLayerSummary = serializationService.Deserialize<SummaryFile>(stream);
			}

			int numberOfDataValues = 0;
			double maxRelativeError = double.MinValue;
			double averageRelativeErrorWeightedSum = 0.0;
			double standardDeviation = double.NaN; /**/

			foreach (var mesh in parentLayerSummary.Meshes)
			{
				var firstResults = from result in mesh.Results
								   select getLayerResultRecordName(parentLayerSummary.Id, result.Index) into uri
								   from data in LoadData(uri)
								   select data;

				var secondResults =	from result in mesh.Results
									select getLayerResultRecordName(layerSummary.Id, result.Index) into uri
									from data in LoadData(uri)
									select data;

				var diffs = from a in firstResults
							join b in secondResults on new { Field = a.FieldName, Component = a.ComponentName, a.TimeStep } equals new { Field = b.FieldName, Component = b.ComponentName, b.TimeStep }
							select compareTwoDataDescriptions(a, b);

				foreach (var diff in diffs)
				{
					numberOfDataValues += diff.NumberOfDataValues;
					maxRelativeError = Math.Max(maxRelativeError, diff.MaxRelativeError);
					averageRelativeErrorWeightedSum += diff.AverageRelativeError * diff.NumberOfDataValues;
				}
			}

			double averageRelativeError = averageRelativeErrorWeightedSum / numberOfDataValues;

			return new LayerDiff(numberOfDataValues, maxRelativeError, averageRelativeError, standardDeviation);
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
				MeshFile layerMesh = serializationService.Deserialize<MeshFile>(meshStream);
				return createGeometryFromLayerMesh(layerMesh);
			}
		}

		public IEnumerable<ComponentDataDescription> LoadData(string record)
		{
			using (Stream stream = sourceStorage.Load(record))
			{
				DataFile layerResult = serializationService.Deserialize<DataFile>(stream);
				return createDataDescriptionFromLayerResult(layerResult);
			}
		}

		public AttributeDescription LoadAttribute(string record)
		{
			using (Stream attributeStream = sourceStorage.Load(record))
			{
				DataFile layerAttributes = serializationService.Deserialize<DataFile>(attributeStream);
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

		private IEnumerable<ComponentDataDescription> filterDataByGeometry(GeometryDescription filteredGeometry, IEnumerable<string> originalResultRecordNames)
		{
			const double EMPTY_VALUE = double.NaN;
			FilterGeometryEntityMapping mapping = (FilterGeometryEntityMapping)filteredGeometry.Mapping;
			foreach (ComponentDataDescription oldResult in originalResultRecordNames.SelectMany(record => LoadData(record)))
			{
				double[] newValues;

				switch (oldResult.Location)
				{
					case DataLocationType.Points:
						newValues = new double[filteredGeometry.NumberOfPoints];
						for (int newPointIndex = 0; newPointIndex < filteredGeometry.NumberOfPoints; newPointIndex++)
						{
							int oldPointIndex;
							EdgeIntersection oldEdgeIntersection;
							if (mapping.TryGetOldPointId(newPointIndex, out oldPointIndex))
							{
								newValues[newPointIndex] = oldResult.Values[oldPointIndex];
							}
							else if (mapping.TryGetOldPointEdgeIntersection(newPointIndex, out oldEdgeIntersection))
							{
								newValues[newPointIndex] = interpolateDataValue(
									firstDataValue: oldResult.Values[oldEdgeIntersection.FirstPointId],
									secondDataValue: oldResult.Values[oldEdgeIntersection.SecondPointId],
									edgeCoordinate: oldEdgeIntersection.Coordinate);
							}
							else
							{
								newValues[newPointIndex] = EMPTY_VALUE;
							}
						}
						break;
					case DataLocationType.CellPoints:
						newValues = new double[filteredGeometry.CellConnectivity.Length];
						for (int newCellPointIndex = 0; newCellPointIndex < filteredGeometry.CellConnectivity.Length; newCellPointIndex++)
						{
							int oldCellPointIndex;
							EdgeIntersection oldEdgeIntersection;
							if (mapping.TryGetOldCellPointId(newCellPointIndex, out oldCellPointIndex))
							{
								newValues[newCellPointIndex] = oldResult.Values[oldCellPointIndex];
							}
							else if (mapping.TryGetOldCellPointEdgeIntersection(newCellPointIndex, out oldEdgeIntersection))
							{
								newValues[newCellPointIndex] = interpolateDataValue(
									firstDataValue: oldResult.Values[oldEdgeIntersection.FirstPointId],
									secondDataValue: oldResult.Values[oldEdgeIntersection.SecondPointId],
									edgeCoordinate: oldEdgeIntersection.Coordinate);
							}
							else
							{
								newValues[newCellPointIndex] = EMPTY_VALUE;
							}
						}
						break;
					case DataLocationType.Cells:
						newValues = new double[filteredGeometry.NumberOfCells];
						for (int newCellIndex = 0; newCellIndex < filteredGeometry.NumberOfCells; newCellIndex++)
						{
							int oldCellIndex;
							if (mapping.TryGetOldCellId(newCellIndex, out oldCellIndex))
							{
								newValues[newCellIndex] = oldResult.Values[oldCellIndex];
							}
							else
							{
								newValues[newCellIndex] = EMPTY_VALUE;
							}
						}
						break;
					default:
						throw new NotSupportedException();
				}

				ComponentDataDescription newResult = new ComponentDataDescription
				{
					FieldName = oldResult.FieldName,
					TimeStep = oldResult.TimeStep,
					ComponentName = oldResult.ComponentName,

					Location = oldResult.Location,
					Values = newValues
				};

				yield return newResult;
			}
		}

		private IEnumerable<IReadOnlyList<ComponentDataDescription>> createDataDescriptionGroups(Guid layerId, IEnumerable<DataFileDescriptor> descriptors, IEnumerable<double> keyTimeSteps)
		{
			double[] keyTimes = keyTimeSteps.ToArray();
			int keyTimeIndex = 0;
			List<ComponentDataDescription> dataListForCurrentTimeInterval = new List<ComponentDataDescription>();
			foreach (var data in descriptors.SelectMany(r => LoadData(getLayerResultRecordName(layerId, r.Index))))
			{
				if (keyTimeIndex < keyTimes.Length)
				{
					if (data.TimeStep >= keyTimes[keyTimeIndex])
					{
						if (dataListForCurrentTimeInterval.Count > 0)
						{
							yield return dataListForCurrentTimeInterval;
							dataListForCurrentTimeInterval = new List<ComponentDataDescription>();
						}
						keyTimeIndex += 1;
					}
				}
				dataListForCurrentTimeInterval.Add(data);
			}
			yield return dataListForCurrentTimeInterval;
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

		private MeshFileDescriptor generateDataFilesForMesh(int meshIndex, Guid layerId, GeometryDescription geometry, IEnumerable<AttributeDescription> attributeDescriptions, IEnumerable<IReadOnlyList<DataDescription>> dataDescriptionGroups, ref int attributeIndex, ref int resultIndex)
		{
			progressReporter?.Report(new OperationState("Generating mesh file"));

			MeshFile layerMesh = createLayerMeshFromGeometry(geometry, layerId, meshIndex);
			storeLayerFile(layerMesh, getLayerMeshRecordName(layerId, meshIndex));

			// process attributes
			var attributeDescriptors = new List<DataFileDescriptor>();
			foreach (var attribute in attributeDescriptions)
			{
				progressReporter?.Report(new OperationState($"Generating attribute file '{attribute.Name}'"));

				DataFile elementPropertyAttributeLayer = createAttributeLayerFile(attribute.Name, attribute.Values, DataLocationType.Cells, layerId, attributeIndex, meshIndex);
				storeLayerFile(elementPropertyAttributeLayer, getLayerAttributeRecordName(layerId, elementPropertyAttributeLayer.Index));
				attributeDescriptors.Add(DataFileDescriptor.CreateFrom(elementPropertyAttributeLayer));
				attributeIndex++;
			}

			// process results
			var resultDescriptors = new List<DataFileDescriptor>();
			var compressionCounter = new CompressionCounter();
			foreach (var dataDescriptionGroup in dataDescriptionGroups)
			{
				DataDescription firstDataField = dataDescriptionGroup.FirstOrDefault();
				if (firstDataField != null)
				{
					IEnumerable<DataDescription> restDataFields = dataDescriptionGroup.Skip(1);

					progressReporter?.Report(new OperationState($"Generating result file for field '{firstDataField.FieldName}' component {string.Join(", ", Enumerable.Range(0, firstDataField.NumberOfComponents).Select(index => $"'{firstDataField.GetComponentName(index)}'"))} {(dataDescriptionGroup.Count == 1 ? $"(time step: {firstDataField.TimeStep})" : $"({dataDescriptionGroup.Count} time steps)")}"));

					for (int componentIndex = 0; componentIndex < firstDataField.NumberOfComponents; componentIndex++)
					{
						var layerResult = createLayerResultFromDataDescriptions(firstDataField, restDataFields, dataDescriptionGroup.Count, componentIndex, layerId, resultIndex, meshIndex);
						resultDescriptors.Add(DataFileDescriptor.CreateFrom(layerResult));
						storeLayerFile(layerResult, getLayerResultRecordName(layerId, layerResult.Index));
						compressionCounter.Increment(layerResult.Compression, layerResult.Encoding);
						resultIndex += 1;
					}
				}
			}
			progressReporter?.Report(new OperationState(compressionCounter.ToString()));

			MeshFileDescriptor meshFileDescriptor = new MeshFileDescriptor
			{
				Index = meshIndex,
				Attributes = attributeDescriptors.ToArray(),
				Results = resultDescriptors.ToArray(),
			};

			return meshFileDescriptor;
		}

		private SummaryFile generateSummaryFile(string layerName, Guid? parentLayerId, Guid newLayerId, Filter filter, IEnumerable<MeshFileDescriptor> meshFileDescriptors)
		{
			SummaryFile layerSummary = new SummaryFile
			{
				Id = newLayerId,
				Name = layerName,
				ParentId = parentLayerId,
				Filter = filter,
				Meshes = meshFileDescriptors.ToArray(),
				//TimeSteps = meshFileDescriptors.SelectMany(m => m.TimeSteps).Distinct().OrderBy(t => t).ToArray(),
			};

			progressReporter?.Report(new OperationState("Generating summary file"));
			storeLayerFile(layerSummary, getLayerSummaryRecordName(newLayerId));
			return layerSummary;
		}

		private MeshFile createLayerMeshFromGeometry(GeometryDescription geometry, Guid layerId, int meshIndex)
		{
			MeshFile layerMesh = new MeshFile { LayerId = layerId, Index = meshIndex };

			layerMesh.NumberOfPoints = geometry.NumberOfPoints;
			layerMesh.PointCoordinates = encodeGeometryDataArray(geometry.PointCoordinates, trimEnd: false);

			float[] center;
			float radius;
			geometry.CalculateCenterAndRadius(out center, out radius);
			layerMesh.Center = center;
			layerMesh.Radius = radius;

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

			return layerMesh;
		}

		private DataFile createAttributeLayerFile(string attributeName, int[] attributeValues, DataLocationType location, Guid layerId, int dataIndex, int meshIndex)
		{
			Debug.Assert(attributeValues != null);

			DataFile attributeLayer = new DataFile
			{
				LayerId = layerId,
				FieldName = attributeName,
				ComponentName = null,
				Index = dataIndex,
				MeshIndex = meshIndex,
				TimeSteps = null,
				Location = location
			};

			EncodingParameters encoding;
			attributeLayer.Data = encodeAttributes(attributeValues, out encoding);
			attributeLayer.Encoding = encoding;
			return attributeLayer;
		}

		private GeometryDescription createGeometryFromLayerMesh(MeshFile layerMesh)
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

		private DataFile createLayerResultFromDataDescriptions(DataDescription firstDataField, IEnumerable<DataDescription> restDataFields, int dataFieldCount, int componentIndex, Guid layerId, int dataIndex, int meshIndex)
		{
			Debug.Assert(firstDataField != null);
			Debug.Assert(restDataFields != null);
			Debug.Assert(dataFieldCount > 0);

			DataFile layerResult = new DataFile
			{
				LayerId = layerId,
				Index = dataIndex,
				MeshIndex = meshIndex,
				FieldName = firstDataField.FieldName,
				ComponentName = firstDataField.GetComponentName(componentIndex),
				Location = firstDataField.Location,
				TimeSteps = restDataFields.Prepend(firstDataField).Select(d => d.TimeStep).ToArray()
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

		private IEnumerable<ComponentDataDescription> createDataDescriptionFromLayerResult(DataFile layerResult)
		{
			int timeStepIndex = 0;
			foreach (double[] decompressedData in decodeAndDecompressData(layerResult.Data, layerResult.Encoding, layerResult.Compression))
			{
				ComponentDataDescription data = new ComponentDataDescription
				{
					FieldName = layerResult.FieldName,
					TimeStep = layerResult.TimeSteps[timeStepIndex++],
					ComponentName = layerResult.ComponentName,
					Location = layerResult.Location,
					Values = decompressedData
				};
				yield return data;
			}
		}

		private AttributeDescription createAttributeDescriptionFromDataLayerAttribute(DataFile layerAttributes)
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
			//return $"{layerId}/{layerId}.summary{serializationService.FileExtension}";
			return $"{layerId}/summary{serializationService.FileExtension}";
		}

		private string getLayerMeshRecordName(Guid layerId, int index)
		{
			//return $"{layerId}/{layerId}.mesh{serializationService.FileExtension}";
			return $"{layerId}/mesh.{index}{serializationService.FileExtension}";
		}

		private string getLayerAttributeRecordName(Guid layerId, int index)
		{
			//return $"{layerId}/{layerId}.{index}.attribute{serializationService.FileExtension}";
			return $"{layerId}/attribute.{index}{serializationService.FileExtension}";
		}

		private string getLayerResultRecordName(Guid layerId, int index)
		{
			//return $"{layerId}/{layerId}.{index}.result{serializationService.FileExtension}";
			return $"{layerId}/result.{index}{serializationService.FileExtension}";
		}

		private static LayerDiff compareTwoDataDescriptions(ComponentDataDescription a, ComponentDataDescription b)
		{
			Debug.Assert(a.Values.Length == b.Values.Length);
			double maxRelativeError = double.MinValue;
			double averageRelativeErrorWeightedSum = 0.0;
			int numberOfDataValues = 0;

			double minValue = double.MaxValue, maxValue = double.MinValue;
			double maxAbsoluteError = double.MinValue;
			double absoluteErrorSum = 0.0;

			for (int i = 0; i < a.Values.Length; i++)
			{
				if (double.IsNaN(a.Values[i]) || double.IsNaN(b.Values[i]))
					continue;
				minValue = Math.Min(minValue, Math.Min(a.Values[i], b.Values[i]));
				maxValue = Math.Max(maxValue, Math.Max(a.Values[i], b.Values[i]));
				double error = Math.Abs(a.Values[i] - b.Values[i]);
				maxAbsoluteError = Math.Max(maxAbsoluteError, error);
				absoluteErrorSum += error;
				numberOfDataValues += 1;
			}

			double range = maxValue - minValue;
			double maxRelativeErrorPerComponent = (range > 0.0) ? maxAbsoluteError / range : 0.0;
			double averageRelativeErrorPerComponent = (range > 0.0 && numberOfDataValues > 0) ? absoluteErrorSum / (range * numberOfDataValues) : 0.0;
			maxRelativeError = Math.Max(maxRelativeError, maxRelativeErrorPerComponent);
			averageRelativeErrorWeightedSum += averageRelativeErrorPerComponent * numberOfDataValues;

			double averageRelativeError = (numberOfDataValues > 0) ? averageRelativeErrorWeightedSum / numberOfDataValues : 0.0;
			return new LayerDiff(numberOfDataValues, maxRelativeError, averageRelativeError, standardDeviation: double.NaN);
		}

		#endregion
	}
}

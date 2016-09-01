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
using MeshEditor.Common;
using MeshEditor.Common.Extensions;
using MeshEditor.LayerManager.MeshFiltering;
using System.Threading;

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
		ILogger logger;

		public LayerGenerator(
			IReadStorageService sourceStorage,
			IWriteStorageService destinationStorage,
			ISerializationService serializationService = null,
			ICompressionService compressionService = null,
			IEncodingService encodingService = null,
			ILogger logger = null)
		{
			this.sourceStorage = sourceStorage;
			this.destinationStorage = destinationStorage;
			this.serializationService = serializationService ?? new JsonSerializationService();
			this.compressionService = compressionService ?? new TransparentCompressionService();
			this.encodingService = encodingService ?? new Base64EncodingService();
			this.logger = logger;
		}

		#endregion

		#region Public methods

		#region Console app Entry points

		public SummaryFile GenerateMasterLayer(string layerName, IEnumerable<IAnalysisResultImportService> analysisResultImportServices, IEnumerable<double> keyTimeSteps, string fieldName = null)
		{
			if (analysisResultImportServices == null)
			{
				throw new ArgumentNullException(nameof(analysisResultImportServices));
			}

			Guid newLayerId = Guid.NewGuid();
			int attributeIndex = 1;
			int resultIndex = 1;
			var meshDescriptors = new List<MeshFileDescriptor>();
			foreach (var analysisResultImportService in analysisResultImportServices)
			{
				IReadOnlyList<AttributeDescription> attributeDescriptions;
				GeometryDescription geometry = analysisResultImportService.ReadGeometry(out attributeDescriptions);

				// divide dataDescriptions to time step chunks according to --keytimes option

				IEnumerable<IReadOnlyList<DataDescription>> dataDescriptionsChunks;

				if (keyTimeSteps.Any())
				{
					dataDescriptionsChunks = from result in analysisResultImportService.ReadData(geometry) // TODO: pass logger to import service to view progress
											 where (fieldName == null || fieldName == result.FieldName)
											 group result by result.FieldName into resultGroup
											 from list in createDataDescriptionGroups(resultGroup, keyTimeSteps)
											 select list;
				}
				else // optimization
				{
					dataDescriptionsChunks = from result in analysisResultImportService.ReadData(geometry)
											 where (fieldName == null || fieldName == result.FieldName)
											 select new[] { result };
				}

				var meshDescriptor = generateDataFilesForMesh(meshDescriptors.Count + 1, newLayerId, geometry, attributeDescriptions, dataDescriptionsChunks, ref attributeIndex, ref resultIndex);
				meshDescriptors.Add(meshDescriptor);
			}
			return generateSummaryFile(layerName, null, newLayerId, null, meshDescriptors);
		}

		public SummaryFile GenerateFilterLayer(Guid parentLayerId, Filter filter, string layerName, IEnumerable<double> keyTimeSteps, string fieldName = null)
		{
			// find parentLayer in storage and download summary
			SummaryFile parentLayer = LoadLayerSummary(parentLayerId);

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
				// TODO: use MeshSliceCreator
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
							var attribute = LoadAttribute(parentLayerId, attributeDescriptor.Index);
							meshFilterCreator = new MeshPartitionCreator(attributeSelectionFilter, attribute);
						}
						break;
				}

				GeometryDescription originalGeometry = LoadGeometry(parentLayerId, parentMesh.Index);
				GeometryDescription filteredGeometry = meshFilterCreator.Create(originalGeometry);

				// TODO: check if filteredGeometry is not empty

				// filter attributes
				var originalAttributeRecordNames = parentMesh.Attributes.Select(a => getLayerAttributeRecordName(parentLayerId, a.Index));
				IEnumerable<AttributeDescription> filteredAttributeDescriptions = filterAttributesByGeometry(filteredGeometry, originalAttributeRecordNames);

				// filter results
				// divide filteredDataDescriptions to time step chunks according to --keytimes option

				IEnumerable<IReadOnlyList<DataDescription>> filteredDataDescriptionsChunks;

				if (keyTimeSteps.Any())
				{
					filteredDataDescriptionsChunks = from result in parentMesh.Results
													 where (fieldName == null || fieldName == result.FieldName)
													 group result by new { result.FieldName, result.ComponentName } into resultGroup
													 from list in createDataDescriptionGroups(filterDataByGeometry(filteredGeometry, resultGroup.Select(r => getLayerResultRecordName(parentLayerId, r.Index))), keyTimeSteps)
													 select list;
				}
				else
				{
					filteredDataDescriptionsChunks = from data in filterDataByGeometry(filteredGeometry, from result in parentMesh.Results
																										 where (fieldName == null || fieldName == result.FieldName)
																										 select getLayerResultRecordName(parentLayerId, result.Index))
													 select new[] { data };
				}

				var meshFileDesriptor = generateDataFilesForMesh(parentMesh.Index, newLayerId, filteredGeometry, filteredAttributeDescriptions, filteredDataDescriptionsChunks, ref attributeIndex, ref resultIndex);
				meshFileDescriptors.Add(meshFileDesriptor);
			}

			return generateSummaryFile(filterLayerName, parentLayerId, newLayerId, filter, meshFileDescriptors);
		}

		public SummaryFile CompressLayer(Guid layerId, IEnumerable<double> keyTimeSteps, string layerName = null, string fieldName = null)
		{
			// find layer in storage and download summary
			SummaryFile layerSummary = LoadLayerSummary(layerId);

			Guid compressedLayerId = Guid.NewGuid();
			var meshFileDescriptors = new List<MeshFileDescriptor>();
			int attributeIndex = 1;
			int resultIndex = 1;
			foreach (var mesh in layerSummary.Meshes)
			{
				GeometryDescription geometry = LoadGeometry(layerId, mesh.Index);
				IEnumerable<AttributeDescription> attributeDescriptions = mesh.Attributes.Select(a => LoadAttribute(layerId, a.Index));

				var dataDescriptionGroups = from result in mesh.Results
											where (fieldName == null || fieldName == result.FieldName)
											group result by new { result.FieldName, result.ComponentName } into descriptorsGroup
											from list in createDataDescriptionGroups(descriptorsGroup.SelectMany(r => LoadData(layerId, r.Index)), keyTimeSteps)
											select list;

				var meshFileDesriptor = generateDataFilesForMesh(mesh.Index, compressedLayerId, geometry, attributeDescriptions, dataDescriptionGroups, ref attributeIndex, ref resultIndex);
				meshFileDescriptors.Add(meshFileDesriptor);
			}
			return generateSummaryFile(layerName ?? "time compression", layerId, compressedLayerId, new TimeCompressionFilter { FieldName = fieldName }, meshFileDescriptors);
		}

		#endregion

		public SummaryFile LoadLayerSummary(Guid layerId)
		{
			// find parentLayer in storage and download summary
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(layerId)))
			{
				return serializationService.Deserialize<SummaryFile>(stream);
			}
		}

		public async Task<SummaryFile> LoadLayerSummaryAsync(Guid layerId, CancellationToken cancellationToken)
		{
			// find parentLayer in storage and download summary
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(layerId)))
			{
				return await serializationService.DeserializeAsync<SummaryFile>(stream, cancellationToken);
			}
		}

		public GeometryDescription LoadGeometry(Guid layerId, int meshIndex)
		{
			using (Stream meshStream = sourceStorage.Load(getLayerMeshRecordName(layerId, meshIndex)))
			{
				MeshFile layerMesh = serializationService.Deserialize<MeshFile>(meshStream);
				return createGeometryFromLayerMesh(layerMesh);
			}
		}

		public async Task<GeometryDescription> LoadGeometryAsync(Guid layerId, int meshIndex, CancellationToken cancellationToken)
		{
			using (Stream meshStream = sourceStorage.Load(getLayerMeshRecordName(layerId, meshIndex)))
			{
				MeshFile layerMesh = await serializationService.DeserializeAsync<MeshFile>(meshStream, cancellationToken);
				return createGeometryFromLayerMesh(layerMesh);
			}
		}

		public IEnumerable<ComponentDataDescription> LoadData(Guid layerId, int dataIndex)
		{
			return loadData(getLayerResultRecordName(layerId, dataIndex));
		}

		public Task<IEnumerable<ComponentDataDescription>> LoadDataAsync(Guid layerId, int dataIndex, CancellationToken cancellationToken)
		{
			return loadDataAsync(getLayerResultRecordName(layerId, dataIndex), cancellationToken);
		}

		public AttributeDescription LoadAttribute(Guid layerId, int attributeIndex)
		{
			return loadAttribute(getLayerAttributeRecordName(layerId, attributeIndex));
		}

		public Task<AttributeDescription> LoadAttributeAsync(Guid layerId, int attributeIndex, CancellationToken cancellationToken)
		{
			return loadAttributeAsync(getLayerAttributeRecordName(layerId, attributeIndex), cancellationToken);
		}

		#endregion

		#region Private methods

		private IEnumerable<ComponentDataDescription> loadData(string record)
		{
			using (Stream stream = sourceStorage.Load(record))
			{
				DataFile layerResult = serializationService.Deserialize<DataFile>(stream);
				return createDataDescriptionFromLayerResult(layerResult);
			}
		}

		private async Task<IEnumerable<ComponentDataDescription>> loadDataAsync(string record, CancellationToken cancellationToken)
		{
			using (Stream stream = sourceStorage.Load(record))
			{
				DataFile layerResult = await serializationService.DeserializeAsync<DataFile>(stream, cancellationToken);
				return createDataDescriptionFromLayerResult(layerResult);
			}
		}

		private AttributeDescription loadAttribute(string record)
		{
			using (Stream attributeStream = sourceStorage.Load(record))
			{
				DataFile layerAttributes = serializationService.Deserialize<DataFile>(attributeStream);
				return createAttributeDescriptionFromDataLayerAttribute(layerAttributes);
			}
		}

		private async Task<AttributeDescription> loadAttributeAsync(string record, CancellationToken cancellationToken)
		{
			using (Stream attributeStream = sourceStorage.Load(record))
			{
				DataFile layerAttributes = await serializationService.DeserializeAsync<DataFile>(attributeStream, cancellationToken);
				return createAttributeDescriptionFromDataLayerAttribute(layerAttributes);
			}
		}

		private IEnumerable<AttributeDescription> filterAttributesByGeometry(GeometryDescription filteredGeometry, IEnumerable<string> originalAttributeRecordNames)
		{
			FilterGeometryEntityMapping mapping = (FilterGeometryEntityMapping)filteredGeometry.Mapping;
			foreach (AttributeDescription oldAttribute in originalAttributeRecordNames.Select(record => loadAttribute(record)))
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
			foreach (ComponentDataDescription oldResult in originalResultRecordNames.SelectMany(record => loadData(record)))
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

		private IEnumerable<IReadOnlyList<DataDescription>> createDataDescriptionGroups(IEnumerable<DataDescription> dataDescriptions, IEnumerable<double> keyTimeSteps)
		{
			Debug.Assert(keyTimeSteps != null);
			double[] keyTimes = keyTimeSteps.ToArray();
			int keyTimeIndex = 0;
			List<DataDescription> dataListForCurrentTimeInterval = new List<DataDescription>();
			foreach (var dataComponent in dataDescriptions)
			{
				if (keyTimeIndex < keyTimes.Length)
				{
					if (dataComponent.TimeStep >= keyTimes[keyTimeIndex])
					{
						if (dataListForCurrentTimeInterval.Count > 0)
						{
							yield return dataListForCurrentTimeInterval;
							dataListForCurrentTimeInterval = new List<DataDescription>();
						}
						keyTimeIndex += 1;
					}
				}
				dataListForCurrentTimeInterval.Add(dataComponent);
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
			logger?.LogOperationProgress("Generating mesh file");

			MeshFile layerMesh = createLayerMeshFromGeometry(geometry, layerId, meshIndex);
			storeLayerFile(layerMesh, getLayerMeshRecordName(layerId, meshIndex));

			// process attributes
			var attributeDescriptors = new List<DataFileDescriptor>();
			foreach (var attribute in attributeDescriptions)
			{
				logger?.LogOperationProgress($"Generating attribute file '{attribute.Name}'");

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

					for (int componentIndex = 0; componentIndex < firstDataField.NumberOfComponents; componentIndex++)
					{
						logger?.LogOperationProgress($"Generating result file for field '{firstDataField.FieldName}' component '{firstDataField.GetComponentName(componentIndex)}' {(dataDescriptionGroup.Count == 1 ? $"(time step: {firstDataField.TimeStep})" : $"({dataDescriptionGroup.Count} time steps)")}");

						var layerResult = createLayerResultFromDataDescriptions(firstDataField, restDataFields, dataDescriptionGroup.Count, componentIndex, layerId, resultIndex, meshIndex);
						resultDescriptors.Add(DataFileDescriptor.CreateFrom(layerResult));

						storeLayerFile(layerResult, getLayerResultRecordName(layerId, layerResult.Index));

						compressionCounter.Increment(layerResult.Compression, layerResult.Encoding);
						resultIndex += 1;
					}
				}
			}

			logger?.LogMessage(compressionCounter.ToString());

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
			logger?.LogOperationProgress("Generating summary file");

			SummaryFile layerSummary = new SummaryFile
			{
				Id = newLayerId,
				Name = layerName,
				ParentId = parentLayerId,
				Filter = filter,
				Meshes = meshFileDescriptors.ToArray()
			};
			var fields = new Dictionary<string, FieldDescriptor>();
			foreach (var fieldGroup in from mesh in meshFileDescriptors
									   from result in mesh.Results
									   group new { Mesh = mesh, Result = result } by result.FieldName into resultGroup
									   select resultGroup)
			{
				var components = new Dictionary<string, ComponentDescriptor>();
				fields[fieldGroup.Key] = new FieldDescriptor { Components = components };
				foreach (var componentGroup in from result in fieldGroup
											   group result by result.Result.ComponentName into resultGroup
											   select resultGroup)
				{
					var timeSteps = new Dictionary<double, TimeStepDescriptor>();
					components[componentGroup.Key] = new ComponentDescriptor { TimeSteps = timeSteps };
					foreach (var timeStepGroup in from result in componentGroup
												  from timeStep in result.Result.TimeSteps
												  group result by timeStep into resultGroup
												  select resultGroup)
					{
						if (timeStepGroup.Count() > 1)
						{
							logger.LogWarning($"Multiple data components for {fieldGroup.Key}/{componentGroup.Key}/t={timeStepGroup.Key}. Data indices: " + string.Join(", ", timeStepGroup.Select(t => t.Result.Index)));
						}
						var timeStep = timeStepGroup.First();
						timeSteps[timeStepGroup.Key] = new TimeStepDescriptor
						{
							MeshIndex = timeStep.Mesh.Index,
							DataIndex = timeStep.Result.Index
						};
					}
				}
			}

			layerSummary.Fields = fields;

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
			return $"{layerId}/{index}.mesh{serializationService.FileExtension}";
		}

		private string getLayerAttributeRecordName(Guid layerId, int index)
		{
			//return $"{layerId}/{layerId}.{index}.attribute{serializationService.FileExtension}";
			return $"{layerId}/{index}.attribute{serializationService.FileExtension}";
		}

		private string getLayerResultRecordName(Guid layerId, int index)
		{
			//return $"{layerId}/{layerId}.{index}.result{serializationService.FileExtension}";
			return $"{layerId}/{index}.result{serializationService.FileExtension}";
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

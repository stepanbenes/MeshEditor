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
using MeshEditor.Common.Logging;
using MeshEditor.Common.Extensions;
using MeshEditor.LayerManager.MeshFiltering;
using System.Threading;
using System.Globalization;

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

			public double GetCompressionFactor() => (double)compressedDataLength / inputDataLength;

			public double GetEncodingFactor() => (double)encodedDataLength / compressedDataLength;

			public double GetOverallFactor() => (double)encodedDataLength / inputDataLength;

			public long GetMemoryConsumption() => encodedDataLength * sizeof(double);

			public override string ToString()
			{
				StringBuilder text = new StringBuilder();
				{
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
				}
				return text.ToString();
			}
		}

		#endregion

		#region Fields, constructor

		readonly IReadStorageService sourceStorage;
		readonly IWriteStorageService destinationStorage;
		readonly ISerializationService serializationService;
		readonly ICompressionService compressionService;
		readonly IEncodingService encodingService;
		readonly ILogger logger;

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

		public SummaryFile GenerateMasterLayer(string layerName, IEnumerable<IAnalysisResultImportService> analysisResultImportServices, IEnumerable<decimal> keyTimeSteps, string fieldName = null)
		{
			if (analysisResultImportServices == null)
			{
				throw new ArgumentNullException(nameof(analysisResultImportServices));
			}

			Guid newLayerId = Guid.NewGuid();
			int attributeIndex = 1;
			int resultIndex = 1;
			var meshDescriptors = new List<MeshFileDescriptor>();
			var fieldDescriptors = new Dictionary<string, FieldDescriptor>();
			foreach (var analysisResultImportService in analysisResultImportServices)
			{
				IReadOnlyList<AttributeDescription> attributeDescriptions;
				GeometryDescription geometry = analysisResultImportService.ReadGeometry(out attributeDescriptions);
				if (geometry.IsEmpty)
					continue;

				// divide dataDescriptions to time step chunks according to --keytimes option
				IEnumerable<IReadOnlyList<DataDescription>> dataGroups;

				if (keyTimeSteps.Any())
				{
					dataGroups = from result in analysisResultImportService.ReadData(geometry) // TODO: pass logger to import service to view progress
								 where (fieldName == null || fieldName == result.FieldName)
								 group result by result.FieldName into resultGroup
								 from list in createDataDescriptionGroups(resultGroup, keyTimeSteps)
								 select list;
				}
				else // optimization
				{
					dataGroups = from result in analysisResultImportService.ReadData(geometry)
								 where (fieldName == null || fieldName == result.FieldName)
								 select new[] { result };
				}

				var meshDescriptor = generateMeshFileAndAttributeFiles(meshDescriptors.Count + 1, newLayerId, geometry, attributeDescriptions, /*timeSteps:*/ null, ref attributeIndex);

				Func<decimal, int> meshIndexFromTimeStepProvider = _ => meshDescriptor.Index;
				var resultDescriptors = generateDataFilesForFields(newLayerId, meshIndexFromTimeStepProvider, dataGroups, ref resultIndex);
				var fieldsMap = convertDataFileDescriptorsToFieldDescriptors(resultDescriptors, meshIndexFromTimeStepProvider);
				mergeFieldDescriptors(fieldDescriptors, fieldsMap);

				meshDescriptor.TimeSteps = findAllTimeStepsSorted(resultDescriptors).ToArray();
				meshDescriptors.Add(meshDescriptor);
			}
			return generateSummaryFile(layerName, null, newLayerId, null, meshDescriptors, fieldDescriptors);
		}

		public SummaryFile GenerateFilterLayer(Guid parentLayerId, Filter filter, string layerName, IEnumerable<decimal> keyTimeSteps, string fieldName = null)
		{
			// find parentLayer in storage and download summary
			SummaryFile parentLayer = LoadLayerSummary(parentLayerId);

			string filterLayerName = constructFilterLayerName();

			Guid newLayerId = Guid.NewGuid();
			var meshFileDescriptors = new List<MeshFileDescriptor>();
			int meshIndex = 1;
			int attributeIndex = 1;
			int resultIndex = 1;
			var filteredGeometryMap = new Dictionary<decimal, GeometryDescription>();

			foreach (var parentMesh in parentLayer.Meshes)
			{
				GeometryDescription originalGeometry = LoadGeometry(parentLayerId, parentLayer.MeshFallbackLayerId, parentMesh.Index);
				if (originalGeometry.IsEmpty)
					throw new InvalidOperationException($"Geometry is empty (mesh index: {parentMesh.Index})");

				IMeshFilterCreator meshFilterCreator = constructMeshFilterCreator(parentMesh);

				var filteredGeometries = meshFilterCreator.Create(originalGeometry, parentMesh.TimeSteps);

				foreach (var (filteredGeometry, filteredMeshTimeSteps) in filteredGeometries)
				{
					if (filteredGeometry.IsEmpty)
						continue;
					var attributesToFilter = filter is DeformationFilter ? Enumerable.Empty<DataFileDescriptor>() : parentMesh.Attributes;
					var meshFileDescriptor = constructFilteredMesh(filteredGeometry, filteredMeshTimeSteps, attributesToFilter);
					meshFileDescriptors.Add(meshFileDescriptor);

					foreach (var timeStep in filteredMeshTimeSteps)
					{
						filteredGeometryMap.Add(timeStep, filteredGeometry);
					}
				}
			}

			// filter results
			// divide filteredDataDescriptions to time step chunks according to --keytimes option
			IEnumerable<IReadOnlyList<DataDescription>> filteredDataDescriptionsChunks;

			if (keyTimeSteps.Any())
			{
				filteredDataDescriptionsChunks = from g in getResultIndicesGroupedByTimeStep(parentLayer, fieldName)
												 from list in createDataDescriptionGroups(filterDataByGeometry(filteredGeometryMap, parentLayer, g), keyTimeSteps)
												 select list;
			}
			else
			{
				filteredDataDescriptionsChunks = from data in filterDataByGeometry(filteredGeometryMap, parentLayer, getResultIndicesGroupedByTimeStep(parentLayer, fieldName).SelectMany(g => g))
												 select new[] { data };
			}

			Func<decimal, int> meshIndexFromTimeStepProvider = createMeshIndexFromTimeStepProvider(meshFileDescriptors);
			var resultDescriptors = generateDataFilesForFields(newLayerId, meshIndexFromTimeStepProvider, filteredDataDescriptionsChunks, ref resultIndex);
			var fieldDescriptors = convertDataFileDescriptorsToFieldDescriptors(resultDescriptors, meshIndexFromTimeStepProvider);

			return generateSummaryFile(filterLayerName, parentLayer, newLayerId, filter, meshFileDescriptors, fieldDescriptors);

			// local functions >>>

			IMeshFilterCreator constructMeshFilterCreator(MeshFileDescriptor parentMesh)
			{
				switch (filter)
				{
					case SurfaceFilter surfaceFilter:
						{
							return new MeshSurfaceCreator(surfaceFilter);
						}
					case SliceFilter sliceFilter:
						{
							return new MeshSliceCreator(sliceFilter);
						}
					case AttributeSelectionFilter attributeSelectionFilter:
						{
							var attributeDescriptor = parentMesh.Attributes.Single(a => a.FieldName == attributeSelectionFilter.AttributeName);
							var attribute = LoadAttribute(parentLayerId, parentLayer.AttributeFallbackLayerId, attributeDescriptor.Index);
							return new MeshPartitionCreator(attributeSelectionFilter, attribute);
						}
					case DeformationFilter deformationFilter:
						{
							var dataComponentDescriptors = from index in getResultIndicesGroupedByTimeStep(parentLayer, deformationFilter.DeformationFieldName).SelectMany(g => g)
														   from d in LoadData(parentLayerId, parentLayer.DataFallbackLayerId, index)
														   group d by d.TimeStep;
							return new DeformedMeshCreator(deformationFilter, dataComponentDescriptors.ToDictionary(g => g.Key, g => g.OrderBy(d => d.ComponentName).ToList()));
						}
					case IsoSurfaceFilter isoSurfaceFilter:
						{
							var dataComponentDescriptors = from index in getResultIndicesGroupedByTimeStep(parentLayer, isoSurfaceFilter.FieldName).SelectMany(g => g)
														   from d in LoadData(parentLayerId, parentLayer.DataFallbackLayerId, index)
														   group d by d.TimeStep;
							return new MeshIsoSurfaceCreator(isoSurfaceFilter, dataComponentDescriptors.ToDictionary(g => g.Key, g => g.OrderBy(d => d.ComponentName).ToList()));
						}
					default:
						throw new NotSupportedException();
				}
			}

			string constructFilterLayerName()
			{
				switch (filter)
				{
					case SurfaceFilter _:
						return layerName ?? "surface";
					case SliceFilter sliceFilter:
						return layerName ?? $"slice {sliceFilter.Offset}";
					case AttributeSelectionFilter attributeSelectionFilter:
						return layerName ?? $"{attributeSelectionFilter.AttributeName}: {string.Join(", ", attributeSelectionFilter.AttributeSelection)}";
					case DeformationFilter deformationFilter:
						return layerName ?? $"deformation (scale: {deformationFilter.RelativeScale?.ToString(CultureInfo.InvariantCulture)})".TrimEnd(); // TODO: use FormattableString.Invariant
					default:
						throw new NotSupportedException();
				}
			}

			MeshFileDescriptor constructFilteredMesh(GeometryDescription filteredGeometry, IReadOnlyList<decimal> timeSteps, IEnumerable<DataFileDescriptor> attributes)
			{
				// do not include attributes and data if mapping would be 1 : 1 (filteredGeometry.Mapping is null or Mapping is IdentityGeometryMapping)

				// filter attributes
				IEnumerable<AttributeDescription> filteredAttributeDescriptions = filterAttributesByGeometry(
					filteredGeometry,
					originalLayer: parentLayer,
					originalAttributeIndices: attributes.Select(a => a.Index)
				);

				return generateMeshFileAndAttributeFiles(meshIndex++, newLayerId, filteredGeometry, filteredAttributeDescriptions, timeSteps, ref attributeIndex);
			}
		}

		public SummaryFile CompressLayer(Guid layerId, IEnumerable<decimal> keyTimeSteps, string layerName = null, string fieldName = null)
		{
			// find layer in storage and download summary
			SummaryFile layerSummary = LoadLayerSummary(layerId);

			Guid compressedLayerId = Guid.NewGuid();
			var meshFileDescriptors = new List<MeshFileDescriptor>();
			int attributeIndex = 1;
			int resultIndex = 1;
			foreach (var mesh in layerSummary.Meshes)
			{
				GeometryDescription geometry = LoadGeometry(layerId, layerSummary.MeshFallbackLayerId, mesh.Index);
				IEnumerable<AttributeDescription> attributeDescriptions = mesh.Attributes.Select(a => LoadAttribute(layerId, layerSummary.AttributeFallbackLayerId, a.Index));
				var meshFileDesriptor = generateMeshFileAndAttributeFiles(mesh.Index, compressedLayerId, geometry, attributeDescriptions, mesh.TimeSteps, ref attributeIndex);
				meshFileDescriptors.Add(meshFileDesriptor);
			}
			var dataDescriptionGroups = from g in getResultIndicesGroupedByTimeStep(layerSummary, fieldName)
										from list in createDataDescriptionGroups(g.SelectMany(index => LoadData(layerId, layerSummary.DataFallbackLayerId, index)), keyTimeSteps)
										select list;
			Func<decimal, int> meshIndexFromTimeStepProvider = createMeshIndexFromTimeStepProvider(meshFileDescriptors);
			var resultDescriptors = generateDataFilesForFields(compressedLayerId, meshIndexFromTimeStepProvider, dataDescriptionGroups, ref resultIndex);
			var fieldDescriptors = convertDataFileDescriptorsToFieldDescriptors(resultDescriptors, meshIndexFromTimeStepProvider);
			return generateSummaryFile(layerName ?? "time compression", layerSummary, compressedLayerId, new TimeCompressionFilter { FieldName = fieldName }, meshFileDescriptors, fieldDescriptors);
		}

		public LayerDiff CreateDiff(Guid layerId)
		{
			SummaryFile childLayerSummary;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(layerId)))
			{
				childLayerSummary = serializationService.Deserialize<SummaryFile>(stream);
			}

			if (childLayerSummary.Filter?.Type != FilterType.TimeCompression)
			{
				logger?.LogWarning($"'{FilterType.TimeCompression}' layer filter was expected instead of '{childLayerSummary.Filter?.Type}'");
			}

			if (!childLayerSummary.ParentId.HasValue)
				throw new ArgumentException("Layer is master layer (has no parent), can't create diff.");

			SummaryFile parentLayerSummary;
			using (var stream = sourceStorage.Load(getLayerSummaryRecordName(childLayerSummary.ParentId.Value)))
			{
				parentLayerSummary = serializationService.Deserialize<SummaryFile>(stream);
			}

			var componentDiffs = new List<ComponentDiff>();

			{

				var parentResults = from resultIndex in getResultIndicesGroupedByTimeStep(parentLayerSummary).SelectMany(g => g)
									from data in LoadData(parentLayerSummary.Id, parentLayerSummary.DataFallbackLayerId, resultIndex)
									select data;

				var childResults = from resultIndex in getResultIndicesGroupedByTimeStep(childLayerSummary).SelectMany(g => g)
								   from data in LoadData(childLayerSummary.Id, childLayerSummary.DataFallbackLayerId, resultIndex)
								   select data;

				var timeStepGroups = from a in parentResults
									 join b in childResults on new { a.FieldName, a.ComponentName, a.TimeStep } equals new { b.FieldName, b.ComponentName, b.TimeStep }
									 group (a, b) by new { a.FieldName, a.ComponentName } into g
									 select g;


				foreach (var timeStepGroup in timeStepGroups)
				{
					var componentDiff = ComponentDiff.CreateFrom(timeStepGroup);
					logger?.LogOperationProgress("  " + componentDiff.ToString());
					componentDiffs.Add(componentDiff);
				}
			}

			return LayerDiff.CreateFrom(componentDiffs);
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

		public GeometryDescription LoadGeometry(Guid layerId, Guid? fallbackLayerId, int meshIndex)
		{
			string record = getLayerMeshRecordName(fallbackLayerId ?? layerId, meshIndex);
			using (Stream meshStream = sourceStorage.Load(record))
			{
				MeshFile layerMesh = serializationService.Deserialize<MeshFile>(meshStream);
				return createGeometryFromLayerMesh(layerMesh);
			}
		}

		public async Task<GeometryDescription> LoadGeometryAsync(Guid layerId, Guid? fallbackLayerId, int meshIndex, CancellationToken cancellationToken)
		{
			string record = getLayerMeshRecordName(fallbackLayerId ?? layerId, meshIndex);
			using (Stream meshStream = sourceStorage.Load(record))
			{
				MeshFile layerMesh = await serializationService.DeserializeAsync<MeshFile>(meshStream, cancellationToken);
				return createGeometryFromLayerMesh(layerMesh);
			}
		}

		public IEnumerable<ComponentDataDescription> LoadData(Guid layerId, Guid? fallbackLayerId, int dataIndex)
		{
			string record = getLayerResultRecordName(fallbackLayerId ?? layerId, dataIndex);
			using (Stream stream = sourceStorage.Load(record))
			{
				DataFile layerResult = serializationService.Deserialize<DataFile>(stream);
				return createDataDescriptionsFromLayerResultForAllTimeSteps(layerResult);
			}
		}

		public async Task<ComponentDataDescription> LoadDataForSingleTimeStepAsync(Guid layerId, Guid? fallbackLayerId, int dataIndex, decimal timeStep, CancellationToken cancellationToken)
		{
			string record = getLayerResultRecordName(fallbackLayerId ?? layerId, dataIndex);
			using (Stream stream = sourceStorage.Load(record))
			{
				DataFile layerResult = await serializationService.DeserializeAsync<DataFile>(stream, cancellationToken);
				return createDataDescriptionFromLayerResultForSingleTimeStep(layerResult, timeStep);
			}
		}

		public AttributeDescription LoadAttribute(Guid layerId, Guid? fallbackLayerId, int attributeIndex)
		{
			string record = getLayerAttributeRecordName(fallbackLayerId ?? layerId, attributeIndex);
			using (Stream attributeStream = sourceStorage.Load(record))
			{
				DataFile layerAttributes = serializationService.Deserialize<DataFile>(attributeStream);
				return createAttributeDescriptionFromDataLayerAttribute(layerAttributes);
			}
		}

		public async Task<AttributeDescription> LoadAttributeAsync(Guid layerId, Guid? fallbackLayerId, int attributeIndex, CancellationToken cancellationToken)
		{
			string record = getLayerAttributeRecordName(fallbackLayerId ?? layerId, attributeIndex);
			using (Stream attributeStream = sourceStorage.Load(record))
			{
				DataFile layerAttributes = await serializationService.DeserializeAsync<DataFile>(attributeStream, cancellationToken);
				return createAttributeDescriptionFromDataLayerAttribute(layerAttributes);
			}
		}

		#endregion

		#region Private methods

		private IEnumerable<AttributeDescription> filterAttributesByGeometry(GeometryDescription filteredGeometry, SummaryFile originalLayer, IEnumerable<int> originalAttributeIndices)
		{
			if (filteredGeometry.Mapping is IdentityGeometryEntityMapping)
				yield break;

			IFilterGeometryEntityMapping mapping = (IFilterGeometryEntityMapping)filteredGeometry.Mapping;
			foreach (AttributeDescription originalAttribute in originalAttributeIndices.Select(attributeIndex => LoadAttribute(originalLayer.Id, originalLayer.AttributeFallbackLayerId, attributeIndex)))
			{
				int[] newValues;
				switch (originalAttribute.Location)
				{
					case DataLocationType.Points:
						newValues = new int[filteredGeometry.NumberOfPoints];
						for (int newPointIndex = 0; newPointIndex < newValues.Length; newPointIndex++)
						{
							if (mapping.TryMapPoint(newPointIndex, out int oldIndex))
							{
								newValues[newPointIndex] = originalAttribute.Values[oldIndex];
							}
							else if (mapping.TryMapPointEdgeIntersection(newPointIndex, out EdgeIntersection oldEdgeIntersection))
							{
								newValues[newPointIndex] = interpolateAttributeValue(
									firstAttributeValue: originalAttribute.Values[oldEdgeIntersection.FirstPointId],
									secondAttributeValue: originalAttribute.Values[oldEdgeIntersection.SecondPointId],
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
								if (mapping.TryMapCellPoint(newCellPointIndex, out int oldCellPointIndex))
								{
									newValues[newCellPointIndex] = originalAttribute.Values[oldCellPointIndex];
								}
								else if (mapping.TryMapCellPointEdgeIntersection(newCellPointIndex, out EdgeIntersection oldEdgeIntersection))
								{
									newValues[newCellPointIndex] = interpolateAttributeValue(
										firstAttributeValue: originalAttribute.Values[oldEdgeIntersection.FirstPointId],
										secondAttributeValue: originalAttribute.Values[oldEdgeIntersection.SecondPointId],
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
							if (mapping.TryMapCell(newCellIndex, out int oldIndex))
							{
								newValues[newCellIndex] = originalAttribute.Values[oldIndex];
							}
							//else -> no attribute value (zero is default)
						}
						break;
					default:
						throw new NotSupportedException();
				}

				AttributeDescription newAttribute = new AttributeDescription
				{
					Name = originalAttribute.Name,
					Location = originalAttribute.Location,
					Values = newValues
				};

				yield return newAttribute;
			}
		}

		private IEnumerable<ComponentDataDescription> filterDataByGeometry(IDictionary<decimal, GeometryDescription> filteredGeometryMap, SummaryFile originalLayer, IEnumerable<int> originalResultDataIndices)
		{
			const double EMPTY_VALUE = double.NaN;
			foreach (int originalDataIndex in originalResultDataIndices)
			{
				foreach (ComponentDataDescription originalResult in LoadData(originalLayer.Id, originalLayer.DataFallbackLayerId, originalDataIndex))
				{
					GeometryDescription filteredGeometry;
					if (!filteredGeometryMap.TryGetValue(originalResult.TimeStep, out filteredGeometry))
						continue;

					if (filteredGeometry.Mapping is IdentityGeometryEntityMapping)
						continue;

					IFilterGeometryEntityMapping mapping = (IFilterGeometryEntityMapping)filteredGeometry.Mapping;
					double[] newValues;

					switch (originalResult.Location)
					{
						case DataLocationType.Points:
							newValues = new double[filteredGeometry.NumberOfPoints];
							for (int newPointIndex = 0; newPointIndex < filteredGeometry.NumberOfPoints; newPointIndex++)
							{
								if (mapping.TryMapPoint(newPointIndex, out int oldPointIndex))
								{
									newValues[newPointIndex] = originalResult.Values[oldPointIndex];
								}
								else if (mapping.TryMapPointEdgeIntersection(newPointIndex, out EdgeIntersection oldEdgeIntersection))
								{
									newValues[newPointIndex] = interpolateDataValue(
										firstDataValue: originalResult.Values[oldEdgeIntersection.FirstPointId],
										secondDataValue: originalResult.Values[oldEdgeIntersection.SecondPointId],
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
								if (mapping.TryMapCellPoint(newCellPointIndex, out int oldCellPointIndex))
								{
									newValues[newCellPointIndex] = originalResult.Values[oldCellPointIndex];
								}
								else if (mapping.TryMapCellPointEdgeIntersection(newCellPointIndex, out EdgeIntersection oldEdgeIntersection))
								{
									newValues[newCellPointIndex] = interpolateDataValue(
										firstDataValue: originalResult.Values[oldEdgeIntersection.FirstPointId],
										secondDataValue: originalResult.Values[oldEdgeIntersection.SecondPointId],
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
								if (mapping.TryMapCell(newCellIndex, out int oldCellIndex))
								{
									newValues[newCellIndex] = originalResult.Values[oldCellIndex];
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
						FieldName = originalResult.FieldName,
						TimeStep = originalResult.TimeStep,
						ComponentName = originalResult.ComponentName,

						Location = originalResult.Location,
						Values = newValues
					};

					yield return newResult;
				}
			}
		}


		private IEnumerable<IEnumerable<int>> getResultIndicesGroupedByTimeStep(SummaryFile summaryFile, string fieldName = null)
		{
			//from result in results
			//where (fieldName == null || fieldName == result.FieldName)
			//group result by new { result.FieldName, result.ComponentName } into resultGroup

			foreach (var field in summaryFile.Fields.Keys)
			{
				if (fieldName == null || fieldName == field)
				{
					foreach (var component in summaryFile.Fields[field].Components.Keys)
					{
						yield return groupIterator().Distinct().ToList();

						IEnumerable<int> groupIterator()
						{
							foreach (var time in summaryFile.Fields[field].Components[component].TimeSteps)
							{
								yield return time.Value.DataIndex;
							}
						}
					}
				}
			}
		}

		private Func<decimal, int> createMeshIndexFromTimeStepProvider(IEnumerable<MeshFileDescriptor> meshFileDescriptors)
		{
			var map = new Dictionary<decimal, int>();
			foreach (var meshFileDescriptor in meshFileDescriptors)
			{
				foreach (var timeStep in meshFileDescriptor.TimeSteps)
				{
					map.Add(timeStep, meshFileDescriptor.Index);
				}
			}
			return t => map[t];
		}

		private IEnumerable<decimal> findAllTimeStepsSorted(IEnumerable<DataFileDescriptor> resultDescriptors)
		{
			var sortedSet = new SortedSet<decimal>();
			foreach (var resultDescriptor in resultDescriptors)
			{
				foreach (var timeStep in resultDescriptor.TimeSteps)
				{
					sortedSet.Add(timeStep);
				}
			}
			return sortedSet;
		}


		private IEnumerable<IReadOnlyList<DataDescription>> createDataDescriptionGroups(IEnumerable<DataDescription> dataDescriptions, IEnumerable<decimal> keyTimeSteps)
		{
			Debug.Assert(keyTimeSteps != null);
			decimal[] keyTimes = keyTimeSteps.ToArray();
			int keyTimeIndex = 0;
			List<DataDescription> dataListForCurrentTimeInterval = new List<DataDescription>();
			foreach (var dataComponent in dataDescriptions)
			{
				if (keyTimeIndex < keyTimes.Length && dataComponent.TimeStep >= keyTimes[keyTimeIndex])
				{
					if (dataListForCurrentTimeInterval.Count > 0)
					{
						yield return dataListForCurrentTimeInterval;
						dataListForCurrentTimeInterval = new List<DataDescription>();
					}
					keyTimeIndex += 1;
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
			return firstDataValue + edgeCoordinate * (secondDataValue - firstDataValue);
		}

		private MeshFileDescriptor generateMeshFileAndAttributeFiles(int meshIndex, Guid layerId, GeometryDescription geometry, IEnumerable<AttributeDescription> attributeDescriptions, IReadOnlyList<decimal> timeSteps, ref int attributeIndex)
		{
			logger?.LogOperationProgress("Generating mesh file" + buildTimeStepStatusText(timeSteps?.Count ?? 0, timeSteps?[0]));
			MeshFile layerMesh = createLayerMeshFromGeometry(geometry, layerId, meshIndex);
			storeLayerFile(layerMesh, getLayerMeshRecordName(layerId, meshIndex));

			// process attributes
			var attributeDescriptors = new List<DataFileDescriptor>();
			foreach (var attribute in attributeDescriptions)
			{
				logger?.LogOperationProgress($"Generating attribute file '{attribute.Name}'");

				DataFile elementPropertyAttributeLayer = createAttributeLayerFile(attribute.Name, attribute.Values, attribute.Location, layerId, attributeIndex, meshIndex);
				storeLayerFile(elementPropertyAttributeLayer, getLayerAttributeRecordName(layerId, elementPropertyAttributeLayer.Index));
				attributeDescriptors.Add(DataFileDescriptor.CreateFrom(elementPropertyAttributeLayer));
				attributeIndex++;
			}

			MeshFileDescriptor meshFileDescriptor = new MeshFileDescriptor
			{
				Index = meshIndex,
				Attributes = attributeDescriptors.ToArray(),
				TimeSteps = timeSteps?.ToArray()
			};

			return meshFileDescriptor;
		}

		private IEnumerable<DataFileDescriptor> generateDataFilesForFields(Guid layerId, Func<decimal, int> meshIndexFromTimeStepProvider, IEnumerable<IReadOnlyList<DataDescription>> dataDescriptionGroups, ref int resultIndex)
		{
			// process results
			var resultDescriptors = new List<DataFileDescriptor>();
			var compressionCounter = new CompressionCounter();
			foreach (var dataDescriptionGroup in dataDescriptionGroups)
			{
				DataDescription firstDataField = dataDescriptionGroup.FirstOrDefault();
				if (firstDataField != null)
				{
					int meshIndex = meshIndexFromTimeStepProvider(firstDataField.TimeStep);
					IEnumerable<DataDescription> restDataFields = dataDescriptionGroup.Skip(1);

					for (int componentIndex = 0; componentIndex < firstDataField.NumberOfComponents; componentIndex++)
					{
						logger?.LogOperationProgress($"Generating result file for field '{firstDataField.FieldName}' component '{firstDataField.GetComponentName(componentIndex)}'" + buildTimeStepStatusText(dataDescriptionGroup.Count, firstDataField.TimeStep));

						var layerResult = createLayerResultFromDataDescriptions(firstDataField, restDataFields, dataDescriptionGroup.Count, componentIndex, layerId, resultIndex, meshIndex);
						resultDescriptors.Add(DataFileDescriptor.CreateFrom(layerResult));

						storeLayerFile(layerResult, getLayerResultRecordName(layerId, layerResult.Index));

						compressionCounter.Increment(layerResult.Compression, layerResult.Encoding);
						resultIndex += 1;
					}
				}
			}

			logger?.LogMessage(compressionCounter.ToString());

			return resultDescriptors;
		}

		private static string buildTimeStepStatusText(int numberOfTimeSteps, decimal? firstTimeStep)
		{
			if (numberOfTimeSteps <= 0)
				return "";
			if (numberOfTimeSteps == 1)
				return $" (time step: {firstTimeStep})";
			return $" ({numberOfTimeSteps} time steps)";
		}

		private Dictionary<string, FieldDescriptor> convertDataFileDescriptorsToFieldDescriptors(IEnumerable<DataFileDescriptor> resultDescriptors, Func<decimal, int> meshIndexFromTimeStepProvider)
		{
			var fields = new Dictionary<string, FieldDescriptor>();
			foreach (var resultDescriptor in resultDescriptors)
			{
				if (!fields.TryGetValue(resultDescriptor.FieldName, out var field))
					field = fields[resultDescriptor.FieldName] = new FieldDescriptor { Components = new Dictionary<string, ComponentDescriptor>() };
				if (!field.Components.TryGetValue(resultDescriptor.ComponentName, out var component))
					component = field.Components[resultDescriptor.ComponentName] = new ComponentDescriptor { TimeSteps = new Dictionary<decimal, TimeStepDescriptor>() };
				foreach (var timeStep in resultDescriptor.TimeSteps)
				{
					component.TimeSteps.Add(timeStep, new TimeStepDescriptor { DataIndex = resultDescriptor.Index, MeshIndex = meshIndexFromTimeStepProvider(timeStep) });
				}
			}
			return fields;
		}

		private static void mergeFieldDescriptors(Dictionary<string, FieldDescriptor> baseFields, Dictionary<string, FieldDescriptor> mergeWithFields)
		{
			foreach (var (k, d) in mergeWithFields)
			{
				if (baseFields.TryGetValue(k, out var field))
				{
					mergeComponentDescriptors(field.Components, d.Components);
				}
				else
				{
					baseFields.Add(k, d);
				}
			}

			void mergeComponentDescriptors(Dictionary<string, ComponentDescriptor> baseComponents, Dictionary<string, ComponentDescriptor> mergeWithComponents)
			{
				foreach (var (k, d) in mergeWithComponents)
				{
					if (baseComponents.TryGetValue(k, out var component))
					{
						mergeTimeStepDescriptors(component.TimeSteps, d.TimeSteps);
					}
					else
					{
						baseComponents.Add(k, d);
					}
				}

				void mergeTimeStepDescriptors(Dictionary<decimal, TimeStepDescriptor> baseTimeSteps, Dictionary<decimal, TimeStepDescriptor> mergeWithTimeSteps)
				{
					foreach (var (k, d) in mergeWithTimeSteps)
					{
						baseTimeSteps.Add(k, d); // no conflict expected
					}
				}
			}
		}

		private SummaryFile generateSummaryFile(string layerName, SummaryFile parentLayer, Guid newLayerId, Filter filter, IEnumerable<MeshFileDescriptor> meshFileDescriptors, Dictionary<string, FieldDescriptor> fieldDescriptors)
		{
			logger?.LogOperationProgress("Generating summary file");

			SummaryFile layerSummary = new SummaryFile
			{
				Id = newLayerId,
				Name = layerName,
				ParentId = parentLayer?.Id,
				Filter = filter,
				Meshes = meshFileDescriptors.ToArray(),
				Fields = fieldDescriptors
			};

			// specialize attribute and field descriptors for deformation filter
			if (filter is DeformationFilter)
			{
				Debug.Assert(parentLayer != null);

				copyAttributeDescriptions(parentLayer, layerSummary);
				layerSummary.AttributeFallbackLayerId = parentLayer.AttributeFallbackLayerId ?? parentLayer.Id;

				copyFieldDescriptions(parentLayer, layerSummary);
				layerSummary.DataFallbackLayerId = parentLayer.DataFallbackLayerId ?? parentLayer.Id;
			}

			storeLayerFile(layerSummary, getLayerSummaryRecordName(newLayerId));

			return layerSummary;

			void copyAttributeDescriptions(SummaryFile source, SummaryFile destination)
			{
				var sourceTimeMeshMap = new Dictionary<decimal, MeshFileDescriptor>();
				foreach (var mesh in source.Meshes)
				{
					foreach (var timeStep in mesh.TimeSteps)
					{
						sourceTimeMeshMap.Add(timeStep, mesh);
					}
				}

				// copy attributes
				foreach (var mesh in destination.Meshes)
				{
					var distinctAttributes = new HashSet<DataFileDescriptor>();
					foreach (var timeStep in mesh.TimeSteps)
					{
						foreach (var attribute in sourceTimeMeshMap[timeStep].Attributes)
						{
							distinctAttributes.Add(attribute);
						}
					}
					mesh.Attributes = distinctAttributes.OrderBy(a => a.Index).Select(a => DataFileDescriptor.CreateFrom(a)).ToArray();
				}
			}

			void copyFieldDescriptions(SummaryFile source, SummaryFile destination)
			{
				var destinationTimeMeshMap = new Dictionary<decimal, MeshFileDescriptor>();
				foreach (var mesh in destination.Meshes)
				{
					foreach (var timeStep in mesh.TimeSteps)
					{
						destinationTimeMeshMap.Add(timeStep, mesh);
					}
				}
				destination.Fields = createFieldsFrom(source.Fields);

				Dictionary<string, FieldDescriptor> createFieldsFrom(Dictionary<string, FieldDescriptor> sourceFields)
				{
					var fields = new Dictionary<string, FieldDescriptor>();
					foreach (var (fieldName, fieldDescriptor) in sourceFields)
					{
						fields.Add(fieldName, createFieldFrom(fieldDescriptor));
					}
					return fields;

					FieldDescriptor createFieldFrom(FieldDescriptor sourceField)
					{
						var field = new FieldDescriptor { Components = new Dictionary<string, ComponentDescriptor>() };
						foreach (var (componentName, componentDescriptor) in sourceField.Components)
						{
							field.Components.Add(componentName, createComponentFrom(componentDescriptor));
						}
						return field;

						ComponentDescriptor createComponentFrom(ComponentDescriptor sourceComponent)
						{
							var component = new ComponentDescriptor { TimeSteps = new Dictionary<decimal, TimeStepDescriptor>() };
							foreach (var (timeStep, timeStepDescriptor) in sourceComponent.TimeSteps)
							{
								var newTimeStepDescriptor = new TimeStepDescriptor
								{
									DataIndex = timeStepDescriptor.DataIndex,
									MeshIndex = destinationTimeMeshMap[timeStep].Index
								};
								component.TimeSteps.Add(timeStep, newTimeStepDescriptor);
							}
							return component;
						}
					}
				}
			}
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
			geometry.NumberOfCoordinateComponents = (layerMesh.NumberOfPoints > 0) ? geometry.PointCoordinates.Length / layerMesh.NumberOfPoints : 0;
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

		private IEnumerable<ComponentDataDescription> createDataDescriptionsFromLayerResultForAllTimeSteps(DataFile layerResult)
		{
			int timeStepIndex = 0;
			foreach (double[] decompressedData in decodeAndDecompressAllRows(layerResult.Data, layerResult.Encoding, layerResult.Compression))
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

		private ComponentDataDescription createDataDescriptionFromLayerResultForSingleTimeStep(DataFile layerResult, decimal timeStep)
		{
			int timeStepIndex = Array.IndexOf(layerResult.TimeSteps, timeStep);
			Debug.Assert(timeStepIndex >= 0);
			double[] decompressedData = decodeAndDecompressSingleRow(layerResult.Data, timeStepIndex, layerResult.Encoding, layerResult.Compression);
			var result = new ComponentDataDescription
			{
				FieldName = layerResult.FieldName,
				TimeStep = timeStep,
				ComponentName = layerResult.ComponentName,
				Location = layerResult.Location,
				Values = decompressedData
			};
			return result;
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

		private IEnumerable<double[]> decodeAndDecompressAllRows(string data, EncodingParameters encodingParameters, CompressionParameters compressionParameters)
		{
			double[] compressedValues = encodingService.Decode<double>(data, TrimOptions.BeginEnd, encodingParameters);
			ICompressionService selectedCompressionService = CompressionServiceFactory.Create(compressionParameters.Method, logger);
			return selectedCompressionService.Decompress(compressedValues, compressionParameters);
		}

		private double[] decodeAndDecompressSingleRow(string data, int rowIndex, EncodingParameters encodingParameters, CompressionParameters compressionParameters)
		{
			double[] compressedValues = encodingService.Decode<double>(data, TrimOptions.BeginEnd, encodingParameters);
			ICompressionService selectedCompressionService = CompressionServiceFactory.Create(compressionParameters.Method, logger);
			return selectedCompressionService.Decompress(compressedValues, rowIndex, compressionParameters);
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
			return encodingService.Encode(geometryData, trimEnd ? TrimOptions.End : TrimOptions.None, out _);
		}

		private T[] decodeGeometryDataArray<T>(string data, bool expandEnd, int originalLength = 0) where T : struct
		{
			EncodingParameters encodingParameters = new EncodingParameters { OriginalLength = originalLength, Length = originalLength };
			return encodingService.Decode<T>(data, expandEnd ? TrimOptions.End : TrimOptions.None, encodingParameters);
		}

		private string getLayerSummaryRecordName(Guid layerId) => $"{layerId}/summary{serializationService.FileExtension}";

		private string getLayerMeshRecordName(Guid layerId, int index) => $"{layerId}/{index}.mesh{serializationService.FileExtension}";

		private string getLayerAttributeRecordName(Guid layerId, int index) => $"{layerId}/{index}.attribute{serializationService.FileExtension}";

		private string getLayerResultRecordName(Guid layerId, int index) => $"{layerId}/{index}.result{serializationService.FileExtension}";

		#endregion
	}
}

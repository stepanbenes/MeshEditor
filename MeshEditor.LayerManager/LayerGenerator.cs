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

		public SummaryFile GenerateMasterLayer(string layerName, IEnumerable<IAnalysisResultImportService> analysisResultImportServices, IEnumerable<double> keyTimeSteps, string fieldName = null)
		{
			if (analysisResultImportServices == null)
			{
				throw new ArgumentNullException(nameof(analysisResultImportServices));
			}

			Guid newLayerId = Guid.NewGuid();
			int attributeIndex = 1;
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

				var meshDescriptor = generateDataFilesForMesh(meshDescriptors.Count + 1, newLayerId, geometry, attributeDescriptions, /*timeSteps:*/ null, ref attributeIndex);

				Func<double, int> meshIndexFromTimeStepProvider = _ => meshDescriptor.Index;
				var resultDescriptors = generateDataFilesForFields(newLayerId, meshIndexFromTimeStepProvider, dataDescriptionGroups: dataGroups);
				var fieldsMap = convertDataFileDescriptorsToFieldDescriptors(resultDescriptors, meshIndexFromTimeStepProvider);
				foreach (var (fn, fd) in fieldsMap)
				{
					fieldDescriptors.Add(fn, fd);
				}

				meshDescriptor.TimeSteps = findAllTimeStepsSorted(resultDescriptors).ToArray();
				meshDescriptors.Add(meshDescriptor);
			}
			return generateSummaryFile(layerName, null, newLayerId, null, meshDescriptors, fieldDescriptors);
		}

		public SummaryFile GenerateFilterLayer(Guid parentLayerId, Filter filter, string layerName, IEnumerable<double> keyTimeSteps, string fieldName = null)
		{
			// find parentLayer in storage and download summary
			SummaryFile parentLayer = LoadLayerSummary(parentLayerId);

			string filterLayerName = constructFilterLayerName();

			Guid newLayerId = Guid.NewGuid();
			var meshFileDescriptors = new List<MeshFileDescriptor>();
			int meshIndex = 1;
			int attributeIndex = 1;
			var filteredGeometryMap = new Dictionary<double, GeometryDescription>();

			foreach (var parentMesh in parentLayer.Meshes)
			{
				GeometryDescription originalGeometry = LoadGeometry(parentLayerId, parentMesh.Index);
				if (originalGeometry.IsEmpty)
					throw new InvalidOperationException($"Geometry is empty (mesh index: {parentMesh.Index})");

				IMeshFilterCreator meshFilterCreator = constructMeshFilterCreator(parentMesh);

				var filteredGeometries = meshFilterCreator.Create(originalGeometry, parentMesh.TimeSteps);

				foreach (var (filteredGeometry, filteredMeshTimeSteps) in filteredGeometries)
				{
					if (filteredGeometry.IsEmpty)
						continue;
					var meshFileDescriptor = constructFilteredMesh(filteredGeometry, filteredMeshTimeSteps, parentMesh.Attributes);
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
												 from list in createDataDescriptionGroups(filterDataByGeometry(filteredGeometryMap, g.Select(index => getLayerResultRecordName(parentLayerId, index))), keyTimeSteps)
												 select list;
			}
			else
			{
				filteredDataDescriptionsChunks = from data in filterDataByGeometry(filteredGeometryMap, from index in getResultIndicesGroupedByTimeStep(parentLayer, fieldName).SelectMany(g => g)
																										select getLayerResultRecordName(parentLayerId, index))
												 select new[] { data };
			}

			Func<double, int> meshIndexFromTimeStepProvider = createMeshIndexFromTimeStepProvider(meshFileDescriptors);
			var resultDescriptors = generateDataFilesForFields(newLayerId, meshIndexFromTimeStepProvider, filteredDataDescriptionsChunks);
			var fieldDescriptors = convertDataFileDescriptorsToFieldDescriptors(resultDescriptors, meshIndexFromTimeStepProvider);
			return generateSummaryFile(filterLayerName, parentLayerId, newLayerId, filter, meshFileDescriptors, fieldDescriptors);

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
							var attribute = LoadAttribute(parentLayerId, attributeDescriptor.Index);
							return new MeshPartitionCreator(attributeSelectionFilter, attribute);
						}
					case DeformationFilter deformationFilter:
						{
							var dataComponentDescriptors = from index in getResultIndicesGroupedByTimeStep(parentLayer, deformationFilter.DeformationFieldName).SelectMany(g => g)
														   from d in LoadData(parentLayerId, index)
														   group d by d.TimeStep;
							return new DeformedMeshCreator(deformationFilter, dataComponentDescriptors.ToDictionary(g => g.Key, g => g.OrderBy(d => d.ComponentName).ToList()));
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
					case DeformationFilter _:
						return layerName ?? "deformed";
					default:
						throw new NotSupportedException();
				}
			}

			MeshFileDescriptor constructFilteredMesh(GeometryDescription filteredGeometry, IEnumerable<double> timeSteps, IEnumerable<DataFileDescriptor> attributes)
			{
				// do not include attributes and data if mapping would be 1 : 1 (filteredGeometry.Mapping is null or Mapping is IdentityGeometryMapping)

				// filter attributes
				var originalAttributeRecordNames = attributes.Select(a => getLayerAttributeRecordName(parentLayerId, a.Index));
				IEnumerable<AttributeDescription> filteredAttributeDescriptions = filterAttributesByGeometry(filteredGeometry, originalAttributeRecordNames);

				return generateDataFilesForMesh(meshIndex++, newLayerId, filteredGeometry, filteredAttributeDescriptions, timeSteps, ref attributeIndex);
			}
		}

		public SummaryFile CompressLayer(Guid layerId, IEnumerable<double> keyTimeSteps, string layerName = null, string fieldName = null)
		{
			// find layer in storage and download summary
			SummaryFile layerSummary = LoadLayerSummary(layerId);

			Guid compressedLayerId = Guid.NewGuid();
			var meshFileDescriptors = new List<MeshFileDescriptor>();
			int attributeIndex = 1;
			foreach (var mesh in layerSummary.Meshes)
			{
				GeometryDescription geometry = LoadGeometry(layerId, mesh.Index);
				IEnumerable<AttributeDescription> attributeDescriptions = mesh.Attributes.Select(a => LoadAttribute(layerId, a.Index));
				var meshFileDesriptor = generateDataFilesForMesh(mesh.Index, compressedLayerId, geometry, attributeDescriptions, mesh.TimeSteps, ref attributeIndex);
				meshFileDescriptors.Add(meshFileDesriptor);
			}
			var dataDescriptionGroups = from g in getResultIndicesGroupedByTimeStep(layerSummary, fieldName)
										from list in createDataDescriptionGroups(g.SelectMany(index => LoadData(layerId, index)), keyTimeSteps)
										select list;
			Func<double, int> meshIndexFromTimeStepProvider = createMeshIndexFromTimeStepProvider(meshFileDescriptors);
			var resultDescriptors = generateDataFilesForFields(compressedLayerId, meshIndexFromTimeStepProvider, dataDescriptionGroups);
			var fieldDescriptors = convertDataFileDescriptorsToFieldDescriptors(resultDescriptors, meshIndexFromTimeStepProvider);
			return generateSummaryFile(layerName ?? "time compression", layerId, compressedLayerId, new TimeCompressionFilter { FieldName = fieldName }, meshFileDescriptors, fieldDescriptors);
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
									from data in LoadData(parentLayerSummary.Id, resultIndex)
									select data;

				var childResults = from resultIndex in getResultIndicesGroupedByTimeStep(childLayerSummary).SelectMany(g => g)
								   from data in LoadData(childLayerSummary.Id, resultIndex)
								   select data;

				var timeStepGroups = from a in parentResults
									 join b in childResults on new { a.FieldName, a.ComponentName, a.TimeStep } equals new { b.FieldName, b.ComponentName, b.TimeStep }
									 group new Tuple<ComponentDataDescription, ComponentDataDescription>(a, b) by new { a.FieldName, a.ComponentName } into g
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
			IFilterGeometryEntityMapping mapping = (IFilterGeometryEntityMapping)filteredGeometry.Mapping;
			foreach (AttributeDescription oldAttribute in originalAttributeRecordNames.Select(record => loadAttribute(record)))
			{
				int[] newValues;
				switch (oldAttribute.Location)
				{
					case DataLocationType.Points:
						newValues = new int[filteredGeometry.NumberOfPoints];
						for (int newPointIndex = 0; newPointIndex < newValues.Length; newPointIndex++)
						{
							if (mapping.TryMapPoint(newPointIndex, out int oldIndex))
							{
								newValues[newPointIndex] = oldAttribute.Values[oldIndex];
							}
							else if (mapping.TryMapPointEdgeIntersection(newPointIndex, out EdgeIntersection oldEdgeIntersection))
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
								if (mapping.TryMapCellPoint(newCellPointIndex, out int oldCellPointIndex))
								{
									newValues[newCellPointIndex] = oldAttribute.Values[oldCellPointIndex];
								}
								else if (mapping.TryMapCellPointEdgeIntersection(newCellPointIndex, out EdgeIntersection oldEdgeIntersection))
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
							if (mapping.TryMapCell(newCellIndex, out int oldIndex))
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

		private IEnumerable<ComponentDataDescription> filterDataByGeometry(IDictionary<double, GeometryDescription> filteredGeometryMap, IEnumerable<string> originalResultRecordNames)
		{
			const double EMPTY_VALUE = double.NaN;
			foreach (ComponentDataDescription oldResult in originalResultRecordNames.SelectMany(record => loadData(record)))
			{
				var filteredGeometry = filteredGeometryMap[oldResult.TimeStep];
				IFilterGeometryEntityMapping mapping = (IFilterGeometryEntityMapping)filteredGeometry.Mapping;
				double[] newValues;

				switch (oldResult.Location)
				{
					case DataLocationType.Points:
						newValues = new double[filteredGeometry.NumberOfPoints];
						for (int newPointIndex = 0; newPointIndex < filteredGeometry.NumberOfPoints; newPointIndex++)
						{
							if (mapping.TryMapPoint(newPointIndex, out int oldPointIndex))
							{
								newValues[newPointIndex] = oldResult.Values[oldPointIndex];
							}
							else if (mapping.TryMapPointEdgeIntersection(newPointIndex, out EdgeIntersection oldEdgeIntersection))
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
							if (mapping.TryMapCellPoint(newCellPointIndex, out int oldCellPointIndex))
							{
								newValues[newCellPointIndex] = oldResult.Values[oldCellPointIndex];
							}
							else if (mapping.TryMapCellPointEdgeIntersection(newCellPointIndex, out EdgeIntersection oldEdgeIntersection))
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
							if (mapping.TryMapCell(newCellIndex, out int oldCellIndex))
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

		private Func<double, int> createMeshIndexFromTimeStepProvider(IEnumerable<MeshFileDescriptor> meshFileDescriptors)
		{
			var map = new Dictionary<double, int>();
			foreach (var meshFileDescriptor in meshFileDescriptors)
			{
				foreach (var timeStep in meshFileDescriptor.TimeSteps)
				{
					map.Add(timeStep, meshFileDescriptor.Index);
				}
			}
			return t => map[t];
		}

		private IEnumerable<double> findAllTimeStepsSorted(IEnumerable<DataFileDescriptor> resultDescriptors)
		{
			var sortedSet = new SortedSet<double>();
			foreach (var resultDescriptor in resultDescriptors)
			{
				foreach (var timeStep in resultDescriptor.TimeSteps)
				{
					sortedSet.Add(timeStep);
				}
			}
			//foreach (var field in fieldDescriptors.Keys)
			//{
			//	foreach (var component in fieldDescriptors[field].Components.Keys)
			//	{
			//		foreach (var time in fieldDescriptors[field].Components[component].TimeSteps.Keys)
			//		{
			//			sortedSet.Add(time);
			//		}
			//	}
			//}
			return sortedSet;
		}


		private IEnumerable<IReadOnlyList<DataDescription>> createDataDescriptionGroups(IEnumerable<DataDescription> dataDescriptions, IEnumerable<double> keyTimeSteps)
		{
			Debug.Assert(keyTimeSteps != null);
			double[] keyTimes = keyTimeSteps.ToArray();
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

		private MeshFileDescriptor generateDataFilesForMesh(int meshIndex, Guid layerId, GeometryDescription geometry, IEnumerable<AttributeDescription> attributeDescriptions, IEnumerable<double> timeSteps, ref int attributeIndex)
		{
			logger?.LogOperationProgress("Generating mesh file");

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

		private IEnumerable<DataFileDescriptor> generateDataFilesForFields(Guid layerId, Func<double, int> meshIndexFromTimeStepProvider, IEnumerable<IReadOnlyList<DataDescription>> dataDescriptionGroups)
		{
			int resultIndex = 1;

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

			return resultDescriptors;
		}

		private Dictionary<string, FieldDescriptor> convertDataFileDescriptorsToFieldDescriptors(IEnumerable<DataFileDescriptor> resultDescriptors, Func<double, int> meshIndexFromTimeStepProvider)
		{
			var fields = new Dictionary<string, FieldDescriptor>();
			foreach (var resultDescriptor in resultDescriptors)
			{
				if (!fields.TryGetValue(resultDescriptor.FieldName, out var field))
					field = fields[resultDescriptor.FieldName] = new FieldDescriptor { Components = new Dictionary<string, ComponentDescriptor>() };
				if (!field.Components.TryGetValue(resultDescriptor.ComponentName, out var component))
					component = field.Components[resultDescriptor.ComponentName] = new ComponentDescriptor { TimeSteps = new Dictionary<double, TimeStepDescriptor>() };
				foreach (var timeStep in resultDescriptor.TimeSteps)
				{
					component.TimeSteps.Add(timeStep, new TimeStepDescriptor { DataIndex = resultDescriptor.Index, MeshIndex = meshIndexFromTimeStepProvider(timeStep) });
				}
			}
			return fields;
		}

		private SummaryFile generateSummaryFile(string layerName, Guid? parentLayerId, Guid newLayerId, Filter filter, IEnumerable<MeshFileDescriptor> meshFileDescriptors, Dictionary<string, FieldDescriptor> fieldDescriptors)
		{
			logger?.LogOperationProgress("Generating summary file");

			SummaryFile layerSummary = new SummaryFile
			{
				Id = newLayerId,
				Name = layerName,
				ParentId = parentLayerId,
				Filter = filter,
				Meshes = meshFileDescriptors.ToArray(),
				Fields = fieldDescriptors
			};

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
			ICompressionService selectedCompressionService = CompressionServiceFactory.Create(compressionParameters.Method, logger);
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
			return encodingService.Encode(geometryData, trimEnd ? TrimOptions.End : TrimOptions.None, out _);
		}

		private T[] decodeGeometryDataArray<T>(string data, bool expandEnd, int originalLength = 0) where T : struct
		{
			EncodingParameters encodingParameters = new EncodingParameters { OriginalLength = originalLength, Length = originalLength };
			return encodingService.Decode<T>(data, expandEnd ? TrimOptions.End : TrimOptions.None, encodingParameters);
		}

		private string getLayerSummaryRecordName(Guid layerId)
		{
			return $"{layerId}/summary{serializationService.FileExtension}";
		}

		private string getLayerMeshRecordName(Guid layerId, int index)
		{
			return $"{layerId}/{index}.mesh{serializationService.FileExtension}";
		}

		private string getLayerAttributeRecordName(Guid layerId, int index)
		{
			return $"{layerId}/{index}.attribute{serializationService.FileExtension}";
		}

		private string getLayerResultRecordName(Guid layerId, int index)
		{
			return $"{layerId}/{index}.result{serializationService.FileExtension}";
		}

		#endregion
	}
}

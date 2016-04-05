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

namespace MeshEditor.LayerManager
{
	public class LayerGenerator
	{
		#region Fields, constructor

		private static readonly CellType DefaultCellType = CellType.TriangleLinear;

		IReadStorageService sourceStorage;
		IWriteStorageService destinationStorage;
		ISerializationService serializationService;
		ICompressionService compressionService;
		IEncodingService encodingService;
		IProgress<OperationState> progressReporter;

		public LayerGenerator(
			IReadStorageService sourceStorage = null,
			IWriteStorageService destinationStorage = null,
			ISerializationService serializationService = null,
			ICompressionService compressionService = null,
			IEncodingService encodingService = null,
			IProgress<OperationState> progressReporter = null)
		{
			this.sourceStorage = sourceStorage ?? new LocalFileSystemStorageService();
			this.destinationStorage = destinationStorage ?? new LocalFileSystemStorageService();
			this.serializationService = serializationService ?? new JsonSerializationService();
			this.compressionService = compressionService ?? new TransparentCompressionService();
			this.encodingService = encodingService ?? new Base64EncodingService();
			this.progressReporter = progressReporter;
		}

		#endregion

		#region Public methods

		#region Console app Entry points

		public SummaryLayerFile GenerateMasterLayer(Uri location, string layerName, IGeometryImportService geometryImportService, IDataImportService dataImportService = null)
		{
			if (geometryImportService == null)
			{
				throw new ArgumentNullException(nameof(geometryImportService));
			}

			IReadOnlyList<AttributeDescription> attributeDescriptions;
			GeometryDescription geometry = geometryImportService.ReadGeometry(out attributeDescriptions);
			IEnumerable<DataDescription> dataDescriptions = dataImportService?.ReadData(geometry) ?? Enumerable.Empty<DataDescription>();
			SummaryLayerFile layerFile = generateLayerFiles(location, layerName, geometry, attributeDescriptions, dataDescriptions, filter: null);
			return layerFile;
		}

		public SummaryLayerFile GenerateFilterLayer(Uri location, Guid parentLayerId, FilterBase filter, string layerName = null)
		{
			Uri layerDirectoryUri = new Uri(location, $"{parentLayerId}/");
			Uri parentLayerFileUri = new Uri(layerDirectoryUri, $"{parentLayerId}.layer.json");

			// find parentLayer in storage and download summary
			SummaryLayerFile parentLayer;
			using (var stream = sourceStorage.Load(parentLayerFileUri))
			{
				parentLayer = serializationService.Deserialize<SummaryLayerFile>(stream);
			}

			Uri meshFileUri = new Uri(layerDirectoryUri, $"{parentLayerId}.mesh.json");

			GeometryDescription originalGeometry = LoadGeometry(meshFileUri);
			GeometryDescription filteredGeometry;
			string filterLayerName;

			switch (filter.Type)
			{
				case FilterType.Surface:
				case FilterType.Slice:
				case FilterType.Clip:
				case FilterType.IsoSurface:
				case FilterType.StreamLines:
					throw new NotImplementedException();
				case FilterType.AttributeSelection:
					{
						var attributeSelectionFilter = (AttributeSelectionFilter)filter;
						var attributeDescriptor = parentLayer.Attributes.Single(a => a.FieldName == attributeSelectionFilter.AttributeName);
						var attributeFileUri = new Uri(layerDirectoryUri, $"{parentLayerId}.{attributeDescriptor.Index}.attribute.json");
						var attribute = LoadAttribute(attributeFileUri);
						filteredGeometry = filterGeometryByCellAttribute(originalGeometry, attribute, attributeSelectionFilter.AttributeSelection);
						filterLayerName = layerName ?? $"{attributeSelectionFilter.AttributeName}: {string.Join(", ", attributeSelectionFilter.AttributeSelection)}";
					}
					break;
				default:
					throw new NotSupportedException();
			}

			var originalResultFileUris = parentLayer.Results.Select(r => new Uri(layerDirectoryUri, $"{parentLayerId}.{r.Index}.result.json"));
			IEnumerable<DataDescription> filteredDataDescriptions = filterDataByGeometry(filteredGeometry, originalResultFileUris);

			var originalAttributeFileUris = parentLayer.Attributes.Select(a => new Uri(layerDirectoryUri, $"{parentLayerId}.{a.Index}.attribute.json"));
			IEnumerable<AttributeDescription> filteredAttributeDescriptions = filterAttributesByGeometry(filteredGeometry, originalAttributeFileUris);

			return generateLayerFiles(location, filterLayerName, filteredGeometry, filteredAttributeDescriptions, filteredDataDescriptions, filter);
		}

		public void CompressLayer(Uri location, Guid layerId, string field = null, string component = null)
		{
			string layerDirectory = $"{layerId}/";
			Uri layerFileUri = new Uri(location, $"{layerDirectory}{layerId}.layer.json");

			// find layer in storage and download summary
			SummaryLayerFile layerSummary;
			using (var stream = sourceStorage.Load(layerFileUri))
			{
				layerSummary = serializationService.Deserialize<SummaryLayerFile>(stream);
			}

			var resultGroups = from result in layerSummary.Results
							   where (field == null || field == result.FieldName) && (component == null || component == result.ComponentName)
							   group result by new { result.FieldName, result.ComponentName } into g
							   select g;

			var resultDescriptors = new List<DataLayerDescriptor>();
			int dataIndex = 1;
			foreach (var resultGroup in resultGroups)
			{
				Uri[] resultFileUris = resultGroup.Select(result => new Uri(location, $"{layerDirectory}{layerId}.{result.Index}.result.json")).ToArray();
				Debug.Assert(resultFileUris.Length > 0);
				DataDescription firstDataField = LoadData(resultFileUris[0]).Single(); // TODO: support already compressed results
				IEnumerable<DataDescription> restDataFields = resultFileUris.Skip(1).Select(uri => LoadData(uri).Single()); // TODO: support already compressed results
				DataLayerFile dataLayerFile = createLayerResultFromDataDescriptions(firstDataField, restDataFields, resultFileUris.Length, 0, layerId, dataIndex);

				storeLayerFile(dataLayerFile, location, layerDirectory, $"{layerId}.{dataIndex}.result");

				resultDescriptors.Add(DataLayerDescriptor.CreateFrom(dataLayerFile));
				dataIndex += 1;
			}

			// delete all previous result files (not overwritten by new result files)
			foreach (int dataIndexToDelete in layerSummary.Results.Select(r => r.Index).Where(i => i >= dataIndex))
			{
				Uri fileToDelete = new Uri(location, $"{layerDirectory}{layerId}.{dataIndexToDelete}.result.json");
				destinationStorage.Delete(fileToDelete);
			}

			layerSummary.Results = resultDescriptors.ToArray(); // update descriptors
			storeLayerFile(layerSummary, location, layerDirectory, $"{layerId}.layer"); // save updated summary file
		}

		public void AppendDataToLayer(Uri location, Guid layerId, IDataImportService dataImportService)
		{
			throw new NotImplementedException();
		}

		#endregion

		public GeometryDescription LoadGeometry(Uri meshFileUri)
		{
			using (Stream meshStream = sourceStorage.Load(meshFileUri))
			{
				MeshLayerFile layerMesh = serializationService.Deserialize<MeshLayerFile>(meshStream);
				return createGeometryFromLayerMesh(layerMesh);
			}
		}

		public IEnumerable<DataDescription> LoadData(Uri uri)
		{
			using (Stream stream = sourceStorage.Load(uri))
			{
				DataLayerFile layerResult = serializationService.Deserialize<DataLayerFile>(stream);
				return createDataDescriptionFromLayerResult(layerResult);
			}
		}

		public AttributeDescription LoadAttribute(Uri uri)
		{
			using (Stream attributeStream = sourceStorage.Load(uri))
			{
				DataLayerFile layerAttributes = serializationService.Deserialize<DataLayerFile>(attributeStream);
				return createAttributeDescriptionFromDataLayerAttribute(layerAttributes);
			}
		}

		#endregion

		#region Private methods

		private IEnumerable<AttributeDescription> filterAttributesByGeometry(GeometryDescription filteredGeometry, /*mapping, */ IEnumerable<Uri> originalAttributeFileUris)
		{
			throw new NotImplementedException();

			foreach (AttributeDescription data in originalAttributeFileUris.Select(uri => LoadAttribute(uri)))
			{
				// TODO
			}
		}

		private IEnumerable<DataDescription> filterDataByGeometry(GeometryDescription filteredGeometry, /*mapping, */ IEnumerable<Uri> originalResultFileUris)
		{
			throw new NotImplementedException();

			foreach (DataDescription data in originalResultFileUris.SelectMany(uri => LoadData(uri)))
			{
				// TODO
			}
		}

		private SummaryLayerFile generateLayerFiles(Uri location, string layerName, GeometryDescription geometry, IEnumerable<AttributeDescription> attributeDescriptions, IEnumerable<DataDescription> dataDescriptions, FilterBase filter)
		{
			Guid layerId = Guid.NewGuid();

			SummaryLayerFile layerSummary = new SummaryLayerFile
			{
				Id = layerId,
				Name = layerName,
				ParentId = null,
				Filter = filter,
			};

			string layerDirectory = $"{layerId}";

			progressReporter?.Report(new OperationState("Generating mesh file"));

			MeshLayerFile layerMesh = createLayerMeshFromGeometry(geometry, layerId);
			storeLayerFile(layerMesh, location, layerDirectory, $"{layerId}.mesh");

			int attributeIndex = 1, resultIndex = 1;

			var attributeDescriptors = new List<DataLayerDescriptor>();
			foreach (var attribute in attributeDescriptions)
			{
				progressReporter?.Report(new OperationState($"Generating attribute file '{attribute.Name}'"));

				DataLayerFile layerElementProperties = createAttributeLayerFile(attribute.Name, attribute.Values, DataLocationType.Cells, layerId, attributeIndex);
				storeLayerFile(layerElementProperties, location, layerDirectory, $"{layerId}.{layerElementProperties.Index}.attribute");
				attributeDescriptors.Add(DataLayerDescriptor.CreateFrom(layerElementProperties));
				attributeIndex++;
			}
			layerSummary.Attributes = attributeDescriptors.ToArray();

			var resultDescriptors = new List<DataLayerDescriptor>();
			var timeStepsHashSet = new HashSet<double>();
			foreach (var dataField in dataDescriptions)
			{
				progressReporter?.Report(new OperationState($"Generating result file for field '{dataField.Name}' (time step: {dataField.TimeStep})"));

				for (int componentIndex = 0; componentIndex < dataField.NumberOfComponents; componentIndex++)
				{
					var layerResult = createLayerResultFromDataDescriptions(dataField, Enumerable.Empty<DataDescription>(), 1, componentIndex, layerId, resultIndex);
					resultDescriptors.Add(DataLayerDescriptor.CreateFrom(layerResult));
					foreach (var timeStep in layerResult.TimeSteps)
					{
						timeStepsHashSet.Add(timeStep);
					}
					storeLayerFile(layerResult, location, layerDirectory, $"{layerId}.{layerResult.Index}.result");
					resultIndex += 1;
				}
			}

			layerSummary.TimeSteps = timeStepsHashSet.OrderBy(t => t).ToArray();
			layerSummary.Results = resultDescriptors.ToArray();

			progressReporter?.Report(new OperationState("Generating summary file"));

			storeLayerFile(layerSummary, location, layerDirectory, $"{layerId}.layer");

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
				DataDescription data = new DataDescription();

				data.Name = layerResult.FieldName;
				data.TimeStep = layerResult.TimeSteps[timeStepIndex++];
				data.ComponentNames = new[] { layerResult.ComponentName };
				data.FieldType = FieldType.Scalar;
				data.Location = layerResult.Location;
				data.NumberOfComponents = 1;
				data.Values = decompressedData;

				yield return data;
			}
		}

		private AttributeDescription createAttributeDescriptionFromDataLayerAttribute(DataLayerFile layerAttributes)
		{
			throw new NotImplementedException();
		}

		private GeometryDescription filterGeometryByCellAttribute(GeometryDescription geometry, AttributeDescription attribute, int[] selectionFilter)
		{
			List<CellType> cellTypes = new List<CellType>();
			HashSet<int> remainingPointIndices = new HashSet<int>();
			List<int> cellConnectivity = new List<int>();
			List<int> cellOffsets = new List<int>();
			Dictionary<int, int> oldNewCellIndexMap = new Dictionary<int, int>();

			for (int cellIndex = 0, previousOffset = 0; cellIndex < geometry.NumberOfCells; cellIndex++)
			{
				int currentOffset = geometry.CellOffsets[cellIndex];
				if (selectionFilter.Contains(attribute.Values[cellIndex]))
				{
					for (int offset = previousOffset; offset < currentOffset; offset++)
					{
						int pointIndex = geometry.CellConnectivity[offset];
						remainingPointIndices.Add(pointIndex);
						cellConnectivity.Add(pointIndex);
					}
					cellOffsets.Add(cellConnectivity.Count);
					cellTypes.Add(geometry.CellTypes[cellIndex]);
					oldNewCellIndexMap[cellIndex] = oldNewCellIndexMap.Count;
				}
				previousOffset = currentOffset;
			}

			int numberOfCoordinates = geometry.NumberOfCoordinateComponents;
			List<float> pointCoordinates = new List<float>();
			Dictionary<int, int> oldNewPointIndexMap = new Dictionary<int, int>();
			foreach (int oldPointIndex in remainingPointIndices.OrderBy(p => p))
			{
				for (int coordinateIndex = 0; coordinateIndex < numberOfCoordinates; coordinateIndex++)
				{
					pointCoordinates.Add(geometry.PointCoordinates[oldPointIndex * numberOfCoordinates + coordinateIndex]);
				}
				oldNewPointIndexMap[oldPointIndex] = oldNewPointIndexMap.Count;
			}

			// update cell connectivity (from old point indices to new point indices)
			for (int i = 0; i < cellConnectivity.Count; i++)
			{
				int oldPointIndex = cellConnectivity[i];
				int newPointIndex = oldNewPointIndexMap[oldPointIndex];
				cellConnectivity[i] = newPointIndex;
			}

			GeometryDescription filteredGeometry = new GeometryDescription
			{
				NumberOfCoordinateComponents = numberOfCoordinates,
				PointCoordinates = pointCoordinates.ToArray(),
				CellConnectivity = cellConnectivity.ToArray(),
				CellOffsets = cellOffsets.ToArray(),
				CellTypes = cellTypes.ToArray(),
				PointIdIndexMap = oldNewPointIndexMap,
				CellIdIndexMap = oldNewCellIndexMap
			};
			return filteredGeometry;
		}

		private void storeLayerFile<T>(T layerObject, Uri location, string layerDirectory, string recordName)
		{
			Uri uri = new Uri(location, Path.Combine(layerDirectory ?? "", recordName + serializationService.FileExtension));
			using (Stream stream = destinationStorage.Save(uri))
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
			ICompressionService selectedCompressionService = CompressionServiceFactory.Create(compressionParameters.Method);
			double[] compressedValues = encodingService.Decode<double>(data, TrimOptions.BeginEnd, encodingParameters);
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

		#endregion
	}
}

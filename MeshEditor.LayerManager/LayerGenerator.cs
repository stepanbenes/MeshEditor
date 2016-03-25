using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.DataTransformation;
using MeshEditor.LayerManager.Filters;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager
{
	public class LayerGenerator
	{
		#region Fields, constructor

		private static readonly CellType DefaultCellType = CellType.TriangleLinear;

		IStorageService storageService;
		ILayerSerializer layerSerializer;
		ICompressionService compressionService;
		IEncodingService encodingService;

		public LayerGenerator(
			IStorageService storageService = null,
			ILayerSerializer layerSerializer = null,
			ICompressionService compressionService = null,
			IEncodingService encodingService = null)
		{
			this.storageService = storageService ?? new LocalFileSystemStorageService();
			this.layerSerializer = layerSerializer ?? new JsonLayerSerializer();
			this.compressionService = compressionService ?? new TransparentCompressionService();
			this.encodingService = encodingService ?? new Base64EncodingService();
		}

		#endregion

		#region Public methods

		public void AppendDataToLayer(Uri location, Guid ayerId, IDataImportService dataImportService)
		{
			throw new NotImplementedException();
		}

		public void CompressTimeInLayer(Uri location, Guid ayerId, string fieldName = null, string componentName = null)
		{
			throw new NotImplementedException();
		}

		public SummaryLayerFile GenerateFilterLayer(Uri location, Guid parentLayerId, FilterBase filter)
		{
			Uri layerDirectoryUri = new Uri(location, $"{parentLayerId}.layer/");
			Uri parentLayerFileUri = new Uri(layerDirectoryUri, $"{parentLayerId}.layer.json");

			// find parentLayer in storage and download summary
			using (var stream = storageService.Load(parentLayerFileUri))
			{
				var parentLayer = layerSerializer.Deserialize<SummaryLayerFile>(stream);
				Uri meshFileUri = new Uri(layerDirectoryUri, $"{parentLayerId}.mesh.json");

				GeometryDescription geometry = LoadGeometry(meshFileUri);

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
							var attributeSelectionFilter = filter as AttributeSelectionFilter;
							Debug.Assert(attributeSelectionFilter != null);
							DataLayerDescriptor attribute = parentLayer.Attributes.Single(a => a.FieldName == attributeSelectionFilter.AttributeName);
							Uri attributeDataFileUri = new Uri(layerDirectoryUri, $"{parentLayerId}.{attribute.Index}.attribute.json");
							GeometryDescription filteredGeometry = filterGeometryByAttribute(geometry, attributeDataFileUri, attributeSelectionFilter.AttributeSelection);

							var filteredDataDescriptions = Enumerable.Empty<DataDescription>(); // TODO: filter data by attribute

							return generateLayerFiles(location, $"{attributeSelectionFilter.AttributeName} {string.Join(", ", attributeSelectionFilter.AttributeSelection)} layer", filteredGeometry, filteredDataDescriptions, filter);
						}
					default:
						throw new NotSupportedException();
				}
			}
		}

		public SummaryLayerFile GenerateMasterLayer(Uri location, string layerName, IGeometryImportService geometryImportService, IDataImportService dataImportService = null)
		{
			if (geometryImportService == null)
			{
				throw new ArgumentNullException(nameof(geometryImportService));
			}

			GeometryDescription geometry = geometryImportService.ReadGeometry();
			IEnumerable<DataDescription> dataDescriptions = dataImportService?.ReadData(geometry) ?? Enumerable.Empty<DataDescription>();
			SummaryLayerFile layerFile = generateLayerFiles(location, layerName, geometry, dataDescriptions, filter: null);
			return layerFile;
		}

		public GeometryDescription LoadGeometry(Uri uri)
		{
			using (Stream stream = storageService.Load(uri))
			{
				MeshLayerFile layerMesh = layerSerializer.Deserialize<MeshLayerFile>(stream);
				return createGeometryFromLayerMesh(layerMesh);
			}
		}

		public IEnumerable<DataDescription> LoadData(Uri uri)
		{
			using (Stream stream = storageService.Load(uri))
			{
				DataLayerFile layerResult = layerSerializer.Deserialize<DataLayerFile>(stream);
				return createDataDescriptionFromLayerResult(layerResult);
			}
		}

		#endregion

		#region Protected methods

		protected void StoreLayerFile<T>(T layerObject, Uri location, string layerDirectory, string recordName)
		{
			Uri uri = new Uri(location, Path.Combine(layerDirectory ?? "", recordName + layerSerializer.FileExtension));
			using (Stream stream = storageService.Save(uri))
			{
				layerSerializer.Serialize(layerObject, stream);
			}
		}

		#endregion

		#region Private methods

		private SummaryLayerFile generateLayerFiles(Uri location, string layerName, GeometryDescription geometry, IEnumerable<DataDescription> dataDescriptions, FilterBase filter)
		{
			Guid layerId = Guid.NewGuid();

			SummaryLayerFile layerSummary = new SummaryLayerFile
			{
				Id = layerId,
				Name = layerName,
				ParentId = null,
				Filter = filter,
			};

			string layerDirectory = $"{layerId}.layer";

			MeshLayerFile layerMesh = createLayerMeshFromGeometry(geometry, layerId);
			StoreLayerFile(layerMesh, location, layerDirectory, $"{layerId}.mesh");

			int attributeIndex = 1, resultIndex = 1;

			if (geometry.CellAttributes != null)
			{
				DataLayerFile layerElementProperties = createAttributeLayerFile(geometry.CellAttributes, "ElementProperties", DataLocationType.Cells, layerId, attributeIndex);
				StoreLayerFile(layerElementProperties, location, layerDirectory, $"{layerId}.{layerElementProperties.Index}.attribute");
				attributeIndex++;
				layerSummary.Attributes = new[] { DataLayerDescriptor.CreateFrom(layerElementProperties) };
			}

			var resultDescriptors = new List<DataLayerDescriptor>();
			var timeStepsHashSet = new HashSet<double>();
			foreach (var dataField in dataDescriptions)
			{
				foreach (var layerResult in createLayerResultFromDataDescription(dataField, layerId, resultIndex))
				{
					resultDescriptors.Add(DataLayerDescriptor.CreateFrom(layerResult));
					foreach (var timeStep in layerResult.TimeSteps)
						timeStepsHashSet.Add(timeStep);
					StoreLayerFile(layerResult, location, layerDirectory, $"{layerId}.{layerResult.Index}.result");
				}
				resultIndex += dataField.NumberOfComponents;
			}

			layerSummary.TimeSteps = timeStepsHashSet.OrderBy(t => t).ToArray();
			layerSummary.Results = resultDescriptors.ToArray();

			StoreLayerFile(layerSummary, location, layerDirectory, $"{layerId}.layer");

			return layerSummary;
		}

		private MeshLayerFile createLayerMeshFromGeometry(GeometryDescription geometry, Guid layerId)
		{
			MeshLayerFile layerMesh = new MeshLayerFile { LayerId = layerId };

			layerMesh.NumberOfPoints = geometry.NumberOfPoints;
			layerMesh.PointCoordinates = encodeGeometryDataArray(geometry.PointCoordinates, trimEnd: false);

			layerMesh.NumberOfCells = geometry.NumberOfCells;

			layerMesh.CellConnectivity = encodeGeometryDataArray(geometry.CellConnectivity, trimEnd: false);

			// TODO: set offsets and types to null if all cells are linear triangles
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

		private DataLayerFile createAttributeLayerFile(int[] attributeValues, string attributeName, DataLocationType location, Guid layerId, int dataIndex)
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

		private IEnumerable<DataLayerFile> createLayerResultFromDataDescription(DataDescription dataField, Guid layerId, int dataIndex)
		{
			int numberOfComponents = dataField.NumberOfComponents;
			for (int componentIndex = 0; componentIndex < numberOfComponents; componentIndex++)
			{
				DataLayerFile layerResult = new DataLayerFile
				{
					LayerId = layerId,
					FieldName = dataField.Name,
					ComponentName = dataField.ComponentNames?[componentIndex],
					Index = dataIndex + componentIndex,
					TimeSteps = new[] { dataField.TimeStep ?? 0 },
					Location = dataField.Location
				};

				double[] allValues = dataField.Data;
				double[] componentValues = new double[dataField.Data.Length / numberOfComponents];

				for (int hip = 0, hop = componentIndex; hop < allValues.Length; hip += 1, hop += numberOfComponents)
				{
					componentValues[hip] = allValues[hop];
				}

				EncodingParameters encoding;

				layerResult.Data = compressAndEncodeDataValues(componentValues, out encoding);
				layerResult.Encoding = encoding;

				yield return layerResult;
			}
		}

		private IEnumerable<DataDescription> createDataDescriptionFromLayerResult(DataLayerFile layerResult)
		{
			DataDescription data = new DataDescription();

			data.Name = layerResult.FieldName;
			data.TimeStep = layerResult.TimeSteps?.Single();
			data.ComponentNames = new[] { layerResult.ComponentName };
			data.FieldType = FieldType.Scalar;
			data.Location = layerResult.Location;
			data.NumberOfComponents = 1;
			data.Data = decodeAndDecompressData(layerResult.Data, layerResult.Encoding);

			yield return data;
		}

		private GeometryDescription filterGeometryByAttribute(GeometryDescription geometry, Uri attributeDataFileUri, int[] attributeSelection)
		{
			using (Stream stream = storageService.Load(attributeDataFileUri))
			{
				DataLayerFile layerResult = layerSerializer.Deserialize<DataLayerFile>(stream);
				int[] attributes = decodeAttributes(layerResult.Data, layerResult.Encoding);

				// ----------------

				List<CellType> cellTypes = new List<CellType>();
				List<int> cellAttributes = new List<int>();
				HashSet<int> remainingPointIndices = new HashSet<int>();
				List<int> cellConnectivity = new List<int>();
				List<int> cellOffsets = new List<int>();

				for (int cellIndex = 0, previousOffset = 0; cellIndex < geometry.NumberOfCells; cellIndex++)
				{
					int currentOffset = geometry.CellOffsets[cellIndex];
					if (attributeSelection.Contains(attributes[cellIndex]))
					{
						for (int offset = previousOffset; offset < currentOffset; offset++)
						{
							int pointIndex = geometry.CellConnectivity[offset];
							remainingPointIndices.Add(pointIndex);
							cellConnectivity.Add(pointIndex);
						}
						cellOffsets.Add(cellConnectivity.Count);
						cellTypes.Add(geometry.CellTypes[cellIndex]);
						cellAttributes.Add(attributes[cellIndex]);
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
					CellAttributes = cellAttributes.ToArray()
				};
				return filteredGeometry;
			}
		}

		private string compressAndEncodeDataValues(double[] dataValues, out EncodingParameters encodingParameters)
		{
			double[] compressedValues = compressionService.Compress(dataValues);
			return encodingService.Encode(compressedValues, TrimOptions.BeginEnd, out encodingParameters);
		}

		private double[] decodeAndDecompressData(string data, EncodingParameters encodingParameters)
		{
			double[] compressedValues = encodingService.Decode<double>(data, TrimOptions.BeginEnd, encodingParameters);
			return compressionService.Decompress(compressedValues);
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

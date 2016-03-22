using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager
{
	public class LayerGenerator
	{
		#region Fields, constructor

		IStorageService storageService;
		ICompressionService compressionService;
		ILayerSerializer layerSerializer;

		public LayerGenerator(
			IStorageService storageService = null,
			ILayerSerializer layerSerializer = null,
			ICompressionService compressionService = null)
		{
			this.storageService = storageService ?? new LocalFileSystemStorageService();
			this.layerSerializer = layerSerializer ?? new JsonLayerSerializer();
			this.compressionService = compressionService ?? new GenericCompressionService();
		}

		#endregion

		#region Public methods

		public void AppendData(Guid layer, IDataImportService dataImportService)
		{
			throw new NotImplementedException();
		}

		public void CompressTime(Guid layer, string fieldName = null, string componentName = null)
		{
			throw new NotImplementedException();
		}

		public Guid GenerateFrom(Guid parentLayer, FilterDescriptor filter)
		{
			Guid layerId = Guid.NewGuid();
			// TODO: find parentLayer in storage and download summary
			throw new NotImplementedException();
			return layerId;
		}

		public Guid Generate(string projectName, Uri projectLocation, IGeometryImportService geometryImportService, IDataImportService dataImportService = null)
		{
			if (geometryImportService == null)
			{
				throw new ArgumentNullException(nameof(geometryImportService));
			}

			Guid layerId = Guid.NewGuid();

			SummaryLayerFile layerSummary = new SummaryLayerFile
			{
				Id = layerId,
				Name = projectName,
				ParentId = null,
				Filters = null,
			};

			GeometryDescription geometry = geometryImportService.ReadGeometry();

			MeshLayerFile layerMesh = createLayerMeshFromGeometry(geometry, layerId);
			string layerDirectory = Path.Combine(projectLocation.LocalPath, $"{projectName}.{layerId}.layer"); // TODO: make valid file name from projectName

			StoreLayerFile(layerMesh, projectLocation, layerDirectory, $"{layerId}.mesh");

			var resultDescriptors = new List<ResultDescriptor>();
			var timeStepsHashSet = new HashSet<double>();
			int dataIndex = 1;
			foreach (var dataField in dataImportService?.ReadData(geometry) ?? Enumerable.Empty<DataDescription>())
			{
				foreach (var layerResult in createLayerResultFromDataDescription(dataField, layerId, dataIndex))
				{
					resultDescriptors.Add(ResultDescriptor.CreateFrom(layerResult));

					foreach (var timeStep in layerResult.TimeSteps)
						timeStepsHashSet.Add(timeStep);

					StoreLayerFile(layerResult, projectLocation, layerDirectory, $"{layerId}.{layerResult.Index}.data");
				}
				dataIndex += dataField.NumberOfComponents;
			}

			layerSummary.TimeSteps = timeStepsHashSet.OrderBy(t => t).ToArray();
			layerSummary.Results = resultDescriptors.ToArray();

			StoreLayerFile(layerSummary, projectLocation, layerDirectory, $"{layerId}.layer");

			return layerId;
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

		protected void StoreLayerFile<T>(T layerObject, Uri projectLocation, string layerDirectory, string recordName)
		{
			Uri uri = new Uri(projectLocation, Path.Combine(layerDirectory ?? "", recordName + layerSerializer.FileExtension));
			using (Stream stream = storageService.Save(uri))
			{
				layerSerializer.Serialize(layerObject, stream);
			}
		}

		#endregion

		#region Private methods

		private static readonly CellType DefaultCellType = CellType.TriangleLinear;

		private MeshLayerFile createLayerMeshFromGeometry(GeometryDescription geometry, Guid layerId)
		{
			MeshLayerFile layerMesh = new MeshLayerFile { LayerId = layerId };

			layerMesh.NumberOfPoints = geometry.NumberOfPoints;
			layerMesh.PointCoordinates = compressionService.Encode(geometry.PointCoordinates);

			layerMesh.NumberOfCells = geometry.NumberOfCells;

			layerMesh.CellConnectivity = compressionService.Encode(geometry.CellConnectivity);

			// TODO: set offsets and types to null if all cells are linear triangles
			if (!geometry.CellTypes.All(cellType => cellType == DefaultCellType))
			{
				layerMesh.CellTypes = compressionService.TrimAndEncode(convertCellTypeArrayToByteArray(geometry.CellTypes));
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

		private GeometryDescription createGeometryFromLayerMesh(MeshLayerFile layerMesh)
		{
			GeometryDescription geometry = new GeometryDescription();

			geometry.PointCoordinates = compressionService.Decode<float>(layerMesh.PointCoordinates);
			geometry.NumberOfCoordinateComponents = geometry.PointCoordinates.Length / layerMesh.NumberOfPoints;
			geometry.CellConnectivity = compressionService.Decode<int>(layerMesh.CellConnectivity);

			if (layerMesh.CellTypes != null)
			{
				geometry.CellTypes = convertByteArrayToCellTypeArray(compressionService.DecodeAndExpand<byte>(layerMesh.CellTypes, layerMesh.NumberOfCells));
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

				// TODO: throw away all-NaN arrays and compact single-value arrays

				for (int hip = 0, hop = componentIndex; hop < allValues.Length; hip += 1, hop += numberOfComponents)
				{
					componentValues[hip] = allValues[hop];
				}

				CompressionDescriptor compressionParameters;

				layerResult.Data = compressionService.CompressAndEncode(componentValues, out compressionParameters);
				layerResult.Compression = compressionParameters;

				yield return layerResult;
			}
		}

		private IEnumerable<DataDescription> createDataDescriptionFromLayerResult(DataLayerFile layerResult)
		{
			DataDescription data = new DataDescription();

			data.Name = layerResult.FieldName;
			data.TimeStep = layerResult.TimeSteps.Single();
			data.ComponentNames = new[] { layerResult.ComponentName };
			data.FieldType = FieldType.Scalar;
			data.Location = layerResult.Location;
			data.NumberOfComponents = 1;
			data.Data = compressionService.DecodeAndDecompress(layerResult.Data, layerResult.Compression);

			yield return data;
		}

		private static byte[] convertCellTypeArrayToByteArray(CellType[] source)
		{
			byte[] result = new byte[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				result[i] = (byte)source[i];
			}
			return result;
		}

		private static CellType[] convertByteArrayToCellTypeArray(byte[] source)
		{
			CellType[] result = new CellType[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				result[i] = (CellType)source[i];
			}
			return result;
		}

		#endregion
	}
}

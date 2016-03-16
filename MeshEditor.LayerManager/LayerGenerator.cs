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
			this.compressionService = compressionService ?? new GeneralCompressionService();
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

			LayerSummary layerSummary = new LayerSummary
			{
				Id = layerId,
				Name = projectName,
				ParentId = null,
				Filters = null,
			};

			GeometryDescription geometry = geometryImportService.ReadGeometry();

			LayerMesh layerMesh = createLayerMeshFromGeometry(geometry, layerId);
			string layerDirectory = Path.Combine(projectLocation.LocalPath, $"{projectName}.{layerId}.layer");

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

					StoreLayerFile(layerResult, projectLocation, layerDirectory, $"{layerId}.{layerResult.Index}.result");
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
				LayerMesh layerMesh = layerSerializer.Deserialize<LayerMesh>(stream);
				return createGeometryFromLayerMesh(layerMesh);
			}
		}

		public IEnumerable<DataDescription> LoadData(Uri uri)
		{
			using (Stream stream = storageService.Load(uri))
			{
				LayerResult layerResult = layerSerializer.Deserialize<LayerResult>(stream);
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

		protected string CompressData(double[] dataValues, out CompressionDescriptor compressionParameters)
		{
			compressionParameters = new CompressionDescriptor(); // works as in/out bag of parameters that should be then written to output layer file
			byte[] compressedData = compressionService.Compress(dataValues, compressionParameters);
			return Convert.ToBase64String(compressedData);
		}

		protected double[] DecompressData(string encodedData, CompressionDescriptor compressionParameters)
		{
			byte[] compressedData = Convert.FromBase64String(encodedData);
			return compressionService.Decompress(compressedData, compressionParameters);
		}

		protected static string ConvertArrayToBase64String<T>(T[] values) where T : struct
		{
			// determine the correct type
			Type itemType = typeof(T);
			Type actualType = itemType.IsEnum ? Enum.GetUnderlyingType(itemType) : itemType;
			byte[] bytes;
			if (actualType != typeof(byte))
			{
				bytes = new byte[values.Length * System.Runtime.InteropServices.Marshal.SizeOf(actualType)];
				Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			}
			else
			{
				bytes = (byte[])(object)values; // evade C# array cast limitation
			}
			return Convert.ToBase64String(bytes);
		}

		protected static T[] ConvertBase64StringToArray<T>(string base64String) where T : struct
		{
			// determine the correct type
			Type itemType = typeof(T);
			Type actualType = itemType.IsEnum ? Enum.GetUnderlyingType(itemType) : itemType;
			byte[] bytes = Convert.FromBase64String(base64String);
			if (actualType != typeof(byte))
			{
				T[] values = new T[bytes.Length / System.Runtime.InteropServices.Marshal.SizeOf<T>()];
				Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
				return values;
			}
			return (T[])(object)bytes; // evade C# array cast limitation
		}

		#endregion

		#region Private methods

		private LayerMesh createLayerMeshFromGeometry(GeometryDescription geometry, Guid layerId)
		{
			LayerMesh layerMesh = new LayerMesh { LayerId = layerId };

			layerMesh.NumberOfPoints = geometry.NumberOfPoints;
			layerMesh.PointCoordinates = ConvertArrayToBase64String(geometry.PointCoordinates);

			layerMesh.NumberOfCells = geometry.NumberOfCells;

			layerMesh.CellConnectivity = ConvertArrayToBase64String(geometry.CellConnectivity);
			layerMesh.CellOffsets = ConvertArrayToBase64String(geometry.CellOffsets);
			layerMesh.CellTypes = ConvertArrayToBase64String(geometry.CellTypes);
			
			// TODO: ommit offsets and types if all cells are linear triangles

			//MeshFaceGenerator faceGenerator = new MeshFaceGenerator();
			//faceGenerator.ProcessGeometry(geometry);
			//layerMesh.NumberOfTriangles = faceGenerator.NumberOfTriangles;
			//layerMesh.TriangleConnectivity = ConvertArrayToBase64String(faceGenerator.TriangleConnectivity);
			//layerMesh.NumberOfEdges = faceGenerator.NumberOfEdges;
			//layerMesh.EdgeConnectivity = ConvertArrayToBase64String(faceGenerator.EdgeConnectivity);

			return layerMesh;
		}

		private GeometryDescription createGeometryFromLayerMesh(LayerMesh layerMesh)
		{
			GeometryDescription geometry = new GeometryDescription();

			geometry.PointCoordinates = ConvertBase64StringToArray<float>(layerMesh.PointCoordinates);
			geometry.NumberOfCoordinateComponents = geometry.PointCoordinates.Length / layerMesh.NumberOfPoints;
			geometry.CellConnectivity = ConvertBase64StringToArray<int>(layerMesh.CellConnectivity);
			geometry.CellOffsets = (layerMesh.CellOffsets != null) ? ConvertBase64StringToArray<int>(layerMesh.CellOffsets) : Enumerable.Range(1, layerMesh.NumberOfCells).Select(i => i * 3).ToArray();
			geometry.CellTypes = (layerMesh.CellTypes != null) ? ConvertBase64StringToArray<CellType>(layerMesh.CellTypes) : Enumerable.Repeat(CellType.TriangleLinear, layerMesh.NumberOfCells).ToArray();

			return geometry;
		}

		private IEnumerable<LayerResult> createLayerResultFromDataDescription(DataDescription dataField, Guid layerId, int dataIndex)
		{
			int numberOfComponents = dataField.NumberOfComponents;
			for (int componentIndex = 0; componentIndex < numberOfComponents; componentIndex++)
			{
				LayerResult layerResult = new LayerResult
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

				CompressionDescriptor compressionParameters;

				layerResult.Data = CompressData(componentValues, out compressionParameters);
				layerResult.Compression = compressionParameters;

				yield return layerResult;
			}
		}

		private IEnumerable<DataDescription> createDataDescriptionFromLayerResult(LayerResult layerResult)
		{
			DataDescription data = new DataDescription();

			data.Name = layerResult.FieldName;
			data.TimeStep = layerResult.TimeSteps.Single();
			data.ComponentNames = new[] { layerResult.ComponentName };
			data.FieldType = FieldType.Scalar;
			data.Location = layerResult.Location;
			data.NumberOfComponents = 1;
			data.Data = DecompressData(layerResult.Data, layerResult.Compression);

			yield return data;
		}

		#endregion
	}
}

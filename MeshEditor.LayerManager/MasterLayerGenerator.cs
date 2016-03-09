using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager
{
	public class MasterLayerGenerator : LayerGenerator
	{
		#region Fields, constructor

		public MasterLayerGenerator(
			IStorageService storageService = null,
			ILayerSerializer layerSerializer = null,
			ICompressionService compressionService = null)
			: base(
				  storageService,
				  layerSerializer,
				  compressionService)
		{ }

		#endregion

		#region Public methods

		public Guid Generate(/*Guid projectGuid, */ string projectName, IGeometryImportService geometryImportService, IDataImportService dataImportService = null)
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

			LayerMesh layerMesh = createLayerMeshFrom(geometry, layerId);

			StoreLayerFile(layerMesh, $"{layerId}.mesh");

			var resultDescriptors = new List<ResultDescriptor>();
			var timeStepsHashSet = new HashSet<double>();
			int dataIndex = 1;
			foreach (var dataField in dataImportService?.ReadData(geometry) ?? Enumerable.Empty<DataDescription>())
			{
				foreach (var layerResult in createLayerResultFrom(dataField, layerId, dataIndex))
				{
					resultDescriptors.Add(ResultDescriptor.CreateFrom(layerResult));

					foreach (var timeStep in layerResult.TimeSteps)
						timeStepsHashSet.Add(timeStep);

					StoreLayerFile(layerResult, $"{layerId}.{layerResult.Index}.result");
				}
				dataIndex += dataField.NumberOfComponents;
			}

			layerSummary.TimeSteps = timeStepsHashSet.OrderBy(t => t).ToArray();
			layerSummary.Results = resultDescriptors.ToArray();

			StoreLayerFile(layerSummary, $"{layerId}.summary");

			return layerId;
		}

		#endregion

		#region Private methods

		private LayerMesh createLayerMeshFrom(GeometryDescription geometry, Guid layerId)
		{
			LayerMesh layerMesh = new LayerMesh { LayerId = layerId };

			layerMesh.NumberOfPoints = geometry.NumberOfPoints;
			layerMesh.PointCoordinates = ConvertArrayToBase64String(geometry.PointCoordinates);

			MeshFaceGenerator faceGenerator = new MeshFaceGenerator();
			faceGenerator.ProcessGeometry(geometry);

			layerMesh.NumberOfTriangles = faceGenerator.NumberOfTriangles;
			layerMesh.TriangleConnectivity = ConvertArrayToBase64String(faceGenerator.TriangleConnectivity);

			layerMesh.NumberOfEdges = faceGenerator.NumberOfEdges;
			layerMesh.EdgeConnectivity = ConvertArrayToBase64String(faceGenerator.EdgeConnectivity);

			return layerMesh;
		}

		private IEnumerable<LayerResult> createLayerResultFrom(DataDescription dataField, Guid layerId, int dataIndex)
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
					Location = dataField.LocationType.ToString()
				};

				Dictionary<string, object> compressionParameters;


				double[] allValues = dataField.Data;
				double[] componentValues = new double[dataField.Data.Length / numberOfComponents];

				for (int hip = 0, hop = componentIndex; hop < allValues.Length; hip += 1, hop += numberOfComponents)
				{
					componentValues[hip] = allValues[hop];
				}

				layerResult.Data = CompressData(componentValues, out compressionParameters);
				layerResult.Compression = compressionParameters;

				yield return layerResult;
			}
		}

		#endregion
	}
}

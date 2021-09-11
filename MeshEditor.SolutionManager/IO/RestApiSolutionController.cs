using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.Common.Logging;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using RestSharp;

namespace MeshEditor.SolutionManager.IO
{
	class RestApiSolutionController : ISolutionController
	{
		#region Fields, constructor

		private string uri;
		private ISerializationService serializer;
		private ILogger logger;

		public RestApiSolutionController(string uri, ILogger logger)
		{
			Debug.Assert(uri != null);
			this.uri = uri;
			this.logger = logger;
			serializer = new JsonSerializationService();
		}

		#endregion

		#region Public methods

		public Solution CreateNew(object solutionLocator, IEnumerable<AnalysisResult> analysisResults, string projectName = null /*ignored*/)
		{
			if (!(solutionLocator is int))
				throw new ArgumentException("Solution id is not specified", nameof(solutionLocator));

			var request = createCreateNewRequest();

			request.AddHeader("Accept", "application/json");
			request.AddHeader("Content-Type", "application/json");
			request.RequestFormat = DataFormat.Json;

			int solutionId = (int)solutionLocator;

			var body = new
			{
				Id = solutionId,
				Results = analysisResults.ToArray()
			};

			string jsonString = request.JsonSerializer.Serialize(body);
			request.AddParameter("application/json; charset=utf-8", jsonString, ParameterType.RequestBody);

			var response = executeRequest(request);

			var newSolution = parseResponse<Solution>(response);
			var headerParameterLocation = response.Headers.FirstOrDefault(parameter => parameter.Name == "Location");
			if (headerParameterLocation != null)
			{
				newSolution.Location = headerParameterLocation.Value?.ToString();
			}
			return newSolution;
		}

		public IEnumerable<ISolutionInfo> GetAll()
		{
			RestRequest request = createGetAllRequest();
			var response = executeRequest(request);
			return parseResponse<IEnumerable<SolutionInfo>>(response);
		}

		public async Task<IEnumerable<ISolutionInfo>> GetAllAsync(CancellationToken cancellationToken)
		{
			RestRequest request = createGetAllRequest();
			var response = await executeRequestAsync(request, cancellationToken);
			return parseResponse<IEnumerable<SolutionInfo>>(response);
		}

		public Solution Get(object solutionLocator)
		{
			if (!(solutionLocator is int))
				throw new ArgumentException("Solution id is not specified", nameof(solutionLocator));

			int solutionId = (int)solutionLocator;
			RestRequest request = createGetRequest(solutionId);
			var response = executeRequest(request);
			return parseResponse<Solution>(response);
		}

		public async Task<Solution> GetAsync(object solutionLocator, CancellationToken cancellationToken)
		{
			if (!(solutionLocator is int))
				throw new ArgumentException("Solution id is not specified", nameof(solutionLocator));

			int solutionId = (int)solutionLocator;
			RestRequest request = createGetRequest(solutionId);
			var response = await executeRequestAsync(request, cancellationToken);
			return parseResponse<Solution>(response);
		}

		public void Delete(object solutionLocator)
		{
			if (!(solutionLocator is int))
				throw new ArgumentException("Solution id is not specified", nameof(solutionLocator));

			int solutionId = (int)solutionLocator;
			var request = createDeleteRequest(solutionId);
			executeRequest(request);
		}

		public async Task DeleteAsync(object solutionLocator, CancellationToken cancellationToken)
		{
			if (!(solutionLocator is int))
				throw new ArgumentException("Solution id is not specified", nameof(solutionLocator));

			int solutionId = (int)solutionLocator;
			var request = createDeleteRequest(solutionId);
			await executeRequestAsync(request, cancellationToken);
		}

		public Solution AddLayer(object solutionLocator, Solution.Layer parentLayer, Solution.Layer newLayer)
		{
			if (!(solutionLocator is int))
				throw new ArgumentException("Solution id is not specified", nameof(solutionLocator));

			int solutionId = (int)solutionLocator;

			var request = createAddLayerRequest(solutionId);
			request.RequestFormat = DataFormat.Json;

			//request.AddBody(serializedSolution);
			//request.AddQueryParameter("state", ((int)analysisState).ToString());

			var body = new
			{
				Id = newLayer.Id,
				ParentLayerId = parentLayer?.Id,
				SolutionId = solutionId,
				Name = newLayer.Name,
				FilterType = newLayer.FilterType
			};

			string jsonString = request.JsonSerializer.Serialize(body);
			request.AddParameter("application/json; charset=utf-8", jsonString, ParameterType.RequestBody);

			var response = executeRequest(request);
			return parseResponse<Solution>(response);
		}

		public Solution DeleteLayer(object solutionLocator, Solution.Layer layerToDelete)
		{
			if (!(solutionLocator is int))
				throw new ArgumentException("Solution id is not specified", nameof(solutionLocator));

			int solutionId = (int)solutionLocator;

			RestRequest request = createDeleteLayerRequest(solutionId, layerToDelete);
			var response = executeRequest(request);
			return parseResponse<Solution>(response);
		}

		public async Task<Solution> DeleteLayerAsync(object solutionLocator, Solution.Layer layerToDelete, CancellationToken cancellationToken)
		{
			if (!(solutionLocator is int))
				throw new ArgumentException("Solution id is not specified", nameof(solutionLocator));

			int solutionId = (int)solutionLocator;

			RestRequest request = createDeleteLayerRequest(solutionId, layerToDelete);
			var response = await executeRequestAsync(request, cancellationToken);
			return parseResponse<Solution>(response);
		}

		#endregion

		#region Private methods

		private IRestResponse executeRequest(RestRequest request)
		{
			logRequest(request);
			var client = new RestClient(uri);
			var response = client.Execute(request);
			logResponse(response);
			return response;
		}

		private async Task<IRestResponse> executeRequestAsync(RestRequest request, CancellationToken cancellationToken)
		{
			logRequest(request);
			var client = new RestClient(uri);
			var response = await client.ExecuteAsync(request, cancellationToken);
			logResponse(response);
			return response;
		}

		private void logRequest(RestRequest request)
		{
			logger?.LogOperationProgress($"{request.Method} {request.Resource}");
		}

		private void logResponse(IRestResponse response)
		{
			logger?.LogOperationProgress($"Status: {response.StatusDescription} ({(int)response.StatusCode})");
			if (response.ErrorException != null)
				throw response.ErrorException;
			if (isErrorStatusCode(response.StatusCode))
				throw new Exception(/*response.Content*/response.StatusDescription);
		}

		private static bool isSuccessStatusCode(HttpStatusCode statusCode)
		{
			return ((int)statusCode >= 200) && ((int)statusCode <= 299);
		}

		private static bool isErrorStatusCode(HttpStatusCode statusCode)
		{
			return ((int)statusCode >= 400) && ((int)statusCode <= 599);
		}

		private T parseResponse<T>(IRestResponse response)
		{
			using (Stream stream = generateStreamFromString(response.Content))
			{
				return serializer.Deserialize<T>(stream);
			}
		}

		private static Stream generateStreamFromString(string s)
		{
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}

		private static RestRequest createCreateNewRequest()
		{
			var request = new RestRequest($"api/solution", Method.POST);

			return request;
		}

		private static RestRequest createAddLayerRequest(int solutionId)
		{
			var request = new RestRequest($"api/solution/{solutionId}/layer", Method.POST);
			addHeadersToRequest(request);
			return request;
		}

		private static RestRequest createGetAllRequest()
		{
			var request = new RestRequest("api/solution", Method.GET);
			addHeadersToRequest(request);
			return request;
		}

		private static RestRequest createGetRequest(int solutionId)
		{
			var request = new RestRequest($"api/solution/{solutionId}", Method.GET);
			addHeadersToRequest(request);
			return request;
		}

		private static RestRequest createDeleteRequest(int solutionId)
		{
			var request = new RestRequest($"api/solution/{solutionId}", Method.DELETE);
			addHeadersToRequest(request);
			return request;
		}

		private static RestRequest createDeleteLayerRequest(int solutionId, Solution.Layer layerToDelete)
		{
			var request = new RestRequest($"api/solution/{solutionId}/layer/{layerToDelete.Id}", Method.DELETE);
			addHeadersToRequest(request);
			return request;
		}

		private static void addHeadersToRequest(RestRequest request)
		{
			request.AddHeader("Accept", "application/json");
			request.AddHeader("Content-Type", "application/json");
		}

		#endregion
	}
}

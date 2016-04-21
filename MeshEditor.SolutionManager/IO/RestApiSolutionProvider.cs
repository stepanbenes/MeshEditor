using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.SolutionManager.Logging;
using RestSharp;

namespace MeshEditor.SolutionManager.IO
{
	class RestApiSolutionProvider : ISolutionProvider
	{
		#region Fields, constructor

		private string uri;
		private ISerializationService serializer;
		private ILogger logger;

		public RestApiSolutionProvider(string uri, ILogger logger)
		{
			Debug.Assert(uri != null);
			this.uri = uri;
			this.logger = logger;
			serializer = new JsonSerializationService();
		}

		#endregion

		#region Public methods

		public IEnumerable<ISolutionInfo> GetAll()
		{
			var client = new RestClient(uri);
			var request = new RestRequest("api/solution", Method.GET);

			request.AddHeader("Accept", "application/json");
			request.AddHeader("Content-Type", "application/json");
			//request.RequestFormat = DataFormat.Json;

			var response = executeRequest(client, request);

			return parseResponse<IEnumerable<SolutionBase>>(response);
		}

		public Solution Get(int solutionId)
		{
			var client = new RestClient(uri);
			var request = new RestRequest($"api/solution/{solutionId}", Method.GET);

			request.AddHeader("Accept", "application/json");
			request.AddHeader("Content-Type", "application/json");
			//request.RequestFormat = DataFormat.Json;

			var response = executeRequest(client, request);

			return parseResponse<Solution>(response);
		}

		//public void Create(Solution solution)
		//{
		//	sendSolutionObject(solution, Method.POST);
		//}

		//public void Update(Solution solution)
		//{
		//	sendSolutionObject(solution, Method.PUT);
		//}

		public void CreateNew(SolutionBase solution)
		{
			var client = new RestClient(uri);
			var request = new RestRequest($"api/solution", Method.POST);

			//request.AddUrlSegment("id", simulationId.ToString());
			request.AddHeader("Accept", "application/json");
			request.AddHeader("Content-Type", "application/json");
			request.RequestFormat = DataFormat.Json;
			//request.AddBody(serializedSolution);
			//request.AddQueryParameter("state", ((int)analysisState).ToString());

			string jsonString = request.JsonSerializer.Serialize(solution);
			request.AddParameter("application/json; charset=utf-8", jsonString, ParameterType.RequestBody);

			var response = executeRequest(client, request);
		}

		public void AddLayer(Solution solution, Solution.Layer parentLayer, Solution.Layer newLayer)
		{
			var client = new RestClient(uri);
			var request = new RestRequest($"api/solution/{solution.Id}/layer", Method.POST);

			//request.AddUrlSegment("id", simulationId.ToString());
			request.AddHeader("Accept", "application/json");
			request.AddHeader("Content-Type", "application/json");
			request.RequestFormat = DataFormat.Json;
			//request.AddBody(serializedSolution);
			//request.AddQueryParameter("state", ((int)analysisState).ToString());

			var body = new
			{
				Id = newLayer.Id,
				ParentLayerId = parentLayer?.Id,
				SolutionId = solution.Id,
				Name = newLayer.Name,
				FilterType = newLayer.FilterType
			};

			string jsonString = request.JsonSerializer.Serialize(body);
			request.AddParameter("application/json; charset=utf-8", jsonString, ParameterType.RequestBody);

			var response = executeRequest(client, request);
		}

		#endregion

		#region Private methods

		private IRestResponse executeRequest(RestClient client, RestRequest request)
		{
			logger?.LogMessage($"{request.Method} {request.Resource}");

			var response = client.Execute(request);

			logger?.LogMessage($"Status: {response.StatusDescription} ({(int)response.StatusCode})");
			if (response.ErrorException != null)
				throw response.ErrorException;
			if (isErrorStatusCode(response.StatusCode))
				throw new Exception(/*response.Content*/response.StatusDescription);

			return response;
		}

		private bool isSuccessStatusCode(HttpStatusCode statusCode)
		{
			return ((int)statusCode >= 200) && ((int)statusCode <= 299);
		}

		private bool isErrorStatusCode(HttpStatusCode statusCode)
		{
			return ((int)statusCode >= 400) && ((int)statusCode <= 599);
		}

		private T parseResponse<T>(IRestResponse response)
		{
			using (Stream stream = generateStreamFromString(response.Content, string.IsNullOrEmpty(response.ContentEncoding) ? Encoding.UTF8 : Encoding.GetEncoding(response.ContentEncoding)))
			{
				return serializer.Deserialize<T>(stream);
			}
		}

		private static Stream generateStreamFromString(string s, Encoding encoding)
		{
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream, encoding);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;
using RestSharp;

namespace MeshEditor.SolutionManager.IO
{
	class RestApiSolutionProvider : ISolutionProvider
	{
		#region Fields, constructor

		private string uri;
		private ISerializationService serializer;

		public RestApiSolutionProvider(string uri)
		{
			Debug.Assert(uri != null);
			this.uri = uri;
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

			var response = client.Execute(request);

			//Console.WriteLine("Status code: " + response.StatusCode);
			//Console.WriteLine("ErrorMessage: " + (response.ErrorMessage ?? "None"));

			return parseResponse<IEnumerable<SolutionBase>>(response);
		}

		public Solution Get(ISolutionInfo solutionInfo)
		{
			var client = new RestClient(uri);
			var request = new RestRequest($"api/solution/{solutionInfo.Id}", Method.GET);

			request.AddHeader("Accept", "application/json");
			request.AddHeader("Content-Type", "application/json");
			//request.RequestFormat = DataFormat.Json;

			var response = client.Execute(request);

			//Console.WriteLine("Status code: " + response.StatusCode);
			//Console.WriteLine("ErrorMessage: " + (response.ErrorMessage ?? "None"));

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
			var request = new RestRequest($"api/solution/{solution.Id}", Method.POST);

			//request.AddUrlSegment("id", simulationId.ToString());
			request.AddHeader("Accept", "application/json");
			request.AddHeader("Content-Type", "application/json");
			request.RequestFormat = DataFormat.Json;
			//request.AddBody(serializedSolution);
			//request.AddQueryParameter("state", ((int)analysisState).ToString());

			string jsonString = request.JsonSerializer.Serialize(solution);
			request.AddParameter("application/json; charset=utf-8", jsonString, ParameterType.RequestBody);

			var response = client.Execute(request);

			Console.WriteLine("Status code: " + response.StatusCode);
			Console.WriteLine("ErrorMessage: " + (response.ErrorMessage ?? "None"));
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

			var response = client.Execute(request);

			Console.WriteLine("Status code: " + response.StatusCode);
			Console.WriteLine("ErrorMessage: " + (response.ErrorMessage ?? "None"));
		}

		#endregion

		#region Private methods

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

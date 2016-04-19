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

		public void Create(Solution solution)
		{
			throw new NotImplementedException();
		}

		public void Update(Solution solution)
		{
			throw new NotImplementedException();
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

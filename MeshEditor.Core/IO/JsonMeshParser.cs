using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Data;
using Newtonsoft.Json;
using OpenTK;

namespace MeshEditor.IO
{
	class JsonMeshParser : IMeshFileParser
	{

		private class MeshFile
		{
			public Guid LayerId { get; set; }
			public string PointCoordinates { get; set; }
			public string EdgeConnectivity { get; set; }
			public string TriangleConnectivity { get; set; }
		}

		MeshFile meshFile;
		int lineNumber, nodeCount, elementCount;

		public JsonMeshParser(string filename)
		{
			Filename = filename;
		}

		public string Filename { get; }

		public int CurrentLineNumber => lineNumber;

		public int NodeCount => nodeCount;

		public int ElementCount => elementCount;

		public IEnumerable<Node> ReadNodes()
		{
			parseInput();

			double[] coordinates = convertBase64StringToArray<double>(meshFile.PointCoordinates);
			this.nodeCount = coordinates.Length / 3;

			for (int i = 0; i < nodeCount; i++)
			{
				Vector3 position = new Vector3((float)coordinates[i * 3], (float)coordinates[i * 3 + 1], (float)coordinates[i * 3 + 2]);
				Node node = new Node(i + 1, position, properties: null);
				yield return node;
			}
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			parseInput();

			int[] connectivity = convertBase64StringToArray<int>(meshFile.TriangleConnectivity);
			for (int i = 0; i < connectivity.Length; i += 3)
			{
				int[] nodeIDs = Utilities.Functions.GetSliceOfArray(connectivity, i, 3);
				yield return new ElementDraft { ID = i / 3, NodeIDs = nodeIDs, Type = ElementType.TriangleLinear };
			}
		}

		public void Dispose()
		{
			meshFile = null;
			lineNumber = 0;
		}

		#region Private methods

		private void parseInput()
		{
			if (meshFile != null)
				return;
			// deserialize JSON directly from a file
			using (var reader = File.OpenText(Filename))
			using (var jsonReader = new JsonTextReader(reader))
			{
				JsonSerializer serializer = new JsonSerializer();
				this.meshFile = serializer.Deserialize<MeshFile>(jsonReader);
				lineNumber = jsonReader.LineNumber;
			}
		}

		private static TItem[] convertBase64StringToArray<TItem>(string base64string) where TItem : struct
		{
			//byte[] bytes = new byte[values.Length * System.Runtime.InteropServices.Marshal.SizeOf<TItem>()];
			//Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			//return Convert.ToBase64String(bytes);

			byte[] bytes = Convert.FromBase64String(base64string);
			TItem[] values = new TItem[bytes.Length / System.Runtime.InteropServices.Marshal.SizeOf<TItem>()];
			Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
			return values;
		}

		#endregion

	}
}

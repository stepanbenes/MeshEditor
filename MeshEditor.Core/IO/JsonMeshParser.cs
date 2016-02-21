using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Data;
using OpenTK;

namespace MeshEditor.IO
{
	class JsonMeshParser : JsonFileParserBase, IMeshFileParser
	{

		private class MeshFile
		{
			public Guid LayerId { get; set; }
			public string PointCoordinates { get; set; }
			public string EdgeConnectivity { get; set; }
			public string TriangleConnectivity { get; set; }
		}

		MeshFile meshFile;
		int nodeCount, elementCount;

		public JsonMeshParser(string filename)
			: base(filename)
		{ }

		public int NodeCount => nodeCount;

		public int ElementCount => elementCount;

		public IEnumerable<Node> ReadNodes()
		{
			if (meshFile == null)
			{
				meshFile = ParseInput<MeshFile>();
			}

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
			if (meshFile == null)
			{
				meshFile = ParseInput<MeshFile>();
			}

			int[] connectivity = convertBase64StringToArray<int>(meshFile.TriangleConnectivity);
			this.elementCount = connectivity.Length / 3;
			for (int i = 0; i < elementCount; i++)
			{
				int[] nodeIDs = Utilities.Functions.GetSliceOfArray(connectivity, i * 3, 3);
				yield return new ElementDraft { ID = i, NodeIDs = nodeIDs, Type = ElementType.TriangleLinear };
			}

			/**/
			// Consider edges as beams
			int[] edgeConnectivity = convertBase64StringToArray<int>(meshFile.EdgeConnectivity);
			int edgeCount = edgeConnectivity.Length / 2;
			int index = elementCount;
			for (int i = 0; i < edgeCount; i++)
			{
				int[] nodeIDs = Utilities.Functions.GetSliceOfArray(edgeConnectivity, i * 2, 2);
				yield return new ElementDraft { ID = ++index, NodeIDs = nodeIDs, Type = ElementType.BeamLinear };
			}
		}

		public void Dispose()
		{
			meshFile = null;
		}
	}
}

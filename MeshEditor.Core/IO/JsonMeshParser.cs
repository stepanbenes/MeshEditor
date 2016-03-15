using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Data;
using MeshEditor.LayerManager.Data;
using OpenTK;

namespace MeshEditor.IO
{
	class JsonMeshParser : JsonFileParserBase, IMeshFileParser
	{
		LayerMesh meshFile;
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
				meshFile = ParseInput<LayerMesh>();
			}

			float[] coordinates = convertBase64StringToArray<float>(meshFile.PointCoordinates);
			this.nodeCount = coordinates.Length / 3;

			for (int i = 0; i < nodeCount; i++)
			{
				Vector3 position = new Vector3((float)coordinates[i * 3], (float)coordinates[i * 3 + 1], (float)coordinates[i * 3 + 2]);
				Node node = new Node(i, position, properties: null);
				yield return node;
			}
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			if (meshFile == null)
			{
				meshFile = ParseInput<LayerMesh>();
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
				yield return new ElementDraft { ID = index++, NodeIDs = nodeIDs, Type = ElementType.BeamLinear };
			}
		}

		public void Dispose()
		{
			meshFile = null;
		}
	}
}

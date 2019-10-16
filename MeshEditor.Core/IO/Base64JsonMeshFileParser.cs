using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Data;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.IO
{
	public class Base64JsonMeshFileParser : IMeshFileParser
	{
		private readonly LayerMeshFileParser layerMeshFileParser;

		public Base64JsonMeshFileParser(string filename)
		{
			Filename = filename;
			layerMeshFileParser = createLayerMeshFileParser();
		}

		private LayerMeshFileParser createLayerMeshFileParser()
		{
			IStorageService localStorage = new LocalFileSystemStorageService(basePath: Path.GetDirectoryName(Filename));
			var layerGenerator = new LayerGenerator(
				sourceStorage: localStorage,
				destinationStorage: localStorage);
			var geometry = layerGenerator.LoadGeometry(recordName: Path.GetFileName(Filename));
			return new LayerMeshFileParser(layerName: null, geometry, elementPropertyAttribute: null, mappingFromGeometryEntityIndicesToIds: null);
		}

		public string Filename { get; }

		public int NodeCount => layerMeshFileParser.NodeCount;
		public int ElementCount => layerMeshFileParser.ElementCount;
		public int CurrentLineNumber => layerMeshFileParser.CurrentLineNumber;


		public IEnumerable<Node> ReadNodes() => layerMeshFileParser.ReadNodes();

		public IEnumerable<ElementDraft> ReadElements() => layerMeshFileParser.ReadElements();

		public void Dispose() => layerMeshFileParser.Dispose();
	}
}

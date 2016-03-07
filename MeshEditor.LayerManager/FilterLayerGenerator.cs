using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Filters;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager
{
	public class FilterLayerGenerator : LayerGenerator
	{
		#region Fields, constructor

		public FilterLayerGenerator(
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

		public Guid GenerateFrom(Guid parentLayer, FilterDescriptor filter)
		{
			Guid layerId = Guid.NewGuid();

			// TODO: find parentLayer in storage and download summary

			throw new NotImplementedException();

			return layerId;
		}

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace MeshEditor.Data
{
	public interface IMultiLayerScene
	{
		Vector3? PositionOffset { get; }

		float? ResizeFactor { get; }

		Guid? SelectedLayer { get; set; }

		IReadOnlyCollection<Guid> GetVisibleLayers();

		void SetMeshForLayer(Guid layerId, Mesh newMesh);
	}
}
